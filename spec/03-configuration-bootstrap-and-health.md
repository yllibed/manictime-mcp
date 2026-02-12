# WS-03 — Configuration, Bootstrap, and Health

## Objectives

- Guarantee deterministic startup behavior.
- Detect incompatible environments early.
- Provide actionable health diagnostics for users and tooling.

## Scope

- Data directory discovery.
- Startup validations (file presence, process state, schema preflight trigger).
- Health resource contract and diagnostics output.

## Non-Scope

- SQL query implementation.
- Screenshot parsing internals.

## Functional Requirements

- Resolve data directory through deterministic fallback order.
- Validate required files before MCP server becomes fully available.
- Emit warnings for non-fatal conditions and fail fast for fatal conditions.
- Expose a health payload through `manictime://health`.
- Expose installation/configuration issues in a model-consumable shape so clients can surface actionable remediation to the end user.

## Non-Functional Requirements

- Startup checks must be fast and side-effect free.
- Health checks must avoid expensive repeated work.
- All checks must remain read-only.

## Technical Design

### Directory resolution order

1. `MANICTIME_DATA_DIR` (all platforms)
2. Windows only: `HKCU\\SOFTWARE\\Finkit\\ManicTime\\DataDirectory` (non-empty)
3. Windows only: `%LOCALAPPDATA%\\Finkit\\ManicTime\\`
4. Non-Windows without `MANICTIME_DATA_DIR`: unresolved (explicit installation issue; no implicit fallback)

### Validation policy

- Fatal:
  - data directory unresolved after platform-specific fallback chain
  - missing `ManicTimeReports.db`
  - missing required schema tables/columns
- Warning:
  - ManicTime process not running
  - screenshot directory absent or empty (possible retention/capture settings issue)
  - `PRAGMA quick_check` not equal to `ok`

### Health shape (resource)

- resolved data directory
- db file existence and size
- schema validation status
- process running state
- warnings list
- installation issue list with stable machine-readable issue codes and remediation hints
- screenshot availability section:
  - status (`available` | `unavailable` | `unknown`)
  - likely reason (`retention` | `capture_disabled` | `unknown`)
  - remediation hint (for example, review ManicTime screenshot capture and retention settings)

## Implementation Autonomy

This workstream can be implemented independently and consumed by other workstreams as a service contract.

## Testing Requirements

- Resolution tests across all fallback states.
- Fatal/warning classification tests.
- Health resource serialization tests.
- Performance test: startup validation time budget.

## Risks and Mitigations

- Risk: registry differences across installations.
  - Mitigation: resilient fallback chain and clear warning messages.
- Risk: expensive health checks.
  - Mitigation: cache startup checks and refresh only lightweight process status.

## Maintainability Considerations

- Keep validations composable and individually testable.
- Use explicit error codes for machine-readable diagnostics.
- Keep user-facing messages stable and documented.

## Exit Criteria

- Startup behavior is deterministic and documented.
- Health resource is implemented and validated.
- Fatal/warning policy tested and enforced.
