# WS-11 — CI, Release, and Publishing (GitHub Actions)

## Objectives

- Standardize CI and release automation with GitHub Actions.
- Prevent broken packages from reaching the registry.
- Ensure packaging and full test suites are validated before push and before publish.

## Scope

- GitHub Actions workflow strategy.
- Local pre-push validation requirements.
- Release pipeline gates and package publishing process.
- MCP package publication metadata and manifest expectations.

## Non-Scope

- Application feature implementation.
- Deep operational platform governance (documented briefly only).

## Functional Requirements

- CI must run on pull requests and on protected branch pushes.
- CI must run build + full test suite + coverage + package dry-run.
- Release publish job must be blocked unless CI is green.
- Release workflow must publish NuGet package only from tagged releases.
- MCP package must include required metadata files for client discovery/configuration (for example `.mcp/server.json` in package content).
- Workspace default configuration must not attach the in-development project server as a persistent MCP dependency.
- MCP protocol verification in automation must use short-lived start/test/stop executions only.

## Non-Functional Requirements

- Fast feedback for PRs (parallel jobs where possible).
- Deterministic build/test environment.
- Detailed diagnostics available as artifacts for failures.

## Workflow Design

### Workflow A — `ci.yml`

Triggers:
- pull_request
- push (main/develop/release branches)

Required jobs:
- restore
- build (`.slnx`)
- test (all test projects, MTP, MSTest, AwesomeAssertions)
- coverage collection + report generation
- package dry-run (`dotnet pack`, no publish)
- `dnx` smoke validation for package startup

Artifacts:
- test result files
- coverage raw + HTML summary
- packaged nupkg from dry-run

### Workflow B — `release.yml`

Triggers:
- Git tag matching release pattern
- manual dispatch (maintainers only)

Required gates:
- successful completion of `ci.yml`
- version/tag consistency check
- package signature and metadata checks

Actions:
- build release artifacts
- run final smoke tests
- publish to NuGet
- create GitHub release notes

### MCP packaging manifest note

- Maintain `.mcp/server.json` in the package as the canonical MCP server metadata manifest for published consumption.
- Keep local development client config separate from packaged manifest metadata.

## Pre-Push Validation Policy

Before any push, local validation must run:

1. `dotnet restore <solution.slnx>`
2. `dotnet build <solution.slnx> -warnaserror`
3. `dotnet test --solution <solution.slnx>`
4. coverage command used by CI
5. `dotnet pack` dry-run

Recommended implementation:
- repository script (`scripts/prepush.ps1`) used by developers and CI parity checks.

## Reporting and Coverage

- Produce machine-readable test results and upload as artifacts.
- Generate coverage report in standard format plus human-readable summary.
- Enforce initial minimum coverage threshold (for example 80% line coverage), adjustable through ADR.
- PR status must show failing tests with enough detail to diagnose quickly.

## Security and Reliability

- Use least-privilege GitHub token permissions.
- Keep NuGet API key in GitHub Actions secrets only.
- Prevent publish from forks/untrusted contexts.
- Pin external action versions by commit SHA where practical.

## Implementation Autonomy

This workstream can be implemented independently after test and packaging standards are defined.

## Testing Requirements

- Validate workflow syntax and branch protection rules.
- Simulate failed tests and confirm release pipeline blocks publish.
- Simulate packaging failure and confirm CI fails.
- Validate artifact upload/download and report readability.

## Risks and Mitigations

- Risk: flaky pipelines reduce trust.
  - Mitigation: deterministic environment setup and test stabilization rules.
- Risk: release publishes unverified artifacts.
  - Mitigation: strict gate dependencies and tag/version verification.

## Maintainability Considerations

- Keep workflows modular and reusable.
- Centralize repeated command sequences in scripts.
- Review pipeline duration and optimize periodically.

## Exit Criteria

- CI workflow active and required by branch protection.
- Release workflow active and gated on CI.
- Pre-push command parity documented and operational.
