# Implementation Plan: Site Blueprint and Managed Site Content

**Branch**: `[001-shared-managed-site-content]` | **Date**: 2026-09-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from [spec.md](spec.md)

**Note**: This plan covers the Site Blueprint, Managed Site, Managed Site Admin Portal, URL synchronization, content composition, page override, layer contribution, placeholder, preview, and scoped permission design.

## Summary

Build a VendallionCMS-managed OrchardCore module family for a single OrchardCore tenant that acts as the Site Blueprint. The ManagedSites module registers Managed Sites and their URLs, assigns managed-site clearance through token claims/scopes, exposes a React-based Managed Site Admin Portal for scoped content editing and preview, and modifies request composition based on the incoming URL. Runtime composition resolves incoming requests to a Managed Site, combines Site Blueprint content with Managed Site content, keeps shell URL mappings synchronized, supports independent layer contributions, supports overrideable blueprint pages, and lets Managed Site administrators fill Site Blueprint placeholders without crossing managed-site scope boundaries.

## Technical Context

**Language/Version**: C# on .NET 10.0 for OrchardCore modules; TypeScript/React for the Managed Site Admin Portal assets.

**Primary Dependencies**: OrchardCore modular framework, OrchardCore Content Management, Autoroute, Layers, Navigation, OpenAPI/API authentication, Users/Roles/Permissions, OrchardCore cache infrastructure, existing OrchardCore preview behavior, React asset pipeline.

**Storage**: OrchardCore document/content storage within the blueprint tenant through versioned data migrations for new settings, content parts, indexes, and mapping documents.

**Testing**: xUnit unit/integration tests in OrchardCore test projects; browser/functional coverage for the Managed Site Admin Portal and preview flow where needed.

**Target Platform**: OrchardCore CMS web application running on .NET 10.0 in a single tenant that acts as the Site Blueprint; Managed Sites are URL-scoped content contexts, not separate OrchardCore tenants.

**Project Type**: Modular CMS extension plus authenticated React admin portal.

**Performance Goals**: Managed-site URL resolution and composition must not introduce visible page-render delay; published content updates should appear on matching requests immediately after OrchardCore cache invalidation completes according to configured cache management settings.

**Constraints**: Preserve the existing OrchardCore tenant boundary, respect OrchardCore routing and preview behavior, keep all admin mutations scoped to token-derived managed-site clearance, and preserve backward compatibility with existing content and routing behavior.

**Scale/Scope**: One Site Blueprint with multiple Managed Sites in the initial scope; multiple URL registrations, scoped content items, menu placeholders, layer contributions, and page overrides per Managed Site.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Gate Status**: PASS with tracked design obligations.

- **Modular and Multi-Tenant Integrity**: PASS. Design keeps behavior in VendallionCMS modules inside the active OrchardCore tenant and scopes runtime/editor actions to managed-site context without requiring separate tenants per Managed Site.
- **Backward Compatibility**: PASS. Existing OrchardCore routing, content lifecycle, preview, layers, navigation, and APIs remain the baseline; new behavior is additive.
- **Verifiable Behavioral Quality**: PASS. Plan requires unit/integration coverage for routing, authorization, composition, preview, and recoverability, plus portal flow validation.
- **Documentation as Shipped Artifact**: PASS. Canonical docs and module README updates are required before implementation completion.
- **Simplicity and Operational Efficiency**: PASS. Initial implementation uses one module family and existing OrchardCore extension points before introducing broader abstractions.
- **Data Evolution**: PASS. New schema/content-definition changes must use versioned data migrations.
- **Security and Reliability**: PASS. Token claims/scopes are the source of managed-site clearance and all privileged operations must re-check scope.

## Project Structure

### Documentation (this feature)

```text
specs/001-shared-managed-site-content/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/OrchardCore.Modules/VendallionCMS.ManagedSites/
├── Assets/
│   └── managed-site-admin/
├── Controllers/
├── Drivers/
├── Handlers/
├── Indexes/
├── Migrations/
├── Models/
├── Services/
├── Settings/
├── ViewModels/
├── Views/
├── wwwroot/
├── Assets.json
├── Manifest.cs
└── Startup.cs

test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/
├── Authorization/
├── Composition/
├── Preview/
├── Routing/
└── Navigation/

src/docs/reference/modules/ManagedSites/
└── README.md
```

**Structure Decision**: Implement a lean v1 as a single `VendallionCMS.ManagedSites` OrchardCore module with feature toggles for admin portal, routing, permissions, composition, preview, and customization support. Split into separate projects only if module activation or deployment boundaries require it later.

## Phase 0 Research Summary

See [research.md](research.md) for resolved technical decisions around the hybrid targeting model, Site Blueprint and Managed Site data ownership, authorization claims/scopes, shell URL synchronization, preview simulation, and React admin asset packaging.

## Phase 1 Design Summary

See [data-model.md](data-model.md) for entities, validation rules, and state transitions. See [contracts/managed-sites-api.md](contracts/managed-sites-api.md) for the API surface used by the Managed Site Admin Portal. See [quickstart.md](quickstart.md) for validation scenarios.

## Post-Design Constitution Check

**Gate Status**: PASS.

- Tenant safety is preserved by keeping all data inside the active blueprint tenant and applying managed-site scoped request resolution and authorization checks.
- Backward compatibility is preserved because existing OrchardCore content, routing, layers, navigation, and preview behavior are extended rather than replaced.
- Testability is covered by explicit unit, integration, and portal validation scenarios in [quickstart.md](quickstart.md).
- Documentation obligations are captured in the planned source structure and quickstart validation.
- Data evolution is constrained to migrations and recoverable state transitions.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
