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
- Q: How is managed-site clearance represented? → A: Signed authorization claims and scopes.
- Q: How should preview compose managed-site pages? → A: OrchardCore preview pipeline simulation.
- Q: When a managed site has more than one customization for the same blueprint page, which composition order should determine the final page? → A: Layer items are independent of page overrides; if a blueprint page is overridden, the blueprint page placeholders are not rendered. **(Superseded by Session 2026-09-14.)**
- Q: What should happen to existing managed-site page overrides when a Site Blueprint administrator later marks the blueprint page as not overrideable? → A: Existing overrides stop rendering but remain recoverable for admin review. **(Generalized by Session 2026-09-14.)**
- Q: When a managed-site placeholder has no assigned managed-site content, what should render in that placeholder? → A: Render blueprint fallback content if defined; otherwise render empty. **(Superseded by Session 2026-09-14.)**
- Q: What is the authority for resolving the active Managed Site in public requests and administrative service requests? → A: Public requests resolve Managed Site context from the incoming URL; administrative service requests use their requested Managed Site scope authorized by signed clearance and active portal session, with any client-provided Managed Site scope metadata treated only as optional consistency metadata.

### Session 2026-09-14

- Q: Should managed-site customization keep four separate mechanisms (page overrides, navigation contributions, layer contributions, placeholder assignments)? → A: No. Replace all four with a single reusable Managed Content capability attached to content items.
- Q: What determines whether a managed site may customize a given content item? → A: Two independent scopes configured per content item by the Site Blueprint administrator: an edit scope listing which managed sites may override the item, and a display scope listing which request contexts render the item. **(Qualified by Session 2026-09-20.)**
- Q: How is managed-site override content stored? → A: As a separate content item of the same content type, owned by the managed site and linked to the source content item, so the existing draft and publish lifecycle applies per managed site.
- Q: How can a managed site add content where the Site Blueprint placed none? → A: By overriding a container content item and supplying its own child items inside that override. Standalone managed-site items outside any blueprint-placed container are out of scope for this version.
- Q: What renders when no managed site is resolved for the request? → A: The original content, subject to the item's display scope, which can include or exclude the Site Blueprint context explicitly.

### Session 2026-09-17

- Q: How is a Managed Site addressed? → A: The same way an OrchardCore tenant is, by a Hostname holding one or more host names and a single URL Prefix. A Managed Site no longer keeps a list of arbitrary address entries.
- Q: Should a site be explicitly designated as a Site Blueprint, with its own identifier and name? → A: No. Enabling the Managed Sites feature is the designation. The tenant holds the common content and the Managed Sites together, so there is nothing to toggle and nothing for a Managed Site to point at. The designation setting, the blueprint identifier, and the blueprint entity are removed; "Site Blueprint" remains only as the name of the tenant's common-content context.

### Session 2026-09-20

- Q: May a managed site be given the right to override an item it is not shown? → A: No. The display scope must cover the edit scope. Granting the override marks the managed site as seeing the item and fixes that control, and the rule is reapplied when the change is persisted. The display scope may still reach wider than the edit scope, and a managed site leaves the display scope by leaving the edit scope.
- Q: What authorizes a managed-site editor to author the content item that holds their override? → A: Their managed-site clearance, evaluated as a content authorization decision on items owned by that managed site. Requiring tenant-wide content permissions instead would hand every managed-site editor the ability to change Site Blueprint content, which FR-011 forbids. The rule is enforced wherever content authorization is asked, not only on the portal endpoints.
- Q: How are the time-to-complete success criteria verified? → A: By a usability walkthrough recorded during polish. They stay measurable targets rather than becoming aspirational notes.
- Q: Does a managed site's URL Prefix still apply when it also names host names? → A: No. A host name claims every path on that host and the prefix is not consulted. The prefix exists so several managed sites can share one host, so it has work to do only for a managed site that named no host of its own. This differs from OrchardCore tenant matching, which keys a tenant on host and prefix together; the divergence is deliberate, because a managed site with both configured would otherwise answer only under a path where no content is routed.
- Q: Does a managed site's URL Prefix move out of the request path? → A: Yes, onto the request path base, the way a tenant's own prefix does, so the content routes underneath resolve unchanged and generated links carry the prefix back. Only a prefix that was actually matched is moved, so a managed site resolved by host name has nothing stripped.
- Q: Are all content items carrying managed content stored in their own right? → A: No. A page section is a content item held inside its page, and sections carry managed content far more often than pages do. Managed content addresses an item by its own identifier together with the stored item that holds it, so discovery, override, and suppression all reach contained items.
- Q: What lifecycle states does a managed site have? → A: Two, Enabled and Disabled. A managed site is either serving or switched off. The earlier Draft and Archived states were removed: Draft meant a site nobody could author for, because the portal only offers enabled sites, and Archived was indistinguishable from Disabled in every rule that consulted it. A new managed site is Enabled.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage Site Blueprint Content (Priority: P1)

