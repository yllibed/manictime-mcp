# WS-04 — Database Layer and Schema Governance

## Objectives

- Implement reliable read-only access to `ManicTimeReports.db`.
- Protect against schema drift across ManicTime updates.
- Keep query behavior predictable and memory efficient.
- Leverage pre-aggregated tables for fast summary queries.

## Scope

- SQLite connection lifecycle.
- Query definitions for activities, timelines, usage, tags, date range.
- Pre-aggregated usage queries via `Ar_*ByDay` and `Ar_*ByYear` tables.
- Cross-timeline correlation and pattern analysis queries.
- Schema validator and compatibility checks for core and supplemental tables.

## Non-Scope

- Screenshot filesystem logic.
- MCP transport details.

## Functional Requirements

- Use read-only SQLite mode.
- Support timeline retrieval without relying on non-existent columns.
- Provide bounded-result query methods with hard server caps.
- Return empty-but-valid responses when no rows match.
- Use consistent range semantics for query contracts (`start` inclusive, `end` exclusive).
- Query pre-aggregated tables as the primary path for summary and narrative tools.
- Fall back to computing from core tables when supplemental tables are absent. Use SQL variant selection (not LEFT JOINs on missing tables) because SQLite rejects queries that reference non-existent tables.
- Include tag-aware query support (`Ar_ActivityTag`, `Ar_Tag`) for narrative-capable read models.
- Return resolved read models for high-level use cases (names/colors/labels) to reduce caller-side cross-references.

## Non-Functional Requirements

- No writes, locks, or schema mutations.
- Minimal allocations in high-frequency query paths.
- Graceful retries for transient `SQLITE_BUSY`.
- Prioritize forward compatibility with additive schema changes.

## Technical Design

### Connection

- Connection string: `Data Source={path};Mode=ReadOnly`
- Open per operation or pooled factory, but avoid long-lived shared mutable state.

### Canonical timeline query

```sql
SELECT ReportId, SchemaName, BaseSchemaName
FROM Ar_Timeline
ORDER BY ReportId
```

### Query principles

- Parameterized SQL only.
- Explicit column projection, no `SELECT *`.
- Local time columns (`StartLocalTime`, `EndLocalTime`) for range operations.
- Server-side limits enforced regardless of caller input.
- Async-first service interfaces with cancellation support; avoid sync-over-async at higher layers.
- Prefer resolved joins (`Ar_Group`, `Ar_CommonGroup`, tags) for summary/narrative query outputs to minimize follow-up lookup calls.
- **Capability matrix**: at startup, the schema validator builds a capability matrix recording which supplemental tables are present. Query objects use the matrix to select SQL variants that omit references to absent tables entirely — SQLite rejects any query that references a missing table, even in a LEFT JOIN. Each query object provides a "full" variant (all supplemental tables present) and a "degraded" variant (supplemental tables absent, columns default to NULL in the read model).

### State-of-the-art SQL access pattern

- Use a query-object/repository split:
  - query definitions as immutable, named objects (one per use case)
  - execution through a small database gateway abstraction
- Use prepared commands and typed parameters for repeated query shapes.
- Keep row mapping explicit and allocation-aware.
- Prefer streaming-style reads where practical over full list materialization.
- Separate read model DTOs from MCP transport models to avoid coupling.
- Every query must declare:
  - expected cardinality profile
  - hard cap policy
  - index assumptions
  - fallback behavior when assumptions fail

### Extended schema manifest

The `ManicTimeReports.db` contains 26 tables. The schema manifest classifies each into tiers:

#### Core tables (must exist; startup fails if absent)

| Table | Purpose |
|-------|---------|
| `Ar_Timeline` | Timeline metadata: `ReportId`, `SchemaName`, `BaseSchemaName`, `Name`, `TimelineKey`, `SchemaVersion`, `EnvironmentId` |
| `Ar_Activity` | Raw activity rows: `ActivityId`, `ReportId`, `StartLocalTime`, `EndLocalTime`, `Name`, `GroupId`, `Notes`, `IsActive`, `IsBillable`, `CommonGroupId`, `StartUtcTime`, `EndUtcTime`, `Other` |
| `Ar_Group` | Activity grouping: `GroupId`, `ReportId`, `Name`, `Color`, `Key`, `GroupType`, `FolderId`, `CommonId` |

#### Supplemental tables (use if present; absence is a warning, not fatal)

