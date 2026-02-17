# WS-05 — Screenshot Pipeline

## Objectives

- Provide robust screenshot selection for MCP image responses.
- Keep payload size under control by default.
- Handle incomplete and in-progress files safely.
- Use native MCP content types for efficient image delivery.
- Minimize model-side lookup and cross-reference steps.

## Scope

- Screenshot directory scanning.
- Filename parsing for full and thumbnail variants.
- Sampling, selection, and secure file reading.
- Content type strategy (`ImageContentBlock`, `ResourceLinkBlock`, `Annotations`).
- Progressive resolution workflow (`list`, `get`, `crop`).
- Activity-transition-based sampling.

## Non-Scope

- DB query implementation internals.
- Server-side vision model inference (OCR, object detection). Note: the dual-audience pattern lets the *LLM client* reason about thumbnails using its own vision capabilities — this is not server-side inference.

### Future work (out of scope for this phase)

- **OCR tool** (`ocr_screenshot`): extract text from screenshots or cropped regions server-side, returning structured text + bounding boxes. Requires proof-of-concept with existing screenshots to evaluate OCR engine quality before specifying. Parked for a future workstream.

## Functional Requirements

- Parse both full-size and `.thumbnail` screenshot names.
- Select screenshots by requested time window.
- Accept ISO-8601 date-time and date-only inputs; when time is omitted, interpret as local day-start (`00:00:00`).
- Support interval-based sampling and strict max limits.
- Prefer thumbnails by default when available.
- Prevent path traversal and non-jpg reads.
- Treat missing screenshot directories or empty screenshot data as a valid state.
- When no screenshots are available, classify likely reason as:
  - retention window too short or data already purged
  - screenshot capture disabled in ManicTime settings
  - unknown
- Return remediation guidance suggesting users review ManicTime screenshot retention and capture settings.
- Return screenshot metadata with both:
  - resolved user-facing fields (`displayLocalTime`, dimensions, monitor)
  - opaque machine reference (`screenshotRef`) for follow-up calls
- Keep `screenshotRef` stable for the lifetime of the MCP session.
- Continue operating core non-screenshot tools when screenshot parsing is unavailable or incompatible.
- Report screenshot degradation in both:
  - `manictime://health`
  - screenshot tool response payloads

## Non-Functional Requirements

- Low allocation file metadata processing.
- Predictable behavior under large screenshot volumes.
- Graceful handling when files are temporarily locked.
- Keep screenshot service components isolated and loosely coupled.

## Technical Design

### Canonical filename pattern

```text
^(?<date>\d{4}-\d{2}-\d{2})_(?<time>\d{2}-\d{2}-\d{2})_(?<offset>[+-]\d{2}-\d{2})_(?<width>\d+)_(?<height>\d+)_(?<seq>\d+)_(?<monitor>\d+)(?<thumb>\.thumbnail)?\.jpg$
```

### Correlation model

- No confirmed screenshot FK in `ManicTimeReports.db`.
- Correlate screenshots to activities by timestamp overlap only.
- Keep screenshot pipeline decoupled from database internals by consuming activity transitions through an interface contract (for example `IActivityTransitionProvider`) rather than direct SQL dependencies.

### Parsing/perf guidance

- Use `ReadOnlySpan<char>`-based parsing for hot-path filename decoding.
- Fall back to regex only when needed for maintainability or edge cases.
- Keep parser deterministic and culture-invariant.
- Support parser strategy versioning (for example `v1`, `v2`) to absorb upstream filename format changes without breaking the full server.

### Content type strategy

Replace base64-in-JSON `TextContentBlock` with native MCP content types from the ModelContextProtocol SDK:

