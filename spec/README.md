# ManicTime MCP Specifications

This project is still in **specification mode**. No production implementation details in this directory are considered complete until explicitly approved and scheduled.

## How To Use These Specs

1. Pick one workstream document from this folder.
2. Implement only that workstream's scope and acceptance criteria.
3. Run only the tests declared in that workstream plus the global quality gates.
4. Keep API and data contracts stable unless a documented ADR updates them.

## Design Principles

- Independent workstreams with clear contracts.
- Specification-first development.
- Read-only interaction with ManicTime files.
- Performance and memory optimization as first-class requirements.
- Maintainability over short-term convenience.
- No migration path from earlier code is required. This repository starts as a clean implementation baseline.

## Workstream Map

| ID | Document | Primary Scope | Can Start Independently | Hard Dependencies |
|----|----------|---------------|--------------------------|-------------------|
| WS-01 | `01-system-overview-and-boundaries.md` | Product boundaries and supported modes | Yes | None |
| WS-02 | `02-packaging-runtime-and-deployment.md` | .NET runtime, packaging, startup profile | Yes | WS-01 |
| WS-03 | `03-configuration-bootstrap-and-health.md` | startup config, health, schema preflight | Yes | WS-01 |
| WS-04 | `04-database-layer-and-schema-governance.md` | SQLite access and schema drift strategy | Yes | WS-03 |
| WS-05 | `05-screenshot-pipeline.md` | screenshot indexing and payload strategy | Yes | WS-03 |
| WS-06 | `06-mcp-contract-tools-resources-prompts.md` | tools/resources/prompts contracts | Yes | WS-04, WS-05 |
| WS-07 | `07-performance-and-memory-engineering.md` | startup, allocation, throughput budgets | Yes | WS-02, WS-04, WS-05, WS-06 |
| WS-08 | `08-code-quality-architecture-and-dotnet-standards.md` | architecture and coding standards | Yes | None |
| WS-09 | `09-testing-and-quality-gates.md` | testing matrix and CI quality gates | Yes | WS-02..WS-08 |
| WS-10 | `10-roadmap-and-implementation-workstreams.md` | execution plan and milestones | Yes | WS-01..WS-09 |
| WS-11 | `11-ci-release-and-publishing.md` | GitHub Actions CI/release and pre-push validation | Yes | WS-02, WS-09, WS-10 |
| WS-12 | `12-fixtures-and-test-data-strategy.md` | fixture DB/screenshot strategy and generators | Yes | WS-04, WS-05, WS-09 |
| WS-13 | `13-legal-branding-and-compatibility-positioning.md` | legal/branding guardrails for compatibility claims | Yes | WS-01, WS-11 |
| WS-14 | `14-autonomous-profiling-and-performance-operations.md` | profiling strategy for autonomous agents and CI | Yes | WS-07, WS-09, WS-11, WS-12 |

`Can Start Independently` means specification/design work can start in parallel. Implementation delivery and exit criteria must still respect the `Hard Dependencies` column.

## Global Non-Functional Targets

- **Cold start target:** tool process ready quickly (exact threshold defined in WS-07).
- **Memory target:** low idle and low per-request allocations (budget in WS-07).
- **Reliability target:** no writes to ManicTime data; graceful degradation on partial failures.
- **Data safety target:** treat ManicTime artifacts as high-value user data and apply exceptional caution systematically in both development and runtime.
- **Compatibility target:** `stdio` transport only for v1.
- **Maintainability target:** clean layering, deterministic tests, strict static analysis.

## Required Cross-Cutting Constraints

- Use modern C# and current .NET practices.
- Favor immutable models and explicit contracts.
- Use `Span<T>`/`ReadOnlySpan<T>` aggressively in measured hot paths.
- Avoid premature micro-optimizations that reduce readability without measurable wins.
- Keep all documentation and code comments in English.
- Use central package management.
- Use `.slnx` solution format.
- Enforce tabs for indentation through `.editorconfig`.

## Document Template Contract

Every workstream document in `spec/` includes:

- Objectives
- Scope / Non-scope
- Functional and non-functional requirements
- Technical design details
- Test strategy
- Risks and mitigations
- Maintainability considerations
- Exit criteria

## Change Control

Any contract-breaking change (tool schema, public model, config shape, or package behavior) must be tracked via an ADR in `spec/adr/`.