| Table | Rows (reference) | Purpose |
|-------|-------------------|---------|
| `Ar_CommonGroup` | ~varies | Master lookup across timelines: `CommonId`, `Name`, `Color`, `Key` (exe name), `GroupType` (e.g. `ManicTime/WebSites`), `ReportGroupType`. Required by pre-aggregated queries and enriched activity queries. When absent, queries degrade to `Ar_Group`-only joins. |
| `Ar_ApplicationByDay` | ~21K | Hourly app usage: `CommonId`, `Hour`, `TotalSeconds` |
| `Ar_WebSiteByDay` | ~32K | Hourly website usage: `CommonId`, `Hour`, `TotalSeconds` |
| `Ar_DocumentByDay` | ~3K | Hourly document usage: `CommonId`, `Hour`, `TotalSeconds` |
| `Ar_ApplicationByYear` | ~7K | Daily app usage: `CommonId`, `Hour`, `TotalSeconds`, `DayOfWeek` |
| `Ar_WebSiteByYear` | ~19K | Daily website usage: `CommonId`, `Hour`, `TotalSeconds`, `DayOfWeek` |
| `Ar_DocumentByYear` | ~3K | Daily document usage: `CommonId`, `Hour`, `TotalSeconds`, `DayOfWeek` |
| `Ar_ActivityByHour` | ~436K | Hour-level activity index for fast time-bucketed lookups |
| `Ar_TimelineSummary` | ~4 | Start/end date range per timeline |
| `Ar_Environment` | ~1 | Device name, OS version, ManicTime version, .NET version |
| `Ar_Folder` | ~2 | Document categories (e.g. "Web sites", "Files") |
| `Ar_Tag` | ~varies | Tag definitions; used by enriched activity query (C) and `get_activity_narrative` |
| `Ar_ActivityTag` | ~varies | Activity-to-tag join table; used by enriched activity query (C) and `get_activity_narrative` |

When `Ar_Tag` / `Ar_ActivityTag` are absent, tag-dependent features degrade gracefully (tag fields return NULL). The health resource reports degraded tag support.

#### Informational tables (structurally present, may be empty)

| Table | Purpose |
|-------|---------|
| `Ar_Category` | Category definitions (empty in reference DB) |
| `Ar_CategoryGroup` | Category group definitions (empty in reference DB) |

All remaining tables not listed above are treated as informational.

### New query patterns

#### A. Pre-aggregated usage queries (primary path for summaries)

Hourly app usage for a date range, resolved to names:

```sql
SELECT abd.Hour, cg.Name, cg.Color, cg.Key,
       abd.TotalSeconds
FROM Ar_ApplicationByDay abd
JOIN Ar_CommonGroup cg ON abd.CommonId = cg.CommonId
WHERE abd.Hour >= @startHour AND abd.Hour < @endHour
ORDER BY abd.Hour, abd.TotalSeconds DESC
```

Same pattern applies for `Ar_WebSiteByDay` and `Ar_DocumentByDay`.

- Expected cardinality: low-to-moderate (hours in range x distinct apps).
- Hard cap: 10,000 rows.
- Index assumption: `Ar_ApplicationByDay(Hour)` or equivalent.
- Fallback: compute from `Ar_Activity` with `GROUP BY` if supplemental table is absent.

#### B. Daily totals (one row per app per day)

```sql
SELECT DATE(abd.Hour) AS Day, cg.Name, SUM(abd.TotalSeconds) AS TotalSeconds
FROM Ar_ApplicationByDay abd
JOIN Ar_CommonGroup cg ON abd.CommonId = cg.CommonId
WHERE abd.Hour >= @startHour AND abd.Hour < @endHour
GROUP BY Day, cg.Name
ORDER BY Day, TotalSeconds DESC
```

- Expected cardinality: low (days in range x distinct apps).
- Hard cap: 5,000 rows.
- Fallback: compute from `Ar_Activity`.

#### C. Enriched activity query (when detail is needed)

Full variant (all supplemental tables present):

```sql
SELECT a.ActivityId, a.ReportId, a.StartLocalTime, a.EndLocalTime,
       a.Name, a.Notes, a.IsActive, a.IsBillable,
       a.GroupId, g.Name AS GroupName, g.Color AS GroupColor, g.Key AS GroupKey,
       a.CommonGroupId, cg.Name AS CommonGroupName,
       (
         SELECT JSON_GROUP_ARRAY(t.Name)
         FROM Ar_ActivityTag at
         JOIN Ar_Tag t ON at.TagId = t.TagId
         WHERE at.ActivityId = a.ActivityId
       ) AS Tags
FROM Ar_Activity a
LEFT JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
LEFT JOIN Ar_CommonGroup cg ON a.CommonGroupId = cg.CommonId
WHERE a.ReportId = @timelineId
  AND a.StartLocalTime < @endLocalTime AND a.EndLocalTime > @startLocalTime
ORDER BY a.StartLocalTime
LIMIT @limit
```

Note: `JSON_GROUP_ARRAY` (SQLite 3.38+) returns a proper JSON array, avoiding delimiter ambiguity when tag names contain commas. The row mapper deserializes this directly into `string[]` without string splitting.

Degraded variant (when `Ar_CommonGroup`, `Ar_Tag`, or `Ar_ActivityTag` are absent): omit the corresponding LEFT JOIN / subquery clauses; affected read model fields return NULL.

