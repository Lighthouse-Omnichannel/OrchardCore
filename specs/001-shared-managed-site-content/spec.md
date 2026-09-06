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

## Clarifications

### Session 2026-09-06

- Q: Which content targeting model should managed sites use? → A: Hybrid model.
- Q: Which navigation placeholder types should Site Blueprints support? → A: Placeholder menus and placeholder menu items.
- Q: What content can the Managed Site Admin Portal manage? → A: Managed Site content only.
- Q: How is managed-site clearance represented? → A: Token claims and scopes.
- Q: How should preview compose managed-site pages? → A: OrchardCore preview pipeline simulation.
- Q: When a managed site has more than one customization for the same blueprint page, which composition order should determine the final page? → A: Layer items are independent of page overrides and continue rendering through the layout; if a blueprint page is overridden, the blueprint page placeholders are not rendered.
- Q: What should happen to existing managed-site page overrides when a Site Blueprint administrator later marks the blueprint page as not overrideable? → A: Existing overrides stop rendering but remain recoverable for admin review.
- Q: When a managed-site placeholder has no assigned managed-site content, what should render in that placeholder? → A: Render blueprint fallback content if defined; otherwise render empty.

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

As a content editor with managed-site clearance, I can log in to the Managed Site Admin Portal, select the managed site I am working on for my session, and use OrchardCore APIs to edit managed-site content and preview pages.

**Why this priority**: Editors need a controlled authoring entry point with clear site scope so updates and previews are always applied to the correct managed site.

**Independent Test**: Can be fully tested by signing in as a user with access to multiple managed sites, selecting an active managed site, editing managed-site content through the portal, previewing a page, and verifying all actions remain within the selected site scope.

**Acceptance Scenarios**:

1. **Given** a user has clearance to one managed site, **When** the user signs in, **Then** the session is automatically scoped to that managed site.
2. **Given** a user has clearance to more than one managed site, **When** the user signs in, **Then** the user must select which managed site to manage for the current session before editing content.
3. **Given** an active managed site is selected for the session, **When** the user updates content or previews a page, **Then** the portal performs those actions only within that active managed-site scope through OrchardCore APIs.

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

### User Story 6 - Override an Allowed Blueprint Page (Priority: P2)

As a managed-site administrator, I can override a blueprint page for my managed site when that blueprint page explicitly allows overrides, so my managed site can provide a page-specific experience without changing the Site Blueprint for other managed sites.

**Why this priority**: Page overrides provide a clear managed-site customization path while preserving Site Blueprint governance.

**Independent Test**: Can be fully tested by marking one blueprint page as overrideable, creating an override for one managed site, and confirming only that managed site receives the overridden page content.

**Acceptance Scenarios**:

1. **Given** a blueprint page is marked as overrideable and a managed-site administrator has clearance for a managed site, **When** the administrator creates a managed-site override for that page, **Then** requests for that managed site use the managed-site override instead of the blueprint page content.
2. **Given** a blueprint page is not marked as overrideable, **When** a managed-site administrator attempts to override it, **Then** the override is blocked and the blueprint page remains authoritative.
3. **Given** two managed sites exist and only one has a page override, **When** the page is requested in each managed-site context, **Then** only the managed site with the override receives the overridden content.

---

### User Story 7 - Add Managed-Site Items Through Blueprint Layers (Priority: P3)

As a managed-site administrator, I can define additional managed-site content items that appear through blueprint-defined layers for my managed site, so local content can supplement the Site Blueprint without replacing the whole page.

**Why this priority**: Supplemental layer items support localized additions while keeping common page structure and governance intact.

**Independent Test**: Can be fully tested by configuring a blueprint layer contribution point, adding managed-site-specific items, and confirming they appear only for the matching managed site.

**Acceptance Scenarios**:

1. **Given** a Site Blueprint exposes a layer contribution point for managed sites, **When** a managed-site administrator adds content items for that layer, **Then** those items are included when the page is composed for that managed site.
2. **Given** managed-site layer items exist for one managed site, **When** another managed site renders the same blueprint page, **Then** the first managed site's layer items are not included.
3. **Given** a blueprint layer contribution point is removed or disabled, **When** a page is composed, **Then** dependent managed-site contributions are not rendered and remain administratively recoverable.

---

