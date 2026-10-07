# Tasks: Site Blueprint and Managed Site Content

**Input**: Design documents from `specs/001-shared-managed-site-content/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/managed-sites-api.md](contracts/managed-sites-api.md), [quickstart.md](quickstart.md)

**Tests**: Test tasks are included because the OrchardCore constitution requires automated verification for behavioral changes.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Revision 2026-09-14**: The four separate customization mechanisms (page overrides, navigation contributions, layer contributions, placeholder assignments) were replaced by a single Managed Content capability. Phases 6, 7, 9, and 10 of the previous plan collapsed into the two Managed Content phases below. Tasks T020, T021, and T027 were completed against the superseded design and are re-opened as T067 and T070.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other tasks in the same phase because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User story label from [spec.md](spec.md).
- Every task includes an exact target file or directory path.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the module, test, asset, and documentation structure needed by all stories.

- [X] T001 Create module directory structure in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/`
- [X] T002 Create test project directory structure in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/`
- [X] T003 Create module project file in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/VendallionCMS.ManagedSites.csproj`
- [X] T004 Create test project file in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/VendallionCMS.ManagedSites.Tests.csproj`
- [X] T005 Add module project to solution in `OrchardCore.slnx`
- [X] T006 Add test project to solution in `OrchardCore.slnx`
- [X] T007 Create module manifest with VendallionCMS feature IDs in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Manifest.cs`
- [X] T008 Create feature constants in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/ManagedSitesConstants.cs`
- [X] T009 Create module startup shell in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`
- [X] T010 Create admin asset manifest in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets.json`
- [X] T011 Create React admin asset package in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/package.json`
- [X] T012 Create React admin Vite configuration in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/vite.config.ts`
- [X] T013 Create module README stub in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/README.md`
- [X] T014 Create canonical docs directory in `src/docs/reference/modules/ManagedSites/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared domain models, migrations, indexes, interfaces, authorization, and API plumbing required by every user story.

- [X] T015 Create SiteBlueprint model in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/SiteBlueprint.cs`
- [X] T016 Create ManagedSite model in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedSite.cs`
- [X] T017 Create UrlRegistration model in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/UrlRegistration.cs`
- [X] T018 Create ManagedSiteClearance model in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedSiteClearance.cs`
- [X] T019 Create ActiveManagedSiteSessionScope model in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ActiveManagedSiteSessionScope.cs`
- [X] ~~T020 Create page customization models in `Models/PageCustomizationModels.cs`~~ **Superseded by the Managed Content design; removal tracked as T067.**
- [X] ~~T021 Create navigation customization models in `Models/NavigationCustomizationModels.cs`~~ **Superseded by the Managed Content design; removal tracked as T067.**
- [X] T022 Create document/index models in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Indexes/ManagedSitesIndexes.cs`
- [X] T023 Create initial data migration in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Migrations/ManagedSitesMigrations.cs`
- [X] T024 Create permission definitions in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Permissions.cs`
- [X] T025 Create managed-site scope authorization service interface in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/IManagedSiteAuthorizationService.cs`
- [X] T026 Create managed-site repository/service interfaces in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteServiceInterfaces.cs`
- [X] ~~T027 Create composition service interfaces in `Services/CompositionServiceInterfaces.cs`~~ **Superseded by the Managed Content design; rewrite tracked as T070.**
- [X] T028 Create API base controller with common authorization helpers in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitesApiControllerBase.cs`
- [X] T029 Register foundational services and permissions in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`
- [X] T030 Create shared test fixtures in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedSitesTestFixture.cs`
- [X] T031 Create test data builders in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedSitesTestData.cs`

**Checkpoint**: Module compiles with empty service implementations and can be enabled without changing runtime behavior.

---

## Phase 3: User Story 1 - Manage Site Blueprint Content (Priority: P1)

**Goal**: Site Blueprint administrators can mark the active site as a Site Blueprint and manage blueprint content while unauthorized users are blocked.

**Independent Test**: Assign blueprint management access to one user, update blueprint content, and confirm unauthorized users cannot modify it.

**Superseded 2026-09-17**: The explicit designation was removed. Enabling the Managed Sites feature is
now what makes a tenant the Site Blueprint, so the toggle, the name, the blueprint identifier, the
blueprint entity, and their settings screen no longer exist. The toggle had gated nothing: its
identifier was hardcoded, so both branches of every caller behaved identically. The
`ManageSiteBlueprint` permission survived here, because it governed who may manage common content and,
from User Story 5 onward, who may configure managed content scopes.

**Superseded 2026-10-03**: The permissions were reduced to two. `ManageManagedSites` now covers
everything that governs a Managed Site, which is defining it, deciding what content it may override, and
granting clearance to it; `EditManagedSiteContent` opens the Managed Site Admin Portal and is bounded
further by the holder's clearance. `ManageSiteBlueprint` and `ManageManagedSiteClearances` are gone,
their work folded into `ManageManagedSites`. The line between the two is what keeps a Managed Site
editor from widening their own reach, since all three ways of doing so sit on the governing side.

On a tenant set up before this change, re-check the roles. `ManageSiteBlueprint` and
`ManageManagedSiteClearances` no longer resolve to anything, and a role that held `ManageManagedSites`
only so its users could open the portal now governs Managed Sites as well, which is wider than was
intended for it. Such a role should be moved to `EditManagedSiteContent`.

### Tests for User Story 1

- [X] ~~T032 [US1] Add blueprint permission tests~~ **Removed with the designation.**
- [X] ~~T033 [US1] Add blueprint settings migration tests~~ **Removed with the designation.**

### Implementation for User Story 1

- [X] ~~T034 [US1] Implement Site Blueprint settings model~~ **Removed with the designation.**
- [X] ~~T035 [US1] Implement Site Blueprint settings driver~~ **Removed with the designation.**
- [X] ~~T036 [US1] Implement Site Blueprint settings editor view~~ **Removed with the designation.**
- [X] ~~T037 [US1] Implement Site Blueprint service~~ **Removed with the designation.**
- [X] T038 [US1] Register admin menu entries in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/AdminMenu.cs`
- [X] T039 [US1] Wire admin services in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`
- [X] T145 [US1] Remove the Site Blueprint designation, entity, service, and settings screen across `src/OrchardCore.Modules/VendallionCMS.ManagedSites/`
- [X] T146 [US1] Remove `BlueprintId` from `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedSite.cs` and `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Indexes/ManagedSitesIndexes.cs`

**Checkpoint**: A tenant with the feature enabled is the Site Blueprint, and common-content authorization is governed by the `ManageManagedSites` permission.

---

## Phase 4: User Story 2 - Define and Manage Managed Site Content By URL (Priority: P2)

**Goal**: Administrators can define Managed Sites, assign unique URLs, and manage scoped content only when authorized.

**Independent Test**: Create one Managed Site, register URLs, assign an editor, and verify only that editor can manage the site's content.

### Tests for User Story 2

- [X] T040 [P] [US2] Add Managed Site CRUD tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/ManagedSiteDefinitionTests.cs`
- [X] T041 [P] [US2] Add URL uniqueness tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/UrlRegistrationTests.cs`
- [X] T042 [P] [US2] Add scoped content authorization tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ManagedSiteContentAuthorizationTests.cs`

