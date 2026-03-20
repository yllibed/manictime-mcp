# WS-06 — MCP Contract: Tools, Resources, and Prompts

## Objectives

- Define stable Repl-first command contracts that are exposed through MCP.
- Keep output compact, structured, and model-friendly.
- Ensure behavior remains deterministic across clients.
- Minimize round-trips and cross-tool lookups by returning resolved, ready-to-present data.

## Scope

- Repl route hierarchy, parameter rules, and output schemas.
- Resource URIs and payload contracts.
- Prompt contracts that orchestrate tool usage.

## Non-Scope

- Internal DB and filesystem implementation details.

## Functional Requirements

- Expose these tools:
  - `get_timelines`
  - `get_activities`
  - `get_computer_usage`
  - `get_tags`
  - `get_application_usage`
  - `get_document_usage`
  - `get_daily_summary`
  - `get_activity_narrative` (new)
  - `get_period_summary` (new)
  - `get_website_usage` (new)
  - `list_screenshots` (new)
  - `get_screenshot` (new)
  - `crop_screenshot` (new)
- Remove `get_screenshots` (base64 payload pattern is out-of-scope going forward).
- Expose these resources:
  - `manictime://config`
  - `manictime://timelines`
  - `manictime://health`
  - `manictime://guide` (new)
  - `manictime://screenshot/{screenshotRef}` (new)
  - `manictime://environment` (new)
  - `manictime://data-range` (new)
- Expose these prompts:
  - `daily_review` (new)
  - `weekly_review` (new)
  - `screenshot_investigation` (new)
- Keep date and datetime parameters ISO-8601.
- Standardize range semantics for contract clarity:
  - `start*` parameters are inclusive.
  - `end*` parameters are exclusive.
- Date-only inputs are expanded to local midnight at day start (`T00:00:00` local time).
- Apply hard caps server-side regardless of caller values.

## Non-Functional Requirements

- Contract responses should avoid unnecessary narration.
- Errors must be machine-actionable and human-readable.
- Structured output is preferred over large unstructured text.
- Minimize token cost while maximizing information density.
- Prefer denormalized outputs so models do not need repeated lookup calls.

## Technical Design

### Repl-first command model

- The public application surface is a Repl command graph; MCP primitives are derived from that graph through `Repl.Mcp`.
- Commands must be grouped in meaningful contexts. Required top-level contexts:
  - `timeline`
  - `activity`
  - `usage`
  - `summary`
  - `screenshot`
  - `resource`
  - `prompt`
- Route hierarchy is the source of truth for local CLI help, interactive use, documentation export, and MCP discovery.
- Flattened MCP names may change as a byproduct of route hierarchy, but the route hierarchy itself must remain stable and documented.
- Use `Repl.Mcp` with `stdio` transport only for v1 and `ResourceUriScheme = "manictime"`.

### Contract principles

- Explicit `outputSchema` for tools with structured payloads.
- Include truncation metadata when limits are applied. All tools with hard caps must include a standard `truncation` block in the response:
  ```
  truncation: {
    truncated: bool,
    returnedCount: number,
    totalAvailable: number | null
  }
  ```
  `totalAvailable` is null when the server cannot cheaply compute the total (e.g. streaming queries).
- Keep optional fields nullable and documented.
- Include concise diagnostics in tool/resource outputs when relevant, so models can report setup problems precisely.
- Prefer compact enums/codes plus short explanation fields over verbose free text.
- For screenshot-related calls returning no images, include a structured reason code and remediation hint.
- For degraded capabilities, report the status in both:
  - `manictime://health`
  - the affected tool response payload
- Return resolved user-facing fields by default (names, colors, labels, display timestamps).
- Internal identifiers must be treated as machine-facing references:
  - include them only as optional or opaque references
  - avoid requiring user-visible ID cross-references to interpret output
- While the specification is still evolving, backward compatibility with older MCP contracts is not a requirement. Contract changes must remain explicit and documented.

### Repl parameter model

- Prefer Repl temporal range types over ad hoc `startDate`/`endDate` string pairs when the command semantics are inherently range-based:
  - `ReplDateRange` for date-based usage and summary windows
  - `ReplDateTimeRange` for screenshot and point-in-time investigative windows
  - `ReplDateTimeOffsetRange` only when offset-preserving semantics are materially required
- Keep ISO-8601 semantics, but express them through Repl range parsing rather than bespoke string parsing where practical.
- Reusable query knobs must be factored into `[ReplOptionsGroup]` types when shared across multiple commands, especially:
  - pagination / limits
  - summary/detail flags
  - screenshot crop coordinates and units
  - screenshot save options
