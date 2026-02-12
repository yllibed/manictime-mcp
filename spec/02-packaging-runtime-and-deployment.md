# WS-02 — Packaging, Runtime, and Deployment

## Objectives

- Ship a long-lived, reliable package with fast startup.
- Minimize runtime prerequisites for end users.
- Keep memory footprint low under idle and typical workload.

## Scope

- Target framework and NuGet package metadata.
- Tool packaging profile (`dotnet tool` and `dnx` consumption).
- Publish settings impacting startup time and memory.
- Repository-level package/version management setup.
- SDK pinning and reproducible build setup.
- Versioning strategy tooling.

## Non-Scope

- Business logic implementation.
- MCP tool contract definitions.

## Functional Requirements

- Target `net10.0`.
- Build as MCP package (`PackageType=McpServer`).
- Support Windows-specific RID packaging for primary usage (`win-x64`, `win-arm64`) plus `any` as cross-platform host fallback.
- Non-Windows host execution (for example WSL) is compatibility mode only and requires explicit data directory configuration (`MANICTIME_DATA_DIR`).
- Be invocable through both `dotnet tool` and `dnx`-based MCP configs.
- Use central package management (`Directory.Packages.props`).
- Use `.slnx` as the solution format.
- Pin SDK using `global.json` (`10.x` SDK).
- Use Nerdbank.GitVersioning for package and assembly versioning.
- Enable reproducible build settings; keep lock-file restore optional until upstream tooling support is stable.
- Include MCP package metadata manifest (`.mcp/server.json`) in publish artifacts.

## Non-Functional Requirements

- Optimize for cold-start latency.
- Optimize for low steady-state memory.
- Avoid brittle publish optimizations that break reflection-heavy paths.

## Technical Design

### Required `.csproj` profile

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <PackAsTool>true</PackAsTool>
  <PackageType>McpServer</PackageType>

  <RuntimeIdentifiers>win-x64;win-arm64;any</RuntimeIdentifiers>
  <ToolPackageRuntimeIdentifiers>win-x64;win-arm64;any</ToolPackageRuntimeIdentifiers>

  <PublishSelfContained>true</PublishSelfContained>
  <PublishSingleFile>true</PublishSingleFile>
  <PublishReadyToRun>true</PublishReadyToRun>
  <InvariantGlobalization>true</InvariantGlobalization>
  <PublishTrimmed>false</PublishTrimmed>
</PropertyGroup>
```

### Repository packaging/build configuration

- `Directory.Packages.props` is mandatory for package versions.
- `Directory.Build.props` is mandatory for shared compiler/runtime properties.
- Package versions are forbidden inside individual `.csproj` files unless explicitly exempted.
- `global.json` is mandatory and pinned to .NET 10 SDK.
- Nerdbank.GitVersioning config (`version.json`) is mandatory for version metadata.
- Reproducible build defaults in `Directory.Build.props`:
  - `ContinuousIntegrationBuild=true`
  - `Deterministic=true`
- Do not set `SelfContained=true` at project level; use publish/package profile settings instead.
- If lock files are re-enabled in the future, document policy and CI behavior via ADR.

### Optimization policy

- Default profile favors predictable compatibility.
- NativeAOT is a separate opt-in profile, enabled only after contract/integration tests pass.
- Trimming remains disabled by default in v1.

## Implementation Autonomy

This workstream is implementation-ready without database or tool logic. It can produce a runnable shell package and deployment pipeline.

## Testing Requirements

- Build matrix: all declared RIDs.
- Startup benchmark: process launch to MCP `initialize` completion.
- Memory baseline: idle memory and first-request memory.
- Packaging test: install/run via both `dotnet tool` and `dnx` configuration.
- Verify central package management restore consistency in CI.
- Verify `.slnx` build/test/pack orchestration works end-to-end.
- Verify both Windows RID packages and `any` fallback package paths.
- Verify Nerdbank.GitVersioning generated versions are stable and tag-consistent.

## Risks and Mitigations

- Risk: startup optimizations causing runtime incompatibilities.
  - Mitigation: conservative defaults, opt-in aggressive profiles.
- Risk: package bloat from self-contained publishing.
  - Mitigation: RID targeting, periodic size audits.

## Maintainability Considerations

- Keep publish profile explicit and version-controlled.
- Separate release profiles (`default`, `perf-lab`, `aot-experimental`).
- Document why each publish flag exists.

## Exit Criteria

- Packaging profile compiles and packs successfully.
- Startup/memory baseline captured and published.
- Client launch via `dnx` and `dotnet tool` verified.
