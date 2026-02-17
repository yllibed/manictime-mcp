# ADR 0006 — Payload Efficiency Contract Changes

- Status: Accepted
- Date: 2026-02-17
- Deciders: Project maintainers
- Technical Story: Payload efficiency optimization for narrative and tool responses

## Context

Analysis of a real full-day narrative (91 segments) showed ~50% of the JSON payload was waste: repeated null fields, redundant `applicationColor` on every segment, constant `timelineRef`/`activityRef` in a nested `refs` object, and duplicated summary data. Gap-separated same-app segments forced the LLM to mentally re-merge what the server could have merged. The `get_website_usage` default included sub-6-second visits that added noise.

## Decision

Apply six contract changes to reduce payload size by ~35-50% for typical responses:

1. **Omit null fields globally** — `JsonIgnoreCondition.WhenWritingNull` on serializer options.
2. **Remove `applicationColor` from segments** — color remains available in `topApplications[].color`.
3. **Flatten `refs` object to `screenshotRef`** — remove `timelineRef` and `activityRef` (opaque, unused by consumers), promote `screenshotRef` to a top-level segment field.
4. **Gap-based segment merging** — add `maxGapMinutes` parameter (default 2.0) to `get_activity_narrative` and `get_daily_summary`. Same-app segments separated by gaps within the threshold are merged into continuous work blocks.
5. **`includeSummary` parameter** — add to `get_activity_narrative` (default true). When false, skip `topApplications`/`topWebsites` computation.
6. **Raise default `minMinutes` for `get_website_usage`** — from 0 to 0.5, filtering sub-30s visits by default.

## Decision Drivers

- Token cost: LLM context is expensive; smaller payloads reduce cost and improve response quality.
- Information density: null fields and redundant data dilute signal.
- Segment count: gap-separated same-app segments are conceptually one work block; merging them reduces segment count by ~15-20%.
- Noise filtering: sub-30s website visits are typically redirects or accidental clicks.
- Backward compatibility: the spec explicitly states contract changes are acceptable during evolution (WS-06).

## Considered Options

1. **All six changes together** (chosen) — maximum impact, coherent contract simplification.
2. **Omit nulls only** — safest change but only ~20% reduction.
3. **Deferred to v2** — no immediate benefit, continued token waste.

## Consequences

### Positive

- ~35-50% smaller payloads for typical full-day narratives.
- Cleaner contract: fewer fields, flatter structure.
- New parameters (`maxGapMinutes`, `includeSummary`) give callers control over payload size.
- `SegmentRefs` type deleted — less code to maintain.

### Negative

- Existing consumers parsing `refs.screenshotRef` must update to `screenshotRef`.
- Existing consumers parsing `applicationColor` on segments must read `topApplications[].color` instead.
- `get_website_usage` callers who want all sites must now pass `minMinutes=0` explicitly.

### Neutral

- `totalActiveMinutes` is unaffected — it uses the raw pre-merge sum, not merged segment durations.
- Merged segment `durationMinutes` includes gap time (wall-clock span), representing continuous work blocks.

## Implementation Notes

- Impacted projects/files:
  - `JsonOptions.cs` — WhenWritingNull
  - `NarrativeSegment.cs` — remove ApplicationColor, replace Refs with ScreenshotRef
  - `SegmentRefs.cs` — deleted
  - `NarrativeTools.cs` — gap merging, includeSummary, minMinutes default, mapping changes
  - `ScreenshotSuggestionSelector.cs` — ScreenshotRef access path
  - Test files updated for new assertions
- Migration/backward-compatibility considerations: None required per WS-06 policy.
- Test/verification requirements: All existing tests updated and passing; new tests for gap merging, includeSummary, and minMinutes filtering.

## References

- Spec: `spec/06-mcp-contract-tools-resources-prompts.md`
- WS-06 backward-compatibility policy: "Contract changes must remain explicit and documented."