### Implementation for User Story 2

- [X] T043 [US2] Implement Managed Site service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteService.cs`
- [X] T044 [US2] Implement URL registration service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/UrlRegistrationService.cs`
- [X] T045 [US2] Implement Managed Site admin controller in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/AdminController.cs`
- [X] T046 [US2] Implement Managed Site admin view models in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/ViewModels/ManagedSiteViewModels.cs`
- [X] T047 [US2] Implement Managed Site admin views in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/Admin/` (the original `Views/ManagedSites/` matched no controller name, so MVC could not resolve it)
- [X] T048 [US2] Implement URL conflict validation in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/UrlRegistrationValidator.cs`
- [X] T049 [US2] Implement scoped content authorization service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteAuthorizationService.cs`

### Reopened and added for User Story 2 (2026-09-15)

T040, T045, T046, and T047 were ticked while only a read-only list was delivered: the admin controller
exposed a single `Index` action, `Views/ManagedSites/` held only `Index.cshtml`, `ManagedSiteEditViewModel`
was unreferenced dead code, and `ManagedSiteDefinitionTests` asserted only the default status. The
`PUT /api/managed-sites/{managedSiteId}` endpoint in the API contract had no task at all. Managed Sites
therefore could not be created, which blocks every later story.

- [X] T135 [US2] Support editing and removing definitions in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteService.cs`, including replacing a Managed Site's URL registrations and rejecting duplicates within one submission
- [X] T136 [US2] Implement the Managed Site definitions API endpoint from the contract in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSiteDefinitionsApiController.cs`
- [X] T137 [P] [US2] Add Managed Site definitions API contract tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Contracts/ManagedSiteDefinitionsApiContractTests.cs`
- [X] T138 [P] [US2] Add Managed Site create and edit functional coverage in `test/OrchardCore.Tests.Functional/Tests/VendallionCMS.ManagedSites/ManagedSiteDefinitionAdminTests.cs`
- [X] T139 [US2] Write the mutated settings document back before saving in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteService.cs`, `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/UrlRegistrationService.cs`, and `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/SiteSettingsManagedSiteSessionStore.cs`
- [X] T140 [US2] Give every persisted collection a setter so it deserializes in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/`
- [X] T141 [US2] Rename admin controllers and view folders to the OrchardCore convention: the module's primary admin surface is `AdminController` with `Views/Admin/`, and the portal is `PortalController` with `Views/Portal/`, matching `AdminController`/`LayerRuleController` in OrchardCore.Layers. Both admin URLs are unchanged.
- [X] ~~T142 [US2] Let a registration address a domain as well as a path~~ **Superseded 2026-09-17: a Managed Site is now addressed like a tenant, by Hostname and URL Prefix, so the per-registration address model is replaced. Re-opened as T147.**
- [X] T143 [US2] Synchronize registered domains into the shell request hosts on save and delete, withdrawing only previously applied hosts, in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ShellUrlSynchronizationService.cs`

### Tenant-style addressing (added 2026-09-17)

The spec now addresses a Managed Site the way OrchardCore addresses a tenant: one Hostname holding one
or more host names, plus one URL Prefix. The shipped code still stores a list of `UrlRegistration`
entries carrying a host and a path each, so **the implementation currently diverges from the spec** and
these tasks close that gap.

- [X] T147 [US2] Replace the registration list with `Hostname` and `UrlPrefix` on `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedSite.cs`, and delete `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/UrlRegistration.cs`
- [X] T148 [US2] Expand a Managed Site to one address per host name and decide collisions on the expanded set in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/`, preferring a host-specific Managed Site over a host-agnostic one
- [X] T149 [US2] Source tenant Hostname synchronization from Managed Site host names in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ShellUrlSynchronizationService.cs`, keeping the never-empty guard, which now applies only while the tenant URL Prefix is also empty
- [X] T150 [US2] Replace the single URL textarea with Hostname and URL Prefix fields in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/Admin/Edit.cshtml` and the definitions API request in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSiteDefinitionsApiController.cs`
- [X] T151 [P] [US2] Rewrite addressing tests for the tenant-style model in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/`
- [X] ~~T152 [US2] Migrate stored Managed Sites from registration lists to Hostname and URL Prefix~~ **Not needed: the feature is unreleased, so any Managed Site stored in the old shape is recreated rather than migrated.**
- [X] ~~T144 [P] [US2] Add domain addressing tests~~ **Superseded 2026-09-17 with T142. Re-opened as T151.**

**Checkpoint**: Managed Site definitions, URL registrations, and scoped editor access are functional and tested.

---

## Phase 5: User Story 4 - Managed Site Admin Portal Session (Priority: P2)

**Goal**: Editors use the Managed Site Admin Portal to authenticate, select an active Managed Site, and perform scoped actions.

**Superseded 2026-10-04**: The portal was a React single-page application backed by an HTTP API, and the
API existed only because the application needed one: nothing but the browser called it. Both are
replaced by an OrchardCore admin surface rendered on the server, tracked as T130 to T136 in Phase 10.
The services behind the portal are unchanged, and so is every rule the API enforced, apart from the
optional scope header, which had no meaning outside a client application.

**Independent Test**: Sign in as a multi-site user, select an active Managed Site, edit managed-site content, preview a page, and verify all actions remain scoped.

### Tests for User Story 4

