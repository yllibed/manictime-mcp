# MCP Client Strategy

This document defines how MCP clients should be configured while working in this repository.

## Default Rule

Do not configure the local in-development ManicTime MCP server as a persistent MCP dependency of the same coding workspace.

Reason:

- reduces file-lock risk during build/test cycles
- avoids process lifecycle conflicts for autonomous agents
- keeps build and MCP verification concerns separated

## Recommended Pattern

Use MCP in two modes:

1. Reference mode (always-on): public MCP servers for documentation/reference.
2. Verification mode (ephemeral): start local server only for protocol checks, then stop it.

## Public MCP Recommendations

- Microsoft Learn MCP (official endpoint): `https://learn.microsoft.com/api/mcp`
- GitHub MCP (when available in your client/tooling environment)

## Local Project MCP Verification Pattern

- Build or publish the artifact.
- Start an isolated process with `mcp serve`.
- Run protocol tests.
- Stop process.
- Treat ManicTime data as high-value user data at all times; verification must remain strictly read-only and safety-first.

Typical development command:

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- mcp serve
```

Published-package equivalent:

```bash
dnx -y ManicTimeMcp mcp serve
```

Never keep this process attached as an always-on coding MCP while changing the same repository.

For the difference between MCP, CLI, and REPL usage, plus the `workspace init` soft-roots workflow, see [modes-and-workspaces.md](D:\src\manictime-mcp\docs\modes-and-workspaces.md).

## Local `.mcp.json` Policy In This Repo

- `.mcp.json` includes public/reference MCP servers (for example Microsoft Learn MCP).
- `.mcp.json` must not include a persistent self-attached local ManicTime MCP server from this same workspace.
- Add personal/local overrides outside the repository when needed.
