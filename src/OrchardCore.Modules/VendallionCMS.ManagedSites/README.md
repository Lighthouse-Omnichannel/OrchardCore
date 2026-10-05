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

## Features

| Feature | Provides |
| --- | --- |
| `VendallionCMS.ManagedSites` | The Managed Content part, the scopes, and the composition rules |
| `VendallionCMS.ManagedSites.Routing` | URL resolution and tenant host name synchronization |
| `VendallionCMS.ManagedSites.Permissions` | The two Managed Sites permissions |
| `VendallionCMS.ManagedSites.AdminPortal` | The React Managed Site Admin Portal and admin screens |

## Developing

The portal's client application lives in `Assets/managed-site-admin/` and is built through the
repository's asset pipeline:

```bash
yarn build -n managed-site-admin
```

The specification this module implements is in `specs/001-shared-managed-site-content/`.