- [X] T050 [P] [US4] Add authorized Managed Sites API tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ManagedSitePortalAuthorizationTests.cs`
- [X] T051 [P] [US4] Add active scope selection API tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ActiveManagedSiteSessionTests.cs`
- [X] T052 [P] [US4] Add portal UI scope selection tests in `test/OrchardCore.Tests.Functional/Tests/VendallionCMS.ManagedSites/ManagedSiteAdminPortalTests.cs`
- [X] T053 [P] [US4] Add admin API scope mismatch tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ManagedSiteApiScopeMismatchTests.cs`

### Implementation for User Story 4

- [X] T054 [US4] Implement authorized Managed Sites API endpoint in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitesApiController.cs`
- [X] T055 [US4] Implement active Managed Site session endpoint in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSiteSessionApiController.cs`
- [X] T056 [US4] Implement token claims/scope extraction in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteClearanceService.cs`
- [X] T057 [US4] Implement route/session/token/header scope consistency checks in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitesApiControllerBase.cs`
- [X] T058 [US4] Implement React portal entry point in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/main.tsx`
- [X] T059 [US4] Implement portal API client in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/services/managedSitesApi.ts`
- [X] T060 [US4] Implement active site selector UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/components/ManagedSiteSelector.tsx`
- [X] T061 [US4] Implement scoped editor shell UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/ManagedSiteAdminShell.tsx`
- [X] T062 [US4] Add portal host view in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/Portal/Index.cshtml`

### Clearance issuance for User Story 4 (added 2026-09-14)

Nothing issued Managed Site clearance when Phase 5 was first planned, so the portal could only ever
reach its no-clearance state. These tasks close that gap: an administrator grants clearance on the
user entity, and the platform claims principal factory carries it into both the admin cookie and the
issued access token.

- [X] T130 [US4] Implement Managed Site clearance user settings in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedSiteClearanceSettings.cs`
- [X] T131 [US4] Implement the clearance claims provider in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteClaimsProvider.cs`
- [X] T132 [US4] Implement the clearance editor in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Drivers/ManagedSiteClearanceDisplayDriver.cs` and `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/ManagedSiteClearance.Edit.cshtml`
- [X] T133 [US4] Add the clearance permission and register the editor and claims provider in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Permissions.cs` and `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`
- [X] T134 [P] [US4] Add clearance claim generation tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ManagedSiteClaimsProviderTests.cs`

**Checkpoint**: Portal login, authorized-site listing, auto-selection, multi-site selection, and scoped API access work end to end.

---

## Phase 6: User Story 5 - Scope Blueprint Content With Managed Content (Priority: P2)

**Goal**: Site Blueprint administrators attach the Managed Content part to any content type and configure per-item edit scope and display scope in the standard admin UI.

**Independent Test**: Attach the part to a content type, confirm rendering is unchanged, set both scopes on one item, and verify only named managed sites may edit it and only named contexts render it.

### Tests for User Story 5

- [X] T063 [P] [US5] Add attach-is-inert default tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/ManagedContentPartDefaultsTests.cs`
- [X] T064 [P] [US5] Add edit scope evaluation tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/EditScopeTests.cs`
- [X] T065 [P] [US5] Add display scope evaluation tests including blueprint context in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/DisplayScopeTests.cs`
- [X] T066 [P] [US5] Add scope configuration authorization tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ManagedContentScopeAuthorizationTests.cs`
- [X] T066a [P] [US5] Add display-covers-edit coherence tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/ManagedContentScopeCoherenceTests.cs`

### Implementation for User Story 5

- [X] T067 [US5] Remove superseded customization models in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/PageCustomizationModels.cs` and `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/NavigationCustomizationModels.cs`
- [X] T068 [US5] Implement ManagedContentPart in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedContentPart.cs`
- [X] T069 [US5] Implement edit and display scope value types in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedContentScope.cs`
- [X] T070 [US5] Rewrite composition service interfaces for per-item resolution in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/CompositionServiceInterfaces.cs`
- [X] T071 [US5] Implement managed content scope service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentScopeService.cs`
- [X] T072 [US5] Implement part editor display driver in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Drivers/ManagedContentPartDisplayDriver.cs`
- [X] T073 [US5] Implement part editor view in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/ManagedContentPart.Edit.cshtml`
- [X] T074 [US5] Implement scope configuration authorization handler in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentScopeAuthorizationHandler.cs`
- [X] T075 [US5] Add managed content indexes for edit and display scope queries in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Indexes/ManagedContentIndexes.cs`
- [X] T076 [US5] Add migration registering the part and its indexes in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Migrations/ManagedSitesMigrations.cs`
- [X] T077 [US5] Register managed content part and scope services in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`
- [X] T077a [US5] Enforce FR-028a so the display scope covers the edit scope, fixed in the editor and reapplied on persist, and activate each per-site column only while its own scope mode is Selected, in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedContentPart.cs`, `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/ManagedContentPart.Edit.cshtml`, and `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Drivers/ManagedContentPartDisplayDriver.cs`

**Checkpoint**: The part attaches to any content type, both scopes are configurable by blueprint administrators only, and attaching the part changes nothing visitors see.

---

## Phase 7: User Story 6 - Override Managed Content Per Managed Site (Priority: P2)

**Goal**: Managed-site administrators discover every item they may customize, publish their own version, and have it served only for their managed site.

**Independent Test**: List editable items in the portal, override one for one managed site, and confirm only that managed site receives the override while other contexts receive the original.

### Tests for User Story 6

- [X] T078 [P] [US6] Add override authorization tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ManagedContentOverrideAuthorizationTests.cs`
- [X] T079 [P] [US6] Add override rendering isolation tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/OverrideRenderingTests.cs`
- [X] T080 [P] [US6] Add display-scope-beats-override tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/DisplayScopePrecedenceTests.cs`
- [X] T081 [P] [US6] Add container override child resolution tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/ContainerOverrideTests.cs`
- [X] T082 [P] [US6] Add suppression reason tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/OverrideSuppressionTests.cs`
- [X] T083 [P] [US6] Add override recovery tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/OverrideRecoveryTests.cs`
- [X] T084 [P] [US6] Add managed content API contract tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Contracts/ManagedContentApiContractTests.cs`
- [X] T084a [P] [US6] Add scoped content authorization tests proving managed-site clearance authorizes its own override content and nothing else in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ManagedSiteContentPermissionTests.cs`

### Implementation for User Story 6