As a site blueprint content manager, I can configure blueprint content so common content is centrally managed and available across all managed-site request contexts.

**Why this priority**: Blueprint content is the foundation for consistent experiences and is required before managed-site composition can provide value.

**Independent Test**: Can be fully tested by assigning a user site blueprint management rights, updating blueprint content, and confirming the updated blueprint content is available where expected.

**Acceptance Scenarios**:

1. **Given** a site is marked as a Site Blueprint and a user has site blueprint management access, **When** the user edits blueprint content and publishes changes, **Then** the blueprint content is saved and available for request composition.
2. **Given** a user without site blueprint management access, **When** they attempt to modify blueprint content, **Then** the action is denied and blueprint content remains unchanged.

---

### User Story 2 - Define and Manage Managed Site Content By Address (Priority: P2)

As a managed-site content editor, I can define a managed site, give it a host name and a URL prefix the way a tenant is addressed, and manage content for that managed site so requests to that address return context-specific content.

**Why this priority**: Address-scoped managed-site ownership enables delegated content management without giving full site blueprint control.

**Independent Test**: Can be fully tested by creating one managed site, giving it a host name or URL prefix, assigning an editor, and verifying that only the assigned editor can manage that managed site's content.

**Acceptance Scenarios**:

1. **Given** a managed site exists with an address and an authorized managed-site editor, **When** the editor updates managed-site content, **Then** the change is saved under that managed site.
2. **Given** a user who is not authorized for a managed site, **When** they attempt to modify that managed site's content, **Then** the action is denied.
3. **Given** an address is claimed by no managed site, **When** a request arrives for it, **Then** it resolves to the Site Blueprint context.

---

### User Story 3 - Compose Request Content from Site Blueprint and Managed Site Layers (Priority: P3)

As an end user visiting a URL, I receive content composed from site blueprint content and, when applicable, managed-site content for the matched managed site.

**Why this priority**: Composition behavior delivers the primary end-user outcome and validates the value of both site blueprint and managed-site editing models.

**Independent Test**: Can be fully tested by preparing blueprint content and managed-site content for a registered URL, requesting that URL, and confirming the response reflects the correct content for that managed site.

**Acceptance Scenarios**:

1. **Given** a request URL is registered to a managed site, **When** the request is processed, **Then** the response resolves each content item to the managed-site content for that matched managed site where it exists, and to blueprint content otherwise.
2. **Given** a request URL is not registered to a managed site, **When** the request is processed, **Then** the response includes blueprint content only, subject to each item's display scope.
3. **Given** blueprint content and managed-site content are both updated, **When** the same URL is requested after publication, **Then** the response reflects the latest published versions of both content layers.

---

### User Story 4 - Manage Content Through the Managed Site Admin Portal Session (Priority: P2)

As a content editor with managed-site clearance, I can log in to the Managed Site Admin Portal, select the managed site I am working on for my session, and use platform content services to edit managed-site content and preview pages.

**Why this priority**: Editors need a controlled authoring entry point with clear site scope so updates and previews are always applied to the correct managed site.

**Independent Test**: Can be fully tested by signing in as a user with access to multiple managed sites, selecting an active managed site, editing managed-site content through the portal, previewing a page, and verifying all actions remain within the selected site scope.

**Acceptance Scenarios**:

1. **Given** a user has clearance to one managed site, **When** the user signs in, **Then** the session is automatically scoped to that managed site.
2. **Given** a user has clearance to more than one managed site, **When** the user signs in, **Then** the user must select which managed site to manage for the current session before editing content.
3. **Given** an active managed site is selected for the session, **When** the user updates content or previews a page, **Then** the portal performs those actions only within that active managed-site scope through platform content services.

