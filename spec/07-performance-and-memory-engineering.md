# WS-07 — Performance and Memory Engineering

## Objectives

- Optimize cold start and steady-state memory.
- Prevent regressions through measurable budgets.
- Enforce data-budget discipline in every large-response path.

## Scope

- Runtime performance budgets.
- Allocation strategy for query and parsing paths.
- Response shaping and truncation policies.

## Non-Scope

- Feature-level functional behavior.

## Performance Targets Policy

- WS-07 defines measurement dimensions, engineering constraints, and enforcement mechanics.
- Concrete thresholds (budgets, deltas, and environment-specific baselines) are defined by WS-14 profiling policy.
- At minimum, track and gate:
  - cold start (`process spawn` -> MCP `initialize` ready) with p50/p95
  - idle memory after startup stabilization
  - non-image request allocation budgets
  - screenshot metadata selection allocation budgets

## Functional Requirements

- Enforce strict hard caps for all potentially large outputs.
- Include truncation flags and returned-count metadata.
- Default to thumbnails for screenshot payloads.

## Technical Design

### Hot-path coding guidance

- Use `ReadOnlySpan<char>` for parsing fixed-format file names and date fragments.
- Prefer `TryParse` APIs and avoid exception-driven control flow.
- Favor pooled buffers for temporary large arrays where justified.
- Keep LINQ in non-hot paths; use explicit loops in measured hot paths.

### Memory discipline

- Avoid materializing full-day activity sets when only aggregates are needed.
- Stream or iterate results where practical.
- Do not keep large caches unless hit-rate and memory impact are measured.

### Measurement strategy

- Benchmark representative scenarios with repeatable fixtures.
- Capture p50/p95 latency and allocation stats.
- Store baseline artifacts for regression comparison.

## Implementation Autonomy

This workstream can run in parallel as a benchmark and profiling harness, then feed constraints back into implementation workstreams.

## Testing Requirements

- Benchmark tests for startup and core tool paths.
- Allocation tests (`GC.GetAllocatedBytesForCurrentThread` or profiler-based).
- Stress tests with large date windows and dense screenshot folders.
- Regression gate tests in CI for budget deltas.

## Risks and Mitigations

- Risk: over-optimization harms readability.
  - Mitigation: require profiling evidence for low-level optimizations.
- Risk: environment-dependent benchmark noise.
  - Mitigation: use controlled benchmark profiles and variance thresholds.

## Maintainability Considerations

- Keep optimization rationale close to code.
- Document every non-obvious allocation optimization.
- Revalidate budgets after major runtime/package updates.

## Exit Criteria

- Baseline metrics captured and approved.
- Budget gates active in CI.
- Hot-path parsing and response shaping validated against targets.