- [X] T085 [US6] Implement ManagedContentOverride model and suppression reason in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedContentOverride.cs`
- [X] T086 [US6] Implement override service with one-published-per-site enforcement in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentOverrideService.cs`
- [X] T087 [US6] Implement suppression evaluation service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentSuppressionService.cs`
- [X] T088 [US6] Implement render-time override resolution service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentResolutionService.cs`
- [X] T089 [US6] Implement the render path serving override or original in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentItemDisplayManager.cs` **Moved from the part display driver: a part driver contributes its own shapes and cannot withdraw its siblings', so the swap wraps `IContentItemDisplayManager`, the one place a content item becomes a shape.**
- [X] T090 [US6] Implement managed content discovery and override API controller in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedContentApiController.cs`
- [X] T091 [US6] Implement managed content API view models in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/ViewModels/ManagedContentApiModels.cs`
- [X] T092 [US6] Add migration for override storage and indexes in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Migrations/ManagedSitesMigrations.cs`
- [X] T093 [US6] Implement portal editable content list page in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/ManagedContentListPage.tsx`
- [X] T094 [US6] Implement portal override editor page in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/ManagedContentOverridePage.tsx`
- [X] T095 [US6] Implement portal suppressed override recovery page in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/SuppressedOverridesPage.tsx`
- [X] T096 [US6] Add managed content API client methods in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/services/managedSitesApi.ts`
- [X] T097 [US6] Register override and resolution services in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`
- [X] T097a [US6] Implement FR-011a content authorization handler granting managed-site clearance authority over its own override content only in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteContentAuthorizationHandler.cs`
- [X] T097b [US6] Implement scoped override content creation so an editor authors an override without tenant-wide content permissions in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedContentApiController.cs`
- [X] T097c [US6] Make override resolution deterministic when duplicates exist per FR-035a in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentOverrideService.cs`
- [X] T097d [US6] Address content items contained in another item, so page sections are discovered, overridden, and evaluated, in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentContainment.cs` and `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Indexes/ManagedContentIndexes.cs`
- [X] T097e [US6] Reduce the Managed Site lifecycle to Enabled and Disabled in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedSite.cs`
- [X] T097f [US6] Register the resource management tag helpers so the portal host page loads its bundle, in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/_ViewImports.cshtml`
- [X] T097g [US6] Tell a caller holding clearance for no enabled Managed Site apart from one holding none, in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSiteSessionApiController.cs`

**Checkpoint**: Overrides are discoverable, scoped, suppressible with a recorded reason, recoverable, and render only for their owning managed site.

---

## Phase 8: User Story 3 - Request Content Composition (Priority: P3)

**Goal**: Incoming requests resolve to a Managed Site by URL and every managed content item resolves to that site's override or to the original content.

**Independent Test**: Prepare blueprint content and overrides for registered URLs, request each URL, and confirm the composed response uses the correct content per item.

### Tests for User Story 3

- [X] T098 [P] [US3] Add URL resolver tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/ManagedSiteUrlResolverTests.cs`
- [X] T099 [P] [US3] Add composition resolution tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/ManagedSiteCompositionServiceTests.cs`
- [X] T100 [P] [US3] Add shell URL synchronization tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/ShellUrlSynchronizationTests.cs` (delivered early with US2)
- [X] T101 [P] [US3] Add public request scope metadata distrust tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/PublicManagedSiteScopeMetadataTests.cs`
- [X] T102 [P] [US3] Add composition cache invalidation tests for published content changes in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/CompositionCacheInvalidationTests.cs`
- [X] T103 [P] [US3] Add cache dependency tests for URL, override, and scope changes in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/CompositionCacheDependencyTests.cs`

### Implementation for User Story 3

- [X] T104 [US3] Implement Managed Site URL resolver in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteUrlResolver.cs`
- [X] T105 [US3] Implement request composition context accessor in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentResolutionService.cs` (delivered early with US6; the render path needs somewhere to read the resolved Managed Site from, and the middleware that fills it is T106)
- [X] T106 [US3] Implement request pipeline middleware in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteRequestMiddleware.cs`
- [X] T107 [US3] Ensure request pipeline middleware derives Managed Site context from URL resolution and ignores client Managed Site headers in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteRequestMiddleware.cs`
- [X] T108 [US3] Implement shell URL synchronization service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ShellUrlSynchronizationService.cs` (delivered early with US2; hosts only, see research.md)
- [X] T109 [US3] Implement composition cache state service using OrchardCore cache infrastructure in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteCompositionCacheService.cs`
- [X] T110 [US3] Implement content, override, and scope change invalidation handler in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Handlers/ManagedSiteCompositionInvalidationHandler.cs`
- [X] T111 [US3] Register middleware, synchronization, cache, and invalidation services in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`

**Checkpoint**: URL-based Managed Site resolution and composed request behavior work without treating Managed Sites as separate OrchardCore tenants.

---

## Phase 9: Preview, Integration, and Cross-Story Verification

**Purpose**: Connect preview behavior, public API contracts, and end-to-end validation across all user stories.