---

### User Story 5 - Scope Blueprint Content to Managed Sites With Managed Content (Priority: P2)

As a site blueprint administrator, I can mark any content item as managed content and declare which managed sites may override it and which request contexts display it, so one governance mechanism covers pages, navigation entries, layer widgets, and any other content type.

**Why this priority**: This is the single control surface that replaces separate page override, navigation, layer, and placeholder mechanisms. Every managed-site customization depends on it.

**Independent Test**: Can be fully tested by attaching managed content to a content type, configuring edit and display scopes on one content item, and verifying that only the named managed sites can override it and only the named contexts render it.

**Acceptance Scenarios**:

1. **Given** a content type has managed content attached, **When** a blueprint administrator edits a content item of that type, **Then** the administrator can set the edit scope and the display scope for that item.
2. **Given** an item's edit scope names two managed sites, **When** a third managed site's administrator opens the portal, **Then** that item is not listed as editable for the third managed site.
3. **Given** an item's display scope excludes a managed site, **When** a request resolves to that managed site, **Then** the item does not render for that request.
4. **Given** a managed-site administrator without blueprint management access, **When** they attempt to change an item's edit scope or display scope, **Then** the action is denied.
5. **Given** a content item without managed content attached, **When** any request renders it, **Then** the item renders exactly as it did before the feature was enabled.

---

### User Story 6 - Override Managed Content for a Managed Site (Priority: P2)

As a managed-site administrator, I can discover every content item I am allowed to customize, provide my own version of it, and have my version served for my managed site only.

**Why this priority**: This delivers the end-user visible customization and validates that one override mechanism serves all content types.

**Independent Test**: Can be fully tested by listing editable items in the portal, overriding one item for one managed site using an account with managed-site clearance only, and confirming that only that managed site receives the overridden content while all other contexts receive the original.

**Acceptance Scenarios**:

1. **Given** an item's edit scope includes the active managed site, **When** the administrator opens the portal content list, **Then** the item appears with its current override status.
2. **Given** an editable item has no override, **When** the administrator creates and publishes an override, **Then** requests resolving to that managed site render the override instead of the original content.
3. **Given** two managed sites can both edit an item and only one has published an override, **When** the item renders in each managed-site context, **Then** only the managed site with the override receives the overridden content.
4. **Given** a published override exists, **When** the blueprint administrator removes that managed site from the item's edit scope, **Then** the override stops rendering and remains visible to authorized administrators for recovery.
5. **Given** a container item is overridden, **When** the page renders for the owning managed site, **Then** the container's child items come from the override and the original children do not render.
6. **Given** a managed-site administrator holding clearance for their managed site and no tenant-wide content permissions, **When** they create, edit, and publish the content item that holds their override, **Then** the actions are allowed for that item and denied for the blueprint item it replaces and for any other managed site's override.

---

### Edge Cases