- Commands that need guided answers or confirmations must support Repl answer-prefill via `--answer:*`, which maps to MCP `answer.*` arguments.
- Commands should use Repl annotations where applicable:
  - `.ReadOnly()` for safe autonomous reads
  - `.Destructive()` for write or deletion semantics
  - `.Idempotent()` for safe retries
  - `.OpenWorld()` when interacting with the filesystem or external systems beyond in-memory command dispatch
  - `.AutomationHidden()` for interactive-only helpers
  - `.WithDetails(...)` for richer agent-facing guidance when the short description is insufficient

### Prompt principles

- Prompts should call summary tools first.
- Screenshots must be optional and bounded.
- Prompt text must not assume a specific model vendor behavior.
- Prompt text should steer models to:
  - present resolved information to users
  - keep internal references/IDs for tool chaining only

---

## New Tools

Preferred Repl route hierarchy:

- `timeline list`
- `activity list`
- `activity tags`
- `usage applications`
- `usage documents`
- `usage websites`
- `summary daily`
- `summary narrative`
- `summary period`
- `screenshot list`
- `screenshot get`
- `screenshot crop`
- `screenshot save`
- `resource config`
- `resource timelines`
- `resource health`
- `resource guide`
- `resource environment`
- `resource data-range`
- `prompt daily-review`
- `prompt weekly-review`
- `prompt screenshot-investigation`

### `get_activity_narrative`

Flagship tool for answering "what did I do?" in a single call. Returns a pre-structured narrative with aggregates and segments, eliminating multi-round-trip patterns.

Preferred Repl command: `summary narrative`

```
Parameters:
  period: ReplDateRange (inclusive start, exclusive end semantics)
  includeWebsites: bool (default true)
  minDurationMinutes: number (default 0, filters short segments)
  maxGapMinutes: number (default 2.0, merges same-app segments within this gap)
  includeSummary: bool (default false, set true to include topApplications/topWebsites)
  maxSegments: int? (default 200, max 2000, controls segment truncation limit)

Response: {
  startDate: string,
  endDate: string,
  totalActiveMinutes: number,
  segments: [{
    start: string,
    end: string,
    durationMinutes: number,
    application: string | null,
    document: string | null,          // omitted when null
    website: string | null,           // omitted when null
    tags: string[] | null,            // omitted when null
    screenshotRef: string | null      // omitted when null; flat field, not nested
  }],
  topApplications: [{ name: string, color: string, totalMinutes: number }],
  topWebsites: [{ name: string, totalMinutes: number }],  // omitted when null
  suggestedScreenshots: [{ ... }] | null,                  // omitted when null
  truncation: {
    truncated: bool,
    returnedCount: number,
    totalAvailable: number | null     // omitted when null
  },
  diagnostics: {
    degraded: bool,
    reasonCode: string | null,        // omitted when null
    remediationHint: string | null    // omitted when null
  }
}
```

Note: null fields are omitted from the JSON response (WhenWritingNull). See ADR-0006.

Implementation: query `Ar_ApplicationByDay` + `Ar_WebSiteByDay` + `Ar_DocumentByDay` joined to `Ar_CommonGroup` for aggregates. Cross-reference with `Ar_Activity` for segment boundaries and with `Ar_ActivityTag`/`Ar_Tag` for segment tags (returned as `tags: string[]`, deserialized from the `JSON_GROUP_ARRAY` output of WS-04 Query C).

Design note: `websites` and `notes` were removed from the segment schema because (a) website data lives in a separate timeline (`ManicTime/BrowserUrls`) and would require cross-timeline merging per segment, which is disproportionately complex for this phase, and (b) the `Ar_Activity.Notes` column is empty in all observed databases. Website data is available at the aggregate level via `topWebsites` and the dedicated `get_website_usage` tool. `includeIdleGaps`/`idleGaps` were removed because idle-gap semantics are weak without `ComputerUsage` timeline context (on/off/locked/idle); raw segment gaps are better served by `get_computer_usage`.

Active/Away clipping (ADR-0007): segments are clipped to Active intervals from the `ManicTime/ComputerUsage` timeline. Activities spanning Away/Locked/Off periods are split at Active boundaries, so `totalActiveMinutes` reflects only time the computer was actively used. If no ComputerUsage timeline exists, clipping is skipped (graceful degradation). The `website` field uses tolerant matching (5s gap tolerance + carry-forward for consecutive browser segments) to improve coverage.

