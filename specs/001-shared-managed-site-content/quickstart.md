# Quickstart: Site Blueprint and Managed Site Content

This guide describes validation scenarios for the Site Blueprint and Managed Site feature after implementation.

## Prerequisites

- .NET SDK matching `global.json`.
- Node.js and Yarn versions required by the repository.
- OrchardCore CMS web app restored and buildable.
- A single OrchardCore tenant with the VendallionCMS Managed Sites features enabled, which makes it the Site Blueprint.
- Two test users: one holding `ManageManagedSites`, who governs Managed Sites, and one holding
  `EditManagedSiteContent` plus clearance to a Managed Site, who edits that site's content. Both need
  access to the admin, because the portal is hosted there.

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
3. Create two Managed Sites in the tenant. A new Managed Site is Enabled; the only other state is Disabled.
4. Give one Managed Site a host name and give the other only a URL prefix.
5. Request each Managed Site address, and a path that belongs to neither.

**Expected outcome**:

- Each Managed Site address resolves to its owning Managed Site.
- The Managed Site naming a host name answers every path on that host, and its URL prefix, if it has one, is not consulted.
- The Managed Site naming no host name answers the paths under its prefix on every other host, and that prefix is moved onto the request path base so the content routes underneath resolve unchanged.
- Unclaimed addresses render Site Blueprint content only.
- Two Managed Sites naming the same host name are rejected whatever their prefixes; one naming a host name and one naming none are both accepted.
- The tenant Hostname is updated without a manual refresh step. Host names are added even to a tenant whose Hostname is empty, because a tenant answers only on the host names it declares.
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
4. Create and publish an override for the first Managed Site only, using an account holding managed-site clearance and no tenant-wide content permission.
5. Request the page in both Managed Site contexts and in the blueprint context.

**Expected outcome**:

- The portal lists only items whose edit scope includes the active Managed Site, including sections stored inside a page rather than in their own right.
- Creating the override needs no permission over the tenant's content, and the new version starts from the blueprint content.
- What renders is substituted as the page loads, so a Liquid template that walks the content tree sees the Managed Site's version, not only shape-based rendering.
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

- One mechanism covers navigation entries, layer widgets, and page content with no separate configuration, whether an item is stored in its own right or inside another.
- Layer widgets resolve independently of whether any page was overridden.
- The overridden container renders its own children and the original container's children do not render.
- Each Managed Site sees only its own overrides.

## Scenario 6: Preview

1. Create draft managed-content overrides for the active Managed Site.
2. Build a preview link from the portal's Preview screen, with unpublished work included.
3. Open the link, then open it again as a user holding no preview clearance for that Managed Site.
4. Compare preview output to published rendering rules.

**Expected outcome**:

- The preview address is the Managed Site's own, so it is composed by the pipeline that serves the site rather than by a separate renderer, and applies the same display scope and override rules as published rendering.
- Draft overrides appear for a caller holding preview clearance for that Managed Site.
- The same link shows only published content to anyone without that clearance, because asking for drafts in an address is not being granted them.
- Previewing a Disabled Managed Site is refused rather than showing Site Blueprint content under its name.

## Scenario 7: Regression and Isolation Tests

Run the relevant test project once implemented:

```powershell
dotnet test test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/VendallionCMS.ManagedSites.Tests.csproj
```

**Expected outcome**:

- Routing, authorization, managed content, composition, preview, recovery, and service registration tests pass.
- Content types without Managed Content attached render identically to before, asserted against the page's own content tree rather than a resolution result.
- The module's service registrations form no dependency cycle, and no content handler reaches the content manager through its constructor.

The test project covers scenarios 3, 4, 6, 8 and 9 at the level of the services behind them. Scenarios 1, 2 and 5 need a running tenant and a browser, because what they check is configuration and rendering rather than behaviour a service can be asked about.

Those three were walked in a browser, and all three pass as of 2026-10-04. The walkthrough record in
`tasks.md` under T119 has the detail, including the seven defects it found.

One rule to know before scenario 5: overriding a container replaces everything inside it. A managed site
that overrides a page stops seeing its own overrides of that page's sections until the page override is
removed.

A note for anyone following scenario 1 on a fresh tenant: a tenant whose hostname is empty answers on
every host, and creating the first host-named Managed Site narrows it to that host. Declare the tenant's
own host alongside the Managed Site's, or the Site Blueprint's address, and the admin with it, stops
answering.

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