- [X] T112 [P] Add preview composition tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Preview/ManagedSitePreviewTests.cs`
- [X] T113 [P] Add composed rendering integration tests, covering a request arriving at a URL and the contained content it receives, in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/ManagedSiteCompositionIntegrationTests.cs`
- [X] T113a [P] Add service registration tests that reject a dependency cycle among the module's services, and a content handler that reaches the content manager through its constructor, in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Registration/TenantServiceRegistrationTests.cs`
- [X] T114 [P] Add backward-compatibility tests proving types without the part render unchanged in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/UnattachedContentRegressionTests.cs`
- [X] T115 Implement preview service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSitePreviewService.cs`
- [X] T116 Implement preview API endpoint in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitePreviewApiController.cs`
- [X] T117 Implement portal preview UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/PreviewPage.tsx`
- [X] T118 Ensure all API routes match `specs/001-shared-managed-site-content/contracts/managed-sites-api.md` in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/`
- [X] T119 Validate quickstart scenarios in `specs/001-shared-managed-site-content/quickstart.md` **Walked in a browser against an isolated tenant. Scenarios 1, 2 and 5 all pass as of 2026-10-04. Seven defects were found and fixed along the way; see the walkthrough record below.**


### T119 walkthrough record (2026-10-02)

Walked with Playwright against a throwaway tenant (separate `App_Data`, port 5119, Agency recipe) so the
developer's own tenant was untouched. Two Managed Sites were created as the scenario asks, one naming a
host and one naming only a prefix, plus a third to check that a host-named and a host-agnostic Managed
Site can coexist.

**Scenario 1 — URL resolution: passes.** Each address resolves to its owner; a host-named Managed Site
answers every path on its host and its prefix is not consulted; a prefix-named one answers under its
prefix on every other host with the prefix moved onto the path base, so content underneath resolves and
generated links carry the prefix back; unclaimed addresses fall through to the Site Blueprint; a second
Managed Site on a taken host is rejected whatever its prefix.

**Scenario 2 — portal scope selection: passes.** A user cleared for one Managed Site is scoped to it
automatically; one cleared for two must choose, and a scoped call before choosing is refused with 409;
a Managed Site the user holds no clearance for is neither listed nor reachable, and asking for its
content directly is refused with 403.

**Scenario 5 — content types beyond pages: partly passes.** A layer widget and a section inside a page
are both overridable, and each Managed Site sees only its own override. Menus are not composed: see the
open defect below.

**Fixed while walking:**

1. *A Managed Site addressed by URL prefix never rendered.* The middleware that resolves the Managed Site
   was added from the module's `Configure`, which OrchardCore runs after `UseRouting`, so moving the
   prefix onto the path base came after the endpoint had been chosen from the original path: the Managed
   Site matched and then served a 404. It is now added through an `IStartupFilter`, which runs ahead of
   routing, the same position OrchardCore rebases a tenant's own prefix from. Covered by
   `Routing/ManagedSitePipelinePositionTests.cs`.
2. *A clearance granting edit but not view listed nothing.* `edit`, `publish` and `preview` now imply
   `view`. Covered by `Authorization/ManagedSiteClearanceScopeTests.cs`.
3. *An override of an item carrying an alias, a route, or a layer membership.* The copy inherited parts
   that claim a place of its own: an alias or route made the override impossible to publish at all, and a
   layer membership had one Managed Site's widget drawn on every other Managed Site and on the Site
   Blueprint. Those parts are now dropped from the copy. Covered by
   `ManagedContent/OverrideIdentityTests.cs`.

**Preview walked 2026-10-04 — works on a prefix-addressed Managed Site, cannot work on a host-named one.**
Scenario 6 was covered by tests rather than walked, so it was walked. What holds: a draft override is
invisible to published rendering; an anonymous caller who puts the drafts flag in the address gets
nothing extra, so asking is still not being granted; the link the portal builds is the Managed Site's
own address, composed by the pipeline that serves the site; and on a Managed Site addressed by URL
prefix a cleared editor sees their draft while everyone else sees published content.

What does not hold is draft preview on a Managed Site that names a host. The editor signs in on the
admin's host and the preview link opens the Managed Site's host, so the authentication cookie, which is
scoped to the host that issued it, does not travel. The cleared editor arrives anonymous and is served
published content, silently. Confirmed by tracing the decision: `requested=true authenticated=False
clearance=False`, against the same account that works through a prefix.

This is the cost of composing preview at the Managed Site's own address, which the specification asks
for deliberately and which is right for fidelity. Three ways out, none taken: carry the grant in the
link as a short-lived signed token bound to the Managed Site, the user and an expiry, which is what
"signed clearance" elsewhere in this specification already implies and which needs no cookie; widen the
authentication cookie to a parent domain, which only works where the hosts share one and gives the
cookie a longer reach than it has now; or state in the documentation that previewing drafts on a
host-named Managed Site means signing in on that host first.

Separately: the built link carries no port, as `//alpha.localhost/?managed-site-drafts=1`. Correct on 80
and 443, wrong anywhere else, which is every development machine.

**Found in use, 2026-10-03 — one Managed Site was shown another's content.** Signing in to one Managed
Site and opening the portal's content list showed an item whose content belonged to a different Managed
Site's override, under an ordinary-looking name. An override of a container is a copy of that container,
so it carries copies of its children, and each copy keeps both the scopes the blueprint gave it and the
identifier of the item it was copied from. The edit scope index indexed those copies, so an item could
be described by two rows, one naming the blueprint container and one naming somebody's override, and the
listing keeps one row per identifier: whichever came first decided which container was opened. Fixed in
three places. `ManagedContentEditScopeIndexProvider` no longer indexes an override or anything inside
one, which is covered by `ManagedContent/EditScopeIndexTests.cs`. The portal listing and the Managed
Content locator both now refuse a row whose container is an override, because rows written before this
stay until the override they describe is saved again.

**Resolved 2026-10-03 — menu entries are composed; menu containers are not.** The cause was not the menu
at all. A content item keeps every part it has been asked for, and the platform asks for all of them
while loading, to hand them to the part handlers, which happens before any content handler sees the
loaded item. The load-time swap edits the stored JSON, so it was invisible to anything reading a part:
the menu went on handing out the Site Blueprint's entries however the JSON beneath them had been
rewritten. Page sections hid this, because they are drawn through the display manager, where a separate
decorator substitutes them; only consumers reading parts straight off the item were affected, and menus
are the main one. `ManagedContentCompositionHandler` now drops the parts a substitution changed
underneath, so they are read again, which is covered by `Composition/SubstitutedPartRefreshTests.cs`.
Verified in a browser: a Managed Site renders its own version of a menu entry while the Site Blueprint
and every other Managed Site render the original.

**Resolved 2026-10-04 — an item stored in its own right is substituted too.** Load-time substitution
replaced the items stored inside the item being loaded but never the item being loaded, so an override
of a menu, a widget or a whole page was listed, published and reported as rendering while the Site
Blueprint's version went on being served. `ManagedContentCompositionHandler` now gives the loaded item
the Managed Site's content when one exists, and then visits nothing inside it, because what it holds is
that Managed Site's own content rather than a set of further items for it to override.

Identity is deliberately left alone: the database key, the content item identifier and the type stay the
ones the request resolved, so routing, caching and invalidation go on keying as before; what changes is
the content, and the display text, which templates show and which is not part of the content. Each
property is removed rather than overwritten, which also drops the copy of that part the platform kept
while loading. Covered by `Composition/TopLevelSubstitutionTests.cs`.

Checked against the risk this carries, which is that loading a content item on a public request now
means something different. Only two content items load on such a request, the page and the menu, so the
reach is small and known. Identity is unchanged. Handlers run once per item per request, so the swap is
not repeated. The Site Blueprint survived repeated substituted requests and a restart, so nothing is
written back. The admin still shows Site Blueprint content and still offers the scopes, because no
Managed Site is resolved for an admin or API request. Draft gating is unchanged, since the same
clearance decides it.

One consequence worth stating in the documentation Phase 10 covers: overriding a container replaces
everything inside it, so a Managed Site that overrides a page stops seeing its own overrides of that
page's sections until the page override is removed. That is the rule scenario 5 asks for, and it was
verified both ways round, but it will surprise an editor who meets it by accident.

**Earlier finding — menus are not composed.** A `Menu` override and an override of a `LinkMenuItem` inside a
menu are both listed by the portal, publish successfully, and report `Published` with no suppression, yet
the rendered navigation shows the Site Blueprint's entries on the Managed Site. The control holds: a
change to the blueprint menu itself renders immediately, so the theme does render the menu content item,
and a section inside a page composes correctly on the same request, so load-time composition works. The
cause is specific to the menu path and is not a draft/published mismatch (republishing the menu changes
nothing). Until this is resolved, scenario 5's first expected outcome, that one mechanism covers
navigation entries as well as layer widgets and page content, does not hold for navigation entries.

