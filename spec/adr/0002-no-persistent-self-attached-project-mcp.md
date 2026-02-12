# ADR 0002 — No Persistent Self-Attached Project MCP In Workspace

- Status: Accepted
- Date: 2026-02-12
- Deciders: Project maintainers
- Technical Story: WS-11, agent workflow policy

## Context

Running a workspace's in-development MCP server as an always-on MCP dependency can create process and file-lock contention, especially on Windows during build/test cycles.

## Decision

Do not attach the in-development project MCP server persistently in the same workspace by default.

Use short-lived start/test/stop execution only for MCP protocol verification.

## Decision Drivers

- Reduce file locking and output collision risks.
- Improve autonomy and reliability for coding agents.
- Keep development and protocol verification concerns separated.

## Considered Options

1. Persistent self-attached MCP in workspace
2. No persistent self-attach, ephemeral verification only

## Pros and Cons of the Options

### Persistent self-attached MCP in workspace

- Pros:
  - immediate local MCP availability
- Cons:
  - file lock risk
  - harder lifecycle management
  - interference with parallel builds/tests

### No persistent self-attach, ephemeral verification only

- Pros:
  - cleaner build/test workflow
  - fewer process contention issues
  - easier CI parity
- Cons:
  - slightly more setup for manual ad-hoc checks

## Consequences

### Positive

- More stable autonomous and CI workflows.

### Negative

- Manual MCP checks require explicit launch commands.

### Neutral

- Public MCP servers remain usable as always-on references.

## Implementation Notes

- `.mcp.json` may include public/reference MCP servers (for example Microsoft Learn MCP) but must not include a persistent self-attached local project MCP server.
- Agent instructions (`AGENTS.md`, `CLAUDE.md`, Copilot instructions) enforce this policy.
- Protocol tests launch local server in ephemeral mode only.

## References

- `docs/mcp-client-strategy.md`
- `spec/11-ci-release-and-publishing.md`