### User Story 8 - Fill Blueprint Page Placeholders With Managed-Site Content (Priority: P3)

As a managed-site administrator, I can fill managed-site placeholders on blueprint content items with my own content items, so the Site Blueprint controls page layout while each managed site controls specific content areas.

**Why this priority**: Placeholder filling gives precise delegated control and is useful when a full page override would be too broad.

**Independent Test**: Can be fully tested by adding a managed-site placeholder to a blueprint page, assigning managed-site content items to that placeholder, and verifying composed output for each managed site.

**Acceptance Scenarios**:

1. **Given** a blueprint content item exposes a managed-site placeholder, **When** a managed-site administrator assigns content items to that placeholder, **Then** those items appear in that placeholder for the active managed site.
2. **Given** the same placeholder exists on a blueprint page used by multiple managed sites, **When** each managed-site administrator assigns different content items, **Then** each managed site renders its own placeholder content.
3. **Given** no managed-site content is assigned to a placeholder, **When** the blueprint page is rendered, **Then** the placeholder renders blueprint fallback content if fallback content is defined, otherwise it renders empty.

---

### Edge Cases

- A URL is accidentally registered to multiple managed sites; the system must prevent ambiguous routing assignments.
- A previously registered managed-site URL is removed; subsequent requests to that URL should no longer include that managed site's content layer.
- A managed site is disabled or no longer accessible to editors; existing blueprint content delivery must continue without regression.
- Blueprint content for a URL exists, but managed-site content is missing for the matched managed site; response should still render blueprint content.
- A managed-site editor loses access while edits are in progress; unauthorized updates must not be applied.
- A managed site has content for a URL but no managed-site menu contribution for required placeholders; the page still renders and navigation degrades gracefully.
- Placeholder names are changed by site blueprint administrators after managed-site entries already exist; affected entries must be handled predictably and remain administratively recoverable.
- A user's token contains no managed-site clearance claims; the Managed Site Admin Portal must deny managed-site editing access.
- A user's managed-site clearance changes during an active session; subsequent privileged actions must honor the updated clearance before content changes are accepted.
- A blueprint page changes from overrideable to non-overrideable after managed-site overrides already exist; existing overrides must stop rendering but remain administratively reviewable and recoverable.
- A managed-site override exists for a blueprint page that is later unpublished or deleted; the managed-site administrator must not accidentally publish orphaned content into an invalid page context.
- Multiple customization modes are configured for the same blueprint page; composition rules must produce deterministic output.
- Placeholder names or layer contribution points are renamed; affected managed-site assignments must remain discoverable and recoverable.
- Blueprint fallback content exists for a placeholder; the composed page must prefer managed-site content when present, use fallback when no managed-site content is available, and render empty when neither exists.

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
- **FR-013**: The solution MUST provide a Managed Site Admin Portal for editors to manage Managed Site content only.
- **FR-014**: The Managed Site Admin Portal MUST be React-based.
- **FR-015**: The Managed Site Admin Portal MUST authenticate users before allowing access to content management functions.
- **FR-016**: The Managed Site Admin Portal MUST use OrchardCore APIs for managed-site content updates and page preview operations.
- **FR-017**: The system MUST maintain managed-site clearances per user from token claims and scopes and expose only authorized managed sites to that user in the portal.
- **FR-018**: If a user has clearance to more than one managed site, the user MUST select one managed site as the active scope for the current session before performing content actions.
- **FR-019**: If a user has clearance to exactly one managed site, the system MUST automatically set that managed site as the active scope for the session.
- **FR-020**: Content edits and preview actions initiated from the Managed Site Admin Portal MUST execute only against the active managed site selected for the session.
- **FR-021**: The Site Blueprint MUST support defining both placeholder menus and placeholder menu items in its navigation model.
- **FR-022**: Managed-site administrators MUST be able to populate only the placeholders defined by the Site Blueprint for their own managed site scope.
- **FR-023**: The composed navigation output for a request MUST include Site Blueprint navigation structure plus managed-site placeholder contributions from the matched managed site.
- **FR-024**: Managed-site content targeting MUST use a hybrid model that can support both route-scoped content entries and layer-based targeting where each model is appropriate.
- **FR-025**: The solution MUST preserve compatibility with OrchardCore URL routing behavior so managed-site URL resolution remains consistent with existing site URL handling expectations.
- **FR-026**: Page preview from the Managed Site Admin Portal MUST use OrchardCore's existing preview capability while simulating the managed tenant pipeline needed to compose site blueprint and managed-site content.
- **FR-027**: The system MUST allow Site Blueprint administrators to mark each blueprint page as overrideable or not overrideable.
- **FR-028**: Managed-site administrators MUST be able to create page-level overrides only for blueprint pages that allow overrides.
- **FR-029**: A managed-site page override MUST apply only to the managed site for which it was created.
- **FR-030**: When a valid managed-site page override exists, the composed response for that managed site MUST use the override in place of the blueprint page content for that page.
- **FR-031**: When no valid managed-site page override exists, the composed response MUST use the blueprint page content.
- **FR-031a**: When a blueprint page with existing managed-site overrides is later marked as not overrideable, those existing overrides MUST stop rendering and MUST remain recoverable for authorized administrator review.
- **FR-032**: The system MUST allow Site Blueprint administrators to define layer contribution points where managed sites can add supplemental content items.
- **FR-033**: Managed-site administrators MUST be able to add, edit, order, and remove managed-site content items for layer contribution points within their managed-site scope.
- **FR-034**: Managed-site layer contributions MUST render only for the managed site that owns them.
- **FR-035**: The system MUST allow Site Blueprint administrators to define managed-site placeholders on blueprint content items.
- **FR-036**: Managed-site administrators MUST be able to assign their own managed-site content items to blueprint placeholders within their managed-site scope.
- **FR-037**: Managed-site placeholder content MUST render only in the matching managed-site context.
- **FR-038**: The system MUST define deterministic precedence rules where layer contributions remain independent of page overrides and continue rendering through the layout.
- **FR-039**: The Managed Site Admin Portal MUST let authorized managed-site administrators manage page overrides, layer contributions, and placeholder assignments for the active managed site only.
- **FR-040**: Preview MUST show the composed page for the active managed site, including draft managed-site overrides, layer contributions, and placeholder assignments visible to the current user.
- **FR-041**: The system MUST prevent managed-site administrators from changing blueprint-level override permissions, layer contribution point definitions, or placeholder definitions unless they also have Site Blueprint management access.
- **FR-042**: The system MUST keep managed-site customization records administratively recoverable when related blueprint pages, placeholders, or layer contribution points are renamed, disabled, unpublished, or removed.
- **FR-043**: When a managed-site page override is active, placeholders defined on the overridden blueprint page MUST NOT render for that request.
- **FR-044**: Page-level content MAY contribute items to independent layout layers, and those layer items MUST NOT be treated as mutually exclusive with page rendering or page placeholders.
- **FR-045**: When no managed-site content is assigned to a placeholder, the placeholder MUST render blueprint fallback content if defined, otherwise it MUST render empty.

