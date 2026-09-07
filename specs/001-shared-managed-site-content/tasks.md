# Tasks: Site Blueprint and Managed Site Content

**Input**: Design documents from `specs/001-shared-managed-site-content/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/managed-sites-api.md](contracts/managed-sites-api.md), [quickstart.md](quickstart.md)

**Tests**: Test tasks are included because the OrchardCore constitution requires automated verification for behavioral changes.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

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
- [X] T020 Create page customization models in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/PageCustomizationModels.cs`
- [X] T021 Create navigation customization models in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/NavigationCustomizationModels.cs`
- [X] T022 Create document/index models in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Indexes/ManagedSitesIndexes.cs`
- [X] T023 Create initial data migration in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Migrations/ManagedSitesMigrations.cs`
- [X] T024 Create permission definitions in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Permissions.cs`
- [X] T025 Create managed-site scope authorization service interface in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/IManagedSiteAuthorizationService.cs`
- [X] T026 Create managed-site repository/service interfaces in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteServiceInterfaces.cs`
- [X] T027 Create composition service interfaces in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/CompositionServiceInterfaces.cs`
- [X] T028 Create API base controller with common authorization helpers in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitesApiControllerBase.cs`
- [X] T029 Register foundational services and permissions in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`
- [X] T030 Create shared test fixtures in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedSitesTestFixture.cs`
- [X] T031 Create test data builders in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/ManagedSitesTestData.cs`

**Checkpoint**: Module compiles with empty service implementations and can be enabled without changing runtime behavior.

---

## Phase 3: User Story 1 - Manage Site Blueprint Content (Priority: P1)

**Goal**: Site Blueprint administrators can mark the active site as a Site Blueprint and manage blueprint content while unauthorized users are blocked.

**Independent Test**: Assign blueprint management access to one user, update blueprint content, and confirm unauthorized users cannot modify it.

### Tests for User Story 1

- [X] T032 [P] [US1] Add blueprint permission tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/SiteBlueprintAuthorizationTests.cs`
- [X] T033 [P] [US1] Add blueprint settings migration tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/SiteBlueprint/SiteBlueprintMigrationTests.cs`

### Implementation for User Story 1

- [X] T034 [US1] Implement Site Blueprint settings model in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Settings/SiteBlueprintSettings.cs`
- [X] T035 [US1] Implement Site Blueprint settings driver in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Drivers/SiteBlueprintSettingsDisplayDriver.cs`
- [X] T036 [US1] Implement Site Blueprint settings editor view in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/SiteBlueprintSettings.Edit.cshtml`
- [X] T037 [US1] Implement Site Blueprint service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/SiteBlueprintService.cs`
- [X] T038 [US1] Register Site Blueprint admin menu entries in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/AdminMenu.cs`
- [X] T039 [US1] Wire Site Blueprint settings and admin services in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`

**Checkpoint**: Site Blueprint designation and blueprint-content authorization are functional and tested.

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
- [X] T045 [US2] Implement Managed Site admin controller in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitesAdminController.cs`
- [X] T046 [US2] Implement Managed Site admin view models in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/ViewModels/ManagedSiteViewModels.cs`
- [X] T047 [US2] Implement Managed Site admin views in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/ManagedSites/`
- [X] T048 [US2] Implement URL conflict validation in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/UrlRegistrationValidator.cs`
- [X] T049 [US2] Implement scoped content authorization service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteAuthorizationService.cs`

**Checkpoint**: Managed Site definitions, URL registrations, and scoped editor access are functional and tested.

---

## Phase 5: User Story 4 - Managed Site Admin Portal Session (Priority: P2)

**Goal**: Editors use the React-based Managed Site Admin Portal to authenticate, select an active Managed Site, and perform API-backed scoped actions.

**Independent Test**: Sign in as a multi-site user, select an active Managed Site, edit managed-site content, preview a page, and verify all actions remain scoped.

### Tests for User Story 4

