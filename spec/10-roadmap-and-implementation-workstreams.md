# WS-10 — Roadmap and Implementation Workstreams

## Objectives

- Provide an execution plan aligned with the split specifications.
- Keep implementation incremental and test-driven.
- Make dependencies explicit to reduce blocking.

## Scope

- Milestones, sequencing, and delivery criteria.
- Workstream handoff expectations.

## Non-Scope

- Detailed code-level task decomposition.

## Milestone Plan

### Milestone M1 — Foundation

- WS-01 accepted.
- WS-02 package/runtime baseline in place.
- WS-08 quality standards approved.
- Repository docs baseline in place (`README.md` + `docs/`).

### Milestone M2 — Data and Health Core

- WS-03 startup/health contracts implemented.
- WS-04 database layer implemented with schema governance.

### Milestone M3 — Screenshot and MCP Surface

- WS-05 screenshot pipeline implemented.
- WS-06 tools/resources/prompts contracts implemented.

### Milestone M4 — Hardening

- WS-07 performance and memory budgets enforced.
- WS-09 test and quality gates fully integrated.
- WS-11 CI/release automation fully integrated.
- WS-12 fixture and synthetic data strategy integrated.
- WS-13 legal/branding compatibility positioning integrated.
- WS-14 autonomous profiling operations integrated.

## Suggested Execution Order

1. WS-08 (quality standards) and WS-02 (packaging baseline)
2. WS-03 (bootstrap/health)
3. WS-04 (database)
4. WS-05 (screenshots)
5. WS-06 (MCP contracts)
6. WS-07 (perf/memory hardening)
7. WS-09 (test gates finalization)
8. WS-11 (CI/release and pre-push validation)
9. WS-12 (fixtures and test data strategy)
10. WS-13 (legal and compatibility positioning)
11. WS-14 (autonomous profiling and performance operations)

## Per-Workstream Definition of Done

- Requirements implemented.
- Tests in the workstream test plan passing.
- Risks reviewed and mitigations applied.
- Documentation updated with final decisions.

## Risk Register (Program-Level)

- Schema drift from ManicTime updates.
- Contract drift between MCP tools and clients.
- Performance regressions from feature expansion.
- Maintainability erosion from rushed optimizations.

## Maintainability Plan

- Keep spec and implementation in sync at every milestone.
- Require ADR for any contract or boundary change.
- Re-run baseline performance suite before each release.

## Exit Criteria

- All workstreams reach Done state.
- CI gates green on main branch.
- Package validated in target MCP clients with stdio transport.