Payload efficiency (ADR-0006): `applicationColor` was removed from segments (available in `topApplications[].color`). The nested `refs` object was flattened to a single `screenshotRef` field — `timelineRef` and `activityRef` were opaque and unused by consumers. Null fields are omitted globally via `WhenWritingNull`. Gap-based merging (`maxGapMinutes`) reduces segment count by merging nearby same-app blocks.

Hard caps: default 200 segments (configurable via `maxSegments`, max 2000), max 50 top applications, max 50 top websites.

### `get_period_summary`

Multi-day aggregation for weekly/monthly overviews.

Preferred Repl command: `summary period`

```
Parameters:
  period: ReplDateRange (inclusive start, exclusive end semantics, max 31 days)

Response: {
  days: [{
    date: string,
    totalActiveMinutes: number,
    topApp: string,
    firstActivity: string,
    lastActivity: string
  }],
  aggregate: {
    topApps: [{ name: string, color: string, totalMinutes: number }],
    topWebsites: [{ name: string, totalMinutes: number }],
    avgDailyMinutes: number,
    busiestDay: string,
    quietestDay: string
  },
  patterns: {
    dayOfWeekDistribution: [{
      dayOfWeek: number,
      totalMinutes: number
    }]
  },
  truncation: {
    truncated: bool,
    returnedCount: number,
    totalAvailable: number | null
  },
  diagnostics: {
    degraded: bool,
    reasonCode: string | null,
    remediationHint: string | null
  }
}
```

Implementation: uses `Ar_ApplicationByDay` for daily breakdowns and `Ar_ApplicationByYear` for day-of-week patterns, both joined to `Ar_CommonGroup`.

Hard caps: max 31 days, max 50 top apps, max 50 top websites.

### `get_website_usage`

Exposes web tracking data from `Ar_WebSiteByDay`.

Preferred Repl command: `usage websites`

```
Parameters:
  period: ReplDateRange (inclusive start, exclusive end semantics, max 31 days)
  limit: number (default 50, max 200)
  minMinutes: number (default 0.5, filters brief visits; set 0 to include all)

Response: {
  breakdownGranularity: "hour" | "day",
  websites: [{
    name: string,
    totalMinutes: number,
    timeBreakdown: [{
      period: string,
      minutes: number
    }]
  }],
  truncation: {
    truncated: bool,
    returnedCount: number,
    totalAvailable: number | null
  },
  diagnostics: {
    degraded: bool,
    reasonCode: string | null,
    remediationHint: string | null
  }
}
```

Granularity rule: `breakdownGranularity` is `"hour"` when the date range is 7 days or fewer (each `period` is an ISO-8601 datetime hour); `"day"` when the range exceeds 7 days (each `period` is an ISO-8601 date). This is deterministic and based solely on the requested range, not on result volume.

Hard caps: max 31-day window, max 200 websites. Maximum breakdown entries per website: 168 (7 days x 24 hours) in hourly mode, or 31 in daily mode.

### `list_screenshots`

Metadata-only screenshot listing. Zero image bytes. See WS-05 for full design.

Preferred Repl command: `screenshot list`

```
Parameters:
  window: ReplDateTimeRange (inclusive start, exclusive end)
  maxCount: number (default 20, max 100)
  samplingStrategy: string (default "activity_transition", also "interval")

Response: structured JSON metadata; lazy-fetch resource URIs may be included when transport-compatible
  {
    screenshots: [{
      screenshotRef: string,
      timestamp: string,
      displayLocalTime: string,
      width: number,
      height: number,
      monitor: number,
      hasThumbnail: bool,
      resourceUri: string
    }],
    sampling: string,
    truncation: {
      truncated: bool,
      returnedCount: number,
      totalAvailable: number | null
    },
    diagnostics: {
      degraded: bool,
      reasonCode: string | null,
      remediationHint: string | null
    }
  }
```

Notes:
- `list_screenshots` is the discovery entry point for screenshot availability and timestamps.
- When no screenshots are found, return `screenshots: []` with structured diagnostics (`reasonCode`, `remediationHint`) rather than an error.
- When multiple screenshots share the same timestamp, return all as distinct entries with distinct `screenshotRef` values.

### `get_screenshot`

Single image retrieval with dual-audience delivery. See WS-05 for full design.

Preferred Repl command: `screenshot get`

