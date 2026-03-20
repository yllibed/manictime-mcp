# WS-05 — Screenshot Pipeline

## Objectives

- Provide robust screenshot selection for Repl commands exposed through MCP.
- Keep payload size under control by default.
- Handle incomplete and in-progress files safely.
- Support screenshot retrieval and persistence flows that remain compatible with the active `Repl.Mcp` transport behavior.
- Minimize model-side lookup and cross-reference steps.

## Scope

- Screenshot directory scanning.
- Filename parsing for full and thumbnail variants.
- Sampling, selection, and secure file reading.
- Repl command output strategy for screenshot metadata, retrieval, crop, and save operations.
- Progressive resolution workflow (`list`, `get`, `crop`, `save`).
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

### Command output strategy

The screenshot workflow is hosted as Repl commands and exposed through `Repl.Mcp`. The transport contract must therefore be compatible with the current `Repl.Mcp` behavior, which is text-first for command results and resources.

- `screenshot list` returns structured JSON metadata only.
- `screenshot get` returns structured JSON including resolved metadata plus image payload in a text-safe representation.
- `screenshot crop` returns structured JSON including crop metadata plus the cropped image payload in a text-safe representation.
- `screenshot save` persists the original or cropped image to disk and returns the resolved output path plus size.
- `manictime://screenshot/{screenshotRef}` remains optional as a lazy-fetch resource when the active transport can serve it without introducing a competing image contract. The command workflow remains canonical.

The application must not depend on native MCP image blocks being the only supported delivery model. If a future `Repl.Mcp` release adds richer image support, those richer blocks may be layered in without changing the command semantics defined here.

#### Save workflow

Screenshot persistence is a first-class workflow:

- the agent may provide `outputPath` when it wants a file persisted for a report or asset pipeline
- the server validates the resolved path against MCP client-declared roots
- traversal and out-of-root writes must be rejected deterministically
- when `outputPath` is omitted, the server generates a deterministic filename from the screenshot timestamp
- optional crop parameters may be combined with save so the persisted asset is immediately report-ready

#### Token cost model

Image token cost in multimodal LLMs is determined by pixel resolution, not by wire encoding. Base64 increases payload size on the stdio pipe but does not affect the LLM's vision token budget. The levers for controlling token cost are:
- **Resolution**: prefer thumbnails and cropped regions over full-size screenshots.
- **Progressive resolution**: start with metadata (`list`), fetch a single screenshot (`get`), then crop or save selectively.
- **Explicit persistence**: use `save` only when the user or agent needs a durable asset.

See ADR-0003 for the decision rationale.

### Progressive resolution workflow

The screenshot pipeline exposes three tiers of detail, each a separate MCP tool:

1. **`list_screenshots`** — Metadata only. Zero image bytes.
   - Returns: `screenshotRef`, timestamp, display-local timestamp, dimensions, monitor index, thumbnail availability.
   - Timestamp collision behavior: when multiple files share the same timestamp, return all candidates as separate entries with distinct `screenshotRef` values.
   - Availability discovery: this is the canonical first call when the model does not know whether screenshots exist in a period.
   - Empty behavior: return empty list + structured reason/remediation diagnostics, not transport/tool failure.
   - Content: structured JSON result from the Repl command.
   - Use case: model surveys available screenshots, then chains follow-up calls without extra lookup.

2. **`get_screenshot`** — Single image retrieval for model or user follow-up.
   - Input: `screenshotRef` from `list_screenshots`.
   - Returns structured JSON with resolved metadata plus image payload in a transport-compatible text-safe representation.
   - When a thumbnail exists, it should be the default retrieval form to control payload size.
   - Full-size retrieval remains available when required by crop or save operations.

3. **`crop_screenshot`** — Region-of-interest extraction (model-driven).
   - Input: `screenshotRef` from `list_screenshots`.
   - Designed for model-driven workflows: the model inspects the thumbnail returned by `get_screenshot`, identifies a region of interest, then requests a full-resolution crop.
   - Crop parameters are percentage-first (`coordinateUnits = percent`) for model ergonomics:
     - `x`, `y`, `width`, `height` default range `0..100`.
   - Optional normalized mode is supported (`coordinateUnits = normalized`) with `0.0..1.0` values.
   - Percentage and normalized coordinates are resolution-independent: the same proportional region maps identically regardless of whether the model is viewing a thumbnail or full-size image (same aspect ratio).
   - Server resolves coordinates into full-image pixels and crops from the full-size screenshot.
   - Out-of-range or partially out-of-bounds input is clamped to valid image bounds.
   - Returns: structured JSON containing cropped-image metadata and payload in the active transport-compatible format.
   - Requires SkiaSharp dependency for JPEG processing (see ADR-0004).

4. **`save_screenshot`** — Persist the original or cropped image to disk.
   - Input: `screenshotRef` from `list_screenshots`, optional `outputPath`, optional crop options.
   - Validates the destination against MCP client roots.
   - Returns the final resolved path and file size.
   - This is the canonical path when the agent needs a durable artifact for reports, markdown, or downstream tooling.

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
- Command-output tests verifying transport-compatible structured JSON for list/get/crop.
- Progressive resolution integration tests (`list` -> `get` -> `crop` workflow).
- Save workflow tests (`list` -> `get`/`crop` -> `save`) including agent-supplied output paths.
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
- Separate transport formatting from screenshot retrieval logic.
- Add golden-file tests for filename evolution.

## Exit Criteria

- Full and thumbnail parsing validated.
- Selection and limits deterministic.
- Secure read behavior verified.
- Progressive resolution workflow (`list` -> `get` -> `crop`) operational.
- Activity-transition sampling functional with fallback.
- Repl/MCP-compatible screenshot retrieval, crop, and save outputs implemented.
- Screenshot responses expose resolved display fields and opaque machine refs.
