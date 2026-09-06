# Data Model: Site Blueprint and Managed Site Content

## Site Blueprint

Represents the primary site context that owns common content, shared navigation structure, placeholders, page override policies, and layer contribution points.

**Fields**:
- `Id`: Stable blueprint identifier.
- `Name`: Human-readable name shown to administrators.
- `IsEnabled`: Whether blueprint composition is active.
- `UrlRegistrations`: URLs owned by the blueprint context.
- `PlaceholderDefinitions`: Menu and page placeholder definitions owned by the blueprint.
- `LayerContributionPoints`: Blueprint-defined layer contribution points available to Managed Sites.

**Relationships**:
- Has many Managed Sites.
- Has many URL Registrations.
- Has many Blueprint Pages.
- Owns Menu Placeholder and Managed-Site Placeholder definitions.

**Validation Rules**:
- Name is required and unique within the blueprint tenant.
- URL registrations must not conflict with Managed Site URL registrations.
- Placeholder identifiers must be stable and unique within the blueprint.

## Managed Site

Represents a child site scope that owns URL registrations, scoped content, navigation contributions, page overrides, layer contributions, and placeholder assignments.

**Fields**:
- `Id`: Stable managed-site identifier.
- `BlueprintId`: Parent Site Blueprint identifier.
- `Name`: Human-readable name.
- `UrlRegistrations`: URLs mapped to this Managed Site.
- `Status`: Draft, Enabled, Disabled, or Archived.

**Relationships**:
- Belongs to one Site Blueprint.
- Has many URL Registrations.
- Has many Managed Site Clearances.
- Has many Managed-Site Page Overrides, Layer Contributions, Placeholder Assignments, and Managed Navigation Contributions.

**Validation Rules**:
- Name is required and unique within its Site Blueprint.
- URL registrations must be unique across all Managed Sites under the same blueprint tenant.
- Disabled Managed Sites must not allow new editor mutations.

## URL Registration

Maps an incoming URL to the Site Blueprint context or a specific Managed Site.

**Fields**:
- `Id`: Stable registration identifier.
- `OwnerType`: SiteBlueprint or ManagedSite.
- `OwnerId`: Identifier of the owning blueprint or managed site.
- `Url`: Canonical URL path or host/path entry.
- `Status`: Active or Disabled.

**Relationships**:
- Belongs to either one Site Blueprint or one Managed Site.
- Updates shell URL state when active mappings change.

**Validation Rules**:
- URL is required and normalized before comparison.
- Active URL values must be unique in the blueprint tenant.
- Disabled URLs do not participate in managed-site resolution.

## Managed Site Clearance

Represents token claims/scopes assigning a user to one or more Managed Sites.

**Fields**:
- `UserId`: User identity reference.
- `ManagedSiteId`: Managed Site identifier.
- `Scopes`: Allowed actions such as view, edit, publish, preview, or manage navigation.
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
- `AllowedActions`: Actions such as view, edit, publish, manage navigation, manage placeholders, or manage overrides.

**Relationships**:
- Used by Site Blueprint management access and Managed Site Clearance.
- Evaluated by administrative services before content changes are accepted.

**Validation Rules**:
- Site Blueprint permissions apply only to blueprint-level configuration and content.
- Managed Site permissions apply only within authorized Managed Site scope.
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

## Blueprint Page

A Site Blueprint page that provides common page content and declares whether managed-site page overrides are allowed.

**Fields**:
- `ContentItemId`: Blueprint page content identifier.
- `BlueprintId`: Owning Site Blueprint.
- `AllowManagedSiteOverride`: Whether page-level overrides are allowed.
- `PlaceholderDefinitions`: Managed-site placeholders exposed by the page.
- `FallbackContent`: Optional fallback content for empty placeholders.

**Relationships**:
- May have many Managed-Site Page Overrides.
- May define many Managed-Site Placeholders.

**Validation Rules**:
- Managed-site overrides can be created only when `AllowManagedSiteOverride` is true.
- When `AllowManagedSiteOverride` changes to false, existing overrides stop rendering but remain recoverable.

## Managed-Site Page Override

Managed-site-owned content that replaces an overrideable Blueprint Page for one Managed Site.

**Fields**:
- `Id`: Stable override identifier.
- `ManagedSiteId`: Owning Managed Site.
- `BlueprintPageContentItemId`: Target Blueprint Page.
- `OverrideContentItemId`: Managed-site content item used as the override.
- `Status`: Draft, Published, Disabled, or Recoverable.

