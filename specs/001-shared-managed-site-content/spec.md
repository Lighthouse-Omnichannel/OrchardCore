# Feature Specification: Site Blueprint and Managed Site Content

**Feature Branch**: `[001-shared-managed-site-content]`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "An OrchardCore site will be marked as Shared and the users that can manage the shared (common) content will be able to configure the shared site content

A new concept of sub Site is introduced. It will define a new classification of content, that will be served when the incoming url is registerd to that sub site.
Each sub site defines its own urls and the users having access to this child site will be able to manage this sub sites content.

Modifying the main site or the sub sites, it updates the list of the Shell's urls.

An incoming request should compose the content based on the main site's content for that url and additionally the content defined from the sub site content editors

Lets start with this context, and try to create the spec"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage Site Blueprint Content (Priority: P1)

As a site blueprint content manager, I can configure blueprint content so common content is centrally managed and available across all managed-site request contexts.

**Why this priority**: Blueprint content is the foundation for consistent experiences and is required before managed-site composition can provide value.

**Independent Test**: Can be fully tested by assigning a user site blueprint management rights, updating blueprint content, and confirming the updated blueprint content is available where expected.

**Acceptance Scenarios**:

1. **Given** a site is marked as a Site Blueprint and a user has site blueprint management access, **When** the user edits blueprint content and publishes changes, **Then** the blueprint content is saved and available for request composition.
2. **Given** a user without site blueprint management access, **When** they attempt to modify blueprint content, **Then** the action is denied and blueprint content remains unchanged.

---

### User Story 2 - Define and Manage Managed Site Content By URL (Priority: P2)

As a managed-site content editor, I can define a managed site, register its URLs, and manage content for that managed site so requests to those URLs return context-specific content.

**Why this priority**: URL-scoped managed-site ownership enables delegated content management without giving full site blueprint control.

**Independent Test**: Can be fully tested by creating one managed site, registering one or more URLs to it, assigning an editor, and verifying that only the assigned editor can manage that managed site's content.

**Acceptance Scenarios**:

1. **Given** a managed site exists with registered URLs and an authorized managed-site editor, **When** the editor updates managed-site content, **Then** the change is saved under that managed site.
2. **Given** a user who is not authorized for a managed site, **When** they attempt to modify that managed site's content, **Then** the action is denied.
3. **Given** a URL is not registered to any managed site, **When** content administrators view managed-site mappings, **Then** the URL is shown as unassigned.

---

### User Story 3 - Compose Request Content from Site Blueprint and Managed Site Layers (Priority: P3)

As an end user visiting a URL, I receive content composed from site blueprint content and, when applicable, additional content from the matched managed site.

**Why this priority**: Composition behavior delivers the primary end-user outcome and validates the value of both site blueprint and managed-site editing models.

**Independent Test**: Can be fully tested by preparing blueprint content and managed-site content for a registered URL, requesting that URL, and confirming the response includes both layers in the defined composition order.

**Acceptance Scenarios**:

1. **Given** a request URL is registered to a managed site, **When** the request is processed, **Then** the response includes site blueprint content for that URL plus the managed-site content for that matched managed site.
2. **Given** a request URL is not registered to a managed site, **When** the request is processed, **Then** the response includes site blueprint content only.
3. **Given** blueprint content and managed-site content are both updated, **When** the same URL is requested after publication, **Then** the response reflects the latest published versions of both content layers.

---

### User Story 4 - Manage Content Through the Managed Site Admin Portal Session (Priority: P2)

As a content editor with managed-site clearance, I can log in to the Managed Site Admin Portal, select the managed site I am working on for my session, and use OrchardCore APIs to edit content and preview pages.

**Why this priority**: Editors need a controlled authoring entry point with clear site scope so updates and previews are always applied to the correct managed site.

**Independent Test**: Can be fully tested by signing in as a user with access to multiple managed sites, selecting an active managed site, editing content through the application, previewing a page, and verifying all actions remain within the selected site scope.

**Acceptance Scenarios**:

1. **Given** a user has clearance to one managed site, **When** the user signs in, **Then** the session is automatically scoped to that managed site.
2. **Given** a user has clearance to more than one managed site, **When** the user signs in, **Then** the user must select which managed site to manage for the current session before editing content.
3. **Given** an active managed site is selected for the session, **When** the user updates content or previews a page, **Then** the application performs those actions only within that active managed-site scope through OrchardCore APIs.

