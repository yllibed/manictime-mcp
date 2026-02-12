# ADR 0001 — Stdio-Only Transport in v1

- Status: Accepted
- Date: 2026-02-12
- Deciders: Project maintainers
- Technical Story: WS-01, WS-06

## Context

The project targets local MCP client integrations where reliability, startup speed, and low operational overhead are primary requirements.

## Decision

Use stdio as the only transport in v1. HTTP transport is out of scope.

## Decision Drivers

- Simpler deployment and lower operational complexity.
- Strong compatibility with current MCP client workflows.
- Better focus on core functionality and data quality.

## Considered Options

1. stdio only
2. stdio + HTTP
3. HTTP only

## Pros and Cons of the Options

### stdio only

- Pros:
  - minimal operational surface
  - straightforward local integration
- Cons:
  - no remote transport in v1

### stdio + HTTP

- Pros:
  - broader transport flexibility
- Cons:
  - larger implementation and security surface

### HTTP only

- Pros:
  - network-native integration path
- Cons:
  - unnecessary complexity for v1 local use case

## Consequences

### Positive

- Faster delivery and lower maintenance in v1.

### Negative

- HTTP-specific scenarios deferred to future work.

### Neutral

- Transport expansion remains possible via future ADR.

## Implementation Notes

- Impacted projects/files: host wiring, docs, CI smoke tests.
- Migration/backward-compatibility considerations: N/A in v1.
- Test/verification requirements: enforce only stdio registration and end-to-end stdio tests.

## References

- `spec/01-system-overview-and-boundaries.md`
- `spec/06-mcp-contract-tools-resources-prompts.md`