**State Transitions**:
- Draft -> Published after approval/publication.
- Published -> Disabled when the Blueprint Page becomes non-overrideable.
- Disabled -> Recoverable for administrator review and cleanup.

**Validation Rules**:
- Exactly one active published override per Managed Site and Blueprint Page.
- Override does not render when the Blueprint Page is not overrideable.
- Blueprint Page placeholders do not render when the override is active.

## Layer Contribution Point

A blueprint-defined location where Managed Sites can add supplemental content items rendered by the layout.

**Fields**:
- `Id`: Stable contribution point identifier.
- `BlueprintId`: Owning Site Blueprint.
- `Name`: Human-readable name.
- `LayerName`: Target layer name.
- `IsEnabled`: Whether contributions render.

**Relationships**:
- Has many Managed-Site Layer Contributions.

**Validation Rules**:
- Identifier is stable across display name changes.
- Disabled contribution points do not render contributions but keep records recoverable.

## Managed-Site Layer Contribution

Managed-site-owned content assigned to a Layer Contribution Point.

**Fields**:
- `Id`: Stable contribution identifier.
- `ManagedSiteId`: Owning Managed Site.
- `ContributionPointId`: Target Layer Contribution Point.
- `ContentItemIds`: Ordered content items.
- `Status`: Draft, Published, Disabled, or Recoverable.

**Validation Rules**:
- Contributions render only for the owning Managed Site.
- Layer contributions remain independent of page override behavior.
- Contributions whose layer contribution point is renamed, disabled, unpublished, or removed stop rendering when invalid and remain recoverable.

## Managed-Site Placeholder

A blueprint content area intended to be filled by managed-site-specific content items.

**Fields**:
- `Id`: Stable placeholder identifier.
- `BlueprintPageContentItemId`: Owning Blueprint Page.
- `Name`: Human-readable name.
- `FallbackContentItemIds`: Optional blueprint fallback content.
- `IsEnabled`: Whether assignments render.

**Relationships**:
- Has many Placeholder Assignments.

**Validation Rules**:
- Placeholder identifiers remain stable across display name changes.
- Placeholders on a Blueprint Page do not render when that page is replaced by an active Managed-Site Page Override.

## Placeholder Assignment

A relationship between a Managed Site, a Managed-Site Placeholder, and one or more managed-site content items.

**Fields**:
- `Id`: Stable assignment identifier.
- `ManagedSiteId`: Owning Managed Site.
- `PlaceholderId`: Target Managed-Site Placeholder.
- `ContentItemIds`: Ordered managed-site content items.
- `Status`: Draft, Published, Disabled, or Recoverable.

**Validation Rules**:
- Assignments render only for the owning Managed Site.
- If no managed-site content is assigned, render blueprint fallback content when available; otherwise render empty.
- Assignments whose managed-site placeholder is renamed, disabled, unpublished, or removed stop rendering when invalid and remain recoverable.

## Managed Navigation Contribution

Managed-site-owned navigation entries assigned to Site Blueprint placeholder menus or placeholder menu items.

**Fields**:
- `Id`: Stable contribution identifier.
- `ManagedSiteId`: Owning Managed Site.
- `MenuPlaceholderId`: Target placeholder menu or menu item.
- `MenuItemIds`: Ordered menu entries.
- `Status`: Draft, Published, Disabled, or Recoverable.

**Validation Rules**:
- Contributions render only for the matched Managed Site.
- Missing contributions render empty placeholder output without breaking navigation.
- Contributions whose target placeholder is renamed, disabled, unpublished, or removed stop rendering when invalid and remain recoverable.

## Composition Cache State

Tracks cache dependencies and invalidation state for composed responses affected by published changes.

**Fields**:
- `ManagedSiteId`: Optional Managed Site scope affected by the change.
- `Url`: Optional URL affected by the change.
- `AffectedArea`: Content, UrlRegistration, Navigation, PageOverride, LayerContribution, or PlaceholderAssignment.
- `CacheTags`: Cache tags associated with the affected composed output.
- `CacheSignals`: Signals associated with the affected composed output.
- `InvalidatedAt`: Timestamp when composition state became stale.
- `RefreshedAt`: Timestamp when composition state was refreshed.

**Validation Rules**:
- Published changes must trigger OrchardCore cache invalidation for every affected Managed Site or URL.
- Matching requests must reflect the published change immediately after the relevant cache invalidation completes according to configured cache management settings.