```
Parameters:
  screenshotRef: string (opaque reference returned by list_screenshots; stable for the MCP session)

Response: structured JSON including resolved metadata plus image payload in the transport-compatible representation chosen by the runtime
```

Thumbnail-first retrieval remains the default when available to control payload size. Full-size retrieval remains available when required by crop or save operations.

### `crop_screenshot`

Region-of-interest extraction using percentage-first coordinates. Designed for model-driven workflows: the model inspects the thumbnail from `get_screenshot`, identifies a region of interest, then requests a full-resolution crop. See WS-05 and ADR-0004.

Preferred Repl command: `screenshot crop`

```
Parameters:
  screenshotRef: string (opaque reference returned by list_screenshots; stable for the MCP session)
  x: number (from left in selected units)
  y: number (from top in selected units)
  width: number (in selected units)
  height: number (in selected units)
  coordinateUnits: string (default "percent", also "normalized")

Response: structured JSON containing cropped-image metadata and payload
```

Always reads the full-size image for final crop extraction. Percentage and normalized coordinates are resolution-independent (same proportional region regardless of thumbnail vs. full-size viewing context), so no separate `coordinateSpace` parameter is needed. The server resolves proportional coordinates into full-image pixels. Out-of-range or partially out-of-bounds ROI inputs are clamped to valid image bounds.

### `save_screenshot`

Saves a screenshot to the filesystem within an MCP client-declared root directory. This is the first write operation in the server. See ADR-0007.

Preferred Repl command: `screenshot save`

```
Parameters:
  screenshotRef: string (opaque reference from list_screenshots)
  outputPath: string? (relative path + filename, e.g. "assets/screenshot-0930"; .jpg appended if no extension)
  cropX: number? (optional crop left edge, percent or normalized)
  cropY: number? (optional crop top edge)
  cropWidth: number? (optional crop width)
  cropHeight: number? (optional crop height)
  coordinateUnits: string? (default "percent", also "normalized")

Response: { path: string, size: number }
```

Security constraints:
- Output path must resolve within a client-declared MCP root (`file:///` URI).
- Path traversal blocked — the resolved absolute path must start with the root directory.
- Only `.jpg`, `.jpeg`, and `.png` extensions allowed.
- If no roots are declared by the client, the tool returns an error.

The tool reads the full-size screenshot, applies optional crop, then writes to the first matching root. If `outputPath` is omitted, a default name is generated from the screenshot timestamp.

---

## Updated Commands

### `activity list`

Add `includeGroupDetails` parameter (default `true`). JOIN `Ar_Group` and `Ar_CommonGroup` to return enriched fields inline:

- `groupName`: string | null
- `groupColor`: string | null
- `groupKey`: string | null
- `commonGroupName`: string | null

This removes a follow-up lookup in the common path.

### `summary daily`

Replace bare activity/usage counts with narrative output. Internally delegates to `get_activity_narrative` logic to produce structured segments and aggregates. Accepts `maxSegments` parameter (default 200, max 2000) to control segment truncation.

### `usage applications` / `usage documents`

Use pre-aggregated tables (`Ar_ApplicationByDay` / `Ar_DocumentByDay`) as the default and primary path, joined to `Ar_CommonGroup`, with automatic fallback to raw-activity computation if supplemental tables are absent.

Each item includes `totalMinutes` and resolved display fields.

---

## New Resources

### `manictime://guide`

Usage guide for AI models. This resource is intended to be read by the model at session start.

Content includes:

- Tool inventory with recommended usage workflow.
- Decision tree for daily vs multi-day vs drill-down requests.
- Multi-step playbooks with explicit tool chains, including:
  - daily recap
  - weekly recap
  - screenshot investigation
  - "why no screenshots" diagnostics
- Data model explanation: schemas, groups, `CommonGroup`, pre-aggregated tables.
- Screenshot workflow: `screenshot list` -> `screenshot get` -> `screenshot crop` -> `screenshot save`.
- Common questions -> tool mapping table.
- Interpretation tips (for example how `Color` and `Key` should be interpreted).
- Communication guidance: prefer resolved user-facing labels in responses; keep internal refs for tool chaining only.
- Date/time semantics guide (inclusive start, exclusive end, date-only -> local `00:00`).
- Screenshot availability guide:
  - always discover via `list_screenshots` first
  - interpret empty results with diagnostics (`reasonCode`, `remediationHint`)

### `manictime://screenshot/{screenshotRef}`

Lazy-fetch screenshot resource. Clients resolve this URI via `resources/read` to retrieve screenshot data on demand. The URI is keyed by `screenshotRef` (opaque, session-stable) rather than timestamp, making each resource deterministically addressable even when multiple screenshots share the same timestamp.

