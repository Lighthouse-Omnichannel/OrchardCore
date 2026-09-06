# Quickstart: Site Blueprint and Managed Site Content

This guide describes validation scenarios for the Site Blueprint and Managed Site feature after implementation.

## Prerequisites

- .NET SDK matching `global.json`.
- Node.js and Yarn versions required by the repository.
- OrchardCore CMS web app restored and buildable.
- A single OrchardCore tenant configured as the Site Blueprint with the VendallionCMS Managed Sites features enabled.
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

1. Create or enable one Site Blueprint.
2. Register blueprint URL content.
3. Create two Managed Sites under the Site Blueprint.
4. Assign unique URLs to each Managed Site.
5. Request each Managed Site URL.

**Expected outcome**:
- Each Managed Site URL resolves to its owning Managed Site.
- Unassigned URLs render Site Blueprint content only.
- Duplicate active URL registrations are rejected.
- Shell URL mappings are updated without a manual refresh step.
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

## Scenario 3: Blueprint Navigation Placeholders

1. Define a placeholder menu and placeholder menu item in the Site Blueprint navigation model.
2. Assign different Managed Navigation Contributions for two Managed Sites.
3. Render the same navigation in each Managed Site context.

**Expected outcome**:
- Each Managed Site shows only its own navigation contributions.
- Empty placeholders render without breaking navigation.
- Placeholder rename or disable operations leave affected assignments recoverable.

## Scenario 4: Page Override Policy

1. Mark a Blueprint Page as overrideable.
2. Create a Managed-Site Page Override for one Managed Site.
3. Request the page in both the owning and non-owning Managed Site contexts.
4. Mark the Blueprint Page as not overrideable.
5. Request the page again.

**Expected outcome**:
- The owning Managed Site renders the override while the page is overrideable.
- Other Managed Sites render the Blueprint Page.
- After override is disabled at the blueprint level, existing overrides stop rendering.
- Disabled overrides remain visible to authorized administrators for recovery or cleanup.

## Scenario 5: Layers and Page Placeholders

1. Define a Layer Contribution Point in the Site Blueprint.
2. Add Managed-Site Layer Contributions for one Managed Site.
3. Define a Managed-Site Placeholder on a Blueprint Page.
4. Assign Managed Site content to that placeholder.
5. Create an active page override for the same Blueprint Page.

**Expected outcome**:
- Layer contributions continue rendering through the layout even when the page is overridden.
- Blueprint Page placeholders do not render when the Blueprint Page is replaced by a Managed-Site Page Override.
- If no Managed Site content is assigned to a placeholder, fallback content renders when available, otherwise the placeholder renders empty.

## Scenario 6: Preview

1. Create draft Managed-Site content updates, page overrides, layer contributions, and placeholder assignments.
2. Preview the page through the Managed Site Admin Portal for the active Managed Site.
3. Compare preview composition to published rendering rules.

**Expected outcome**:
- Preview uses the active Managed Site scope.
- Draft changes visible to the current user are included in preview.
- Preview composition follows the same precedence rules as published rendering.

## Scenario 7: Regression and Isolation Tests

Run the relevant test project once implemented:

```powershell
dotnet test test/OrchardCore.Tests.Modules/VendallionCMS.ManagedSites/VendallionCMS.ManagedSites.Tests.csproj
```

**Expected outcome**:
- Routing, authorization, composition, preview, navigation, and recovery tests pass.
- Existing OrchardCore routing, content, layer, and navigation behavior remains compatible.

## Scenario 8: Composition Cache Invalidation

1. Publish a Site Blueprint content change used by a Managed Site URL.
2. Publish a Managed Site content change for that URL.
3. Publish changes to navigation contributions, page overrides, layer contributions, and placeholder assignments.
4. Request the affected URLs after each publish.

**Expected outcome**:
- Matching requests reflect each published change immediately after the relevant OrchardCore cache invalidation completes according to configured cache management settings.
- Unaffected Managed Sites continue rendering their previous composed content.

## Scenario 9: Customization Recovery

1. Create Managed Site navigation contributions, layer contributions, and placeholder assignments.
2. Rename, disable, unpublish, or remove the related Site Blueprint navigation placeholder, layer contribution point, or managed-site placeholder.
3. Render the affected Managed Site pages and navigation.
4. Open the affected records in the Managed Site Admin Portal.

**Expected outcome**:
- Invalid navigation, layer, and placeholder customizations stop rendering when their blueprint target is unavailable.
- Invalid customization records remain visible to authorized administrators.
- Authorized administrators can review, reassign, clean up, or recover affected records.