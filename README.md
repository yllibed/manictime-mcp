# ManicTime MCP

[![NuGet](https://img.shields.io/nuget/v/ManicTimeMcp.svg)](https://www.nuget.org/packages/ManicTimeMcp)

A .NET [MCP](https://modelcontextprotocol.io) server that gives AI agents read-only access to your local [ManicTime](https://www.manictime.com) activity data — applications, documents, websites, screenshots, and usage patterns.

> **Compatibility notice** — This project is an independent integration and is not affiliated with or endorsed by ManicTime or Finkit.

## Quick start

Requires the [.NET 10+ SDK](https://dotnet.microsoft.com/download/dotnet). Run directly with `dnx` — no install step needed:

```bash
dnx -y ManicTimeMcp mcp serve
```

This downloads the latest version and starts the server over **stdio**. It expects a local ManicTime Windows desktop installation with local storage.

## Local development modes

Use the project directly while developing:

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- mcp serve
```

Use the same app as a CLI or interactive REPL:

```bash
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj
```

The `--` is required with `dotnet run` because it separates `dotnet` launcher arguments from the app's own `mcp serve` command path.

## Agent configuration

### Visual Studio / VS Code (Copilot)

The package embeds a `.mcp/server.json` manifest. Browse [ManicTimeMcp on NuGet.org](https://www.nuget.org/packages/ManicTimeMcp), open the **MCP Server** tab, and copy the configuration into your IDE.

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

For any MCP-compatible client that accepts a JSON config:

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

## What it provides

The same app exposes three aligned surfaces:

- `MCP`: flattened tool and prompt names such as `timeline_list`, `summary_daily`, and `prompt_daily-review`.
- `CLI`: direct commands such as `timeline list --output:json`.
- `REPL`: the same hierarchical command graph with interactive help and prompts.

### Commands and MCP tools

| Repl / CLI command | MCP tool | Description |
|------|------|-------------|
| `timeline list` | `timeline_list` | List available ManicTime timelines |
| `activity list` | `activity_list` | Raw activities from a specific timeline |
| `activity computer-usage` | `activity_computer-usage` | Computer on/off/idle/locked activities |
| `activity tags` | `activity_tags` | User-defined tags and labels |
| `usage applications` | `usage_applications` | Application usage for a date range |
| `usage documents` | `usage_documents` | Document usage for a date range |
| `usage websites` | `usage_websites` | Website usage with hourly or daily breakdown |
| `summary daily` | `summary_daily` | Structured summary for a single day |
| `summary narrative` | `summary_narrative` | "What did I do?" narrative for a date range |
| `summary period` | `summary_period` | Multi-day overview with patterns and breakdowns |
| `screenshot list` | `screenshot_list` | Discover screenshots with metadata only |
| `screenshot get` | `screenshot_get` | Retrieve a screenshot payload |
| `screenshot crop` | `screenshot_crop` | Crop a region of interest from a screenshot |
| `screenshot save` | `screenshot_save` | Save a screenshot to disk within MCP client roots |

### Resources

| Resource URI | Repl / CLI command | Description |
|----------|------|-------------|
| `manictime://resource/config` | `resource config` | Active ManicTime configuration and resolved data directory |
| `manictime://resource/timelines` | `resource timelines` | Available timelines as a resource payload |
| `manictime://resource/health` | `resource health` | Server health and database status |
| `manictime://resource/guide` | `resource guide` | Repl-first usage guide for AI models and operators |
| `manictime://resource/environment` | `resource environment` | Device and OS information |
| `manictime://resource/data-range` | `resource data-range` | Available data date boundaries |

### Prompts

| Repl / CLI prompt | MCP prompt | Description |
|--------|------|-------------|
| `prompt daily-review` | `prompt_daily-review` | "Summarize my activities for {date}" |
| `prompt weekly-review` | `prompt_weekly-review` | "Summarize my week for {period}" |
| `prompt screenshot-investigation` | `prompt_screenshot-investigation` | "What was I doing during {window}?" |

## Supported scope

- **Supported:** ManicTime Windows desktop with local storage (`ManicTimeReports.db`).
- **Not supported (v1):** ManicTime Server deployments, non-Windows clients, server-centric collectors.
- **Transport:** stdio only.

## Building from source

```bash
dotnet restore src/ManicTimeMcp.slnx
dotnet build src/ManicTimeMcp.slnx -warnaserror
dotnet test --solution src/ManicTimeMcp.slnx
```

See `docs/getting-started.md` for prerequisites and `AGENTS.md` for engineering rules.

## Contributing

1. Read `spec/README.md` for workstream specifications.
2. Pick a workstream and implement only that scope.
3. See `AGENTS.md` for build commands, quality rules, and constraints.

## License

MIT
