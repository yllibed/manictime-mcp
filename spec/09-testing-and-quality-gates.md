# WS-09 — Testing Strategy and Quality Gates

## Objectives

- Ensure high confidence before release.
- Detect regressions early (correctness, compatibility, performance).
- Make all quality gates explicit and automatable.

## Scope

- Unit, integration, protocol, and performance testing.
- CI gates and release criteria.
- Test data strategy.
- Test platform/tooling and reporting standards.

## Non-Scope

- Business feature prioritization.

## Test Pyramid

### Test Platform and Framework Standards

- Use SDK-style test projects with Microsoft Testing Platform (MTP).
- Use MSTest for unit and integration tests.
- Use AwesomeAssertions (FluentAssertions fork) for expressive assertions.
- Keep test projects split by concern, mirroring production project boundaries.
- Baseline package set:
  - `MSTest.Sdk`
  - `AwesomeAssertions`
- Coverage and TRX reporting should use the default extensions bundled by `MSTest.Sdk`; do not duplicate extension package references unless intentionally overriding defaults.

### Unit Tests

- Pure logic only.
- Parser behavior, validation rules, date normalization, limit enforcement.

### Integration Tests

- SQLite fixture databases for data-access behavior.
- Filesystem fixtures for screenshot scanning and selection.

### MCP Protocol Tests

- Start server process in stdio mode.
- Validate `initialize`, `tools/list`, `tools/call`, resource retrieval.
- Assert structured error responses for invalid inputs.

### Performance/Memory Tests

- Cold-start latency measurements.
- Request-level allocation and throughput checks.
- Baseline regression comparisons.

## Quality Gates (CI)

- Build must pass with warnings-as-errors policy.
- Zero warnings is the default quality target as much as practical; any temporary suppression requires explicit rationale.
- All required tests must pass.
- Contract snapshot tests must pass.
- Performance regressions beyond threshold fail CI.
- Packaging smoke test (`dotnet pack` + local run) must pass.
- Test result reporting must include machine-readable artifacts (TRX or equivalent).
- Coverage must be collected and published with a human-readable summary.
- PR checks must surface failing tests with per-test details.

## Test Data Strategy

- Maintain minimal deterministic fixture DBs.
- Keep fixture screenshot sets small but representative.
- Add synthetic stress fixtures for high-volume cases.

## CI Reporting and Coverage Tooling

- Use MTP-compatible test result output for CI ingestion.
- Generate coverage in Cobertura (or equivalent standard format).
- Produce an HTML coverage report artifact for diagnostics.
- Use ReportGenerator (tool or action) to build readable coverage summaries from raw coverage files.
- Publish both raw artifacts and summarized reports in CI.
- Enforce a minimum line/branch coverage threshold (initial threshold defined in WS-11 and revisited as codebase grows).

## Implementation Autonomy

This workstream can be developed independently and then wired into the CI pipeline before full feature completion.

## Risks and Mitigations

- Risk: flaky protocol tests due to timing.
  - Mitigation: deterministic startup handshake and bounded retries.
- Risk: CI perf noise causing false failures.
  - Mitigation: threshold windows and rolling baseline comparison.

## Maintainability Considerations

- Keep fixtures versioned and documented.
- Avoid brittle assertions on non-contract internals.
- Prioritize deterministic tests over broad but unstable coverage.

## Exit Criteria

- Test pyramid implemented with documented ownership.
- CI quality gates active and stable.
- Release pipeline blocked on gate failures.
