# WS-13 — Legal, Branding, and Compatibility Positioning

## Objectives

- Reduce legal/branding risk when presenting compatibility with ManicTime.
- Define safe, consistent public messaging for docs, package metadata, and promotion.
- Ensure installation and compliance caveats are communicated clearly.

## Scope

- Trademark and compatibility claim wording guidelines.
- Public-facing disclaimers and attribution text.
- Promotion and communication guardrails.

## Non-Scope

- Formal legal advice.
- Contract negotiation with third parties.

## Functional Requirements

- All public materials must use explicit independent-positioning language, for example:
  - "Independent integration for ManicTime local data."
  - "Not affiliated with or endorsed by ManicTime/Finkit."
- Avoid claims implying certification, official partnership, or vendor endorsement unless written authorization exists.
- Use the ManicTime name only as necessary for nominative compatibility references.
- Do not use third-party logos/brand assets unless explicit permission is granted.
- Include a standard compatibility disclaimer in README, package metadata, and project website (if any).
- Include a short legal notice describing local data handling assumptions and user responsibility for policy compliance.

## Non-Functional Requirements

- Messaging must be concise, clear, and consistent across all channels.
- Legal-sensitive wording changes require review before publication.

## Technical Design

### Required documentation assets

- `docs/legal-and-branding.md` with approved phrasing templates.
- Reusable disclaimer snippets for README/NuGet/GitHub release notes.
- Release checklist item confirming disclaimer presence.

### Promotion guidance

- Prefer technical claims with measurable behavior (read-only, local processing, stdio transport).
- Avoid comparative or superlative claims that imply vendor endorsement.
- Keep screenshots/demos free of sensitive user data.

### Compliance review gate

- Add a pre-release checklist step for legal/brand wording review.
- If uncertainty exists, route to legal counsel before public launch.

## Implementation Autonomy

This workstream can be implemented independently as documentation + release process constraints.

## Testing Requirements

- Docs lint/check to ensure disclaimer appears in required files.
- Release checklist validation in CI (presence checks for required legal text blocks).

## Risks and Mitigations

- Risk: trademark misuse or implied endorsement.
  - Mitigation: strict wording templates and release gate checks.
- Risk: promotional content exposing sensitive data.
  - Mitigation: demo sanitization policy and review checklist.

## Maintainability Considerations

- Keep approved wording centralized and versioned.
- Minimize ad-hoc phrasing in scattered docs.
- Revisit wording after major product/brand changes.

## Exit Criteria

- Approved compatibility/disclaimer wording documented.
- Required legal text integrated in public docs templates.
- Release process includes legal/branding checkpoint.