### Key Entities *(include if feature involves data)*

- **Site Blueprint**: The primary site context that defines common content, navigation structure, placeholders, and blueprint management rules.
- **Managed Site**: A child site context with its own URL registrations, editor access scope, and managed-site-specific content.
- **URL Registration**: A mapping between an incoming URL and either the site blueprint context alone or a specific managed site.
- **Content Scope Permission**: Authorization boundaries that determine who can manage site blueprint content versus a specific managed site's content.
- **Managed Site Clearance**: Token claims and scopes that determine which managed sites a user can access in the Managed Site Admin Portal.
- **Active Managed Site Session Scope**: The managed site selected by the user (or auto-selected when only one is available) that constrains all editing and preview actions during the current session.
- **Composed Response**: The final delivered content assembled from site blueprint content and optional managed-site content for a matched URL.
- **Menu Placeholder**: A site blueprint-defined placeholder menu or placeholder menu item that can receive managed-site-specific menu contributions.
- **Managed Navigation Contribution**: Managed-site-owned navigation entries assigned to site blueprint placeholders and rendered only within that managed site scope.
- **Blueprint Page**: A Site Blueprint page that provides common page content and declares whether managed-site page overrides are allowed.
- **Page Override Policy**: The blueprint-level rule that determines whether a specific blueprint page can be replaced for a managed site.
- **Managed-Site Page Override**: Managed-site-owned content that replaces an overrideable blueprint page for one managed site.
- **Layer Contribution Point**: A blueprint-defined location where managed sites can add supplemental content items without replacing the page.
- **Managed-Site Layer Contribution**: Managed-site-owned content assigned to a blueprint layer contribution point.
- **Managed-Site Placeholder**: A blueprint content area intended to be filled by managed-site-specific content items.
- **Placeholder Assignment**: A relationship between a managed site, a managed-site placeholder, and one or more managed-site content items.
- **Composition Precedence Rule**: The ordered rule set that keeps layout layer contributions independent while determining whether blueprint page content, managed-site page override content, placeholder content, or blueprint fallback content appears in the final response.

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
- **SC-010**: 100% of portal preview requests display the composed page for the active managed site using the same blueprint and managed-site content rules as a published request.
- **SC-011**: 100% of non-overrideable blueprint pages reject managed-site page override attempts.
- **SC-012**: 100% of managed-site page overrides render only for their owning managed site.
- **SC-013**: 100% of managed-site layer contributions render only for their owning managed site.
- **SC-014**: 100% of managed-site placeholder assignments render only for their owning managed site.
- **SC-015**: 95% of managed-site administrators can create and preview a page override in under 3 minutes after locating the target blueprint page.
- **SC-016**: 95% of managed-site administrators can assign content to an existing blueprint placeholder in under 2 minutes.
- **SC-017**: 100% of composed page previews match the same customization precedence rules used for published page rendering.
- **SC-018**: 100% of customization records affected by blueprint changes remain visible to authorized administrators for review or cleanup.
- **SC-019**: 100% of active managed-site page overrides suppress placeholders from the overridden blueprint page while preserving independently rendered layer contributions.
- **SC-020**: 100% of managed-site page overrides for pages later marked not overrideable stop rendering while remaining visible to authorized administrators for recovery or cleanup.
- **SC-021**: 100% of empty managed-site placeholders render blueprint fallback content when available and render empty when no fallback exists.

