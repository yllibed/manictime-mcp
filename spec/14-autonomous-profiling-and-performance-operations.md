# WS-14 — Autonomous Profiling and Performance Operations

## Objectives

- Define a practical profiling workflow that autonomous agents can execute.
- Catch startup, allocation, and throughput regressions early.
- Keep profiling reproducible and actionable across local and CI environments.

## Scope

- Profiling tools and scenarios.
- Benchmark strategy and operational guardrails.
- Agent-executable profiling workflow design.

## Non-Scope

- Production distributed tracing stack.
- Deep OS-kernel profiling requirements for v1.

## Can Autonomous Agents Do Profiling?

Yes, for most useful cases in this project.

Autonomous agents can reliably run:

- startup timing scripts
- BenchmarkDotNet microbenchmarks
- `dotnet-counters` sampling
- `dotnet-trace` collection and artifact upload
- allocation-focused tests in CI

They should not be the only source of truth for highly noisy machine-level comparisons without controlled environments.

## Functional Requirements

- Provide profiling entry points that autonomous agents can execute non-interactively.
- Cover startup, memory/allocation behavior, and representative hotspot scenarios.
- Publish profiling artifacts in CI where profiling is enabled.
- Let implementation define concrete metrics, baselines, and thresholds according to project maturity and environment stability.
- This workstream is the source of truth for concrete performance thresholds and re-baselining policy.

## Non-Functional Requirements

- Profiling runs should be automatable by non-interactive agents.
- Profiling overhead should be bounded in regular CI (full profiling can run nightly).
- Results should be stable enough to support regression detection.

## Technical Design

### Tooling baseline

- Tooling is implementation-defined.
- Typical options include BenchmarkDotNet, `dotnet-counters`, and `dotnet-trace`.
- Optional deeper platform tools may be used for manual investigations.

### Suggested scenario matrix

1. Cold start to MCP initialize complete.
2. `get_daily_summary` on small/medium/large fixture windows.
3. Screenshot metadata selection with and without thumbnails.
4. Repeated tool invocations for steady-state allocation profiling.

### CI strategy

- PR CI may run lightweight profiling smoke checks.
- Nightly CI may run deeper trend-oriented profiling.
- Release CI may include profiling gates where stable baselines exist.

## Implementation Autonomy

This workstream can be implemented independently using synthetic fixtures and benchmark harnesses.

## Testing Requirements

- Verify profiling workflow executes headlessly when enabled.
- Verify benchmark outputs are machine-readable and archived.
- Verify profiling policy behavior matches the configured CI profile.

## Risks and Mitigations

- Risk: noisy metrics produce false positives.
  - Mitigation: baseline windows, retry-once policy, and nightly trend analysis.
- Risk: profiling too expensive for every PR.
  - Mitigation: tiered checks (smoke on PR, full on schedule).

## Maintainability Considerations

- Keep profiling approach and configuration versioned.
- Document rationale for selected metrics and gates.
- Rebaseline with explicit change notes when profiling policy evolves.

## Exit Criteria

- Profiling approach is documented and automation-ready.
- CI can run at least one profiling tier where configured.
- Metrics/gates policy is explicitly documented (even if initially lightweight).
