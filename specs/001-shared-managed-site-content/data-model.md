# Data Model: Site Blueprint and Managed Site Content

## Site Blueprint

The tenant's own context. It is not a stored record: a tenant is the Site Blueprint exactly when the
Managed Sites feature is enabled on it. The tenant owns the common content, the content structure, and
the managed content configuration that Managed Sites customize, and it owns any URL registration not
claimed by a Managed Site.

## Managed Site

Represents a child site scope that owns URL registrations, scoped content, and managed content overrides.

**Fields**:

- `Id`: Stable managed-site identifier.
- `Name`: Human-readable name.
- `Hostname`: Zero or more host names this Managed Site answers on, held as one separator-delimited value exactly as a tenant holds its request hosts. Empty answers on every host the tenant serves.
- `UrlPrefix`: A single URL path prefix, empty for the root. Managed Sites are addressed like tenants, so there is one prefix rather than a list of paths.
- `Status`: Draft, Enabled, Disabled, or Archived.

**Relationships**:

- Belongs to the tenant acting as the Site Blueprint.
- Has many Managed Site Clearances.
- Has many Managed Content Overrides.
- Is referenced by the Edit Scope and Display Scope of many Managed Content items.

**Validation Rules**:

- Name is required and unique within the tenant.
- Host names and the URL Prefix are normalized before comparison.
- A Managed Site expands to one address per host name paired with its URL Prefix, or to a single host-agnostic address when the Hostname is empty.
- Two Managed Sites conflict when any of their addresses collide: equal prefixes whose host names overlap, where an empty host name overlaps every host.
- A Managed Site naming the request host takes precedence over one that answers on every host.
- Disabled Managed Sites must not allow new editor mutations.

## Tenant Hostname Synchronization

Tracks the host names this module has contributed to the tenant Hostname setting, so a later save can
withdraw exactly those and leave operator-configured host names in place.

**Fields**:

- `AppliedHostnames`: The host names the module last wrote into the tenant Hostname setting.

**Validation Rules**:

- On save or delete, the tenant Hostname becomes the operator host names plus the host names of every enabled Managed Site, where the operator ones are the current entries this module did not previously apply.
- Host names are added even when the tenant Hostname is currently empty, so the tenant then answers only on the host names it declares.
- A tenant Hostname is never emptied while the tenant URL Prefix is also empty, which would turn it into a catch-all. With a URL Prefix set, emptying the Hostname is permitted because the prefix still distinguishes the tenant.
- The tenant URL Prefix is never changed, because a tenant carries one prefix for all of its content.
- Persisting the tenant Hostname reloads the tenant, so the write is deferred until the current request completes and happens only when the resulting value differs.

## Managed Site Clearance

Represents token claims/scopes assigning a user to one or more Managed Sites.

**Fields**:

- `UserId`: User identity reference.
- `ManagedSiteId`: Managed Site identifier.
- `Scopes`: Allowed actions such as view, edit, publish, or preview.
- `EffectiveFrom`: Optional start time.
- `EffectiveTo`: Optional expiration time.

**Relationships**:

- Belongs to one user and one Managed Site.
- Drives active Managed Site selection in the portal.

**Validation Rules**:

- Clearance must reference an enabled Managed Site for active editing.
- API mutations must verify token-derived clearance for the requested Managed Site.

## Content Scope Permission

Represents authorization boundaries for managing Site Blueprint content and Managed Site content.

**Fields**:

- `Name`: Permission identifier.
- `ScopeType`: SiteBlueprint or ManagedSite.
- `AllowedActions`: Actions such as view, edit, publish, or configure managed content.

**Relationships**:

- Used by Site Blueprint management access and Managed Site Clearance.
- Evaluated by administrative services before content changes are accepted.

**Validation Rules**:

- Site Blueprint permissions apply only to blueprint-level configuration and content.
- Managed Site permissions apply only within authorized Managed Site scope.
- Configuring Edit Scope or Display Scope requires Site Blueprint management access.
- Client-visible filtering must not replace server-side permission checks.

## Active Managed Site Session Scope

Represents the Managed Site selected for the user's current portal session.

**Fields**:

- `UserId`: User identity reference.
- `ManagedSiteId`: Selected Managed Site.
- `SelectedAt`: Selection timestamp.

**State Transitions**:

- None -> Selected when one authorized site is auto-selected or a multi-site user chooses one.
- Selected -> Changed when the user chooses another authorized Managed Site.
- Selected -> Invalid when clearance is removed or the Managed Site is disabled.

**Validation Rules**:

- Selected Managed Site must appear in the user's current token claims/scopes.
- Privileged actions must fail if the selected scope is invalid.

## Managed Content

The capability attached to a content item that declares the item customizable per Managed Site. It carries the two scopes that govern editing and rendering. Attaching it to a content type is a content-definition change; configuring it happens per content item in the standard admin UI.

**Fields**:

