# WS-05 — Screenshot Pipeline

## Objectives

- Provide robust screenshot selection for MCP image responses.
- Keep payload size under control by default.
- Handle incomplete and in-progress files safely.

## Scope

- Screenshot directory scanning.
- Filename parsing for full and thumbnail variants.
- Sampling, selection, and secure file reading.

## Non-Scope

- DB query implementation.
- Vision model inference.

## Functional Requirements

- Parse both full-size and `.thumbnail` screenshot names.
- Select screenshots by requested time window.
- Support interval-based sampling and strict max limits.
- Prefer thumbnails by default when available.
- Prevent path traversal and non-jpg reads.
- Treat missing screenshot directories or empty screenshot data as a valid state.
- When no screenshots are available, classify likely reason as:
  - retention window too short or data already purged
  - screenshot capture disabled in ManicTime settings
  - unknown
- Return remediation guidance suggesting users review ManicTime screenshot retention and capture settings.
- Continue operating core non-screenshot tools when screenshot parsing is unavailable or incompatible.

## Non-Functional Requirements

- Low allocation file metadata processing.
- Predictable behavior under large screenshot volumes.
- Graceful handling when files are temporarily locked.

## Technical Design

### Canonical filename pattern

```text
^(?<date>\d{4}-\d{2}-\d{2})_(?<time>\d{2}-\d{2}-\d{2})_(?<offset>[+-]\d{2}-\d{2})_(?<width>\d+)_(?<height>\d+)_(?<seq>\d+)_(?<monitor>\d+)(?<thumb>\.thumbnail)?\.jpg$
```

### Correlation model

- No confirmed screenshot FK in `ManicTimeReports.db`.
- Correlate screenshots to activities by timestamp overlap only.

### Parsing/perf guidance

- Use `ReadOnlySpan<char>`-based parsing for hot-path filename decoding.
- Fall back to regex only when needed for maintainability or edge cases.
- Keep parser deterministic and culture-invariant.
- Support parser strategy versioning (for example `v1`, `v2`) to absorb upstream filename format changes without breaking the full server.

## Implementation Autonomy

This workstream can be implemented independently using filesystem fixtures and synthetic screenshot trees.

## Testing Requirements

- Parser tests for valid/invalid full and thumbnail names.
- Sampling tests for interval and cap behavior.
- Security tests for traversal and extension validation.
- I/O resilience tests for locked/incomplete files.

## Risks and Mitigations

- Risk: filename format changes in future versions.
  - Mitigation: parser abstraction + compatibility tests + strategy fallback.
- Risk: excessive payload cost from image-heavy requests.
  - Mitigation: thumbnail-first defaults and strict caps.
- Risk: screenshots unavailable because retention is short or capture is disabled.
  - Mitigation: clear availability reason codes, graceful empty responses, and actionable settings guidance.

## Maintainability Considerations

- Isolate parser logic in a dedicated component.
- Keep selection policy separate from I/O logic.
- Add golden-file tests for filename evolution.

## Exit Criteria

- Full and thumbnail parsing validated.
- Selection and limits deterministic.
- Secure read behavior verified.