---

### User Story 5 - Build Shared Navigation With Managed-Site Contributions (Priority: P2)

As a site blueprint administrator, I can define navigation placeholders in the site blueprint, and as a managed-site administrator, I can populate those placeholders for my managed site so each managed site contributes its own navigation entries within a blueprint navigation structure.

**Why this priority**: Navigation is a primary user journey entry point and must support both centralized structure and delegated managed-site ownership.

**Independent Test**: Can be fully tested by creating site blueprint placeholders, assigning a managed-site administrator, adding managed-site-specific placeholder entries, and verifying the composed menu output per managed site.

**Acceptance Scenarios**:

1. **Given** the site blueprint defines one or more navigation placeholders, **When** a managed-site administrator edits their managed site's navigation configuration, **Then** they can add or update entries only for those placeholders.
2. **Given** two managed sites share the same placeholder name, **When** each managed-site administrator configures entries, **Then** each managed site renders only its own placeholder entries.
3. **Given** a placeholder has no managed-site entries, **When** the site is rendered, **Then** the placeholder remains empty without breaking navigation rendering.

---

### Edge Cases

- A URL is accidentally registered to multiple managed sites; the system must prevent ambiguous routing assignments.
- A previously registered managed-site URL is removed; subsequent requests to that URL should no longer include that managed site's content layer.
- A managed site is disabled or no longer accessible to editors; existing blueprint content delivery must continue without regression.
- Blueprint content for a URL exists, but managed-site content is missing for the matched managed site; response should still render blueprint content.
- A managed-site editor loses access while edits are in progress; unauthorized updates must not be applied.
- A managed site has content for a URL but no managed-site menu contribution for required placeholders; the page still renders and navigation degrades gracefully.
- Placeholder names are changed by site blueprint administrators after managed-site entries already exist; affected entries must be handled predictably and remain administratively recoverable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a site to be designated as a Site Blueprint.
- **FR-002**: The system MUST support a permission model where only authorized users can manage site blueprint content.
- **FR-003**: The system MUST introduce a managed-site concept that groups content under a child site classification.
- **FR-004**: The system MUST allow each managed site to maintain its own list of registered URLs.
- **FR-005**: The system MUST support managed-site-specific permissions so only authorized users can manage that managed site's content.
- **FR-006**: The system MUST update the shell URL list when site blueprint URL mappings or managed-site URL mappings are added, changed, or removed.
- **FR-007**: The system MUST resolve incoming requests to a managed site when the request URL matches a URL registered to that managed site.
- **FR-008**: The system MUST compose response content by combining site blueprint content with matched managed-site content when both exist for the request URL.
- **FR-009**: The system MUST serve site blueprint content alone when no managed site is matched for the request URL.
- **FR-010**: The system MUST prevent a single URL from being simultaneously assigned to multiple managed sites.
- **FR-011**: The system MUST ensure unauthorized users cannot create, edit, or publish content outside their granted site blueprint or managed-site scope.
- **FR-012**: The system MUST reflect published updates to site blueprint and managed-site content in subsequent requests without requiring manual shell URL refresh actions.
- **FR-013**: The solution MUST provide a Managed Site Admin Portal for editors to manage site blueprint and managed-site content.
- **FR-014**: The Managed Site Admin Portal MUST be React-based.
- **FR-015**: The Managed Site Admin Portal MUST authenticate users before allowing access to content management functions.
- **FR-016**: The Managed Site Admin Portal MUST use OrchardCore APIs for content updates and page preview operations.
- **FR-017**: The system MUST maintain managed-site clearances per user and expose only authorized managed sites to that user in the application.
- **FR-018**: If a user has clearance to more than one managed site, the user MUST select one managed site as the active scope for the current session before performing content actions.
- **FR-019**: If a user has clearance to exactly one managed site, the system MUST automatically set that managed site as the active scope for the session.
- **FR-020**: Content edits and preview actions initiated from the Managed Site Admin Portal MUST execute only against the active managed site selected for the session.
- **FR-021**: The Site Blueprint MUST support defining menu placeholders in its navigation model, at either menu level or menu-item level.
- **FR-022**: Managed-site administrators MUST be able to populate only the placeholders defined by the Site Blueprint for their own managed site scope.
- **FR-023**: The composed navigation output for a request MUST include Site Blueprint navigation structure plus managed-site placeholder contributions from the matched managed site.
- **FR-024**: The solution MUST preserve compatibility with OrchardCore content targeting capabilities so managed-site content can be resolved correctly regardless of whether underlying implementation uses route-scoped entries, layer-based targeting, or an equivalent supported pattern.
- **FR-025**: The solution MUST preserve compatibility with OrchardCore URL routing behavior so managed-site URL resolution remains consistent with existing site URL handling expectations.