- A URL is accidentally registered to multiple managed sites; the system must prevent ambiguous routing assignments.
- A previously registered managed-site URL is removed; subsequent requests to that URL should no longer resolve to that managed site.
- A managed site is disabled or no longer accessible to editors; existing blueprint content delivery must continue without regression.
- Blueprint content for a URL exists, but no override exists for the matched managed site; the response should render the original blueprint content.
- A managed-site editor loses access while edits are in progress; unauthorized updates must not be applied.
- A user's token contains no managed-site clearance claims; the Managed Site Admin Portal must deny managed-site editing access.
- A user's managed-site clearance changes during an active session; subsequent privileged actions must honor the updated clearance before content changes are accepted.
- A managed site is removed from an item's edit scope after it published an override; the override must stop rendering but remain administratively reviewable and recoverable.
- A managed site that holds an override is removed from an item's display scope, which per FR-028a means it also leaves the edit scope; the item must not render for that managed site and the override must remain recoverable.
- The source content item for an existing override is unpublished or deleted; the override must stop rendering and must remain recoverable without being served in an invalid context.
- Managed content is detached from a content type while overrides exist; existing overrides must stop rendering and remain recoverable.
- A container item is overridden and the original container's children carry their own overrides; only content reachable through the active override may render.
- An item is both outside the display scope and has a published override for the same managed site; display scope must win and nothing renders.
- Display scope excludes the Site Blueprint context; requests that resolve to no managed site must not render the item.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Enabling the Managed Sites feature on a tenant MUST make that tenant the Site Blueprint, holding the common content that Managed Sites customize. No separate designation step, identifier, or name is required.
- **FR-002**: The system MUST support a permission model where only authorized users can manage the tenant's common content and configure which Managed Sites may customize it.
- **FR-003**: The system MUST introduce a managed-site concept that groups content under a child site classification.
- **FR-004**: Each managed site MUST be addressed by a Hostname holding zero or more host names and a single URL Prefix. A managed site that names host names answers every path on those hosts, and its URL Prefix is not consulted. A managed site that names no host name answers the paths under its URL Prefix on every host the tenant serves, and an empty prefix there means every path.
- **FR-004a**: A managed site that names host names MUST expand to one address per host name, carrying no prefix. A managed site that names none MUST expand to a single host-agnostic address carrying its prefix.
- **FR-005**: The system MUST support managed-site-specific permissions so only authorized users can manage that managed site's content.
- **FR-006**: When a managed site is saved or removed, the system MUST update the tenant Hostname setting to withdraw the host names that managed site no longer carries and to include the host names it now carries, leaving host names the operator configured on the tenant untouched.
- **FR-006a**: The system MUST add managed-site host names to the tenant Hostname even when it is currently empty. A tenant then answers only on the host names it declares, so every host it must serve has to be declared, including the one operators browse during development.
- **FR-006b**: The system MUST NOT empty a tenant Hostname that was previously set while the tenant URL Prefix is also empty, because the tenant would become a catch-all and could answer for addresses meant for other tenants. When the tenant carries a URL Prefix, emptying the Hostname is permitted: the prefix still distinguishes the tenant, so it does not become a catch-all.
- **FR-006c**: The system MUST NOT change the tenant URL Prefix. A tenant carries one prefix for all of its content, so managed-site prefixes are resolved inside the tenant.
- **FR-007**: The system MUST resolve an incoming request to a managed site when the request host matches one of that managed site's host names, whatever the path; or, for a managed site naming no host name, when the request path falls under its URL Prefix.
- **FR-008**: The system MUST compose response content by resolving each managed content item to the matched managed site's override when one exists, and to the original content otherwise.
- **FR-009**: The system MUST serve original blueprint content when no managed site is matched for the request URL.
- **FR-010**: The system MUST prevent two managed sites from claiming the same address. Two managed sites naming host names collide when they share a host name, whatever their URL Prefixes, because a prefix is not consulted once a host matches. Two managed sites naming no host name collide when their URL Prefixes are equal. A managed site naming host names never collides with one naming none, because precedence divides them.
- **FR-010a**: When more than one managed site could answer a request, the system MUST prefer the managed site whose Hostname names the request host over one that answers on every host, and among managed sites naming no host name, the one with the longer URL Prefix.
- **FR-011**: The system MUST ensure unauthorized users cannot create, edit, or publish content outside their granted site blueprint or managed-site scope.
- **FR-011a**: Managed-site clearance MUST itself authorize content actions on that managed site's own override content, so a managed-site editor can author an override without holding tenant-wide content permissions. That clearance MUST NOT authorize any action on Site Blueprint content or on another managed site's override content, whether the request arrives through the Managed Site Admin Portal or through any other content interface.
- **FR-012**: The system MUST reflect published updates to site blueprint and managed-site content in subsequent requests without requiring manual shell URL refresh actions.
- **FR-013**: The solution MUST provide a Managed Site Admin Portal for editors to manage Managed Site content only.
- **FR-014**: The Managed Site Admin Portal MUST be a web-based authoring experience.
- **FR-015**: The Managed Site Admin Portal MUST authenticate users before allowing access to content management functions.
- **FR-016**: The Managed Site Admin Portal MUST use platform content services for managed-site content updates and page preview operations.
- **FR-017**: The system MUST maintain managed-site clearances per user from signed authorization claims and scopes and expose only authorized managed sites to that user in the portal.
- **FR-018**: If a user has clearance to more than one managed site, the user MUST select one managed site as the active scope for the current session before performing content actions.
- **FR-019**: If a user has clearance to exactly one managed site, the system MUST automatically set that managed site as the active scope for the session.
- **FR-020**: Content edits and preview actions initiated from the Managed Site Admin Portal MUST execute only against the active managed site selected for the session.
- **FR-021**: Public rendering requests MUST resolve the active Managed Site from the incoming URL and store the result in request-scoped Managed Site context.
- **FR-022**: Public rendering requests MUST NOT trust client-provided Managed Site scope metadata for Managed Site resolution.
- **FR-023**: Administrative service requests MUST validate that the requested Managed Site scope, active portal session scope, and signed authorization clearance all refer to the same authorized Managed Site; when client-provided Managed Site scope metadata is present, it MUST match the requested Managed Site scope.

