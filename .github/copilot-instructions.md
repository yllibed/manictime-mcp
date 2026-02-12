# Copilot Instructions

All shared engineering rules live in `AGENTS.md`. Read it first — it is the single source of truth for build commands, project layout, quality rules, cleanup discipline, and known pitfalls.

This file adds only GitHub Copilot-specific guidance.

## Workflow

1. Read `spec/README.md`.
2. Select one workstream.
3. Implement only that workstream scope.
4. Run required build/test/pack checks (see `AGENTS.md`).

## MCP Policy

- Do not attach this repository's in-progress MCP server as a persistent MCP dependency of the coding session.
- Use ephemeral start/test/stop runs for MCP protocol verification.
- Prefer public MCP servers for documentation lookup tasks (for example Microsoft Learn MCP).
