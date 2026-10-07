# Managed Sites (`VendallionCMS.ManagedSites`)

Managed Sites let one OrchardCore tenant serve several sites that share most of their content.

The tenant itself is the **Site Blueprint**: the content everyone gets. A **Managed Site** is an address
within that tenant, plus permission to replace named pieces of the blueprint's content with its own. Two
managed sites can run the same pages, the same navigation and the same widgets, and differ only where
someone has deliberately made them differ.

A managed site is not a tenant. It has no database, no separate content store and no feature set of its
own; the content it serves is the blueprint's, composed per request. That is what makes the shared case
cheap: adding a managed site costs an address and a row, not a site.

## Enabling the feature

Four features, in `Content Management`, `Infrastructure` and `Security`:

| Feature | What it adds |
| --- | --- |
| `Managed Sites` | The Managed Content part, the scopes and the composition rules |
| `Managed Sites Routing` | Resolves an incoming URL to a managed site, and keeps the tenant's host names in step |
| `Managed Sites Permissions` | The two permissions below |
| `Managed Sites Admin Portal` | The portal editors work in, and the admin screens |

Enabling the portal pulls in the other three.

## Permissions

There are two, and the line between them is the point.

**Manage Managed Sites** governs: defining the managed sites, deciding which blueprint content they may
override, and granting people clearance to them. It is security critical, because clearance decides who
may change a managed site's content.

**Edit Managed Site content** opens the portal. Which managed sites it opens onto, and what may be done
in each, comes from the holder's clearance rather than from the permission.

All three ways of widening your own reach — defining a new managed site, putting more content in scope,
granting clearance — sit on the governing side. An editor holding only the second permission cannot
reach any of them. The second is implied by the first, because whoever grants clearance can grant it to
themselves and withholding the portal from them would be a formality.

The portal is hosted in the admin, so an editor also needs `AccessAdminPanel`.

## Clearance

Clearance is granted per user, per managed site, on the user's edit screen. Each grant names what the
holder may do:

| Scope | Allows |
| --- | --- |
| `view` | Seeing the managed site's content in the portal |
| `edit` | Changing it |
| `publish` | Publishing it |
| `preview` | Seeing unpublished work |

`edit`, `publish` and `preview` each imply `view`: a clearance that let someone change content without
reading it would list nothing for them to change. A managed site selected with no action ticked grants
`view` alone.

A user cleared for exactly one managed site is scoped to it automatically. A user cleared for several
must choose one before any scoped action, and everything they then do applies to that managed site only.
Managed sites they hold no clearance for are neither listed nor reachable.

## Addressing

A managed site is addressed by a host name, a URL prefix, or both.

**A host name wins outright.** A managed site naming one answers every path on that host, and its URL
prefix, if it has one, is not consulted. Two managed sites cannot name the same host, whatever their
prefixes say.

**A URL prefix answers on every other host.** The prefix is moved from the path onto the request's path
base, exactly as a tenant's own prefix is, so the content routed underneath resolves unchanged and
generated links carry the prefix back.

An address no managed site claims renders Site Blueprint content.

!!! warning "Declaring a host name narrows the tenant"
    A tenant whose Hostname setting is empty answers on every host. Giving a managed site a host name
    adds it to the tenant's own host names, and from then on the tenant answers only on the host names
    it declares — including, no longer, the one the admin was reached on. Declare the tenant's own host
    alongside the managed site's, or the Site Blueprint's address stops answering.

URL prefixes avoid this entirely, need no host file entries or certificates, and are the better choice
for development.

## Managed Content

Attach the **Managed Content** part to any content type whose items a managed site should be able to
replace. Attaching it alone changes nothing a visitor sees.

The part carries two scopes, and only a user who may govern managed sites can set them.

### Edit scope — who may override this

| Mode | Meaning |
| --- | --- |
| `None` | Nobody. The default, so attaching the part grants nothing until it is configured |
| `Selected` | The named managed sites |
| `All` | Every managed site |

### Display scope — who renders this at all

The same three modes, deciding which managed sites render the item, plus whether it renders in the Site
Blueprint context.

**Display must cover edit.** A managed site that may override an item necessarily renders it, so
granting the right to override also grants the right to display, and the editor shows that rather than
letting the two drift apart. Display can be wider than edit; it is never narrower.

### What can carry the part

Anything. The three cases worth knowing apart are how the content is stored, not what it is:

- **An item stored inside another**, such as a section in a page or an entry in a menu. The most common
  case, and the one the portal names by its container: `Services (Home page)`.
- **An item stored in its own right**, such as a menu, a layer widget or a page.
- **A container**, overridden whole. See below.

## Overriding

An editor opens the portal, picks a managed site, and sees the items whose edit scope includes it.
Creating an override copies the blueprint's content, so they change what differs rather than retyping
the item. Creating and publishing it needs managed-site clearance alone — no permission over the
tenant's content.

The copy deliberately does **not** inherit:

- **The scopes.** An override is an answer to a scoped item, never a scoped item in its own right.
- **An alias or a route.** One address has one owner, and a second claimant is rejected, so an override
  carrying the source's alias could not be published at all.