#### Managed Content Capability

- **FR-024**: The system MUST provide a single reusable Managed Content capability that Site Blueprint administrators can attach to any content type.
- **FR-025**: The Managed Content capability MUST be the only mechanism for defining managed-site-specific variations of blueprint content.
- **FR-026**: For each content item carrying Managed Content, the system MUST allow Site Blueprint administrators to define an edit scope naming which managed sites may override that item.
- **FR-027**: For each content item carrying Managed Content, the system MUST allow Site Blueprint administrators to define a display scope naming which request contexts render that item, where the available contexts are the Site Blueprint context and each managed site.
- **FR-028**: Edit scope and display scope MUST each support all managed sites, an explicitly named subset, or none, and MUST be separately configurable, subject to FR-028a.
- **FR-028a**: The display scope MUST cover the edit scope. When a Site Blueprint administrator grants a managed site the right to override an item, the system MUST also include that managed site in the display scope, MUST present that inclusion as already made and not changeable on its own, and MUST apply it when the change is persisted even if the display control was never submitted. The display scope MAY still reach wider than the edit scope. A managed site leaves the display scope by leaving the edit scope.
- **FR-029**: The default display scope for a newly attached Managed Content item MUST include every context, so attaching the capability alone does not change rendering behavior.
- **FR-030**: The system MUST prevent managed-site administrators from changing edit scope, display scope, or content type attachment unless they also have Site Blueprint management access.
- **FR-031**: Content items that do not carry Managed Content MUST render and behave exactly as they did before the feature was enabled.

#### Managed Content Overrides

- **FR-032**: The Managed Site Admin Portal MUST list every content item whose edit scope includes the active managed site, together with that item's current override status.
- **FR-033**: Managed-site administrators MUST be able to create, update, and publish an override only for items whose edit scope includes their active managed site. Creating the override content MUST be possible with managed-site clearance alone.
- **FR-034**: A managed-content override MUST apply only to the managed site that owns it.
- **FR-035**: At most one active published override MUST exist per managed site and source content item. The system MUST refuse a request that would create a second one.
- **FR-035a**: If more than one published override nonetheless exists for a managed site and source content item, because content was imported or recipe-deployed rather than created through the API, rendering MUST resolve to one of them deterministically rather than arbitrarily, and the surplus MUST be visible to administrators for cleanup.
- **FR-036**: Overrides MUST use the existing content draft and publish lifecycle so each managed site can hold unpublished work without affecting rendered output.
- **FR-037**: When a request resolves to a managed site excluded by an item's display scope, the item MUST NOT render, regardless of whether an override exists.
- **FR-038**: When a request resolves to a managed site included by the display scope and a valid published override exists, the system MUST render the override content in place of the original content.
- **FR-039**: When a request resolves to a managed site included by the display scope and no valid override exists, the system MUST render the original content.
- **FR-040**: When a request resolves to no managed site, the system MUST render original content for items whose display scope includes the Site Blueprint context, and MUST NOT render items that exclude it.
- **FR-041**: When a container item is overridden, the child items reachable through the override MUST render and the original container's children MUST NOT render.
- **FR-042**: Override resolution MUST be evaluated independently per content item, so no cross-item precedence ordering is required to determine composed output.

#### Recoverability and Lifecycle

