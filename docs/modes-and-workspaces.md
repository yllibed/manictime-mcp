# Modes and Workspaces

This project is a **Repl-first** application that exposes the same command graph through three surfaces: `MCP`, `CLI`, and `REPL`.

## Mode overview

| Mode | Purpose | Typical entrypoint |
|---|---|---|
| `MCP` | Serve tools/resources/prompts to an MCP client over stdio | `dnx -y ManicTimeMcp mcp serve` |
| `CLI` | Run one command directly and inspect the result | `dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- timeline list --output:json` |
| `REPL` | Explore the same command graph interactively | `dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj` |

## MCP mode

Use MCP mode when an agent client should discover and call the server over stdio.

Published package:

```bash
dnx -y ManicTimeMcp mcp serve
```

Local development:

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- mcp serve
```

### Important `--` rule

- `dotnet run -- ...` requires the `--` separator.
- MCP client JSON configs do **not** use `--` because they already pass arguments as an array.

Example JSON config:

```json
{
  "mcpServers": {
    "manictime-mcp": {
      "command": "dnx",
      "args": ["-y", "ManicTimeMcp", "mcp", "serve"]
    }
  }
}
```

## CLI mode

Use CLI mode when you want to run one command directly from the same Repl graph.

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- timeline list --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- usage summary --period 2026-03-20..2026-03-21 --type applications --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- resource health --output:json
```

CLI mode is useful for:

- local payload inspection
- troubleshooting route binding
- validating date/time ranges
- checking resource outputs without an MCP client

## REPL mode

Use REPL mode when you want interactive discovery and help.

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj
```

Typical REPL tasks:

- browse contexts such as `timeline`, `usage`, `summary`, `screenshot`, and `workspace`
- inspect `--help`
- experiment with argument shapes before wiring an MCP client

## Workspace roots

`screenshot save` writes only inside MCP client roots.

There are two ways roots can exist in a session:

1. **Native roots**
   The MCP client advertises workspace roots directly.
2. **Soft roots**
   The client does not support roots, so the session initializes them explicitly.

## `workspace init`

`workspace init {path}` is the canonical soft-roots fallback.

Use it when:

- the MCP client does not support native roots
- the agent needs to persist screenshot assets with `screenshot save`

Example:

```text
workspace init C:\reports\weekly-recap
```

This sets a session-scoped workspace root so later `screenshot save` calls can resolve a relative output path safely inside that directory.

## Screenshot workflow

Canonical workflow:

1. `screenshot list`
2. `screenshot get`
3. `screenshot crop`
4. `workspace init` if the client lacks native roots
5. `screenshot save`

### Client with native roots

```text
screenshot list --window 2026-03-20T09:00:00..2026-03-20T10:00:00
screenshot get --screenshotRef <ref>
screenshot crop --screenshotRef <ref> --x 20 --y 15 --width 40 --height 30
screenshot save --screenshotRef <ref> --outputPath assets/focus
```

### Client without native roots

```text
workspace init C:\reports\weekly-recap
screenshot list --window 2026-03-20T09:00:00..2026-03-20T10:00:00
screenshot get --screenshotRef <ref>
screenshot crop --screenshotRef <ref> --x 20 --y 15 --width 40 --height 30
screenshot save --screenshotRef <ref> --outputPath assets/focus
```

### Local CLI exploration

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- screenshot list --window 2026-03-20T09:00:00..2026-03-20T10:00:00 --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- screenshot get --screenshotRef <ref> --output:json
```

CLI mode is best for inspecting payloads. The workspace root flow is primarily relevant to MCP sessions.

## Related docs

- [README](../README.md)
- [getting-started.md](getting-started.md)
- [mcp-client-strategy.md](mcp-client-strategy.md)