### Key Entities *(include if feature involves data)*

- **Site Blueprint**: The primary site context that defines common content, navigation structure, placeholders, and blueprint management rules.
- **Managed Site**: A child site context with its own URL registrations, editor access scope, and managed-site-specific content.
- **URL Registration**: A mapping between an incoming URL and either the site blueprint context alone or a specific managed site.
- **Content Scope Permission**: Authorization boundaries that determine who can manage site blueprint content versus a specific managed site's content.
- **Managed Site Clearance**: The authorization assignment that determines which managed sites a user can access in the Managed Site Admin Portal.
- **Active Managed Site Session Scope**: The managed site selected by the user (or auto-selected when only one is available) that constrains all editing and preview actions during the current session.
- **Composed Response**: The final delivered content assembled from site blueprint content and optional managed-site content for a matched URL.
- **Menu Placeholder**: A site blueprint-defined navigation slot that can receive managed-site-specific menu contributions.
- **Managed Navigation Contribution**: Managed-site-owned navigation entries assigned to site blueprint placeholders and rendered only within that managed site scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of URL requests mapped to managed sites return content that includes both site blueprint and corresponding managed-site layers.
- **SC-002**: 100% of URL requests not mapped to managed sites return site blueprint content without managed-site leakage.
- **SC-003**: 100% of unauthorized edit attempts on site blueprint or managed-site content are blocked with no persisted content change.
- **SC-004**: At least 95% of published content updates (site blueprint or managed-site) are visible on subsequent matching requests within 60 seconds.
- **SC-005**: Site administrators can complete site blueprint or managed-site URL mapping changes in under 3 minutes for standard update tasks.
- **SC-006**: 100% of content edits and preview operations from the Managed Site Admin Portal are executed in the managed-site scope selected for the user's session.
- **SC-007**: 95% of users with access to multiple managed sites can complete sign-in and active-site selection in under 60 seconds.
- **SC-008**: 100% of managed-site navigation contributions appear only in the matched managed site's rendered navigation for placeholder-enabled menus.
- **SC-009**: Site blueprint administrators can create or update placeholder definitions for a standard menu in under 3 minutes.

## Assumptions

- Existing OrchardCore user identities and role/permission management are reused for access control decisions.
- The Managed Site Admin Portal is implemented as a React front end.
- OrchardCore APIs required for content edit and page preview operations are available to the Managed Site Admin Portal under authenticated access.
- Site blueprint content and managed-site content use existing content lifecycle states (draft/published) and are composed from published versions.
- URL matching uses the same canonical request URL interpretation already used by the site.
- Initial scope covers one Site Blueprint with multiple managed sites under that blueprint context.
- Cross-managed-site content inheritance beyond site-blueprint-plus-managed-site composition is out of scope for this first version.
- Changes to URL registrations are expected to be auditable through existing operational logging capabilities.
- Final implementation may use route-scoped content entries, layer-based targeting, or an equivalent OrchardCore-compatible approach, provided required managed-site behavior is preserved.

## Naming Decisions

### Core Domain Terms

- **Site Blueprint**: The primary site that owns common content, shared navigation structure, and placeholders.
- **Managed Site**: A child site scope that owns URL-specific and audience-specific content.

### Capability Group Names

- **Managed Site Definitions**
- **Managed Site URL Registry**
- **Site Blueprint and Managed Content Composition**
- **Managed Site Scope Permissions**
- **Shell URL Synchronization**

### Custom Module Naming Prefix

- New custom modules for this feature use the **VendallionCMS** prefix.
- Planned module family naming: **VendallionCMS.ManagedSites**.