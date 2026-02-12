# WS-04 — Database Layer and Schema Governance

## Objectives

- Implement reliable read-only access to `ManicTimeReports.db`.
- Protect against schema drift across ManicTime updates.
- Keep query behavior predictable and memory efficient.

## Scope

- SQLite connection lifecycle.
- Query definitions for activities, timelines, usage, tags, date range.
- Schema validator and compatibility checks.

## Non-Scope

- Screenshot filesystem logic.
- MCP transport details.

## Functional Requirements

- Use read-only SQLite mode.
- Support timeline retrieval without relying on non-existent columns.
- Provide bounded-result query methods with hard server caps.
- Return empty-but-valid responses when no rows match.

## Non-Functional Requirements

- No writes, locks, or schema mutations.
- Minimal allocations in high-frequency query paths.
- Graceful retries for transient `SQLITE_BUSY`.

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

### Schema governance

- Validate required tables and columns at startup.
- Keep schema manifest as code, versioned and test-covered.
- On drift, fail fast with clear compatibility error.

## Implementation Autonomy

This workstream can be fully implemented and tested using fixture databases without MCP integration.

## Testing Requirements

- Query correctness tests using fixture DB.
- Boundary tests for date windows and limits.
- Drift tests (missing table/column) asserting fatal startup behavior.
- Retry tests for simulated `SQLITE_BUSY`.

## Risks and Mitigations

- Risk: schema changes in future ManicTime versions.
  - Mitigation: schema manifest + explicit compatibility diagnostics.
- Risk: accidental heavy result sets.
  - Mitigation: hard caps + pagination-ready method signatures.

## Maintainability Considerations

- Keep SQL in focused repository methods.
- Avoid hidden implicit mapping; map columns explicitly.
- Isolate schema checks in dedicated validator class.

## Exit Criteria

- All required query methods implemented and tested.
- Schema drift detection in place.
- Memory/latency baseline for main queries captured.