- [ ] T050 [P] [US4] Add authorized Managed Sites API tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ManagedSitePortalAuthorizationTests.cs`
- [ ] T051 [P] [US4] Add active scope selection API tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ActiveManagedSiteSessionTests.cs`
- [ ] T052 [P] [US4] Add portal UI scope selection tests in `test/OrchardCore.Tests.Functional/Tests/VendallionCMS.ManagedSites/ManagedSiteAdminPortalTests.cs`
- [ ] T053 [P] [US4] Add admin API scope mismatch tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Authorization/ManagedSiteApiScopeMismatchTests.cs`

### Implementation for User Story 4

- [ ] T054 [US4] Implement authorized Managed Sites API endpoint in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitesApiController.cs`
- [ ] T055 [US4] Implement active Managed Site session endpoint in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSiteSessionApiController.cs`
- [ ] T056 [US4] Implement token claims/scope extraction in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteClearanceService.cs`
- [ ] T057 [US4] Implement route/session/token/header scope consistency checks in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitesApiControllerBase.cs`
- [ ] T058 [US4] Implement React portal entry point in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/main.tsx`
- [ ] T059 [US4] Implement portal API client in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/services/managedSitesApi.ts`
- [ ] T060 [US4] Implement active site selector UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/components/ManagedSiteSelector.tsx`
- [ ] T061 [US4] Implement scoped editor shell UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/ManagedSiteAdminShell.tsx`
- [ ] T062 [US4] Add portal host view in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/Admin/Portal.cshtml`

**Checkpoint**: Portal login, authorized-site listing, auto-selection, multi-site selection, and scoped API access work end to end.

---

## Phase 6: User Story 5 - Blueprint Navigation With Managed-Site Contributions (Priority: P2)

**Goal**: Site Blueprint administrators define placeholder menus/menu items, and Managed Site administrators fill those placeholders for their own site.

**Independent Test**: Create placeholder menus and items, add different Managed Site contributions, and verify each site renders only its own navigation entries.

### Tests for User Story 5

- [ ] T063 [P] [US5] Add menu placeholder definition tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Navigation/MenuPlaceholderTests.cs`
- [ ] T064 [P] [US5] Add managed navigation contribution tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Navigation/ManagedNavigationContributionTests.cs`
- [ ] T065 [P] [US5] Add navigation contribution API contract tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Contracts/ManagedNavigationContributionApiContractTests.cs`
- [ ] T066 [P] [US5] Add navigation placeholder recovery tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Navigation/NavigationPlaceholderRecoveryTests.cs`

### Implementation for User Story 5

- [ ] T067 [US5] Implement menu placeholder models and settings in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/MenuPlaceholderModels.cs`
- [ ] T068 [US5] Implement menu placeholder editor driver in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Drivers/MenuPlaceholderDisplayDriver.cs`
- [ ] T069 [US5] Implement managed navigation contribution service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedNavigationContributionService.cs`
- [ ] T070 [US5] Implement navigation composition handler in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Handlers/ManagedNavigationCompositionHandler.cs`
- [ ] T071 [US5] Implement portal navigation contribution UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/ManagedNavigationPage.tsx`
- [ ] T072 [US5] Implement navigation placeholder and contribution API endpoints in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/NavigationContributionsApiController.cs`
- [ ] T073 [US5] Implement portal navigation contribution API client methods in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/services/managedSitesApi.ts`
- [ ] T074 [US5] Implement navigation placeholder recovery handling in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedNavigationContributionService.cs`

**Checkpoint**: Placeholder navigation renders per Managed Site and remains recoverable across placeholder changes.

---

## Phase 7: User Story 6 - Override an Allowed Blueprint Page (Priority: P2)

**Goal**: Managed Site administrators can override only blueprint pages that allow overrides, and disabled override policies stop rendering existing overrides while preserving recovery.

**Independent Test**: Mark a page overrideable, create one Managed Site override, verify scoped rendering, mark page non-overrideable, and verify override recovery without rendering.

### Tests for User Story 6

