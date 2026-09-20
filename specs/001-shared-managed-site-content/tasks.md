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
`ManageSiteBlueprint` permission survives, because it governs who may manage common content and, from
User Story 5 onward, who may configure managed content scopes.

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

**Checkpoint**: A tenant with the feature enabled is the Site Blueprint, and common-content authorization is governed by the `ManageSiteBlueprint` permission.

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

**Goal**: Editors use the React-based Managed Site Admin Portal to authenticate, select an active Managed Site, and perform API-backed scoped actions.

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

**Checkpoint**: Overrides are discoverable, scoped, suppressible with a recorded reason, recoverable, and render only for their owning managed site.

---

## Phase 8: User Story 3 - Request Content Composition (Priority: P3)

**Goal**: Incoming requests resolve to a Managed Site by URL and every managed content item resolves to that site's override or to the original content.

**Independent Test**: Prepare blueprint content and overrides for registered URLs, request each URL, and confirm the composed response uses the correct content per item.

### Tests for User Story 3

- [ ] T098 [P] [US3] Add URL resolver tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/ManagedSiteUrlResolverTests.cs`
- [ ] T099 [P] [US3] Add composition resolution tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/ManagedSiteCompositionServiceTests.cs`
- [X] T100 [P] [US3] Add shell URL synchronization tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/ShellUrlSynchronizationTests.cs` (delivered early with US2)
- [ ] T101 [P] [US3] Add public request scope metadata distrust tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/PublicManagedSiteScopeMetadataTests.cs`
- [ ] T102 [P] [US3] Add composition cache invalidation tests for published content changes in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/CompositionCacheInvalidationTests.cs`
- [ ] T103 [P] [US3] Add cache dependency tests for URL, override, and scope changes in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/CompositionCacheDependencyTests.cs`

### Implementation for User Story 3

- [ ] T104 [US3] Implement Managed Site URL resolver in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteUrlResolver.cs`
- [X] T105 [US3] Implement request composition context accessor in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedContentResolutionService.cs` (delivered early with US6; the render path needs somewhere to read the resolved Managed Site from, and the middleware that fills it is T106)
- [ ] T106 [US3] Implement request pipeline middleware in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteRequestMiddleware.cs`
- [ ] T107 [US3] Ensure request pipeline middleware derives Managed Site context from URL resolution and ignores client Managed Site headers in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteRequestMiddleware.cs`
- [X] T108 [US3] Implement shell URL synchronization service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ShellUrlSynchronizationService.cs` (delivered early with US2; hosts only, see research.md)
- [ ] T109 [US3] Implement composition cache state service using OrchardCore cache infrastructure in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteCompositionCacheService.cs`
- [ ] T110 [US3] Implement content, override, and scope change invalidation handler in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Handlers/ManagedSiteCompositionInvalidationHandler.cs`
- [ ] T111 [US3] Register middleware, synchronization, cache, and invalidation services in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`

**Checkpoint**: URL-based Managed Site resolution and composed request behavior work without treating Managed Sites as separate OrchardCore tenants.

---

## Phase 9: Preview, Integration, and Cross-Story Verification

**Purpose**: Connect preview behavior, public API contracts, and end-to-end validation across all user stories.

- [ ] T112 [P] Add preview composition tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Preview/ManagedSitePreviewTests.cs`
- [ ] T113 [P] Add composed rendering integration tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/ManagedSiteCompositionIntegrationTests.cs`
- [ ] T114 [P] Add backward-compatibility tests proving types without the part render unchanged in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedContent/UnattachedContentRegressionTests.cs`
- [ ] T115 Implement preview service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSitePreviewService.cs`
- [ ] T116 Implement preview API endpoint in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitePreviewApiController.cs`
- [ ] T117 Implement portal preview UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/PreviewPage.tsx`
- [ ] T118 Ensure all API routes match `specs/001-shared-managed-site-content/contracts/managed-sites-api.md` in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/`
- [ ] T119 Validate quickstart scenarios in `specs/001-shared-managed-site-content/quickstart.md`

---

## Phase 10: Polish and Cross-Cutting Concerns

**Purpose**: Documentation, accessibility, localization, build validation, and final quality checks.

- [ ] T120 [P] Add localization strings for admin UI and portal host views in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/`
- [ ] T121 [P] Add accessibility checks for portal selection, content list, and override editor flows in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/`
- [ ] T122 [P] Update module README in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/README.md`
- [ ] T123 [P] Write canonical documentation covering the Managed Content part, both scopes, and override recovery in `src/docs/reference/modules/ManagedSites/README.md`
- [ ] T124 Update feature manifest descriptions after implementation in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Manifest.cs`
- [ ] T125 Run module tests with `dotnet test test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/VendallionCMS.ManagedSites.Tests.csproj`
- [ ] T126 Run CMS build with `dotnet build src/OrchardCore.Cms.Web -c Debug -f net10.0`
- [ ] T127 Run asset build with `yarn build`
- [ ] T128 Review final implementation against `specs/001-shared-managed-site-content/spec.md`
- [ ] T129 Review API behavior against `specs/001-shared-managed-site-content/contracts/managed-sites-api.md`

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

- **Total tasks**: 154
- **Setup tasks**: 14
- **Foundational tasks**: 17 (two superseded, one re-opened)
- **US1 tasks**: 10 (six superseded by the implicit blueprint)
- **US2 tasks**: 26 (two superseded by tenant-style addressing)
- **US3 tasks**: 14 (two delivered early with US2)
- **US4 tasks**: 18
- **US5 tasks**: 17
- **US6 tasks**: 20
- **Preview/integration tasks**: 8
- **Polish tasks**: 10
- **Completed**: 125 of 154 (Phases 1-7, plus shell host synchronization and the composition context accessor from Phase 8)
- **Phase 4 is complete.** Managed Sites are defined, addressed, and synchronized to the tenant hostname.
- **Phase 6 is complete.** Managed Content attaches to any content type, both scopes are configurable by blueprint administrators only, and the display scope covers the edit scope.
- **Phase 7 is complete.** A Managed Site discovers what it may override, holds its own version as a content item of the same type, and that version is what renders for it. Overrides stop rendering when their cause is withdrawn, keep the reason, and recover on their own when it is restored. What is not yet wired is the step that tells a public request which Managed Site it belongs to, which is Phase 8: until that middleware exists, every request resolves to the Site Blueprint context and receives original content.