- **A layer membership.** The Layers module knows nothing of managed sites, so a copy that kept its
  membership would be drawn on every site.

Editing an override opens the ordinary content editor for its type, with the scopes hidden and a note
saying which managed site it belongs to and which item it replaces.

!!! note "Overriding a container replaces everything inside it"
    An override of a page or a menu carries copies of that container's children, and those copies are
    what renders. A managed site that overrides a page therefore stops seeing its own overrides of that
    page's sections until the page override is removed. This is deliberate — the override stands in for
    the item rather than extending it — but it surprises editors who meet it by accident.

## How composition works

Content is swapped as it loads, before anything renders, so every consumer sees the managed site's
version: a Liquid template that walks the content tree, a shape that reads a part, a display driver. The
items stored inside a loaded item are swapped, and so is the loaded item itself.

Identity is left alone. The database key, the content item identifier and the content type stay the Site
Blueprint's, because routing, caching and invalidation all key on them; what changes is the content.

Admin requests resolve no managed site, so they always see Site Blueprint content. That is what stops an
editor saving a managed site's content over the blueprint's: the portal shows a managed site's version
because it asks for it, not because the request it arrived on was composed.

The admin answers at the tenant's own address and nowhere else. A managed site's URL prefix in front of
it, `/alpha/Admin`, is not an admin address and is not found; the prefix addresses the content a managed
site serves, not the screens the tenant is administered from. Editors reach the portal at the tenant's
admin whichever managed site they work on, and which one that is comes from the portal, not the URL.

## Preview

The portal builds a preview link from the managed site's own address, so the preview is composed by the
pipeline that serves the site rather than by a separate renderer and cannot drift from what visitors
receive. Asking for unpublished work adds a query value; asking is not being granted, and the request is
honoured only for a caller holding `preview` clearance for the managed site the address resolves to. The
same link shared with anyone else shows published content.

Previewing a disabled managed site is refused rather than showing Site Blueprint content under its name.

!!! warning "Draft preview needs the editor signed in on the managed site's host"
    An editor signs in on the admin's host. A preview link for a managed site that names a *different*
    host opens that host, where the authentication cookie issued by the first does not reach, so the
    editor arrives anonymous and is served published content with no indication why. Managed sites
    addressed by URL prefix are not affected, because the host does not change.

## When an override stops rendering

An override can stop rendering without anybody deleting it. It stays visible to authorized
administrators in the portal, which reports why, and starts rendering again if the condition is
restored — no re-authoring.

| Reason | What happened |
| --- | --- |
| `EditScopeRemoved` | The managed site was removed from the item's edit scope |
| `SourceUnpublished` | The item it replaces was unpublished |
| `SourceDeleted` | The item it replaces was deleted |
| `CapabilityDetached` | Managed Content was detached from the content type |
| `ManagedSiteDisabled` | The managed site was switched off |

A managed site has two states only, Enabled and Disabled. A disabled one resolves no requests and
accepts no editor changes; its host names stay on the tenant, so its addresses still answer and serve
Site Blueprint content rather than failing.

## A site to investigate against

Setting this up by hand takes a while, and getting one step of it wrong produces behaviour that looks
like a defect. The **Managed Sites Development Site** theme carries a setup recipe that does it in one
step: choose `Managed Sites development site` on the setup screen, or run it through AutoSetup with
`RecipeName=ManagedSitesDevelopment`.

What it provisions:

- Three managed sites, `Alpha`, `Beta` and `Gamma`, on the URL prefixes `/alpha`, `/beta` and `/gamma`.
  Prefixes rather than host names, so there is nothing to add to a host file and draft preview works.
- A `Managed Site Content Editor` role holding `EditManagedSiteContent` and `AccessAdminPanel` — what an
  editor needs, and deliberately not the permission to govern managed sites.
- Three editors in that role, `alphaUser`, `betaUser` and `gammaUser`, all with the password
  `Password1!`. Each is cleared for the managed site they are named for; `betaUser` is also cleared to
  view Gamma, so the portal's selection screen is reachable without editing a user first.
- A home page with three sections, a main menu with two entries, and a footer widget on a layer — one of
  each way content composes.

The scopes are set so the portal has something to say in each direction:

| Item | Who may override it |
| --- | --- |
| `Home` page | Every managed site — overriding it replaces the page, its sections included |
| `What we do` section | Every managed site |
| `Where we are` section | Alpha only, so Beta and Gamma do not see it listed |
| `Not for anybody` section | Nobody, so the portal lists content it will not offer |
| `Home` menu entry | Every managed site |
| `About` menu entry | Nobody |
| `Footer note` widget | Every managed site |

Signing in as `alphaUser` goes straight to Alpha, because a user cleared for one managed site is scoped
to it automatically. Signing in as `betaUser` asks which managed site to work on.

!!! warning "For development only"
    The recipe creates accounts with a password written in it. Do not run it anywhere that matters.

## Credits

Built for VendallionCMS.
