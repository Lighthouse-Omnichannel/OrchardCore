# VendallionCMS Managed Sites

Serves several sites from one OrchardCore tenant, sharing most of their content.

The tenant is the Site Blueprint: the content everyone gets. A Managed Site is an address within that
tenant, plus permission to replace named pieces of the blueprint's content with its own. A managed site
is not a tenant — it has no database, no content store and no feature set of its own — so adding one
costs an address and a row rather than a site.

Attach the Managed Content part to a content type to put its items in reach, set which managed sites may
override each item, and editors do the rest from the Managed Site Admin Portal. Content is swapped as it
loads, so every consumer sees the right version: Liquid templates, shapes, and display drivers alike.

See [the documentation](../../docs/reference/modules/ManagedSites/README.md) for addressing rules, the
two scopes, clearance, preview, and what happens when an override stops rendering.

## Feature

One feature, `VendallionCMS.ManagedSites`: the Managed Content part and the scopes, URL resolution and
tenant host name synchronization, the two permissions and the clearance that carries them, the Managed
Site Admin Portal, and the composition that puts a Managed Site's own content in front of the Site
Blueprint's. `Startup.cs` registers them in that order, under a heading each.

## Developing

The module ships no assets: the portal is rendered on the server, so there is nothing to build beyond
the project itself.

The quickest way to see the feature working is the `Managed Sites development site` setup recipe, which
stands up three managed sites and the editors who work in them. See
[the documentation](../../docs/reference/modules/ManagedSites/README.md#a-site-to-investigate-against).

The specification this module implements is in `specs/001-shared-managed-site-content/`.
