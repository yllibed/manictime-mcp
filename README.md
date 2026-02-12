# ManicTime MCP

ManicTime MCP is a .NET MCP server that exposes local ManicTime activity data to MCP-compatible clients over stdio.

## Compatibility Notice

This project is an independent integration and is not affiliated with or endorsed by ManicTime or Finkit.

## Supported Product Scope

- Supported target: ManicTime Windows desktop installation using local storage (`ManicTimeReports.db` and local screenshot folders).
- Not supported in v1:
  - ManicTime Server deployments
  - non-Windows ManicTime client workflows
  - server-centric collector/logger scenarios on other platforms

## Getting Started

See `docs/getting-started.md` for prerequisites and build/test commands.
See `docs/ci-setup.md` for CI bootstrap and workflow baseline.

## Repository Structure

- `spec/` — workstream specifications and engineering requirements.
- `docs/` — usage and contributor-facing documentation.
- `src/` — solution, projects, and build configuration.

## Contributing

1. Read `spec/README.md`.
2. Pick a workstream.
3. Implement only that workstream scope and acceptance criteria.
4. See `AGENTS.md` for build commands, quality rules, and engineering constraints.

## Transport and Scope

- v1 transport: `stdio` only.
- v1 target: local desktop ManicTime DB/files only.
