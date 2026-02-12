# Test Fixtures and Data Strategy

## Fixture Tiers

### Tier A — Minimal Synthetic (default)

Code-based fixtures committed in the test project. All test data is generated programmatically — no external files or scripts required.

| Fixture | Location | Purpose |
|---------|----------|---------|
| `FixtureDatabase` | `Tests/Database/FixtureDatabase.cs` | Creates temp SQLite databases with standard, partial, or drift schemas |
| `FixtureSeeder` | `Tests/Database/FixtureSeeder.cs` | Seeds 4 timelines, 4 groups, 8 activities (including null/edge cases) |
| `FixtureConnectionFactory` | `Tests/Database/FixtureConnectionFactory.cs` | Wraps fixture DB as `IDbConnectionFactory` for repository tests |
| Screenshot fixtures | `Tests/Screenshots/ScreenshotServiceTests.cs` | Creates temp directories with `.jpg` files matching ManicTime naming patterns |
| MCP stubs | `Tests/Mcp/Stub*.cs` | In-memory stubs for `ITimelineRepository`, `IActivityRepository`, `IScreenshotService`, etc. |

### Tier B — Stress Synthetic (on demand)

Generated in CI performance jobs. Deferred to WS-14 (profiling infrastructure).

### Tier C — Sanitized Real-World (never committed)

Optional locally sanitized snapshots of real ManicTime data. These must never be committed and require irreversible anonymization before use.

## Design Principles

- **Deterministic**: Every fixture produces identical output given the same inputs. No random data, no timestamps from `DateTime.Now`.
- **Self-contained**: Tests create and destroy their own fixtures — no shared state between tests.
- **PII-free**: All committed fixtures use synthetic data only. Activity names, document paths, and timestamps are fabricated.
- **Fast**: Tier A fixtures use in-memory or temp-file SQLite. No network, no external processes.

## Schema Coverage

The database fixtures cover:

- **Happy path**: Standard ManicTime schema with all required tables and columns
- **Missing tables**: Partial schemas for schema drift detection
- **Missing columns**: Tables with individual columns removed
- **Null handling**: Activities with null `DisplayName` and `GroupId`
- **Empty databases**: Schema present but no data rows
- **Boundary timestamps**: Activities spanning typical work hours

## Screenshot Coverage

The screenshot filesystem fixtures cover:

- **Canonical filenames**: Standard ManicTime screenshot naming pattern
- **Thumbnail/full pairing**: Both `.thumbnail.jpg` and full-size variants
- **Time window filtering**: Screenshots across multiple dates and times
- **Sampling**: Interval-based down-sampling behavior
- **Security**: Path traversal prevention and extension validation

## Privacy Guardrails

- No real ManicTime data is committed to the repository
- All fixture data is fabricated with generic names (e.g., "devenv.exe", "chrome.exe", "Program.cs")
- Timestamps use fixed dates in 2025 with no personal significance
- The `.gitignore` should exclude any `*.db` files outside the test temp directory