## Assumptions

- Existing OrchardCore user identities and role/permission management are reused for access control decisions.
- The Managed Site Admin Portal is implemented as a React front end.
- OrchardCore APIs required for content edit and page preview operations are available to the Managed Site Admin Portal under authenticated access.
- Managed-site clearance is provided to the portal through token claims and scopes.
- Site blueprint content and managed-site content use existing content lifecycle states (draft/published) and are composed from published versions.
- URL matching uses the same canonical request URL interpretation already used by the site.
- Initial scope covers one Site Blueprint with multiple managed sites under that blueprint context.
- Cross-managed-site content inheritance beyond site-blueprint-plus-managed-site composition is out of scope for this first version.
- Changes to URL registrations are expected to be auditable through existing operational logging capabilities.
- Final implementation uses a hybrid model combining route-scoped content entries and layer-based targeting where appropriate.
- Site Blueprint administrators control which pages can be overridden and where managed-site contributions are allowed.
- Page-level override replaces the blueprint page and suppresses placeholders defined on that blueprint page.
- Layer contributions are independent of the page and its placeholders; when items are defined in layers, the layout renders them according to layer behavior.
- A page can contribute to layers through page-level widget lists, but layer rendering is not mutually exclusive with page rendering or placeholder rendering.
- Placeholder assignments fill specific blueprint-defined content areas; when no managed-site content is assigned, the placeholder uses blueprint fallback content when available and renders empty otherwise.
- Customization auditability and recovery use the same administrative visibility expectations as other managed-site records.

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
- **Blueprint Page Override Policies**
- **Managed-Site Page Overrides**
- **Managed-Site Layer Contributions**
- **Managed-Site Placeholder Assignments**
- **Customization Composition Precedence**

### Managed-Site Customization Terms

- **Blueprint Page Override**: A managed-site-specific replacement for an overrideable blueprint page.
- **Layer Contribution Point**: A blueprint-defined place for managed-site supplemental content.
- **Managed-Site Placeholder**: A blueprint-defined content slot filled by managed-site content.
- **Placeholder Assignment**: The managed-site-specific content selected for a placeholder.

### Custom Module Naming Prefix

- New custom modules for this feature use the **VendallionCMS** prefix.
- Planned module family naming: **VendallionCMS.ManagedSites**.