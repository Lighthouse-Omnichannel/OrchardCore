# Quickstart: Site Blueprint and Managed Site Content

This guide describes validation scenarios for the Site Blueprint and Managed Site feature after implementation.

## Prerequisites

- .NET SDK matching `global.json`.
- Node.js and Yarn versions required by the repository.
- OrchardCore CMS web app restored and buildable.
- A single OrchardCore tenant with the VendallionCMS Managed Sites features enabled, which makes it the Site Blueprint.
- Test users for Site Blueprint administration and Managed Site administration.

## Build Validation

From the repository root:

```powershell
dotnet build src/OrchardCore.Cms.Web -c Debug -f net10.0
```

If Managed Site Admin Portal assets changed:

```powershell
yarn build
```

## Run Locally

```powershell
cd src/OrchardCore.Cms.Web
dotnet run -f net10.0
```

Open the local site URL printed by the application.

## Scenario 1: Site Blueprint and Managed Site URL Resolution

1. Enable the Managed Sites feature, which makes the tenant the Site Blueprint.
2. Register common URL content.
3. Create two Managed Sites in the tenant.
4. Give each Managed Site its own host name, URL prefix, or both.
5. Request each Managed Site address.

**Expected outcome**:

- Each Managed Site address resolves to its owning Managed Site.
- Unclaimed addresses render Site Blueprint content only.
- A Managed Site naming the request host wins over one that answers on every host.
- Colliding addresses are rejected.
- The tenant Hostname is updated without a manual refresh step, and a tenant that answers on every host is left alone.
- Published URL and content changes are reflected on matching requests immediately after OrchardCore cache invalidation completes according to configured cache management settings.

## Scenario 2: Managed Site Admin Portal Scope Selection

1. Sign in as a user with clearance to one Managed Site.
2. Verify the portal auto-selects that Managed Site.
3. Sign in as a user with clearance to two Managed Sites.
4. Verify the portal requires active Managed Site selection before content edits.
5. Attempt to access a Managed Site that is not present in token claims/scopes.

**Expected outcome**:

- Single-site users are scoped automatically.
- Multi-site users must choose a scope.
- Unauthorized Managed Sites are hidden and blocked by API authorization.

## Scenario 3: Attach Managed Content and Configure Scopes

1. Attach Managed Content to the Page content type in the OrchardCore admin UI.
2. Open an existing page and confirm it still renders unchanged in every context.
3. Set the edit scope of that page to one Managed Site.
4. Set the display scope of a second content item to exclude the Site Blueprint context.
5. Sign in as a managed-site administrator and attempt to change either scope.

**Expected outcome**:

- Attaching the part alone changes nothing that a visitor sees.
- The default edit scope grants no Managed Site override rights until configured.
- The item with a restricted display scope does not render for requests that resolve to no Managed Site.
- Managed-site administrators cannot change edit scope or display scope.

## Scenario 4: Override Managed Content Per Managed Site

1. Grant edit scope for one page to two Managed Sites.
2. Sign in to the portal as an administrator of the first Managed Site.
3. Confirm the page appears in the editable content list.
4. Create and publish an override for the first Managed Site only.
5. Request the page in both Managed Site contexts and in the blueprint context.

**Expected outcome**:

- The portal lists only items whose edit scope includes the active Managed Site.
- The first Managed Site renders the override.
- The second Managed Site and the blueprint context render the original content.
- A draft override does not change rendered output until it is published.

## Scenario 5: Content Types Beyond Pages

1. Attach Managed Content to a menu item content type and to a layer widget content type.
2. Override the menu item for one Managed Site.
3. Override a widget that renders through a layer for the same Managed Site.
4. Override a container item, such as a menu, and give it different child items.
5. Render the affected pages in each Managed Site context.

**Expected outcome**:

- One mechanism covers navigation entries, layer widgets, and page content with no separate configuration.
- Layer widgets resolve independently of whether any page was overridden.
- The overridden container renders its own children and the original container's children do not render.
- Each Managed Site sees only its own overrides.

## Scenario 6: Preview

1. Create draft managed-content overrides for the active Managed Site.
2. Preview the page through the Managed Site Admin Portal.
3. Compare preview output to published rendering rules.

**Expected outcome**:

- Preview uses the active Managed Site scope.
- Draft overrides visible to the current user are included in preview.
- Preview applies the same display scope and override resolution rules as published rendering.

## Scenario 7: Regression and Isolation Tests

Run the relevant test project once implemented:

```powershell
dotnet test test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/VendallionCMS.ManagedSites.Tests.csproj
```

**Expected outcome**:

- Routing, authorization, managed content, composition, preview, and recovery tests pass.
- Existing OrchardCore routing, content, layer, and navigation behavior remains compatible.
- Content types without Managed Content attached render identically to before.

## Scenario 8: Composition Cache Invalidation

1. Publish a Site Blueprint content change used by a Managed Site URL.
2. Publish a managed-content override for that URL.
3. Change the edit scope and the display scope of an item used by two Managed Sites.
4. Request the affected URLs after each publish.

**Expected outcome**:

- Matching requests reflect each published change immediately after the relevant OrchardCore cache invalidation completes according to configured cache management settings.
- A scope change invalidates composed output for every Managed Site added to or removed from that scope.
- Unaffected Managed Sites continue rendering their previous composed content.

## Scenario 9: Override Recovery

1. Create published overrides for two Managed Sites on different content items.
2. Remove one Managed Site from an item's edit scope.
3. Unpublish the source content item of the other override.
4. Detach Managed Content from a third content type that has overrides.
5. Render the affected pages, then open the suppressed override list in the portal.

**Expected outcome**:

- Each affected override stops rendering immediately.
- Every suppressed override remains visible to authorized administrators.
- Each suppressed override reports why it stopped rendering, distinguishing scope removal, source removal, and capability detachment.
- Restoring the removed condition allows the override to render again without re-authoring it.