- [ ] T075 [P] [US6] Add page override policy tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/PageOverridePolicyTests.cs`
- [ ] T076 [P] [US6] Add page override rendering tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/ManagedSitePageOverrideTests.cs`
- [ ] T077 [P] [US6] Add disabled override recovery tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/PageOverrideRecoveryTests.cs`

### Implementation for User Story 6

- [ ] T078 [US6] Implement page override policy part/settings in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/PageOverridePolicyPart.cs`
- [ ] T079 [US6] Implement page override policy editor in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Drivers/PageOverridePolicyDisplayDriver.cs`
- [ ] T080 [US6] Implement Managed-Site Page Override service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSitePageOverrideService.cs`
- [ ] T081 [US6] Implement page override API endpoints in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/PageOverridesApiController.cs`
- [ ] T082 [US6] Implement page override composition handler in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Handlers/PageOverrideCompositionHandler.cs`
- [ ] T083 [US6] Implement portal page override UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/PageOverridesPage.tsx`

**Checkpoint**: Page overrides are scoped, policy-controlled, recoverable, and tested.

---

## Phase 8: User Story 3 - Request Content Composition (Priority: P3)

**Goal**: Incoming requests resolve to a Managed Site by URL and compose Site Blueprint content plus Managed Site content without URL leakage.

**Independent Test**: Prepare blueprint and Managed Site content for registered URLs, request each URL, and confirm the composed response uses the correct layers.

### Tests for User Story 3

- [ ] T084 [P] [US3] Add URL resolver tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/ManagedSiteUrlResolverTests.cs`
- [ ] T085 [P] [US3] Add composition service tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/ManagedSiteCompositionServiceTests.cs`
- [ ] T086 [P] [US3] Add shell URL synchronization tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/ShellUrlSynchronizationTests.cs`
- [ ] T087 [P] [US3] Add public request scope metadata distrust tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Routing/PublicManagedSiteScopeMetadataTests.cs`
- [ ] T088 [P] [US3] Add composition cache invalidation tests for published content changes in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/CompositionCacheInvalidationTests.cs`
- [ ] T089 [P] [US3] Add OrchardCore cache dependency tests for URL, navigation, override, layer, and placeholder changes in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/CompositionCacheDependencyTests.cs`

### Implementation for User Story 3

- [ ] T090 [US3] Implement Managed Site URL resolver in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteUrlResolver.cs`
- [ ] T091 [US3] Implement request composition context accessor in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteCompositionContextAccessor.cs`
- [ ] T092 [US3] Implement request pipeline middleware in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteRequestMiddleware.cs`
- [ ] T093 [US3] Ensure request pipeline middleware derives Managed Site context from URL resolution and ignores client Managed Site headers in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteRequestMiddleware.cs`
- [ ] T094 [US3] Implement shell URL synchronization service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ShellUrlSynchronizationService.cs`
- [ ] T095 [US3] Register request middleware and synchronization services in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`
- [ ] T096 [US3] Implement composition cache state service using OrchardCore cache infrastructure in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteCompositionCacheService.cs`
- [ ] T097 [US3] Implement content and mapping change invalidation handler in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Handlers/ManagedSiteCompositionInvalidationHandler.cs`
- [ ] T098 [US3] Register composition cache and invalidation services in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Startup.cs`

**Checkpoint**: URL-based Managed Site resolution and composed request behavior work without treating Managed Sites as separate OrchardCore tenants.

---

## Phase 9: User Story 7 - Managed-Site Items Through Blueprint Layers (Priority: P3)

**Goal**: Managed Site administrators add supplemental items to blueprint-defined layer contribution points, and those items render independently through layout layers.

**Independent Test**: Define a layer contribution point, add Managed Site items, render pages with and without page overrides, and confirm layer items still render only for the owning Managed Site.

### Tests for User Story 7