- Returns transport-compatible screenshot data plus structured metadata.
- `{screenshotRef}` is the opaque reference returned by `list_screenshots`. Each screenshot file maps to exactly one `screenshotRef`, so there is no collision ambiguity.
- Discovery: `list_screenshots` is the canonical way to obtain valid `screenshotRef` values and their corresponding `resourceUri` fields. Direct construction of `screenshotRef` values by clients is not supported.
- Annotations: `Audience = [Role.User]`.

### `manictime://environment`

Device and runtime information from `Ar_Environment` table:

- Device name
- OS version
- ManicTime version
- .NET runtime version

Returns `TextContentBlock` with structured JSON. Useful for diagnostics and compatibility reporting.

### `manictime://data-range`

Available data date range from `Ar_TimelineSummary`:

- Per-timeline start and end dates.
- Active start and end dates.

Allows models to know data boundaries without querying activities. Returns `TextContentBlock` with structured JSON.

---

## New Prompts

### `daily_review`

"Summarize my activities for {date}."

```
Arguments:
  date: string (ISO-8601 date, required)

Prompt text:
  "Use summary narrative with the date-derived range and includeSummary=true
   to retrieve activity data with top-app/
   top-website aggregates. Synthesize a concise daily summary highlighting
   top applications, total active time, and notable patterns. Prefer
   resolved names/colors over internal refs. If suggested screenshots are
   present, use screenshot get + screenshot crop + screenshot save to
   persist the best crops to project assets."
```

### `weekly_review`

"Summarize my week from {startDate} to {endDate}."

```
Arguments:
  startDate: string (ISO-8601 date, required)
  endDate: string (ISO-8601 date, required)

Prompt text:
  "Use summary period with the provided date range to retrieve
   multi-day activity data. Synthesize a weekly overview including
   busiest/quietest days, top applications and websites, day-of-week
   patterns, and total active hours. Prefer resolved labels in the
   final user-facing answer. For notable days, use summary daily +
   screenshot get + screenshot crop + screenshot save for visual context."
```

### `screenshot_investigation`

"What was I doing at {datetime}?"

```
Arguments:
  datetime: string (ISO-8601 datetime, required)

Prompt text:
  "Use screenshot list to find screenshots near {datetime} (within a
   5-minute window). Use screenshot get to retrieve the most relevant
   screenshot. If you spot a region that needs more detail, call
   screenshot crop with percentage ROI coordinates to get a
   full-resolution crop you can analyze. Combine visual findings with
   summary narrative for the same period, then produce a
   user-facing explanation using resolved names rather than internal
   refs."
```

---

## Implementation Autonomy

This workstream can be delivered independently once method-level service interfaces are defined by WS-04 and WS-05.

## Testing Requirements

- Tool schema contract tests.
- Parameter validation tests.
- Error contract tests (invalid dates, out-of-range limits).
- Resource contract tests.
- Prompt argument validation tests.
- End-to-end MCP stdio tests for list/call/resource retrieval.
- Narrative and summary output structure validation.
- Progressive screenshot workflow tests (including model-driven crop: list -> get thumbnail -> inspect -> crop).
- Dual-audience content block tests: verify `get_screenshot` returns `[User, Assistant]` thumbnail + `[User]` full image; verify `crop_screenshot` returns `[User, Assistant]` crop.
- Denormalized-output tests ensuring common user-facing responses do not require extra lookup calls.
- Degraded-capability tests asserting diagnostics appear in both health resource and affected tool outputs.

## Risks and Mitigations

- Risk: client incompatibility for optional MCP features.
  - Mitigation: prefer baseline-compatible contract patterns and document fallbacks.
- Risk: oversized responses for large date windows.
  - Mitigation: mandatory caps + truncation signals.
- Risk: model confusion from internal IDs.
  - Mitigation: resolved display-first payload design + guide resource communication policy.

## Maintainability Considerations

- Centralize tool metadata and parameter constraints.
- Keep contracts explicit and versioned in docs/tests.
- Maintain example payloads for every tool/resource.
- Keep machine references opaque and separate from user-facing fields.

## Exit Criteria

- Tool/resource/prompt contracts implemented and documented.
- Contract tests passing.
- Contract examples published in README/docs.
- Narrative and summary tools operational end-to-end.
- Progressive screenshot and save workflow operational.
- Model guide resource implemented with multi-tool playbooks.
