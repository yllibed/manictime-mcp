# ManicTime MCP

[![NuGet](https://img.shields.io/nuget/v/ManicTimeMcp.svg)](https://www.nuget.org/packages/ManicTimeMcp)

A .NET [MCP](https://modelcontextprotocol.io) server and Repl-based command app that gives AI agents and local operators read-only access to local [ManicTime](https://www.manictime.com) activity data: applications, documents, websites, screenshots, and usage patterns.

> **Compatibility notice** — This project is an independent integration and is not affiliated with or endorsed by ManicTime or Finkit.

## Three aligned modes

The same application exposes three aligned surfaces:

| Mode | Purpose | Typical command |
|---|---|---|
| `MCP` | Serve stdio tools/resources/prompts to an agent client | `dnx -y ManicTimeMcp mcp serve` |
| `CLI` | Run one command directly and emit structured output | `dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- timeline list --output:json` |
| `REPL` | Explore the command graph interactively with help and discovery | `dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj` |

### When to use which mode

- Use `MCP` when Claude, Copilot, Codex, or another MCP-compatible client should call the server over stdio.
- Use `CLI` when you want one direct command locally, especially for testing payloads or troubleshooting output.
- Use `REPL` when you want interactive discovery, `--help`, or to explore routes before wiring an MCP client.

## Launching the app

### Published package

Requires the [.NET 10+ SDK](https://dotnet.microsoft.com/download/dotnet). Run directly with `dnx`:

```bash
dnx -y ManicTimeMcp mcp serve
```

This downloads the latest published package and starts the MCP server over **stdio**.

### Local development

Start the MCP server from source:

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- mcp serve
```

Run one CLI command directly:

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- timeline list --output:json
```

Start the interactive REPL:

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj
```

`dotnet run` requires the `--` separator because it splits `dotnet` launcher arguments from the app's own command path. MCP client JSON configs do **not** use this separator because they already pass arguments as an explicit array.

## Agent configuration

### Visual Studio / VS Code (Copilot)

The package embeds a `.mcp/server.json` manifest. Browse [ManicTimeMcp on NuGet.org](https://www.nuget.org/packages/ManicTimeMcp), open the **MCP Server** tab, and copy the generated configuration into your IDE.

Or add manually to `.vscode/mcp.json`:

```json
{
  "servers": {
    "manictime-mcp": {
      "type": "stdio",
      "command": "dnx",
      "args": ["-y", "ManicTimeMcp", "mcp", "serve"]
    }
  }
}
```

### Claude Code

```bash
claude mcp add manictime-mcp -- dnx -y ManicTimeMcp mcp serve
```

### Claude Desktop

Add to `claude_desktop_config.json` (`%APPDATA%\Claude\claude_desktop_config.json`):

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

### OpenAI Codex CLI

```bash
codex mcp add manictime-mcp -- dnx -y ManicTimeMcp mcp serve
```

### Generic MCP JSON configuration

```json
{
  "mcpServers": {
    "manictime-mcp": {
      "command": "dnx",
      "args": ["-y", "ManicTimeMcp", "mcp", "serve"],
      "transportType": "stdio"
    }
  }
}
```

## Repl-first surface

### Commands and MCP tools

| Repl / CLI command | MCP tool | Description |
|---|---|---|
| `timeline list` | `timeline_list` | List available ManicTime timelines |
| `activity list` | `activity_list` | Raw activities from a specific timeline |
| `activity computer-usage` | `activity_computer-usage` | Computer on/off/idle/locked activities |
| `activity tags` | `activity_tags` | User-defined tags and labels |
| `usage applications` | `usage_applications` | Application usage for a date range |
| `usage documents` | `usage_documents` | Document usage for a date range |
| `usage websites` | `usage_websites` | Website usage with hourly or daily breakdown |
| `summary daily` | `summary_daily` | Structured summary for a single day |
| `summary narrative` | `summary_narrative` | Structured "what did I do?" narrative |
| `summary period` | `summary_period` | Multi-day overview with patterns and breakdowns |
| `screenshot list` | `screenshot_list` | Discover screenshots with metadata only |
| `screenshot get` | `screenshot_get` | Retrieve a screenshot payload |
| `screenshot crop` | `screenshot_crop` | Crop a region of interest from a screenshot |
| `screenshot save` | `screenshot_save` | Save a screenshot within MCP client roots |
| `workspace init` | `workspace_init` | Initialize soft roots for clients without native roots |

### Resources

| Resource URI | Repl / CLI command | Description |
|---|---|---|
| `manictime://resource/config` | `resource config` | Active ManicTime configuration and resolved data directory |
| `manictime://resource/timelines` | `resource timelines` | Available timelines as a resource payload |
| `manictime://resource/health` | `resource health` | Server health and database status |
| `manictime://resource/guide` | `resource guide` | Repl-first usage guide for AI models and operators |
| `manictime://resource/environment` | `resource environment` | Device and OS information |
| `manictime://resource/data-range` | `resource data-range` | Available data date boundaries |

### Prompts

| Repl / CLI prompt | MCP prompt | Description |
|---|---|---|
| `prompt daily-review` | `prompt_daily-review` | Summarize my activities for a date |
| `prompt weekly-review` | `prompt_weekly-review` | Summarize a multi-day period |
| `prompt screenshot-investigation` | `prompt_screenshot-investigation` | Investigate what was happening during a date-time window |

## Workspace and screenshot save workflow

`screenshot save` writes only inside MCP client roots.

- If the MCP client supports native roots, `screenshot save` uses them directly.
- If the client does not support native roots, call `workspace init {path}` first to establish **soft roots** for the session, then call `screenshot save`.
- `workspace init` is a session-scoped MCP helper. It exists mainly for agent workflows, not as a replacement for local filesystem paths in CLI mode.

See [docs/modes-and-workspaces.md](D:\src\manictime-mcp\docs\modes-and-workspaces.md) for end-to-end examples.

## Supported scope

- **Supported:** ManicTime Windows desktop with local storage (`ManicTimeReports.db`).
- **Not supported (v1):** ManicTime Server deployments, non-Windows clients, server-centric collectors.
- **Transport:** stdio only.

## Building from source

```bash
dotnet restore src/ManicTimeMcp.slnx
dotnet build src/ManicTimeMcp.slnx -warnaserror
dotnet test --solution src/ManicTimeMcp.slnx
dotnet pack src/ManicTimeMcp.slnx -c Release
```

See [getting-started.md](D:\src\manictime-mcp\docs\getting-started.md) for prerequisites and local usage, and [AGENTS.md](D:\src\manictime-mcp\AGENTS.md) for engineering rules.

## Contributing

1. Read `spec/README.md` for workstream specifications.
2. Pick a workstream and implement only that scope.
3. See `AGENTS.md` for build commands, quality rules, and constraints.

## License

MIT