- [ ] T099 [P] [US7] Add layer contribution point tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/LayerContributionPointTests.cs`
- [ ] T100 [P] [US7] Add layer contribution rendering tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/ManagedSiteLayerContributionTests.cs`
- [ ] T101 [P] [US7] Add layer contribution recovery tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/LayerContributionRecoveryTests.cs`

### Implementation for User Story 7

- [ ] T102 [US7] Implement layer contribution point editor in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Drivers/LayerContributionPointDisplayDriver.cs`
- [ ] T103 [US7] Implement Managed-Site Layer Contribution service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteLayerContributionService.cs`
- [ ] T104 [US7] Implement layer contribution API endpoints in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/LayerContributionsApiController.cs`
- [ ] T105 [US7] Implement layer contribution composition handler in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Handlers/LayerContributionCompositionHandler.cs`
- [ ] T106 [US7] Implement portal layer contribution UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/LayerContributionsPage.tsx`
- [ ] T107 [US7] Implement layer contribution recovery handling in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSiteLayerContributionService.cs`

**Checkpoint**: Layout layer contributions are independent from page overrides and scoped by Managed Site.

---

## Phase 10: User Story 8 - Blueprint Page Placeholders With Managed-Site Content (Priority: P3)

**Goal**: Managed Site administrators fill blueprint page placeholders with scoped content, with fallback content or empty rendering when no managed-site content exists.

**Independent Test**: Define a placeholder, assign Managed Site content, verify scoped rendering, verify fallback behavior, and verify placeholders are suppressed when the blueprint page is overridden.

### Tests for User Story 8

- [ ] T108 [P] [US8] Add placeholder assignment tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/PlaceholderAssignmentTests.cs`
- [ ] T109 [P] [US8] Add placeholder fallback tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/PlaceholderFallbackTests.cs`
- [ ] T110 [P] [US8] Add override placeholder suppression tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/OverridePlaceholderSuppressionTests.cs`
- [ ] T111 [P] [US8] Add placeholder assignment recovery tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/PlaceholderAssignmentRecoveryTests.cs`

### Implementation for User Story 8

- [ ] T112 [US8] Implement Managed-Site Placeholder part/settings in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Models/ManagedSitePlaceholderPart.cs`
- [ ] T113 [US8] Implement Managed-Site Placeholder editor in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Drivers/ManagedSitePlaceholderDisplayDriver.cs`
- [ ] T114 [US8] Implement Placeholder Assignment service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/PlaceholderAssignmentService.cs`
- [ ] T115 [US8] Implement placeholder assignment API endpoints in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/PlaceholderAssignmentsApiController.cs`
- [ ] T116 [US8] Implement placeholder composition handler in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Handlers/PlaceholderCompositionHandler.cs`
- [ ] T117 [US8] Implement portal placeholder assignment UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/PlaceholderAssignmentsPage.tsx`
- [ ] T118 [US8] Implement placeholder assignment recovery handling in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/PlaceholderAssignmentService.cs`

**Checkpoint**: Placeholder assignments, fallback rendering, and suppression under page overrides are scoped and tested.

---

## Phase 11: Preview, Integration, and Cross-Story Verification

**Purpose**: Connect preview behavior, public API contracts, and end-to-end validation across all user stories.

- [ ] T119 [P] Add preview composition tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Preview/ManagedSitePreviewTests.cs`
- [ ] T120 [P] Add API contract tests for managed-site endpoints in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Contracts/ManagedSitesApiContractTests.cs`
- [ ] T121 [P] Add composed rendering integration tests in `test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/Composition/ManagedSiteCompositionIntegrationTests.cs`
- [ ] T122 Implement preview service in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Services/ManagedSitePreviewService.cs`
- [ ] T123 Implement preview API endpoint in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/ManagedSitePreviewApiController.cs`
- [ ] T124 Implement portal preview UI in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/pages/PreviewPage.tsx`
- [ ] T125 Ensure all API routes match `specs/001-shared-managed-site-content/contracts/managed-sites-api.md` in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Controllers/`
- [ ] T126 Validate quickstart scenarios in `specs/001-shared-managed-site-content/quickstart.md`

---

## Phase 12: Polish and Cross-Cutting Concerns

**Purpose**: Documentation, accessibility, localization, build validation, and final quality checks.