- **`ImageContentBlock`**: Use for inline image delivery (thumbnails, crops, full screenshots). Carries `mimeType` and base64-encoded `Data` in a semantically typed block. The wire encoding is still base64, but the key benefits are: (a) clients recognize the block as an image and render it natively, and (b) the `Audience` annotation controls whether image data enters the LLM context window.
- **`ResourceLinkBlock`**: Use for deferred/lazy image references. Returns a `manictime://screenshot/{screenshotRef}` URI that the client can resolve via `resources/read` on demand. Zero image bytes in the initial response. The required `Name` property uses the display-local timestamp (e.g. `"Screenshot 2025-01-15 10:30:45"`). Using `screenshotRef` (not timestamp) as the URI key ensures deterministic addressing even when multiple screenshots share the same timestamp.
- **`Annotations`**: Apply `Audience` to control which content blocks enter the LLM context vs. are displayed to the human only. Apply `Priority` to control ordering when multiple content blocks are returned.
- **`TextContentBlock`**: Retain for structured metadata alongside images.

#### Dual-audience image delivery

Screenshot tools that return images use a **dual-audience** pattern to balance model reasoning capability against token cost:

- **Model-facing thumbnail** — `ImageContentBlock` with `Audience = [Role.User, Role.Assistant]`. A low-resolution `.thumbnail` variant that the model can see and reason about (e.g. to decide which region to crop). Token cost is driven by image resolution, not by base64 encoding: a small thumbnail costs relatively few vision tokens.
- **Human-facing full image** — `ImageContentBlock` with `Audience = [Role.User]`, or a `ResourceLinkBlock` for lazy fetch. The MCP client renders this for the human but does not inject it into the LLM context. Zero LLM token cost.

This pattern enables **model-driven crop workflows**: the model inspects the thumbnail, identifies regions of interest, then calls `crop_screenshot` with percentage coordinates — all without human intervention. The crop result is also returned dual-audience (cropped region to the model for analysis, full-quality crop to the human for display).

#### Token cost model

Image token cost in multimodal LLMs is determined by pixel resolution, not by wire encoding. Base64 adds ~33% byte overhead on the stdio pipe (negligible for local transport) but does not affect the LLM's vision token budget. The levers for controlling token cost are:
- **Resolution**: send small thumbnails to the model, not full-size screenshots.
- **Audience annotation**: exclude full-resolution images from the LLM context entirely.
- **Progressive resolution**: start with metadata (`list_screenshots`), fetch thumbnails selectively (`get_screenshot`), crop on demand (`crop_screenshot`).

See ADR-0003 for the decision rationale.

### Progressive resolution workflow

The screenshot pipeline exposes three tiers of detail, each a separate MCP tool:

1. **`list_screenshots`** — Metadata only. Zero image bytes.
   - Returns: `screenshotRef`, timestamp, display-local timestamp, dimensions, monitor index, thumbnail availability.
   - Timestamp collision behavior: when multiple files share the same timestamp, return all candidates as separate entries with distinct `screenshotRef` values.
   - Availability discovery: this is the canonical first call when the model does not know whether screenshots exist in a period.
   - Empty behavior: return empty list + structured reason/remediation diagnostics, not transport/tool failure.
   - Content: `TextContentBlock` for metadata + optional `ResourceLinkBlock` per screenshot for lazy fetch.
   - Use case: model surveys available screenshots, then chains follow-up calls without extra lookup.

2. **`get_screenshot`** — Single image retrieval with dual-audience delivery.
   - Input: `screenshotRef` from `list_screenshots`.
   - Returns two `ImageContentBlock` entries using the dual-audience pattern:
     - Thumbnail with `Audience = [Role.User, Role.Assistant]` — the model sees this and can reason about it.
     - Full-size (or `ResourceLinkBlock` for lazy fetch) with `Audience = [Role.User]` — rendered for the human, excluded from LLM context.
   - When no `.thumbnail` variant is available, a single full-size `ImageContentBlock` with `Audience = [Role.User, Role.Assistant]` is returned (model sees the full image, accepting higher token cost).
   - Content: `ImageContentBlock`(s) (image data) + `TextContentBlock` (resolved metadata).

