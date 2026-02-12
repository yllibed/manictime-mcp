# CLAUDE.md

All shared engineering rules live in `AGENTS.md`. Read it first — it is the single source of truth for build commands, project layout, quality rules, cleanup discipline, and known pitfalls.

This file adds only Claude Code-specific guidance.

## Project Mode

This repository is specification-first.

Before coding:

1. Read `spec/README.md`.
2. Choose a single workstream.
3. Implement only the selected workstream scope.

## Required Validation

Run build and tests before finalizing changes:

```
dotnet restore src/ManicTimeMcp.slnx
dotnet build src/ManicTimeMcp.slnx -warnaserror
dotnet test --solution src/ManicTimeMcp.slnx
```

## MCP Workspace Policy

- Do not keep the local in-development ManicTime MCP server attached as a persistent workspace MCP while coding this repo.
- Use short-lived process runs for integration checks only.
- For knowledge retrieval tasks, prefer public MCP servers (for example Microsoft Learn MCP) rather than self-attaching the current project build.
