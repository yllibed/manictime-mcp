# ManicTime MCP

[![NuGet](https://img.shields.io/nuget/v/ManicTimeMcp.svg)](https://www.nuget.org/packages/ManicTimeMcp)

A .NET [MCP](https://modelcontextprotocol.io) server that gives AI agents read-only access to your local [ManicTime](https://www.manictime.com) activity data — applications, documents, websites, screenshots, and usage patterns.

> **Compatibility notice** — This project is an independent integration and is not affiliated with or endorsed by ManicTime or Finkit.

## Quick start

Install as a .NET global tool:

```bash
dotnet tool install -g ManicTimeMcp
```

Then run it:

```bash
manictime-mcp
```

The server communicates over **stdio** and expects a local ManicTime Windows desktop installation with local storage.

## Agent configuration

### Claude Code

```bash
claude mcp add manictime-mcp -- dotnet tool run manictime-mcp
```

Or add to your project's `.mcp.json`:

```json
{
  "mcpServers": {
    "manictime-mcp": {
      "command": "dotnet",
      "args": ["tool", "run", "manictime-mcp"]
    }
  }
}
```

### Claude Desktop

Add to `claude_desktop_config.json` (`%APPDATA%\Claude\claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "manictime-mcp": {
      "command": "dotnet",
      "args": ["tool", "run", "manictime-mcp"]
    }
  }
}
```

### OpenAI Codex CLI

```bash
codex mcp add manictime-mcp -- dotnet tool run manictime-mcp
```

### GitHub Copilot (VS Code)

Add to `.vscode/mcp.json` in your workspace:

```json
{
  "servers": {
    "manictime-mcp": {
      "command": "dotnet",
      "args": ["tool", "run", "manictime-mcp"]
    }
  }
}
```

### Generic MCP JSON configuration

For any MCP-compatible client that accepts a JSON config:

```json
{
  "mcpServers": {
    "manictime-mcp": {
      "command": "dotnet",
      "args": ["tool", "run", "manictime-mcp"],
      "transportType": "stdio"
    }
  }
}
```

## What it provides

### Tools

| Tool | Description |
|------|-------------|
| `get_daily_summary` | Structured summary for a single day — segments, top apps, websites, screenshots |
| `get_activity_narrative` | "What did I do?" for a date range — segments with documents, websites, tags |
| `get_period_summary` | Multi-day overview with per-day breakdown and day-of-week patterns |
| `get_website_usage` | Website usage with hourly or daily breakdown |
| `get_timelines` | List available ManicTime timelines |
| `get_activities` | Raw activities from a specific timeline |
| `list_screenshots` | Discover screenshots with metadata (zero image bytes) |
| `get_screenshot` | Retrieve a screenshot — thumbnail for model, full-size for human |
| `crop_screenshot` | Crop a region of interest from a screenshot |
| `save_screenshot` | Save a screenshot to disk within MCP client roots |

### Resources

| Resource | Description |
|----------|-------------|
| `manictime://health` | Server health and database status |
| `manictime://guide` | Usage guide for AI models — tool workflows, decision trees, playbooks |
| `manictime://environment` | Device and OS information |
| `manictime://data-range` | Available data date boundaries |
| `manictime://screenshot/{ref}` | Lazy-fetch screenshot by reference |

### Prompts

| Prompt | Description |
|--------|-------------|
| `daily_review` | "Summarize my activities for {date}" |
| `weekly_review` | "Summarize my week from {startDate} to {endDate}" |
| `screenshot_investigation` | "What was I doing at {datetime}?" |

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
