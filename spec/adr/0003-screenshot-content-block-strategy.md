# ADR 0003 — Screenshot Delivery Strategy

- Status: Superseded by ADR-0008 and ADR-0009
- Date: 2026-02-16
- Deciders: Project maintainers
- Technical Story: WS-05, WS-06

## Historical Context

This ADR originally selected a native-MCP screenshot strategy centered on `ImageContentBlock`, `ResourceLinkBlock`, and audience annotations. That decision matched an earlier architecture built directly on the ModelContextProtocol SDK and assumed screenshot lazy resources such as `manictime://screenshot/{screenshotRef}` would remain a first-class contract surface.

The application has since moved to a **Repl-first** architecture exposed through `Repl.Mcp`. The active screenshot contract is now command-centric and text-first, not content-block-centric.

## Why this ADR is superseded

The current product behavior differs in several important ways:

- the public source of truth is the Repl command graph, not direct MCP SDK handlers
- screenshot discovery and retrieval are defined through `screenshot list`, `screenshot get`, `screenshot crop`, and `screenshot save`
- screenshot payloads are emitted in a transport-compatible structured representation through `Repl.Mcp`
- `manictime://screenshot/{screenshotRef}` is no longer an active v1 contract resource
- screenshot persistence relies on MCP roots, with `workspace init` available as the soft-roots fallback when native roots are unavailable

Because of those changes, the old `ImageContentBlock`-first decision is no longer the current contract and must not be treated as authoritative guidance.

## Current direction

The active direction is defined by:

- [0008-repl-first-command-graph-and-mcp-integration.md](D:\src\manictime-mcp\spec\adr\0008-repl-first-command-graph-and-mcp-integration.md)
- [0009-temporal-ranges-and-reusable-option-groups.md](D:\src\manictime-mcp\spec\adr\0009-temporal-ranges-and-reusable-option-groups.md)
- [05-screenshot-pipeline.md](D:\src\manictime-mcp\spec\05-screenshot-pipeline.md)
- [06-mcp-contract-tools-resources-prompts.md](D:\src\manictime-mcp\spec\06-mcp-contract-tools-resources-prompts.md)

## Retained historical value

The superseded ADR still captures useful historical reasoning:

- screenshot payload size should stay under tight control
- progressive resolution (`list` -> `get` -> `crop` -> `save`) remains the canonical workflow
- thumbnail-first retrieval is still preferred when available

Those principles remain valid even though the concrete transport contract changed.
