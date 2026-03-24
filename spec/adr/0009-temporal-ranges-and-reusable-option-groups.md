# ADR 0009 — Temporal Ranges and Reusable Option Groups

- Status: Accepted
- Date: 2026-03-20
- Deciders: Project maintainers
- Technical Story: Replace scattered raw date parameters with Repl-native temporal and reusable parameter contracts

## Context

The current contract shape relies heavily on repeated `startDate` / `endDate` string parameters plus repeated command-local knobs such as limits, crop coordinates, summary toggles, and output-path settings.

This causes:

- repeated parsing and validation code
- parameter drift across commands
- weaker CLI ergonomics
- duplicated contract documentation

The Repl parameter system provides temporal range types and reusable option groups that fit this application well.

## Decision

Adopt Repl-native parameter contracts wherever they materially improve the command surface:

1. Use `ReplDateRange` for date-based usage and summary windows.
2. Use `ReplDateTimeRange` for screenshot windows and time-anchored investigations.
3. Use `ReplDateTimeOffsetRange` only when offset preservation is truly required.
4. Keep ISO-8601 semantics, but express them through Repl's temporal range parsing instead of bespoke string parsing where practical.
5. Factor repeated knobs into `[ReplOptionsGroup]` types, especially for:
   - limits and pagination
   - summary/detail flags
   - crop coordinates and coordinate units
   - screenshot save output-path and crop options
6. Use Repl answer-prefill (`--answer:*`) for guided flows that need confirmation or user-provided answers in non-interactive mode.

## Decision Drivers

- Reduce duplicated parsing/validation logic.
- Improve consistency across related commands.
- Improve the local CLI surface without requiring a second contract model.
- Align the documented contract with the actual binding/runtime model.

## Consequences

### Positive

- Stronger validation at the command boundary.
- Better reuse of shared query parameters.
- Cleaner specs and tests.
- Easier command-surface testing with `Repl.Testing`.

### Negative

- Some command signatures will change compared to the older raw-string MCP contract.
- Developers must understand Repl temporal range syntax and option-group behavior.

### Neutral

- Domain services may still receive normalized scalar values after command-layer translation.
- Separate `date` / `datetime` prompt arguments may remain where user-facing prompt clarity is better served by explicit values.

## Implementation Notes

- Use temporal ranges only where the command semantics are truly range-shaped; do not force them into prompts or resources that are clearer with explicit arguments.
- Keep route handlers thin: bind Repl types at the edge, translate once, then delegate to application services.

## References

- Spec: `spec/06-mcp-contract-tools-resources-prompts.md`
- Spec: `spec/08-code-quality-architecture-and-dotnet-standards.md`
- Spec: `spec/09-testing-and-quality-gates.md`