- `ContentItemId`: The source content item carrying the capability.
- `EditScopeMode`: All, Selected, or None.
- `EditScopeManagedSiteIds`: Managed Sites allowed to override when mode is Selected.
- `DisplayScopeMode`: All, Selected, or None.
- `DisplayScopeManagedSiteIds`: Managed Sites that render the item when mode is Selected.
- `DisplayInBlueprintContext`: Whether the item renders when no Managed Site is resolved.

**Relationships**:

- Belongs to one source content item of any content type.
- Has many Managed Content Overrides, at most one active published override per Managed Site.

**State Transitions**:

- Attached -> Configured when a blueprint administrator sets either scope.
- Configured -> Detached when the capability is removed from the content type, which suppresses dependent overrides while keeping them recoverable.

**Validation Rules**:

- Only users with Site Blueprint management access may change either scope.
- Default on attach is `DisplayScopeMode` All with `DisplayInBlueprintContext` true, so attaching alone does not change rendering.
- Default on attach is `EditScopeMode` None, so no Managed Site gains edit rights implicitly.
- Display Scope must cover Edit Scope. Saving a Managed Content item widens the Display Scope to reach every Managed Site its Edit Scope allows to override, so no Managed Site edits content its own visitors never see. The editor shows that coverage as fixed, and the widening is applied on persist whether or not the display control was submitted.
- Display Scope may still reach wider than Edit Scope; an item can be visible everywhere while no Managed Site may override it. A Managed Site leaves the Display Scope by leaving the Edit Scope, and its override remains recoverable while suppressed.
- Referenced Managed Site identifiers must exist in the tenant.

## Managed Content Override

Managed-site-owned content that replaces the original content of one Managed Content item for one Managed Site. Stored as a separate content item of the same content type as the source, so the platform draft and publish lifecycle applies per Managed Site.

**Fields**:

- `Id`: Stable override identifier.
- `ManagedSiteId`: Owning Managed Site.
- `SourceContentItemId`: Target Managed Content item.
- `OverrideContentItemId`: Managed-site content item used as the override.
- `Status`: Draft, Published, Suppressed, or Recoverable.
- `SuppressionReason`: Why the override does not render, when applicable.

**State Transitions**:

- Draft -> Published when the managed-site administrator publishes the override.
- Published -> Suppressed when the owning Managed Site leaves the Edit Scope, the source item is unpublished or deleted, or Managed Content is detached from the type.
- Suppressed -> Recoverable for administrator review, reassignment, or cleanup.
- Suppressed -> Published when the suppressing condition is reversed and the override is still valid.

**Validation Rules**:

- The owning Managed Site must be in the source item's Edit Scope when the override is created or updated.
- Authoring the override content item is authorized by the owning Managed Site's clearance, which grants no authority over the source item or over another Managed Site's override.
- Exactly one active published override per Managed Site and source content item. A request that would create a second one is refused.
- If duplicates nonetheless exist, because content arrived by import or recipe rather than through the API, resolution picks one deterministically and the surplus stays visible for cleanup.
- The override content item must use the same content type as the source content item.
- A suppressed override never renders but remains readable by authorized administrators.
- Display Scope is evaluated before override resolution; an item outside Display Scope renders nothing even when a published override exists.

## Override Suppression Reason

Explains why an existing override does not render, so administrators can act on it.

**Values**:

- `EditScopeRemoved`: The owning Managed Site is no longer in the source item's Edit Scope.
- `SourceUnpublished`: The source content item is no longer published.
- `SourceDeleted`: The source content item no longer exists.
- `CapabilityDetached`: Managed Content was removed from the source content type.
- `ManagedSiteDisabled`: The owning Managed Site is disabled or archived.

**Validation Rules**:

- A suppression reason is required whenever status is Suppressed or Recoverable.
- Suppression reasons must be visible to authorized administrators in the portal.

## Managed Site Request Context

The request-scoped resolution result used by rendering and composition.

**Fields**:

- `ManagedSiteId`: Resolved Managed Site, or none for blueprint-only requests.
- `Url`: Canonical request URL used for resolution.
- `ResolutionSource`: UrlRegistration for public requests, or PortalSession for preview.

**Validation Rules**:

- Public requests must resolve only from URL registration and must ignore client-provided scope metadata.
- Preview requests must resolve from the active portal session scope authorized by signed clearance.

## Composition Cache State

Tracks cache dependencies and invalidation state for composed responses affected by published changes.

**Fields**:

- `ManagedSiteId`: Optional Managed Site scope affected by the change.
- `Url`: Optional URL affected by the change.
- `AffectedArea`: Content, UrlRegistration, ManagedContentScope, or Override.
- `CacheTags`: Cache tags associated with the affected composed output.
- `CacheSignals`: Signals associated with the affected composed output.
- `InvalidatedAt`: Timestamp when composition state became stale.
- `RefreshedAt`: Timestamp when composition state was refreshed.

**Validation Rules**:

- Published changes must trigger OrchardCore cache invalidation for every affected Managed Site or URL.
- Edit Scope and Display Scope changes must invalidate composed output for every Managed Site added or removed.
- Matching requests must reflect the published change immediately after the relevant cache invalidation completes according to configured cache management settings.