- **FR-043**: When a managed site is removed from an item's edit scope, existing overrides owned by that managed site MUST stop rendering and MUST remain recoverable for authorized administrator review.
- **FR-044**: When a source content item is unpublished or deleted, overrides targeting it MUST stop rendering and MUST remain recoverable.
- **FR-045**: When Managed Content is detached from a content type, existing overrides MUST stop rendering and MUST remain recoverable.
- **FR-046**: Overrides that stop rendering for any reason MUST remain visible to authorized administrators for review, reassignment, or cleanup.
- **FR-047**: The system MUST report why an override is not rendering, whenever an administrator reads it, so they can distinguish scope removal, source removal, and capability detachment. The reason MUST reflect the current state rather than the state when the override was last written.

#### Preview and Cache

- **FR-048**: Page preview from the Managed Site Admin Portal MUST use the platform's existing preview capability while simulating the Managed Site request composition pipeline for the active Managed Site URL context.
- **FR-049**: Preview MUST show the composed output for the active managed site, including draft overrides visible to the current user.
- **FR-050**: Published changes to Site Blueprint content, managed-content overrides, edit scope, display scope, and URL registrations MUST use platform cache infrastructure to invalidate affected composition state so subsequent matching requests reflect the published change according to configured cache management settings.
- **FR-051**: The solution MUST preserve compatibility with existing platform URL routing behavior so managed-site URL resolution remains consistent with existing site URL handling expectations.

### Key Entities *(include if feature involves data)*

- **Site Blueprint**: The tenant's own context, holding the common content and content structure that Managed Sites customize. It is implicit in the module being enabled rather than a stored record.
- **Managed Site**: A child site context with its own URL registrations, editor access scope, and managed-site-specific content overrides.
- **Managed Site Address**: The pairing of one host name with a managed site URL Prefix. A managed site with several host names has several addresses, exactly as a tenant does.
- **Content Scope Permission**: Authorization boundaries that determine who can manage site blueprint content versus a specific managed site's content.
- **Managed Site Clearance**: Signed authorization claims and scopes that determine which managed sites a user can access in the Managed Site Admin Portal.
- **Active Managed Site Session Scope**: The managed site selected by the user (or auto-selected when only one is available) that constrains all editing and preview actions during the current session.
- **Managed Site Request Context**: The request-scoped Managed Site resolved from an incoming public URL and used by rendering and composition services.
- **Managed Content**: The capability attached to a content item that declares the item customizable per managed site and carries its edit scope and display scope.
- **Edit Scope**: The set of managed sites permitted to create an override for a given managed content item.
- **Display Scope**: The set of request contexts, including the Site Blueprint context, in which a given managed content item renders.
- **Managed Content Override**: Managed-site-owned content that replaces the original content of one managed content item for one managed site.
- **Override Suppression Reason**: The recorded cause when an override exists but does not render, such as scope removal, source removal, or capability detachment.
- **Composed Response**: The final delivered content assembled by resolving each managed content item to an override or to original content for the matched request context.
- **Composition Cache State**: The cache tags, signals, dependencies, and invalidation state used to ensure composed responses reflect recently published blueprint or managed-site changes according to configured cache management settings.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of URL requests mapped to managed sites resolve each managed content item to that managed site's override when one exists.
- **SC-002**: 100% of URL requests not mapped to managed sites return blueprint content without managed-site leakage.
- **SC-003**: 100% of unauthorized edit attempts on site blueprint or managed-site content are blocked with no persisted content change.
- **SC-004**: 100% of published content updates are visible on subsequent matching requests immediately after platform cache invalidation completes according to configured cache management settings.
- **SC-005**: Site administrators can complete site blueprint or managed-site URL mapping changes in under 3 minutes for standard update tasks.
- **SC-006**: 100% of content edits and preview operations from the Managed Site Admin Portal are executed in the managed-site scope selected for the user's session.
- **SC-007**: 100% of public rendering requests derive Managed Site context from URL resolution rather than client-provided scope metadata.
- **SC-008**: 100% of administrative service requests with mismatched requested scope, session scope, signed clearance, or optional Managed Site scope metadata are rejected without changing content.
- **SC-009**: 95% of users with access to multiple managed sites can complete sign-in and active-site selection in under 60 seconds.
- **SC-010**: 100% of content items carrying managed content expose both an edit scope and a display scope to blueprint administrators.
- **SC-011**: 100% of attempts by managed-site administrators to change edit scope or display scope are rejected without blueprint management access.
- **SC-012**: 100% of content items without managed content attached render identically before and after the feature is enabled.
- **SC-013**: 100% of items listed as editable in the portal have an edit scope that includes the active managed site.
- **SC-014**: 100% of override attempts on items outside the active managed site's edit scope are rejected without persisting content.
- **SC-015**: 100% of published managed-content overrides render only for their owning managed site.
- **SC-016**: 100% of items excluded by display scope render nothing for the excluded context, including when an override exists.
- **SC-017**: 100% of composed responses are produced without applying cross-item precedence ordering.
- **SC-018**: 100% of overrides that stop rendering remain visible to authorized administrators with a recorded suppression reason.
- **SC-019**: 100% of composed page previews match the same resolution rules used for published page rendering.
- **SC-020**: 95% of managed-site administrators can locate an editable item and publish an override in under 3 minutes.
- **SC-021**: 100% of overridden container items render their override's children and none of the original container's children.

