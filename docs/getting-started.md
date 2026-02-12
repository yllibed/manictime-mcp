# Getting Started

## Supported Product Scope (v1)

- Supported: ManicTime Windows desktop with local data storage.
- Not supported: ManicTime Server and non-Windows client/server collection workflows.

## Prerequisites

- .NET 10 SDK (pinned in `global.json`; run `dotnet --version` to verify).
- Git.

## Building and Testing

```
dotnet restore src/ManicTimeMcp.slnx
dotnet build src/ManicTimeMcp.slnx -warnaserror
dotnet test --solution src/ManicTimeMcp.slnx
dotnet pack src/ManicTimeMcp.slnx -c Release
```

**Important .NET 10 notes:**
- `dotnet test` uses the `--solution` flag (not a bare positional path).
- The test runner is Microsoft Testing Platform (MTP), configured in the `"test"` section of `global.json`.
- Engineering target is zero warnings as much as practical; warnings are treated as errors by default.

## Data Safety (Mandatory)

- Treat ManicTime artifacts as high-value user data.
- In both development and runtime, apply exceptional caution systematically.
- Never modify, delete, or move ManicTime data files or screenshot artifacts.
- If safety is uncertain, stop and require explicit confirmation before any risky action.

## Project Layout

```
global.json                 .NET SDK pin (10.0.x) + MSTest.Sdk + MTP test runner
version.json                Nerdbank.GitVersioning (semver source of truth)
nuget.config                Single-source NuGet with package source mapping
src/
  ManicTimeMcp.slnx         Solution (canonical build entry point)
  Directory.Build.props     Shared compiler/analyzer settings
  Directory.Packages.props  Central package version management
  ManicTimeMcp/             MCP server executable
  ManicTimeMcp.Tests/       Tests (MSTest + AwesomeAssertions)
spec/                       Workstream specifications (read-only reference)
docs/                       User and contributor documentation
```

## For Contributors

1. Read `spec/README.md`.
2. Select one workstream and follow its scope.
3. Keep code and tests aligned with the relevant workstream acceptance criteria.
4. Run the full validation chain (restore, build, test, pack) before submitting changes.
5. For CI workflow setup and artifact expectations, see `docs/ci-setup.md`.

## Agent Orientation Files

- `AGENTS.md` — baseline instructions for autonomous coding agents.
- `CLAUDE.md` — Claude Code focused guidance.
- `.github/copilot-instructions.md` — GitHub Copilot coding agent guidance.
- `.mcp.json` — includes approved public MCP entries; does not self-attach this local project MCP.
- `docs/mcp-client-strategy.md` — policy for public MCP usage and local MCP verification mode.

## For Users

Implementation and package usage instructions will be added once the first runnable release candidate exists.
