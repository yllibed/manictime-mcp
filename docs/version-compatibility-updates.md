# ManicTime Version Compatibility Updates

This runbook validates a new ManicTime Windows desktop version against ManicTime MCP and records it as the tested compatible version.

## Scope

- Validate local desktop ManicTime data only.
- Use a local snapshot under `.database/<version>/`.
- Never modify, delete, move, or run write operations against the active ManicTime data directory.
- Do not commit `.database/` contents. The directory is ignored because it contains personal data and large binaries.

## 1. Detect The Installed Version

From the repository root:

```powershell
$installDir = (Get-ItemProperty -Path HKLM:\SOFTWARE\FinKit\ManicTime -ErrorAction Stop).InstallDir
$exe = Join-Path $installDir "ManicTime.exe"
$version = (Get-Item $exe).VersionInfo.ProductVersion
$dataDir = Join-Path $env:LOCALAPPDATA "Finkit\ManicTime"

$version
$dataDir
```

If `HKLM:\SOFTWARE\FinKit\ManicTime` is unavailable, find `ManicTime.exe` manually and use its `ProductVersion`.

## 2. Snapshot The Local Database

Copy the current databases and SQLite sidecars into a versioned local snapshot:

```powershell
$snapshot = Join-Path (Get-Location) ".database\$version"
New-Item -ItemType Directory -Path $snapshot -Force | Out-Null

foreach ($name in @("ManicTimeReports.db", "ManicTimeCore.db")) {
    foreach ($suffix in @("", "-wal", "-shm")) {
        $source = Join-Path $dataDir "$name$suffix"
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination (Join-Path $snapshot "$name$suffix") -Force
        }
    }
}

Get-ChildItem -File $snapshot | Select-Object FullName,Length,LastWriteTime
```

Only `ManicTimeReports.db` is required by the MCP server. `ManicTimeCore.db` is copied as supporting evidence for local investigation.
If ManicTime is actively writing to the databases, close ManicTime first or use a SQLite backup workflow before copying so the snapshot is internally consistent.

## 3. Run Compatibility Smoke Checks

Point the MCP server at the snapshot:

```powershell
$env:MANICTIME_DATA_DIR = (Resolve-Path ".database\$version").Path
```

Run the health and resource checks:

```powershell
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- resource health --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- timeline list --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- resource environment --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- resource data-range --output:json
```

Run at least one query from each major surface using dates that exist in `resource data-range`:

```powershell
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- usage applications --period 2026-07-01..2026-07-02 --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- usage documents --period 2026-07-01..2026-07-02 --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- usage websites --period 2026-07-01..2026-07-02 --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- summary daily 2026-07-01 --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- summary narrative --period 2026-07-01..2026-07-02 --output:json
dotnet run --project src/ManicTimeMcp/ManicTimeMcp.csproj -- screenshot list --window 2026-07-01T09:00:00..2026-07-01T10:00:00 --output:json
```

If `dotnet run` tries to restore packages and fails because the machine has no network access, run the validation again once restore can reach NuGet, or use an already-restored checkout and pass `--no-restore` before `--project`.

## 4. Evaluate Results

The version can be marked compatible when:

- `resource health` reports no fatal issues.
- `schemaStatus` is `valid` or `validWithWarnings`.
- Any warnings are understood and do not break documented v1 behavior.
- Timeline, resource, usage, summary, and screenshot commands return valid JSON payloads.
- No command opens the active ManicTime database for writing.

If the schema has drifted:

- Update `SchemaManifest` only for actual schema changes that affect supported behavior.
- Keep additive new tables or columns optional unless a supported query requires them.
- Add or update focused tests for the affected validator, repository, tool, or resource.
- Re-run the smoke checks against the snapshot.

## 5. Mark The Version As Tested

Update the tested-version metadata:

- `src/ManicTimeMcp/Configuration/HealthService.cs`
  - `TestedManicTimeVersion`
- `src/ManicTimeMcp.Tests/Configuration/HealthServiceTests.cs`
  - `TestVersion`

Do not change package version metadata only to record a ManicTime compatibility validation. Package version changes still follow the release process in `version.json` and CI/release documentation.

## 6. Run Final Quality Gates

Run the standard validation chain:

```powershell
dotnet restore src/ManicTimeMcp.slnx
dotnet build src/ManicTimeMcp.slnx -warnaserror
dotnet test --solution src/ManicTimeMcp.slnx
dotnet pack src/ManicTimeMcp.slnx -c Release
```

Before declaring the update done, record:

- installed ManicTime product version
- snapshot path used for validation
- health status and schema status
- commands run
- any warnings or accepted limitations
- source changes made
