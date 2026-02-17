# ADR 0003 — Screenshot Content Block Strategy

- Status: Accepted
- Date: 2026-02-16
- Deciders: Project maintainers
- Technical Story: WS-05, WS-06

## Context

The current screenshot implementation encodes images as base64 strings inside JSON `TextContentBlock` responses. A single thumbnail is ~67 KB of base64 text embedded in the LLM context window. This wastes thousands of tokens per image and provides no signal to the client about whether the content is intended for human viewing or model reasoning.

The ModelContextProtocol SDK v0.8.0-preview.1 supports richer content types that address these problems natively:
- `ImageContentBlock` carries base64-encoded image data with a `mimeType` field in a semantically typed block.
- `ResourceLinkBlock` provides a URI reference that clients can resolve on demand via `resources/read`.
- `Annotations` support `Audience` (to distinguish human-facing vs. model-facing content) and `Priority` for ordering.

## Decision

Replace base64-in-JSON `TextContentBlock` with native MCP content types for all screenshot delivery, using a **dual-audience** pattern:

- **`ImageContentBlock`** for inline image delivery (thumbnails, crops, full screenshots).
- **`ResourceLinkBlock`** for deferred/lazy image references in metadata-only responses.
- **Dual-audience delivery**: screenshot tools return two content blocks per image:
  - A low-resolution thumbnail with `Annotations.Audience = [Role.User, Role.Assistant]` — the model can see and reason about this (e.g. to select crop regions).
  - A full-resolution image with `Annotations.Audience = [Role.User]` — rendered for the human, excluded from LLM context.
- **`TextContentBlock`** retained for structured metadata alongside images.
- **`get_screenshots`** (legacy base64 contract) is removed from the active contract surface.

## Decision Drivers

- Token efficiency: image token cost in multimodal LLMs is determined by pixel resolution, not wire encoding. Base64 adds ~33% byte overhead on the local stdio pipe (negligible) but does not affect the LLM's vision token budget. The real levers are resolution (small thumbnails are cheap) and audience annotation (full-res images excluded from LLM context entirely).
- Model-driven workflows: the model must be able to inspect thumbnails to autonomously decide which regions to crop. This requires `Audience = [User, Assistant]` on the thumbnail — a pure `[User]`-only policy would make the model blind.
- Semantic clarity: clients should know whether content is for human display or model reasoning.
- Progressive resolution: the list → get → crop workflow requires separating metadata from image bytes.
- SDK alignment: using native content types follows the protocol's intended design.

## Considered Options

1. Native MCP content types (`ImageContentBlock` + `ResourceLinkBlock` + `Annotations`)
2. Keep base64-in-JSON `TextContentBlock` (status quo)
3. External URL references (serve images via HTTP)

## Pros and Cons of the Options

### Option 1: Native MCP content types

- Pros:
  - Semantic typing: clients recognize and render images natively without parsing JSON text.
  - Dual-audience pattern: thumbnails with `Audience = [User, Assistant]` let the model reason visually (cheap — few vision tokens for small resolution); full-res images with `Audience = [User]` are shown to the human only (zero LLM token cost).
  - Enables model-driven crop: the model inspects the thumbnail, identifies regions of interest, and calls `crop_screenshot` autonomously.
  - `ResourceLinkBlock` enables zero-cost metadata-only responses (zero image bytes in initial call).
  - Aligns with protocol design intent and SDK capabilities.
  - Enables progressive resolution workflow naturally.
  - Note: `ImageContentBlock.Data` is still base64-encoded on the wire; the token savings come from low resolution (small thumbnails) and `Audience` annotation (full-res excluded from LLM context), not from a different encoding.
- Cons:
  - Requires clients that understand `ImageContentBlock`.

### Option 2: Keep base64-in-JSON TextContentBlock

- Pros:
  - No client compatibility concerns; works everywhere.
- Cons:
  - ~67 KB per thumbnail in LLM context window.
  - No way to signal human-only content.
  - No lazy-fetch capability.
  - Fundamentally unscalable for multi-image responses.

### Option 3: External URL references

- Pros:
  - Zero bytes in the MCP response itself.
- Cons:
  - Requires running an HTTP server, contradicting the stdio-only transport decision (ADR-0001).
  - Adds network dependency and security surface.
  - Not supported by the current architecture.

## Consequences

### Positive

- Screenshot responses become dramatically more efficient: full-res images cost zero LLM tokens, thumbnails cost relatively few vision tokens.
- AI models can inspect thumbnails, reason about content, and autonomously select crop regions — no human guidance needed.
- AI models can request metadata first, then selectively fetch images via the progressive resolution workflow.
- Human users see full-resolution images rendered natively by their MCP client.
- The dual-audience `Annotations` pattern cleanly separates model-facing and human-facing content.

### Negative

- Clients that do not support `ImageContentBlock` will see only text metadata.
  - Mitigation: all major MCP clients (Claude Desktop, Claude Code) support `ImageContentBlock`. The fallback is text metadata + `ResourceLinkBlock` for deferred access.
- Model-facing thumbnails consume some vision tokens (cost proportional to thumbnail resolution).
  - Mitigation: ManicTime `.thumbnail` variants are small (~320px wide); vision token cost is modest. The tradeoff is necessary for model-driven crop workflows.

### Neutral

- Screenshot workflow is standardized on progressive resolution tools only (`list_screenshots`, `get_screenshot`, `crop_screenshot`).

## Implementation Notes

- Impacted projects/files: screenshot tool handlers, MCP response builders, content block construction.
- Migration/backward-compatibility considerations: no backward compatibility requirement for base64 screenshot contracts in the current spec phase.
- Test/verification requirements: content type tests verifying correct `ImageContentBlock`, `ResourceLinkBlock`, and `Annotations` construction. Dual-audience tests verifying that `get_screenshot` returns `[User, Assistant]` thumbnail + `[User]` full image, and `crop_screenshot` returns `[User, Assistant]` crop.

## References

- `spec/05-screenshot-pipeline.md`
- `spec/06-mcp-contract-tools-resources-prompts.md`
- ModelContextProtocol SDK v0.8.0-preview.1 content type documentation
