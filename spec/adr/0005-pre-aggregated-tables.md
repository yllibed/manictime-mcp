# ADR 0005 — Pre-aggregated Tables as Primary Query Path

- Status: Accepted
- Date: 2026-02-16
- Deciders: Project maintainers
- Technical Story: WS-04, WS-06

## Context

ManicTime pre-computes hourly and daily usage aggregations in dedicated database tables:

| Table | Rows (reference) | Granularity |
|-------|-------------------|-------------|
| `Ar_ApplicationByDay` | ~21K | Hourly app usage (`CommonId`, `Hour`, `TotalSeconds`) |
| `Ar_WebSiteByDay` | ~32K | Hourly website usage |
| `Ar_DocumentByDay` | ~3K | Hourly document usage |
| `Ar_ApplicationByYear` | ~7K | Daily app usage with `DayOfWeek` |
| `Ar_WebSiteByYear` | ~19K | Daily website usage with `DayOfWeek` |
| `Ar_DocumentByYear` | ~3K | Daily document usage with `DayOfWeek` |

These tables join to `Ar_CommonGroup` (~5K rows) which provides the master lookup: `CommonId` → `Name`, `Color`, `Key` (executable name), `GroupType`.

The current implementation computes all summaries from raw `Ar_Activity` (~418K rows) using `GROUP BY` queries. This is orders of magnitude slower for the same results, and forces the MCP server into expensive joins and aggregations that ManicTime has already performed.

## Decision

Use ManicTime's pre-aggregated `Ar_*ByDay` and `Ar_*ByYear` tables as the primary data source for summary and narrative tools, rather than computing aggregations from raw `Ar_Activity`.

Raw `Ar_Activity` queries remain available for:
- Detail/drill-down into individual activity segments.
- Segment boundary detection for narrative construction.
- Fallback when pre-aggregated tables are absent.

## Decision Drivers

- Query performance: pre-aggregated tables are ~10x faster for summary operations.
- Token efficiency: faster queries → faster MCP responses → lower end-to-end latency.
- Data accuracy: ManicTime computes these aggregations with full context; recomputing from raw data risks subtle discrepancies.
- Database load: scanning 418K rows with GROUP BY per summary request is wasteful when ~20K pre-aggregated rows exist.
- Contract ergonomics: high-level tools should return resolved, user-facing values with minimal model-side lookup.

## Considered Options

1. Pre-aggregated tables as primary, raw activities as fallback
2. Raw activities only (status quo)
3. Pre-aggregated tables only (no raw activity queries)

## Pros and Cons of the Options

### Option 1: Pre-aggregated primary, raw fallback

- Pros:
  - Best performance for common summary queries.
  - Graceful degradation if pre-aggregated tables are absent.
  - Raw activity queries remain available for drill-down use cases.
  - Matches ManicTime's own internal query patterns.
- Cons:
  - Two query paths to maintain and test.
  - Must validate consistency between aggregated and raw data.

### Option 2: Raw activities only (status quo)

- Pros:
  - Single query path; simpler implementation.
  - Works with any ManicTime version that has the core tables.
- Cons:
  - Scans 418K rows per summary request.
  - Slower responses for the most common use cases.
  - Does not leverage ManicTime's pre-computed data.

### Option 3: Pre-aggregated only

- Pros:
  - Simplest query layer; no fallback logic.
  - Fastest possible summary queries.
- Cons:
  - No drill-down to individual activity segments.
  - Breaks if pre-aggregated tables are absent (older ManicTime versions).
  - Loses access to `Notes`, `IsActive`, `IsBillable`, and other activity-level fields.

## Consequences

### Positive

- Summary queries (`get_activity_narrative`, `get_period_summary`, `get_website_usage`) become ~10x faster.
- Reduced database I/O and memory allocation for common operations.
- Enables responsive MCP interactions for the "what did I do yesterday?" pattern.
- `Ar_CommonGroup` join provides resolved names and colors without additional lookups.

### Negative

- Schema manifest must classify pre-aggregated tables as "supplemental" and handle their absence gracefully.
- Two query paths require parallel test coverage.
- Pre-aggregated data may lag slightly behind real-time activity if ManicTime hasn't refreshed.

### Neutral

- Raw-activity paths remain available for drill-down and fallback.
- Backward compatibility with older contract variants is not a goal for the current spec phase.

## Implementation Notes

- Impacted projects/files: database query layer, schema manifest, summary/narrative tool handlers.
- Migration/backward-compatibility considerations: breaking contract changes are acceptable in the current spec phase if explicitly documented.
- Test/verification requirements:
  - Pre-aggregated query correctness against fixture DB.
  - Fallback behavior when pre-aggregated tables are absent.
  - Consistency validation: pre-aggregated results should approximate raw-activity computations (within rounding tolerance).

## References

- `spec/04-database-layer-and-schema-governance.md`
- `spec/06-mcp-contract-tools-resources-prompts.md`
