# ADR 0007 — Defect Fixes and save_screenshot Tool

- Status: Accepted
- Date: 2026-02-17
- Deciders: Project maintainers
- Technical Story: Daily-recap integration test revealed 8 actionable defects

## Context

A full-day daily-recap integration test against a real ManicTime database revealed 8 defects in the narrative tools. Issues ranged from critical data accuracy problems (activity segments spanning Away periods inflating reported times by 100+ minutes) to UX irritants (duplicated data, inconsistent totals). Additionally, the server had no way to save screenshots to disk, blocking the daily-recap workflow from embedding real images in markdown.

## Decision

Apply 8 targeted fixes and add one new tool:

1. **Clip segments to Active/Away boundaries** — query the `ManicTime/ComputerUsage` timeline and intersect application activities with Active intervals. Activities spanning Away/Locked/Off periods are split or excluded.
2. **Preserve Document/Website during segment merging** — `MergeTwo()` now carries forward non-null `Document` and `Website` fields from the accumulator segment.
3. **Make segment limit configurable** — add `maxSegments` parameter (default 200, max 2000) to `get_daily_summary` and `get_activity_narrative`.
4. **Fix file path parsing** — Windows paths (e.g. `C:\Users\...`) in the Documents timeline are normalized to `file:///` URIs instead of being misinterpreted as `http://c/...`.
5. **Improve website matching** — add 5-second tolerance for near-miss overlaps and carry-forward logic for consecutive browser segments.
6. **Add `save_screenshot` tool** — writes screenshots to disk within MCP client-declared roots, with path traversal validation and optional crop.
7. **Change `includeSummary` default to `false`** on `get_activity_narrative` — avoids duplicating `topApplications`/`topWebsites` when clients call both `get_daily_summary` and `get_activity_narrative`.
8. **Compute `totalActiveMinutes` after merging** — the value now reflects actual unique active time, consistent with the pre-aggregated path.

## Decision Drivers

- Data accuracy: Away-period inflation was producing 100+ minute errors on real data.
- Consistency: `totalActiveMinutes` differed between `includeSegments=true` and `includeSegments=false` paths.
- Completeness: `Website` field was empty on most browser segments due to strict overlap matching.
- Workflow enablement: no write capability prevented the daily-recap workflow from saving screenshot crops to markdown assets.
- Payload efficiency: `includeSummary=false` default reduces redundant data when using both narrative tools.

## Consequences

### Positive

- Accurate active-time computation — Away/Locked/Off periods excluded.
- Website data populated on most browser segments.
- File paths rendered as proper `file:///` URIs.
- `save_screenshot` enables end-to-end daily recap with embedded images.
- Segment merging preserves document and website context.
- Configurable segment limit supports full-day detail requests.

### Negative

- **Breaking:** `includeSummary` default changed from `true` to `false` on `get_activity_narrative`. Existing callers that relied on the default to get summary data must now pass `includeSummary=true` explicitly.
- **Breaking:** `totalActiveMinutes` semantics changed from pre-merge sum to post-merge sum. Values will be slightly lower (and more accurate) for days with overlapping segments.
- `save_screenshot` is the first write operation — requires MCP roots capability from the client.

### Neutral

- `maxSegments` default (200) is unchanged — existing callers unaffected.
- Active/Away clipping degrades gracefully when no ComputerUsage timeline exists.

## Implementation Notes

- Impacted files:
  - `NarrativeTools.cs` — bugs #1-5, #7-9
  - `ScreenshotToolsV2.cs` — bug #7 (save_screenshot)
  - `IScreenshotService.cs` / `ScreenshotService.cs` — WriteScreenshot method
  - `Log.cs` — new log messages for write operations
  - Test files — 17 new tests
- Migration: clients parsing `topApplications` from `get_activity_narrative` must add `includeSummary=true`.
- Test/verification: 251 tests passing, 0 warnings.

## References

- Spec: `spec/06-mcp-contract-tools-resources-prompts.md`
- ADR-0006: payload efficiency changes (superseded in part by this ADR for `totalActiveMinutes` and `includeSummary` semantics)
