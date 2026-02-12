# WS-08 — Code Quality, Architecture, and .NET Standards

## Objectives

- Enforce excellent code quality and long-term maintainability.
- Apply widely accepted .NET architectural and engineering practices.
- Use modern C# features responsibly, with performance awareness.

## Scope

- Architecture and layering rules.
- Coding standards and language feature usage.
- Static analysis, review expectations, and refactoring policy.

## Non-Scope

- Product feature scope.
- Packaging mechanics.

## Architecture Requirements

- Layered design with explicit boundaries:
  - `Configuration`
  - `Data`
  - `Tools`/`Resources`/`Prompts` adapters
  - `Models`
- Dependency direction: outer layers depend on inner contracts only.
- No direct SQL or filesystem logic inside MCP tool classes.
- Keep domain/service logic testable without MCP host runtime.
- Separate concerns with clear ownership and references using namespaces + folder boundaries at minimum.
- Multi-project decomposition is optional; adopt it only when complexity or packaging concerns justify it.
- Test organization must mirror architecture boundaries (either by dedicated test projects or by namespace/folder-aligned suites in a shared test project).

### Optional multi-project decomposition

- `ManicTimeMcp.Host` (MCP host wiring, DI composition)
- `ManicTimeMcp.Configuration` (settings discovery and health bootstrap)
- `ManicTimeMcp.Data` (SQLite and screenshot data access)
- `ManicTimeMcp.Contracts` (DTOs, schemas, shared contracts)
- `ManicTimeMcp.Application` (orchestration/services)
- `ManicTimeMcp.Transport.Mcp` (tools/resources/prompts adapters)

If using the multi-project option, each project should have matching tests:

- `ManicTimeMcp.Host.Tests`
- `ManicTimeMcp.Configuration.Tests`
- `ManicTimeMcp.Data.Tests`
- `ManicTimeMcp.Application.Tests`
- `ManicTimeMcp.Transport.Mcp.Tests`

## Repository and Build System Standards

- Use `.slnx` as the canonical solution format.
- Use central package management (`Directory.Packages.props`).
- Use shared build configuration in `Directory.Build.props`.
- All repeated project properties must be centralized in `Directory.Build.props` (for example nullability, analyzers, language version, deterministic build flags).
- Minimize third-party dependencies; each dependency requires explicit justification.

## .NET and C# Standards

- Target current .NET LTS and modern C# language version.
- Nullability is mandatory and configured centrally in `Directory.Build.props` (`<Nullable>enable</Nullable>`).
- Nullable-related warnings are treated as errors.
- Prefer immutable records for DTOs and contract models.
- Use file-scoped namespaces and concise constructors when they improve clarity.
- Prefer `required` members when objects must be fully initialized.
- Use latest stable C# features available in the target SDK when they improve correctness, readability, or allocation behavior.
- Prefer `var` whenever the type is obvious or redundant.
- Prefer immutable design by default:
  - no public setters unless explicitly justified
  - `readonly` fields and types where practical
  - functional-style transformations for pure logic

## Performance-Oriented Coding Standards

- Use `ReadOnlySpan<T>` / `Span<T>` aggressively in parsing and transformation hot paths (especially filename/date parsing and text tokenization).
- Keep public API boundaries ergonomic: convert to spans internally and expose stable CLR-friendly types externally.
- Avoid hidden allocations in tight loops (boxing, closure capture, repeated substring operations).
- Prefer `TryParse` APIs and avoid exception-driven control flow.
- Use `in` parameters for private pure methods when passing large value types or readonly structs.
- Use `stackalloc` for short-lived small buffers where profiling justifies it.
- Evaluate SIMD opportunities (`System.Numerics`, vectorized runtime/library paths) on measured hotspots.
- Use pooling (`ArrayPool<T>`) only when profiling shows allocation pressure.
- Use `StringComparer` explicitly for all string-based lookup/sort operations, defaulting to `StringComparer.Ordinal` unless a documented reason requires a different comparer.
- Prefer async APIs for I/O and orchestration paths; avoid sync-over-async and avoid fake async wrappers around purely synchronous CPU work.

## Mandatory Optimization Hotspots

- Screenshot filename parser and metadata extraction.
- Date/time normalization from user parameters.
- Repeated string filtering and grouping logic on high-cardinality datasets.
- Serialization shaping for large tool responses.

## Maintainability Requirements

- Small, focused classes and methods with single responsibility.
- Explicit naming over abbreviations.
- All public members must have XML documentation comments that explain purpose/behavior (what and why, not implementation steps).
- Add inline comments for non-obvious logic or invariants that are not immediately clear from code.
- Keep invariants and assumptions documented close to code.
- Favor composition over inheritance for behavior reuse.
- Keep side effects at edges and keep core logic pure where possible.

## Error Handling Standards

- Use typed exceptions for startup fatal errors.
- Return structured error payloads for MCP tool-level validation failures.
- Never swallow exceptions silently.

## Observability Standards

- Structured logging with stable event IDs for key failures.
- No sensitive data in logs.
- Keep log volume bounded in normal operation.

## Static Analysis and Linting

- Enable analyzers and warnings as errors for core projects.
- Baseline analyzers:
  - built-in Roslyn analyzers
  - Meziantou.Analyzer
  - nullable warnings
  - style and performance analyzers
- Introduce suppressions only with documented rationale.

## Formatting Rules

- `.editorconfig` is mandatory and strict.
- Indentation must use tabs in all source-like files where tabs are supported.
- Formatting deviations fail CI.

## Implementation Autonomy

This workstream is independent and can be applied before implementation starts. It defines mandatory engineering quality constraints for all code work.

## Testing Requirements

- Architecture tests (dependency direction and forbidden references).
- Analyzer compliance checks in CI.
- Code review checklist enforcement for performance and maintainability rules.

## Risks and Mitigations

- Risk: standards become aspirational but not enforced.
  - Mitigation: convert standards to CI gates and review checklist items.
- Risk: misuse of low-level optimizations.
  - Mitigation: require benchmark evidence and readability review.

## Exit Criteria

- Standards accepted and published.
- CI checks mapped to these standards.
- Review checklist updated and mandatory for all PRs.
