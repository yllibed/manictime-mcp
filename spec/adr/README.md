# ADR Index

Architecture Decision Records (ADRs) capture significant, durable decisions.

## Rules

- Use one ADR per decision.
- ADRs are immutable after acceptance; supersede with a new ADR.
- Any contract-breaking decision must have an ADR.

## Naming

- `NNNN-short-title.md` (for example `0001-stdio-only-transport.md`).

## Status Values

- Proposed
- Accepted
- Superseded
- Rejected

## Index

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-stdio-only-transport.md) | Stdio-Only Transport in v1 | Accepted |
| [0002](0002-no-persistent-self-attached-project-mcp.md) | No Persistent Self-Attached Project MCP In Workspace | Accepted |
| [0003](0003-screenshot-content-block-strategy.md) | Screenshot Content Block Strategy | Accepted |
| [0004](0004-image-crop-dependency.md) | Image Crop Dependency (SkiaSharp) | Accepted |
| [0005](0005-pre-aggregated-tables.md) | Pre-aggregated Tables as Primary Query Path | Accepted |

## Initial ADR Candidates

- ~~stdio-only transport for v1~~ → ADR-0001
- net10 + self-contained packaging profile
- centralized build/package/versioning policy
- ~~screenshot strategy and availability diagnostics~~ → ADR-0003, ADR-0004
- ~~no persistent self-attached project MCP in workspace~~ → ADR-0002