**Noted, not defects:**

- A tenant whose hostname is empty answers on every host. Creating the first host-named Managed Site
  narrows it to that host, which is the documented model, but it also takes the Site Blueprint's own
  address and its admin offline until an operator declares that host on the tenant alongside it. Worth
  saying plainly in the documentation Phase 10 covers.
- A managed-site administrator can create and publish an override and edit the override item in the
  content editor, but cannot use the Menu module's nested entry editor, which has its own permission.
- The portal reports `isContainer: false` for a `Menu`, which does contain items.

---

## Review against the specification (T128, 2026-10-07)

Fifty-nine functional requirements, read one at a time against the code and the tests that hold it.
Fifty-seven are met. What follows is the rest, and one thing that was met but untested.

### FR-049 is not met for a managed site that names a host

> Preview MUST show the composed output for the active managed site, including draft overrides visible
> to the current user.

It does, on a managed site addressed by URL prefix. On one that names a host it shows published content
instead, because a sign-in reaches only the host that issued it and the preview link opens another one,
so the editor arrives as nobody. This was settled on 2026-10-06 as a limitation to state rather than
engineer away: carrying the grant in the link would make the link a credential, and the same link shown
to somebody else would then show them unpublished work, which is what Scenario 6 forbids. The portal
now says so before the editor follows the link.

The requirement is therefore knowingly unmet rather than overlooked, and the choice is between amending
it to say which addressing it holds for, and building the handoff that would make it true everywhere: a
one-shot token in the link that the managed site's host exchanges for a short, host-scoped preview
cookie. That is the only option considered that satisfies the requirement without contradicting
Scenario 6.

### FR-036 is met only where the content type has a draft state

> Overrides MUST use the existing content draft and publish lifecycle so each managed site can hold
> unpublished work without affecting what is served.

Overrides do use that lifecycle, exactly and without a parallel one, which is the point of the
requirement. But a content type that is not draftable has no unpublished state to use, so saving a
version of such an item publishes it: the managed site cannot hold work back, and the portal's publish
step never appears. That follows from the content type rather than from this feature, and the feature
has nothing to add to a lifecycle that does not exist. Worth saying in the documentation rather than
leaving an editor to discover that one kind of item behaves unlike the rest.

### FR-035a was implemented and untested

> If more than one published override nonetheless exists for a managed site and source content item,
> because content was imported or deployed, the system MUST serve one deterministically and report the
> others.

The rule was there and nothing exercised it, which is the worst place for a rule to be: it exists for
content that arrives without passing through the refusal that would have prevented it, so it runs only
when something has already gone wrong, and that is not when anybody wants to find out it was never
tried. The choice is now `ManagedContentOverrideService.ChooseServed`, covered by
`ManagedContent/DuplicateOverrideTests.cs`, including the case the rule exists for: whatever order the
store returns the rows in, the same override is served.

### Everything else

The remaining requirements are met and evidenced. Addressing, precedence and the tenant hostname
(FR-004 to FR-010a, FR-051) are covered by the routing tests and were walked in a browser. The
permission model and clearance (FR-002, FR-005, FR-011, FR-011a, FR-017 to FR-020, FR-023, FR-030) are
covered by the authorization and portal tests, and the line the two permissions draw was walked with an
editor account holding one of them. Managed Content and its scopes (FR-024 to FR-029) are covered by the
managed content tests. Rendering and composition (FR-008, FR-009, FR-031, FR-037 to FR-042) are covered
by the composition tests and were watched on three managed sites at once. Recovery (FR-043 to FR-047)
is covered by the recovery and suppression tests. Cache invalidation (FR-012, FR-050) is covered by the
composition cache tests.

---

## Usability walkthrough (T127a, 2026-10-06)

Walked against the development site, after the portal became an admin surface, so nothing measured
against the single-page application carries over.

A note on what this is. The times below are mechanical: a browser doing the steps, with no reading,
deciding or typing in them. They are a floor, not a measurement of a person, and the criteria ask about
people. What the walk measures honestly is the shape of each path, the steps it takes and whether
anything in it stops or misleads somebody, and that is what the numbers are reported beside.

| Criterion | Target | Steps | Mechanical |
| --- | --- | --- | --- |
| SC-005, change a managed site's URL mapping | under 3 minutes | 7 | 2.7s |
| SC-009, sign in and select an active managed site | under 60 seconds | 6 | 2.8s |
| SC-020, find an item and publish a version of it | under 3 minutes | 10 | 4.1s |

Three of those steps are signing in, which every path shares. What is left is four steps to change an
address, three to choose a managed site, and seven to find an item and put a version of it on one
site. At that shape, the targets have room for a person to read and think between every step and still
be met several times over. None of the three paths has a step where the next move is unclear, and the
portal lists what a managed site may customize on the screen it opens on, so SC-020's "locate" costs
nothing beyond reading a list.

**Changed by the walk.** The link from a version to the content editor carried no way back, so an
editor saved their work and was left in a screen with no route to the one they came from: the portal is
the whole of their job, and the content editor is the platform's. It carries a return now, which is
what makes SC-020 ten steps rather than eleven and a guess.

**Noted, not changed.** For a content type with no draft state, saving a version publishes it, so the
portal's publish step never appears and the version is served as soon as it is saved. That follows from
the content type rather than from this feature, but it means SC-020 has one step fewer for some types
and an editor has no way to hold such a version back.

---

## Phase 10: Polish and Cross-Cutting Concerns

**Purpose**: Documentation, accessibility, localization, build validation, and final quality checks.