- Expected cardinality: moderate (activities in time window for one timeline).
- Hard cap: 2,000 rows.
- Index assumption: `Ar_Activity(ReportId, StartLocalTime)`.

#### D. Cross-timeline correlation (app + document at the same time)

Full variant (all supplemental tables present):

```sql
SELECT a.StartLocalTime, a.EndLocalTime, a.Name, t.SchemaName,
       g.Name AS GroupName, g.Color, cg.Name AS CommonName
FROM Ar_Activity a
JOIN Ar_Timeline t ON a.ReportId = t.ReportId
LEFT JOIN Ar_Group g ON a.GroupId = g.GroupId AND a.ReportId = g.ReportId
LEFT JOIN Ar_CommonGroup cg ON a.CommonGroupId = cg.CommonId
WHERE a.StartLocalTime < @endLocalTime AND a.EndLocalTime > @startLocalTime
ORDER BY a.StartLocalTime, t.SchemaName
LIMIT @limit
```

- Expected cardinality: high (spans all timelines in time window).
- Hard cap: 5,000 rows.
- Index assumption: `Ar_Activity(StartLocalTime)`.

#### E. Pattern analysis (day-of-week aggregation)

```sql
SELECT cg.Name, aby.DayOfWeek, SUM(aby.TotalSeconds) AS TotalSeconds
FROM Ar_ApplicationByYear aby
JOIN Ar_CommonGroup cg ON aby.CommonId = cg.CommonId
WHERE aby.Hour >= @startDate AND aby.Hour < @endDate
GROUP BY cg.Name, aby.DayOfWeek
ORDER BY cg.Name, aby.DayOfWeek
```

- Expected cardinality: low (distinct apps x 7 days).
- Hard cap: 1,000 rows.
- Fallback: compute from `Ar_Activity` with `strftime('%w', ...)`.

#### F. Data range query (from Ar_TimelineSummary)

```sql
SELECT ReportId, StartLocalTime, EndLocalTime,
       ActiveStartLocalTime, ActiveEndLocalTime
FROM Ar_TimelineSummary
WHERE StartLocalTime IS NOT NULL
```

- Expected cardinality: very low (one row per timeline).
- Hard cap: 100 rows.
- Fallback: `SELECT MIN(StartLocalTime), MAX(EndLocalTime) FROM Ar_Activity GROUP BY ReportId`.

### Schema governance

- Validate required tables and columns at startup.
- Keep schema manifest as code, versioned and test-covered.
- Core table absence = fatal startup error with clear compatibility message.
- Supplemental table absence = warning (not fatal).
  - Record in the capability matrix (built at startup) so query objects select appropriate SQL variants.
  - Report degraded capabilities in the health resource.
  - Report the same degradation via structured diagnostics in affected tool payloads.
  - Fall back to SQL variants that compute aggregations from core tables only (no references to absent tables).
- New columns and new tables in newer ManicTime versions = additive, safe to ignore by default.
- On drift in core tables, fail fast with clear compatibility error.
- Backward compatibility with older schemas missing required core fields is not a design target in this phase.

## Implementation Autonomy

This workstream can be fully implemented and tested using fixture databases without MCP integration.

## Testing Requirements

- Query correctness tests using fixture DB.
- Boundary tests for date windows and limits.
- Drift tests (missing table/column) asserting fatal startup behavior for core tables.
- Drift tests for supplemental table absence asserting warning + graceful degradation.
- Capability matrix tests: verify SQL variant selection produces valid queries when subsets of supplemental tables are absent (no references to missing tables).
- Pre-aggregated query tests verifying results match equivalent raw-activity computations.
- Cross-timeline correlation tests.
- Retry tests for simulated `SQLITE_BUSY`.

## Risks and Mitigations

- Risk: schema changes in future ManicTime versions.
  - Mitigation: schema manifest + explicit compatibility diagnostics + tiered classification.
- Risk: accidental heavy result sets.
  - Mitigation: hard caps + pagination-ready method signatures.
- Risk: supplemental tables absent in older ManicTime versions.
  - Mitigation: graceful degradation to core-table computation with degraded capability reporting.
- Risk: caller complexity from denormalized joins and fallback behavior.
  - Mitigation: isolate query objects per use case and keep MCP mapping concerns outside repository internals.

## Maintainability Considerations

- Keep SQL in focused repository methods.
- Avoid hidden implicit mapping; map columns explicitly.
- Isolate schema checks in dedicated validator class.
- Separate pre-aggregated query paths from raw-activity query paths for clarity.

## Exit Criteria

- All required query methods implemented and tested.
- Schema drift detection in place for both core and supplemental tiers.
- Pre-aggregated query paths operational with fallback verification.
- Memory/latency baseline for main queries captured.
