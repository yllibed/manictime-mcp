# ADR 0008 — Repl-First Command Graph and MCP Integration

- Status: Accepted
- Date: 2026-03-20
- Deciders: Project maintainers
- Technical Story: Replace the custom MCP host wiring with a Repl-first application surface while preserving functional coverage

## Context

The project currently exposes tools, resources, and prompts through a custom `ModelContextProtocol` host composition. That design made the MCP surface the primary application model, which in turn split local CLI behavior, testing shape, and MCP behavior across parallel abstractions.

The redesign goal is to make the application surface Repl-native:

- one command graph for local CLI, interactive help, and MCP exposure
- hierarchical contexts instead of a flat registry
- transport hosting delegated to `Repl.Mcp`
- in-memory command-surface testing via `Repl.Testing`

This keeps MCP as an exposure mechanism, not the application's organizing principle.

## Decision

Adopt a Repl-first architecture:

1. Build the public application surface as a `ReplApp` command graph.
2. Group feature areas into meaningful Repl contexts.
3. Expose that command graph through `Repl.Mcp`.
4. Keep `stdio` as the only supported MCP transport in v1.
5. Preserve hybrid process behavior:
   - zero args starts MCP mode for compatibility
   - `mcp serve` remains available
   - local CLI / interactive help remain first-class
6. Keep business logic in repositories and application services; Repl handlers remain thin adapters.

## Decision Drivers

- A single command graph reduces duplication across CLI, interactive, docs, and MCP.
- Repl contexts provide better discoverability and a more maintainable surface than a flat tool registry.
- `Repl.Mcp` provides the MCP transport and discovery layer directly from the Repl graph.
- `Repl.Testing` enables fast in-memory multi-step testing without paying process-startup cost for every workflow.

## Consequences

### Positive

- One public surface for CLI, interactive use, docs, and MCP.
- Better grouping and discoverability through contexts.
- Lower ceremony for command registration and testing.
- Easier adoption of temporal ranges, reusable option groups, annotations, and answer-prefill flows.

### Negative

- MCP names may change as a result of route flattening.
- Existing custom MCP adapter classes become legacy and should be removed or replaced.
- Screenshot delivery must remain compatible with the active `Repl.Mcp` transport behavior, which may require text-first payload shaping.

### Neutral

- Existing repositories, DTOs, screenshot services, and data access logic remain reusable.
- `stdio` remains the only supported transport for v1 despite the architecture change.

## Implementation Notes

- Central package management must add `Repl`, `Repl.Mcp`, and `Repl.Testing`.
- `ResourceUriScheme` stays `manictime`.
- Route hierarchy should be documented in WS-06 and validated in tests.

## References

- Spec: `spec/06-mcp-contract-tools-resources-prompts.md`
- Spec: `spec/08-code-quality-architecture-and-dotnet-standards.md`
- Spec: `spec/09-testing-and-quality-gates.md`