3. **`crop_screenshot`** — Region-of-interest extraction (model-driven).
   - Input: `screenshotRef` from `list_screenshots`.
   - Designed for model-driven workflows: the model inspects the thumbnail returned by `get_screenshot`, identifies a region of interest, then requests a full-resolution crop.
   - Crop parameters are percentage-first (`coordinateUnits = percent`) for model ergonomics:
     - `x`, `y`, `width`, `height` default range `0..100`.
   - Optional normalized mode is supported (`coordinateUnits = normalized`) with `0.0..1.0` values.
   - Percentage and normalized coordinates are resolution-independent: the same proportional region maps identically regardless of whether the model is viewing a thumbnail or full-size image (same aspect ratio).
   - Server resolves coordinates into full-image pixels and crops from the full-size screenshot.
   - Out-of-range or partially out-of-bounds input is clamped to valid image bounds.
   - Returns: `ImageContentBlock` with `Audience = [Role.User, Role.Assistant]` (the model can analyze the cropped detail) + `TextContentBlock` (resolved metadata).
   - Requires SkiaSharp dependency for JPEG processing (see ADR-0004).

### Sampling by activity transition

In addition to fixed-interval sampling, support a sampling strategy based on activity segment transitions:

- Consume activity transition events through the decoupled transition provider interface.
- Select one representative screenshot per distinct activity segment change.
- Maximize visual coverage with minimal image count.
- Fall back to time-interval sampling when activity transition data is unavailable.

This strategy is the recommended default for narrative-style queries.

## Implementation Autonomy

This workstream can be implemented independently using filesystem fixtures and synthetic screenshot trees, with activity-transition behavior tested via interface stubs.

## Testing Requirements

- Parser tests for valid/invalid full and thumbnail names.
- Sampling tests for interval and cap behavior.
- Activity-transition sampling tests with fixture transition data.
- Security tests for traversal and extension validation.
- I/O resilience tests for locked/incomplete files.
- Content type tests verifying `ImageContentBlock` and `ResourceLinkBlock` output.
- Dual-audience tests: verify `get_screenshot` returns model-facing thumbnail (`Audience = [User, Assistant]`) and human-facing full image (`Audience = [User]`) as separate content blocks.
- Progressive resolution integration tests (`list` -> `get` -> `crop` workflow).
- Percentage and normalized coordinate crop tests (including bounds validation and clamping behavior).
- Degraded-response tests (reason code + remediation hint in tool payloads).

## Risks and Mitigations

- Risk: filename format changes in future versions.
  - Mitigation: parser abstraction + compatibility tests + strategy fallback.
- Risk: excessive payload cost from image-heavy requests.
  - Mitigation: thumbnail-first defaults, strict caps, and progressive resolution workflow.
- Risk: screenshots unavailable because retention is short or capture is disabled.
  - Mitigation: clear availability reason codes, graceful empty responses, and actionable settings guidance.
- Risk: incorrect ROI mapping when user/model selects area on a thumbnail.
  - Mitigation: percentage-first coordinate contract + explicit `coordinateUnits` + resolution-independent proportional mapping + deterministic transform tests.
- Risk: SkiaSharp dependency adds binary size.
  - Mitigation: acceptable tradeoff for crop capability; optimize runtime packaging for current Windows-first target.

## Maintainability Considerations

- Isolate parser logic in a dedicated component.
- Keep selection policy separate from I/O logic.
- Keep activity-transition sampling behind a dedicated interface to preserve decoupling.
- Separate content-type formatting from screenshot retrieval logic.
- Add golden-file tests for filename evolution.

## Exit Criteria

- Full and thumbnail parsing validated.
- Selection and limits deterministic.
- Secure read behavior verified.
- Progressive resolution workflow (`list` -> `get` -> `crop`) operational.
- Activity-transition sampling functional with fallback.
- Native MCP content types with dual-audience delivery used for all image delivery.
- Screenshot responses expose resolved display fields and opaque machine refs.
