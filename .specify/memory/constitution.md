# OrchardCore Development Constitution

## Purpose

This constitution defines the governing engineering standards for OrchardCore development in this repository. It establishes mandatory quality gates, recommended practices, and module-level annexes to preserve OrchardCore's architecture, maintainability, and long-term compatibility.

## Core Principles

### I. Modular And Multi-Tenant Integrity
All contributions must preserve OrchardCore's modular and multi-tenant architecture.
Designs must maintain tenant isolation and must not introduce tenant-crossing side effects unless explicitly designed, documented, and reviewed.

### II. Backward Compatibility As The Default Contract
Backward compatibility is the default for public APIs, recipes, content definitions, and extension points.
Any intentional break must be explicit, justified, documented, and accompanied by a migration path.

### III. Verifiable Behavioral Quality
Behavioral changes are complete only when verified by automated tests at the appropriate level.
Unverified behavior, even if logically correct, is not considered production-ready.

### IV. Documentation As A Shipped Artifact
Documentation is part of the deliverable, not an optional follow-up.
User-facing changes must be reflected in canonical documentation, with release notes treated as supplemental context.

### V. Simplicity, Clarity, And Operational Efficiency
Prefer the simplest solution that satisfies validated requirements while conforming to established OrchardCore and ASP.NET Core conventions.
Complexity requires explicit justification and measurable benefit.

## Mandatory Requirements

1. Architecture and tenancy:
- Changes must remain modular and tenant-safe.
- New behavior must use existing extension points where possible.

2. Compatibility and migration:
- Public contract changes must preserve backward compatibility by default.
- Any approved break must include migration guidance and compatibility notes.

3. Testing:
- Bug fixes, features, and user-visible behavior changes must include automated tests.
- Relevant tests must pass locally or in CI before merge.

4. Documentation:
- User-facing behavior, setup, configuration, and extension-point changes must update canonical docs under src/docs.
- Release notes cannot be the sole documentation source for current behavior.

5. Data evolution:
- Schema and content-definition changes must be implemented through versioned data migrations.
- Ad hoc or manual-only schema mutation is prohibited.

6. Security and reliability:
- Secret leakage, unsafe defaults, and unvalidated external input handling are prohibited.
- Risky behavior changes must include explicit operational considerations in the PR.

7. Build and tooling consistency:
- Target frameworks, SDK selection, and build conventions must follow repository sources of truth (for example global.json and shared build props/targets).

## Recommended Practices

1. Prefer file-scoped namespaces, explicit naming, appropriate DI lifetimes, and Async suffixes for asynchronous methods.
2. Keep user-facing strings localization-friendly and preserve accessibility expectations in admin and front-end experiences.
3. Use behavior-focused test naming such as {Action}_{Condition}_{ExpectedResult} for discoverability.
4. Assess runtime cost for startup, indexing, and query paths during design review, not only after merge.
5. Favor incremental, reviewable pull requests over large, mixed-scope changes.

## Delivery Workflow And Quality Gates

1. Define scope, expected behavior, and compatibility impact.
2. Implement with established patterns and extension points.
3. Add or update tests aligned to observable behavior.
4. Update canonical docs and API/XML docs where applicable.
5. Validate with appropriate build, test, and asset commands for affected areas.
6. Submit PR with rationale, risks, migration notes, and linked issue context.

Minimum merge readiness:
- Affected projects build successfully.
- Relevant automated tests pass.
- Required documentation updates are included.
- Compatibility and migration impact is clear in the PR.

## Module-Specific Annexes

### Annex A: Documentation And Content Guidance
- Canonical documentation under src/docs must reflect shipped behavior.
- Public API and extension-point changes should include accurate XML summaries and parameter documentation where relevant.
- Release notes summarize change history but do not replace canonical reference content.

### Annex B: Data Migration And Persistence Guidance
- Use DataMigration versioning (CreateAsync and UpdateFromX patterns) for evolving schemas and content definitions.
- Index-table and query-affecting changes must be accompanied by tests validating behavior and migration safety.
- Migration steps should be idempotent and safe across tenant upgrades.

### Annex C: Admin UI Editing Guidance
- Admin edit views should use established Orchard admin layout conventions (including ocat-* structures where applicable).
- New admin fields should prioritize consistency, validation clarity, and accessibility.
- Avoid introducing one-off layout patterns when existing conventions satisfy the need.

### Annex D: Testing Guidance
- Choose the narrowest test level that proves behavior, escalating to integration or functional coverage when contracts cross module boundaries.
- For bug fixes, include a regression test that fails before and passes after the fix.
- Keep test intent explicit and behavior-oriented to reduce maintenance ambiguity.

## Governance

This constitution supersedes ad hoc local development preferences when they conflict with repository-wide engineering policy.

Amendments require:
1. A documented proposal in a pull request.
2. Review by maintainers or designated code owners.
3. Migration guidance when workflow or compatibility expectations change.

Compliance in review:
1. Reviewers must verify mandatory requirements relevant to the change.
2. Any exception must be explicit, justified, and time-bounded.

**Version**: 1.1.0 | **Ratified**: 2026-09-06 | **Last Amended**: 2026-09-06
