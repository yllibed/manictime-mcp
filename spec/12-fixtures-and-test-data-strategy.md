# WS-12 — Fixtures and Test Data Strategy

## Objectives

- Provide deterministic and maintainable test data for all layers.
- Avoid privacy/security risks from real personal activity datasets.
- Keep fixture generation reproducible across contributors and CI.

## Scope

- SQLite fixture datasets for `ManicTimeReports.db` compatible schemas.
- Screenshot fixture sets (full + thumbnail variants, malformed samples).
- Fixture generation scripts and validation checks.

## Non-Scope

- Production data migration.
- Long-term archival of real user datasets.

## Functional Requirements

- Provide synthetic fixture datasets covering:
  - baseline happy-path usage
  - empty windows
  - boundary timestamps
  - high-volume periods
  - schema drift cases (missing/renamed columns)
- Provide screenshot fixtures covering:
  - canonical filename patterns
  - thumbnail/full pairing
  - parser fallback scenarios
  - malformed names and extension/path security cases
- Provide fixture generation scripts that can recreate all shipped fixtures from source definitions.
- Provide fixture verification scripts that assert schema/query assumptions.

## Non-Functional Requirements

- Fixtures must be deterministic and fast to generate.
- Fixture artifacts stored in repo should remain small.
- No PII or customer-identifiable data in committed fixtures.

## Technical Design

### Fixture tiers

- Tier A: minimal synthetic datasets (committed to repo, default in tests).
- Tier B: stress synthetic datasets (generated on demand in CI/perf jobs).
- Tier C: optional locally sanitized real-world datasets (never committed by default).

### Data generation approach

- Use idempotent scripts under `scripts/testdata/`.
- Define datasets from declarative inputs (json/yaml/csv) where practical.
- Enforce invariant checks after generation.

### Privacy and compliance guardrails

- Default policy: do not commit real activity data.
- If local sanitized snapshots are used, require irreversible anonymization and explicit exclusion from source control.

## Implementation Autonomy

This workstream can be implemented independently and consumed by all testing-oriented workstreams.

## Testing Requirements

- Fixture generation tests (determinism and schema validity).
- Fixture integrity tests (row counts, key joins, time-range assumptions).
- Security tests for fixture file handling and path constraints.

## Risks and Mitigations

- Risk: tests pass only on unrealistic data.
  - Mitigation: multi-tier fixtures including stress and edge distributions.
- Risk: accidental inclusion of sensitive user data.
  - Mitigation: strict repo policy + CI scanner checks + exclusion rules.

## Maintainability Considerations

- Keep fixture generators versioned and reviewable.
- Avoid opaque binary fixture-only workflows without regeneration scripts.
- Document fixture provenance and intended scenario coverage.

## Exit Criteria

- Fixture tiers implemented and documented.
- Generation and validation scripts integrated in CI.
- Privacy guardrails enforced by policy and checks.