## Assumptions

- Existing OrchardCore user identities and role/permission management are reused for access control decisions.
- Common content, content structure, and managed content configuration are authored in the standard OrchardCore admin UI; the Managed Site Admin Portal is not used for authoring them.
- A tenant is a Site Blueprint precisely when the Managed Sites feature is enabled on it, so there is no stored blueprint record and a Managed Site carries no blueprint identifier.
- The Managed Site Admin Portal is a browser-based authoring experience used exclusively by managed-site editors and administrators.
- Platform content services required for content edit and page preview operations are available to the Managed Site Admin Portal under authenticated access.
- Managed-site clearance is provided to the portal through signed authorization claims and scopes.
- Public page rendering resolves Managed Site context from the incoming URL; optional Managed Site scope metadata is used only by administrative service requests as consistency metadata.
- Site blueprint content and managed-site overrides use existing content lifecycle states (draft/published) and are composed from published versions.
- URL matching uses the same canonical request URL interpretation already used by the site.
- Initial scope covers one tenant acting as the Site Blueprint with multiple managed sites inside it.
- Cross-managed-site content inheritance beyond blueprint-plus-override resolution is out of scope for this first version.
- Changes to URL registrations are expected to be auditable through existing operational logging capabilities.
- Pages, navigation entries, layer widgets, and page sections are all content items in the platform, so one managed content capability covers every customization case that previously required a dedicated mechanism. Not all of them are stored in their own right: a page section is a content item held inside its page, so managed content must address an item through whatever stores it rather than assume every item is a document.
- The original content of an item is its own fallback; no separate fallback content configuration is required because an item without an override renders its original content.
- A managed site can add content only where the Site Blueprint placed a container item carrying managed content; the managed site overrides that container and supplies its own child items. Standalone managed-site items outside any blueprint-placed container are out of scope for this version.
- Override content items use the same content type as their source item so editors see a familiar editor and the platform validates content consistently.
- Managed content is opt-in per content type, so existing sites are unaffected until a blueprint administrator attaches it.

## Out of Scope

- Managed-site creation of content items that are not overrides of a blueprint-placed managed content item.
- Partial or field-level merging between original content and override content; an override replaces the item's content.
- Cross-managed-site inheritance, where one managed site derives content from another.
- Managed-site authoring of blueprint structure, content type definitions, or managed content scope configuration.

## Naming Decisions

### Core Domain Terms

- **Site Blueprint**: The primary site that owns common content and content structure.
- **Managed Site**: A child site scope that owns URL-specific and audience-specific content overrides.

### Capability Group Names

- **Managed Site Definitions**
- **Managed Site URL Registry**
- **Managed Content Scoping**
- **Managed Content Overrides**
- **Managed Site Scope Permissions**
- **Shell URL Synchronization**

### Managed Content Terms

- **Managed Content**: The capability attached to a content item that makes it customizable per managed site.
- **Edit Scope**: Which managed sites may override the item.
- **Display Scope**: Which request contexts render the item.
- **Managed Content Override**: The managed-site-specific replacement content for one item.

### Custom Module Naming Prefix

- New custom modules for this feature use the **VendallionCMS** prefix.
- Planned module family naming: **VendallionCMS.ManagedSites**.
