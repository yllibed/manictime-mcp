# AGENTS.md

Repository guidance for autonomous coding agents.

## Current State

- The solution scaffolding is in place; feature implementation can begin.
- The source of truth is `spec/README.md` and its linked workstreams.

## Start Here

1. Read `spec/README.md`.
2. Select one workstream.
3. Implement only that workstream scope and acceptance criteria.
4. Run the required build, test, and pack checks before declaring done.

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

## Build and Test Commands

```
dotnet restore src/ManicTimeMcp.slnx
dotnet build src/ManicTimeMcp.slnx -warnaserror
dotnet test --solution src/ManicTimeMcp.slnx
```

**Important .NET 10 notes:**
- `dotnet test` requires `--solution` flag (not a bare positional path).
- The test runner is Microsoft Testing Platform (MTP), configured in the `"test"` section of `global.json`.

## Packaging Smoke Command

```
dotnet pack src/ManicTimeMcp.slnx -c Release
```

## CI Bootstrap Recipe

Use this sequence as the CI baseline (and local pre-push parity):

```
dotnet restore src/ManicTimeMcp.slnx
dotnet build src/ManicTimeMcp.slnx -warnaserror
dotnet test --solution src/ManicTimeMcp.slnx
dotnet pack src/ManicTimeMcp.slnx -c Release
```

Minimum CI requirements:
- Run the four commands above on every PR and protected branch push.
- Fail the build on any warning by keeping warnings-as-errors enabled.
- Publish test results, coverage artifacts, and produced `*.nupkg` files.

## MCP Usage Policy For Agents

- Do not register this repository's in-development server as an always-on MCP in your primary working profile.
- For MCP integration checks, use short-lived black-box runs only (start, test, stop).
- Prefer dedicated public MCP servers for reference/documentation tasks.
- Approved public MCP examples include Microsoft Learn MCP and GitHub MCP (when available in your client environment).

## Engineering Constraints

- `stdio` is the only MCP transport for v1.
- Read-only access to ManicTime data.
- ManicTime data is high-value user data. In both development and runtime, apply exceptional caution systematically: never modify/delete/move ManicTime artifacts, and fail safe when uncertain.
- Favor immutable designs and pure logic where practical.
- Optimize startup time and memory.
- Target zero warnings as an engineering objective (as much as practical), with warnings treated as errors by default.
- Use tabs for code/MSBuild indentation (see `.editorconfig`).
- Public members require XML documentation comments (disabled in test projects).
- Package versions belong exclusively in `src/Directory.Packages.props` (central package management).

## Cleanup Discipline

Actively avoid the lava flow antipattern. After every change:

- Remove dead code, unused usings, orphaned files, and stale references.
- Do not leave commented-out code, placeholder stubs, or TODO-only files behind.
- If a refactoring makes a type, method, or file obsolete, delete it in the same changeset.
- Run the build after cleanup to confirm nothing was still needed.

## Known Pitfalls

- `SelfContained=true` in a csproj prevents test projects from referencing it (NETSDK1151). Use `PublishSelfContained=true` instead — it only applies during `dotnet publish`.
- `ContinuousIntegrationBuild` must be conditional on `$(CI)` to preserve local debug source paths.
- MSTest.Sdk already bundles CodeCoverage and TrxReport extensions. Do not add them again in test csproj files (causes NU1504 duplicate reference).
- With central package management and multiple NuGet sources, a `nuget.config` with `<packageSourceMapping>` is required (NU1507).
- `packages.lock.json` files are **not committed** — Dependabot cannot update them (dependabot/dependabot-core#13474). CPM provides sufficient version determinism.

## Key Standards Documents

- Architecture and coding standards: `spec/08-code-quality-architecture-and-dotnet-standards.md`
- Performance and memory: `spec/07-performance-and-memory-engineering.md`
- Testing and quality gates: `spec/09-testing-and-quality-gates.md`
- CI/release and publishing: `spec/11-ci-release-and-publishing.md`

## Agent-Specific Notes

- Codex: follow this file + workstream specs.
- Claude Code: also see `CLAUDE.md`.
- GitHub Copilot coding agent: also see `.github/copilot-instructions.md`.
