# WS-01 — System Overview and Boundaries

## Objectives

- Define the product boundary for v1.
- Eliminate ambiguous scope before implementation starts.
- Provide explicit support and non-support commitments.

## Scope

- Local desktop ManicTime data on Windows (`ManicTimeReports.db` and screenshot files) as the primary supported mode.
- Limited host compatibility on non-Windows runtimes via `any` packaging when `MANICTIME_DATA_DIR` is explicitly configured to an accessible ManicTime data directory (for example mounted Windows data under WSL).
- MCP server exposed over `stdio` only.
- Read-only activity analytics and screenshot retrieval.

## Non-Scope (v1)

- HTTP transport.
- ManicTime Server API integration.
- ManicTime Server deployment mode.
- Native non-Windows ManicTime client/server collection workflows and automatic OS-specific data discovery outside Windows.
- Data mutation, tagging updates, or write-back operations.
- Cross-machine sync and distributed deployment.

## Functional Requirements

- Expose activity and usage data via MCP tools.
- Expose compact resources for configuration and health.
- Return predictable structured outputs suitable for automated reasoning.
- Degrade gracefully if screenshots are unavailable.

## Non-Functional Requirements

- Startup and memory efficiency are critical.
- Strict read-only access to all ManicTime artifacts.
- ManicTime artifacts are high-value user data; exceptional caution is mandatory both during development and at runtime.
- Deterministic error handling and diagnostics.

## Technical Design Notes

- Transport is JSON-RPC over stdio.
- Process logs go to stderr only.
- MCP contracts are versioned through tool/resource schemas.
- JSON-RPC over stdio means protocol messages are exchanged through the process standard streams:
  - stdin: requests/notifications from client to server
  - stdout: responses/notifications from server to client
  - stderr: operational logs only (never protocol payload)

## Implementation Autonomy

This workstream can be implemented independently as a contract/spec baseline. It provides all boundary decisions required by other workstreams.

## Testing Requirements

- Scope tests: verify unsupported modes are rejected explicitly.
- Contract tests: verify only stdio registration is active.
- Documentation tests: lint for unsupported terms (for example, HTTP endpoints in v1 docs).

## Risks and Mitigations

- Risk: scope creep from API/transport expansion.
  - Mitigation: strict v1 non-scope list and ADR requirement.
- Risk: ambiguous client expectations.
  - Mitigation: explicit capability matrix in docs and package README.

## Maintainability Considerations

- Keep scope decisions centralized in this workstream doc.
- Require ADR for boundary changes.
- Avoid hidden feature toggles that bypass published scope.

## Exit Criteria

- Scope statement accepted by product/engineering.
- v1 non-scope items documented in all user-facing docs.
- Downstream workstreams reference this boundary without conflicts.