- [X] T120 [P] Add localization strings for the admin and portal views in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/` **The views already localized. What did not were the messages a user most often meets: a Managed Site refused for its name or address reached the screen as the developer's description of the refusal, because the service raised a string and the controller showed it. The code is the contract and the wording is the screen's, so `AdminController` says it. The recipe steps localize what they report too.**
- [X] T121 [P] Add accessibility checks for the portal's scope selection, content list, and override screens **Audited with axe against WCAG 2.1 AA on all four screens. Three findings: a link told apart from its sentence only by colour, muted text and a link on a tinted panel short of the contrast minimum, and row actions that read identically out of context, so a reader moving between links heard a list of the same label. All fixed, and the screens are clean. One violation remains on every screen and is not this module's: the user menu in `OrchardCore.Users` has a button with no discernible text.**
- [X] T122 [P] Update module README in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/README.md`
- [X] T123 [P] Write canonical documentation covering the Managed Content part, both scopes, and override recovery in `src/docs/reference/modules/ManagedSites/README.md` **Also listed in `mkdocs.yml`, which it was not before, so the page is reachable.**
- [X] T124 Update feature manifest descriptions after implementation in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Manifest.cs`
- [X] T124a Add a `VendallionCMS.ManagedSitesTheme` theme in `src/OrchardCore.Themes/VendallionCMS.ManagedSitesTheme/`, modelled on `TheAgencyTheme`, whose setup recipe stands up a Managed Sites development site in one step **Referenced from `OrchardCore.Cms.Web` rather than from `OrchardCore.Application.Cms.Targets`, where the built-in themes are referenced, so that no OrchardCore library is changed. Verified by running setup with it: the site serves on each prefix, each editor signs in, and each portal lists exactly what its scopes allow.**
- [X] T124b Add a Managed Sites recipe step in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Recipes/ManagedSitesStep.cs` so a recipe can declare Managed Sites, and a clearance step so it can grant users access to them **The clearance step also creates the user when one does not exist, because the platform's `Users` step takes a password hash rather than a password and so cannot produce an account anybody can sign in to. A password is honoured only on creation; an existing user's is never changed.**
- [X] T124c Add recipe round-trip tests for both steps in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Recipes/ManagedSitesRecipeStepTests.cs`
- [X] T124d Document the development site in `src/docs/reference/modules/ManagedSites/README.md`: what the recipe provisions, who to sign in as, and what to try first


### The development site (T124a–T124d)

**Why**: investigating this feature currently means an hour of clicking. Everything the T119 walkthrough
needed, the content types, the scopes, the Managed Sites, the roles, the users and their clearances, has
to be assembled by hand before a single override can be tried, and getting one step wrong produces
behaviour that looks like a defect. A setup recipe makes a known-good site a one-step job, which is as
useful for a developer reading the code as for anyone reproducing a report.

**What the recipe provisions**:

- The four Managed Sites features, alongside what the Agency recipe already enables.
- A `Managed Site Content Editor` role holding `EditManagedSiteContent` and `AccessAdminPanel`, which is
  the pair a Managed Site editor needs and nothing more. Notably not `ManageManagedSites`, so the site
  demonstrates the line the permissions draw rather than blurring it.
- Managed Content attached to the `Service`, `Page` and `LinkMenuItem` content types, so the three kinds
  of content that compose differently are all represented: a section stored inside a page, an item
  stored in its own right, and an entry nested in a menu.
- Scopes set so there is something to override on each: services open to every Managed Site, one page
  open to two of them, and named menu entries open to one. Leaving one item deliberately out of scope is
  worth as much as putting the others in, because "why can I not see this" is the first question the
  portal raises.
- Three Managed Sites, `alpha`, `beta` and `gamma`, each addressed by URL prefix on the tenant's own
  host. Prefixes rather than host names on purpose: they need no host file entries, no certificates and
  no second sign-in, and draft preview works through them, which it cannot across hosts (see the preview
  walkthrough above).
- Three users, `alphaUser`, `betaUser` and `gammaUser`, each in the editor role and each cleared for the
  one Managed Site they are named for. One of them cleared for two Managed Sites as well, so the portal's
  selection screen is reachable without editing a user first.

**What it needs first**: the recipe vocabulary does not reach this feature yet. There is no step for
declaring a Managed Site, and `UsersStep` copies only the fields it knows, so it carries neither
clearances, which are stored as a section on the user, nor a plaintext password, taking a precomputed
hash instead. T124b covers both gaps; without it the recipe can enable features and shape content but
cannot produce a site anyone can sign in to and use.

- [X] T130 Move the editable-content listing out of `Controllers/ManagedContentApiController.cs` into a service, so the surface that renders it is not the one that computes it **`Services/ManagedContentListService.cs`. Narrowing and paging are separated from reading, because that half is arithmetic over a list and is now covered by `Portal/ManagedContentListQueryTests.cs`, which found that the query object's declared defaults never applied.**
- [X] T131 Add `Controllers/ManagedSitePortalController.cs`: the active Managed Site, the content list with its filters, one item with its override, the suppressed list, and the preview link **Which Managed Site an action acts on is resolved once, so the refusals are the same wherever an editor arrives: no permission, no clearance at all, a choice not yet made, or clearance that does not reach what the action does.**
- [X] T132 Add the portal's admin views in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/ManagedSitePortal/`, using the `ocat-*` admin conventions **Walked in a browser against the development site: a single-site editor lands on their list, a multi-site editor is sent to the chooser and offered only their own, and create, edit and publish put one Managed Site's version on that site alone.**
- [X] T133 Rewrite the API tests as controller tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Portal/`, keeping every rule the API enforced: clearance, active scope, and each refusal **`Portal/ManagedSitePortalAccessTests.cs`, over the real clearance and session services so the refusals under test are the ones the feature makes. One refusal deliberately differs: an item the Managed Site may not customize now answers the same as one that does not exist, so that guessing identifiers tells an editor nothing about what the Site Blueprint holds. The API distinguished them.**
- [X] T134 Remove the single-page application: `Assets/managed-site-admin/`, the `Assets.json` entry, the committed bundles in `wwwroot/Scripts/managed-site-admin/`, and `Controllers/PortalController.cs` with its view **The module now ships no assets at all, so there is nothing to build beyond the project itself.**
- [X] T135 Remove the API controllers that served it, and the models and routes that exist only for them **Five controllers, their base, the authorization policy they shared, and the response models nothing else used. The two models the portal still needs moved to `ViewModels/ManagedContentViewModels.cs`, a file no longer named for an interface that does not exist. The header naming a Managed Site stays, because public rendering ignoring it is a rule worth testing against the thing it forbids.**
- [X] T136 Update `src/docs/reference/modules/ManagedSites/README.md` and the module README where they describe the portal as a client application

**What this changes, and what it must not.** Each API endpoint becomes an action: the authorized
Managed Sites and the session become the portal's own scope selection, the content list keeps its
content type, override status and paging filters, and create, publish and remove become posts from the
item screen. The preview screen keeps building a link to the Managed Site's own address rather than
rendering a preview itself.

What must survive unchanged is every refusal. A caller without clearance for the Managed Site they name
is refused; a caller cleared for several who has chosen none is told to choose; a Managed Site the
caller holds no clearance for is neither listed nor reachable. Those rules are tested today against the
API, and T133 is what stops them being lost in the move. The one rule that goes is the optional scope
header: with no client application there is nothing to send it, and the Managed Site an action applies
to comes from the route and the session.

- [X] T125 Run module tests with `dotnet test test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/VendallionCMS.ManagedSites.Tests.csproj` **358 tests, all passing.**
- [X] T126 Run CMS build with `dotnet build src/OrchardCore.Cms.Web -c Debug -f net10.0` **Succeeds. The gate earned its place: run in full rather than filtered, it showed two analyzer warnings in this module that narrower builds had been hiding, an unguarded log argument and a method that never touched instance state. Both fixed, and the module now builds without a warning of its own.**
- [X] ~~T127 Run asset build with `yarn build`~~ **Retired with the single-page application: the module ships no assets to build.**
- [X] T127a Record a usability walkthrough timing SC-005, SC-009, and SC-020 against the built portal **Walked 2026-10-06 against the development site; the record is below.**
- [X] T128 Review final implementation against `specs/001-shared-managed-site-content/spec.md` **Reviewed 2026-10-07; the record is below. Fifty-nine requirements, two deviations, one coverage gap found and closed.**
- [X] ~~T129 Review API behavior against `specs/001-shared-managed-site-content/contracts/managed-sites-api.md`~~ **Retired with the API. The contract is kept as a description of what the portal does, and T133 is what holds the admin surface to it.**

---

## Dependencies

### Phase Dependencies

- **Phase 1: Setup** has no dependencies.
- **Phase 2: Foundational** depends on Phase 1.
- **User Story phases** depend on Phase 2.
- **Phase 9: Preview, Integration, and Cross-Story Verification** depends on User Stories 3, 4, 5, and 6.
- **Phase 10: Polish and Cross-Cutting Concerns** depends on all user stories and Phase 9.

### User Story Dependencies

- **US1** depends on Phase 2 only and is the MVP slice.
- **US2** depends on Phase 2 and can proceed after US1 establishes blueprint settings.
- **US4** depends on US2 for Managed Site definitions and clearance targets.
- **US5** depends on US2 because both scopes reference Managed Site identifiers.
- **US6** depends on US5 for the part and both scopes, and on US4 for the portal shell that hosts its screens.
- **US3** depends on US2 for URL registrations and on US6 for the resolution service it invokes per request.

### Dependency Graph

```text
Phase 1 -> Phase 2 -> US1 -> US2 -> US4 -> US6 -> US3
                              -> US5 -> US6