- [ ] T127 [P] Add localization strings for admin UI and portal host views in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Views/`
- [ ] T128 [P] Add accessibility checks for portal selection and editor flows in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Assets/managed-site-admin/src/`
- [ ] T129 [P] Update module README in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/README.md`
- [ ] T130 [P] Write canonical documentation in `src/docs/reference/modules/ManagedSites/README.md`
- [ ] T131 Update feature manifest descriptions after implementation in `src/OrchardCore.Modules/VendallionCMS.ManagedSites/Manifest.cs`
- [ ] T132 Run module tests with `dotnet test test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/VendallionCMS.ManagedSites.Tests.csproj`
- [ ] T133 Run CMS build with `dotnet build src/OrchardCore.Cms.Web -c Debug -f net10.0`
- [ ] T134 Run asset build with `yarn build`
- [ ] T135 Review final implementation against `specs/001-shared-managed-site-content/spec.md`
- [ ] T136 Review API behavior against `specs/001-shared-managed-site-content/contracts/managed-sites-api.md`

---

## Dependencies

### Phase Dependencies

- **Phase 1: Setup** has no dependencies.
- **Phase 2: Foundational** depends on Phase 1.
- **User Story phases** depend on Phase 2.
- **Phase 11: Preview, Integration, and Cross-Story Verification** depends on User Stories 2, 3, 4, 6, 7, and 8.
- **Phase 12: Polish and Cross-Cutting Concerns** depends on all user stories and Phase 11.

### User Story Dependencies

- **US1** depends on Phase 2 only and is the MVP slice.
- **US2** depends on Phase 2 and can proceed after US1 establishes blueprint settings.
- **US4** depends on US2 for Managed Site definitions and clearance targets.
- **US5** depends on US2 for Managed Site scope and can run in parallel with US6 after that.
- **US6** depends on US2 for Managed Site scope and can run in parallel with US5 after that.
- **US3** depends on US2 and should integrate after URL registrations exist.
- **US7** depends on US2 and can run in parallel with US8 after composition interfaces exist.
- **US8** depends on US2 and US6 for page override suppression behavior.

### Dependency Graph

```text
Phase 1 -> Phase 2 -> US1 -> US2 -> US4
                              -> US5
                              -> US6 -> US8
                              -> US3
                              -> US7
US3 + US4 + US5 + US6 + US7 + US8 -> Phase 11 -> Phase 12
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
T050, T051, and T052 can run in parallel.
T058, T059, T060, and T061 can run in parallel after the portal package is created.
```

### User Story 5

```text
T063 and T064 can run in parallel.
T067, T068, T069, and T070 touch separate model, driver, service, and handler files.
```

### User Story 6

```text
T075, T076, and T077 can run in parallel.
T078, T079, T080, T081, and T082 can run in parallel after page override contracts are agreed.
```

### User Story 3

```text
T084, T085, and T086 can run in parallel.
T090, T091, T092, and T094 can run in parallel once service interfaces are available.
```

### User Story 7

```text
T099 and T100 can run in parallel.
T102, T103, T104, and T105 touch different implementation surfaces.
```

### User Story 8

```text
T108, T109, and T110 can run in parallel.
T112, T113, T114, T115, and T116 can run in parallel after placeholder model contracts are stable.
```

---

## Implementation Strategy

### MVP First

Complete Phase 1, Phase 2, and User Story 1 first. This establishes the module, blueprint settings, permissions, migrations, and basic validation without changing request composition for visitors.

### Incremental Delivery

1. Deliver US1 to establish Site Blueprint governance.
2. Deliver US2 to create Managed Sites, URL registrations, and scoped authorization.
3. Deliver US4 to expose the Managed Site Admin Portal and active scope selection.
4. Deliver US5 and US6 for navigation placeholders and page overrides.
5. Deliver US3 to connect URL-based request composition.
6. Deliver US7 and US8 for layer contributions and page placeholders.
7. Complete Phase 11 preview and contract validation.
8. Complete Phase 12 documentation, accessibility, localization, build, and test validation.

### Quality Gates

- No Managed Site mutation may rely only on client-side filtering.
- No Managed Site may be treated as a separate OrchardCore tenant.
- URL conflicts must be rejected before shell URL synchronization.
- Page override behavior must suppress blueprint page placeholders but preserve independent layout layer rendering.
- All new schema/content-definition changes must be implemented through migrations.
- Canonical documentation must be updated before implementation is considered complete.

---

## Task Summary

- **Total tasks**: 136
- **Setup tasks**: 14
- **Foundational tasks**: 17
- **US1 tasks**: 8
- **US2 tasks**: 10
- **US3 tasks**: 15
- **US4 tasks**: 13
- **US5 tasks**: 12
- **US6 tasks**: 9
- **US7 tasks**: 9
- **US8 tasks**: 11
- **Preview/integration tasks**: 8
- **Polish tasks**: 10
