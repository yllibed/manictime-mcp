# WS-06 — MCP Contract: Tools, Resources, and Prompts

## Objectives

- Define stable MCP-facing contracts.
- Keep output compact, structured, and model-friendly.
- Ensure behavior remains deterministic across clients.

## Scope

- Tool signatures, parameter rules, and output schemas.
- Resource URIs and payload contracts.
- Prompt contracts that orchestrate tool usage.

## Non-Scope

- Internal DB and filesystem implementation details.

## Functional Requirements

- Expose these tools:
  - `get_timelines`
  - `get_activities`
  - `get_computer_usage`
  - `get_tags`
  - `get_application_usage`
  - `get_document_usage`
  - `get_screenshots`
  - `get_daily_summary`
- Expose these resources:
  - `manictime://config`
  - `manictime://timelines`
  - `manictime://health`
- Keep date parameters ISO-8601.
- Apply hard caps server-side regardless of caller values.

## Non-Functional Requirements

- Contract responses should avoid unnecessary narration.
- Errors must be machine-actionable and human-readable.
- Structured output preferred over large unstructured text.
- Minimize token cost while maximizing information density.

## Technical Design

### Contract principles

- Explicit `outputSchema` for tools with structured payloads.
- Include truncation metadata when limits are applied.
- Keep optional fields nullable and documented.
- Version contracts semantically; avoid silent breaking changes.
- Include concise health/installation diagnostics in tool/resource outputs when relevant, so the model can report setup problems precisely.
- Prefer compact enums/codes plus short explanation fields over verbose free text.
- For screenshot-related calls returning no images, include a structured reason code and remediation hint so the model can guide the user (for example retention too short or capture disabled).

### Prompt principles

- Prompts should call summary tools first.
- Screenshots must be optional and bounded.
- Prompt text must not assume a specific model vendor behavior.

## Implementation Autonomy

This workstream can be delivered independently once method-level service interfaces are defined by WS-04/WS-05.

## Testing Requirements

- Tool schema contract tests.
- Parameter validation tests.
- Error contract tests (invalid dates, out-of-range limits).
- Resource contract tests.
- End-to-end MCP stdio tests for list/call/resource retrieval.

## Risks and Mitigations

- Risk: client incompatibility for optional MCP features.
  - Mitigation: prefer baseline-compatible contract patterns.
- Risk: oversized responses for large date windows.
  - Mitigation: mandatory caps + truncation signals.

## Maintainability Considerations

- Centralize tool metadata and parameter constraints.
- Keep contracts stable; use deprecation period for changes.
- Maintain example payloads for every tool/resource.

## Exit Criteria

- Tool/resource/prompt contracts implemented and documented.
- Schema validation tests passing.
- Contract examples published in README/docs.