US3 + US4 + US5 + US6 -> Phase 9 -> Phase 10
```

---

## Parallel Execution Examples

### User Story 1

```text
T032 and T033 can run in parallel.
T035 and T036 can run after T034 if assigned to different files.
```

### User Story 2

```text
T040, T041, and T042 can run in parallel.
T043 and T044 can run in parallel after foundational interfaces exist.
T045, T046, and T047 can be split by controller, view model, and view files.
```

### User Story 4

```text
T050, T051, T052, and T053 can run in parallel.
T058, T059, T060, and T061 can run in parallel after the portal package is created.
```

### User Story 5

```text
T063, T064, T065, and T066 can run in parallel.
T068, T069, T071, and T075 touch separate model, service, and index files.
T072 and T073 can run in parallel once the part model exists.
```

### User Story 6

```text
T078 through T084 can run in parallel.
T085, T086, T087, and T088 can run in parallel after the part model is stable.
T093, T094, and T095 are separate portal pages and can run in parallel.
```

### User Story 3

```text
T098, T099, T100, and T101 can run in parallel.
T104, T105, T106, and T108 can run in parallel once service interfaces are available.
```

---

## Implementation Strategy

### MVP First

Complete Phase 1, Phase 2, and User Story 1 first. This establishes the module, blueprint settings, permissions, migrations, and basic validation without changing request composition for visitors.

### Incremental Delivery

1. Deliver US1 to establish Site Blueprint governance.
2. Deliver US2 to create Managed Sites, URL registrations, and scoped authorization.
3. Deliver US4 to expose the Managed Site Admin Portal and active scope selection.
4. Deliver US5 to add the Managed Content part and its two scopes.
5. Deliver US6 to add override discovery, authoring, rendering, and recovery.
6. Deliver US3 to connect URL-based request composition.
7. Complete Phase 9 preview and contract validation.
8. Complete Phase 10 documentation, accessibility, localization, build, and test validation.

### Quality Gates

- No Managed Site mutation may rely only on client-side filtering.
- No Managed Site may be treated as a separate OrchardCore tenant.
- URL conflicts must be rejected before shell URL synchronization.
- Attaching the Managed Content part must not change rendering until a blueprint administrator configures a scope.
- Content types without the part attached must render identically to before the feature existed.
- Display scope must be evaluated before override resolution.
- Every suppressed override must record why it stopped rendering and must stay readable by authorized administrators.
- All new schema/content-definition changes must be implemented through migrations.
- Canonical documentation must be updated before implementation is considered complete.

---

## Task Summary

- **Total tasks**: 164
- **Setup tasks**: 14
- **Foundational tasks**: 17 (two superseded, one re-opened)
- **US1 tasks**: 10 (six superseded by the implicit blueprint)
- **US2 tasks**: 26 (two superseded by tenant-style addressing)
- **US3 tasks**: 14 (two delivered early with US2)
- **US4 tasks**: 18
- **US5 tasks**: 17
- **US6 tasks**: 28 (four reopened by the 2026-09-20 requirements review, four added from running the feature)
- **Preview/integration tasks**: 9 (one added after two dependency cycles reached a running tenant unnoticed)
- **Polish tasks**: 11
- **Completed**: 152 of 164 (Phases 1-8, and Phase 9 but for the quickstart walkthrough)
- **Phase 4 is complete.** Managed Sites are defined, addressed, and synchronized to the tenant hostname.
- **Phase 6 is complete.** Managed Content attaches to any content type, both scopes are configurable by blueprint administrators only, and the display scope covers the edit scope.
- **Phase 7 is complete.** A Managed Site discovers what it may override, including sections stored inside a page; creates its own version with its clearance alone, starting from the blueprint content; and that version is what renders for it. Overrides stop rendering when their cause is withdrawn, keep the reason, and recover on their own when it is restored. Duplicates arriving outside the API resolve deterministically and are shown for cleanup.
- **Phase 8 is complete.** A public request resolves to a Managed Site from its URL alone, and rendering serves that Managed Site's overrides. Published changes drop cached composition per Managed Site, and Site Blueprint changes drop it for all of them.
