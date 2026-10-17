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

One feature, `Vendallion Managed Sites`, in `Content Management`. Enabling it brings the whole module:
the Managed Content part and the scopes, URL resolution, the permissions and clearance, the content screens,
and the composition that serves a managed site its own content.

There is deliberately no way to enable part of it. Resolving a URL to a managed site decides nothing
without the content rules that say what it may override, those rules authorize nothing without clearance,
and clearance is granted from the managed site — so every subset short of the whole is a configuration in which
the module cannot do its job.

## Permissions

There are two, and the line between them is the point.

**Manage Managed Sites** governs: defining the managed sites, deciding which blueprint content they may
override, and granting people clearance to them. It is security critical, because clearance decides who
may change a managed site's content.

**Edit Managed Site content** opens the content screens. Which managed sites they open onto, and what may be done
in each, comes from the holder's clearance rather than from the permission.

All three ways of widening your own reach — defining a new managed site, putting more content in scope,
granting clearance — sit on the governing side. An editor holding only the second permission cannot
reach any of them. The second is implied by the first, because whoever grants clearance can grant it to
themselves and withholding the content screens from them would be a formality.

The screens are in the admin, so an editor also needs `AccessAdminPanel`.

## Clearance

Clearance is granted per user, per managed site, from **Access** on the managed site's row in the
managed sites list. The screen has two lists — the users who can work on the site and the users who
cannot — and either can be searched, so a tenant with more users than fit on a screen is worked through
by narrowing rather than by scrolling. Granting it needs **Manage Managed Sites**, the same permission
as defining the site: deciding who may work on a site is the same kind of decision as deciding what the
site is.

The grant is stored on the user, which is what it is — a grant to a person, read when their principal is
built — but it is administered from the site, because the question an administrator arrives with is who
may work on this site, and answering that one user at a time meant opening every user to find out.

**Holding clearance is permission to read.** There is no scope for reading: a user given access to a
managed site can see its content, and a scope says what they may do beyond that.

| Scope | Allows, on top of reading |
| --- | --- |
| `edit` | Changing the managed site's content |
| `publish` | Publishing it |
| `preview` | Seeing unpublished work, in the admin and through a preview link |

A user given access starts with none of them, which is read-only, and the row is where each is ticked.
Clearing them all is a real answer rather than a refusal — the holder reads the site and changes
nothing — and taking the access away is how to say they should not see it at all.

Clearance arriving as a signed claim from an external issuer follows the same rule: `site-a` grants
reading, `site-a:edit,publish` grants those two as well, and `site-a:*` grants every scope.

A user cleared for exactly one managed site goes straight to it. A user cleared for several is offered
the choice, and opening one takes them to its own address. Managed sites they hold no clearance for are
not listed, and naming one is refused.

## The code

Every managed site has a **code**, chosen when it is created and fixed from then on. It is lowercase
letters and digits, beginning with a letter, up to 50 characters: `alpha`, `contoso`, `shop2`.

Everything that refers to a managed site refers to it by the code — the clearance a user holds, the rows
recording which items the site may override and what it has overridden, and the address of its own
admin screens. That is why it cannot be changed: changing it would detach all
of that at once. The editor shows it after creation but does not offer it.

It is chosen rather than generated because people read it. It names the site in a URL
(`/Admin/ManagedSites/Content/alpha`), in a recipe, and in a signed clearance claim — and a claim packs
the code and its scopes into one value separated by colons and commas, which is why punctuation is
refused rather than escaped.

A recipe must declare one. The later steps of the same recipe name the managed site by it, and a second
run finds the same site again by it, so a declaration without a code is reported and skipped rather than
creating a site nothing can refer to.

## Addressing

A managed site is addressed by a host name, a URL prefix, or both.

**A host name wins outright.** A managed site naming one answers every path on that host, and its URL
prefix, if it has one, is not consulted. Two managed sites cannot name the same host, whatever their
prefixes say.

**A URL prefix answers on every other host.** The prefix is moved from the path onto the request's path
base, exactly as a tenant's own prefix is, so the content routed underneath resolves unchanged and
generated links carry the prefix back.

An address no managed site claims renders Site Blueprint content.

**Host names reach the tenant without being declared on it.** A tenant whose own Hostname setting is
empty answers on every host, so a managed site's host already arrives; nothing is added to the tenant
and the addresses it answered on before — the admin's among them — keep answering. A tenant that does
name host names of its own is extended with the ones managed sites name, because there that is the only
way the address reaches the tenant at all. Either way the module takes back exactly what it added and
leaves host names an operator configured alone.

URL prefixes still need no host file entries or certificates, which makes them the simpler choice for
development.

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
  case, and the one the content list names by its container: `Services (Home page)`.
- **An item stored in its own right**, such as a menu, a layer widget or a page.
- **A container**, overridden whole. See below.

## Overriding

An editor opens **Managed Sites → Content**, which goes straight to their managed site when they hold
one and offers the choice when they hold several, and sees the items whose edit scope includes it.

Which managed site is being worked on is in the address — `/Admin/ManagedSites/Content/alpha` — rather
than remembered. An editor can see which site they are on, link to it, bookmark it, and keep two open at
once. Naming one decides nothing: the code is checked against their clearance on every request, so a
bookmark kept after the clearance was withdrawn is refused rather than honoured.

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

### What a managed site cannot author

Everything a managed site holds answers an item the Site Blueprint placed and put in scope. A managed
site cannot create content of its own, and cannot change the shape of content: it cannot define a
content type, attach Managed Content to one, or alter either scope. Those are the blueprint's
decisions, and an editor holding **Edit Managed Site content** reaches none of them.

The one way a managed site adds something is through a container. The blueprint places a container that
holds children — a page with sections, a menu with entries — and puts it in the site's edit scope. The
site overrides that container and supplies its own children in it. What is added that way is still
inside something the blueprint chose to put there.

That is what keeps the shared case cheap. Sites built on one blueprint differ where somebody decided
they differ, and a site that could invent structure of its own would stop being a variation of the
blueprint and start being a site the blueprint cannot describe.

## Media

Every managed site has a folder of its own in the media library, at the root and named by its code, made
when the site is defined. It is where that site's own assets go.

It is at the root because the platform reads a folder's permission from the first part of a path, so a
folder nested under a shared parent would be governed by the parent and could not be told apart from its
siblings. It is made with the site because a permission exists only for a folder that exists: a folder
made on first upload could be granted to nobody beforehand, and the editor who needed it would be the one
person unable to make it.

### What an editor may do

| | Read | Write |
| --- | --- | --- |
| Their own managed site's folder | Yes | Yes |
| A Site Blueprint folder they are granted | Yes | No |
| Another managed site's folder | No | No |
| A Site Blueprint folder they are not granted | No | No |

Reading is granted per folder, so blueprint assets are shared by granting them and another managed site's
folder is kept away by not granting it.

Writing needs refusing rather than withholding. The platform's permission to manage media is implied by
the permission to manage your own, and a write is authorized by that together with permission to view the
folder being written to — so a grant that lets an editor read a folder would also let them change it.
Managed Sites refuses a write to any folder but the editor's own, which is what keeps a blueprint folder
readable without becoming everybody's to rewrite. The refusal is a decision on the path, so it holds for
the media library, the media picker, upload, move, copy and delete alike.

Two people are not subject to it: whoever may **Manage Managed Sites**, because a rule about managed site
editors is not about them, and whoever holds **Manage Media For Others**, because granting that is saying
the library is theirs.

### Setting it up

1. Enable the platform's **Secure Media** feature. Without it, media permissions are tenant-wide and an
   editor granted media access at all reaches every folder.
2. Give the managed site editor role **Manage Own Media**. That is what opens the media library, since the
   permission to manage media is implied by it.
3. Grant that role **View media content in folder '…'** for each Site Blueprint folder its editors should
   be able to reuse.
4. Grant each editor's role the same permission for the folders of the managed sites they work on, and for
   no others.

A folder is granted to a role and clearance is granted to a user, so the two are kept in step by hand.
That is why step 4 usually means a role per managed site rather than one role for every editor: an editor
who works on two managed sites holds two of those roles, and nobody else holds either. The development
recipe does exactly this, and is the quickest way to see the arrangement working.

### What media does not do

**Media is not composed.** An asset referenced by content resolves to the same file whichever managed site
the request resolved to. A managed site that needs a different image overrides the content item that
references it, which is the same mechanism as every other difference between sites.

**A folder outlives its managed site.** Deleting a managed site leaves its folder, because published
content may still reference what is in it and the deletion could not be undone. A managed site later
defined again with the same code finds the earlier site's assets, since the code is what identifies a
site.

**An address already known is still an address.** The rules above govern who may browse, upload and delete
through the admin. Whether somebody holding an asset's URL can fetch it is the platform's own concern.

## How composition works

Content is swapped as it loads, before anything renders, so every consumer sees the managed site's
version: a Liquid template that walks the content tree, a shape that reads a part, a display driver. The
items stored inside a loaded item are swapped, and so is the loaded item itself.

Identity is left alone. The database key, the content item identifier and the content type stay the Site
Blueprint's, because routing, caching and invalidation all key on them; what changes is the content.

Admin requests resolve no managed site, so they always see Site Blueprint content. That is what stops an
editor saving a managed site's content over the blueprint's: the content screens show a managed site's
version because they ask for it, not because the request they arrived on was composed.

The admin answers at the tenant's own address and nowhere else. A managed site's URL prefix addresses the
content that site serves, not the screens the tenant is administered from, so a request that reaches for
the admin through one — `/alpha/Admin` — is redirected to `/Admin`, carrying the rest of the path, the
query and the method with it. Links are built from the request's path base, and a page served for a
managed site has that site's prefix on it, so every admin link such a page draws points under the prefix;
the redirect is what keeps the user menu and the dashboard working from a managed site's pages.

Editors therefore work at the tenant's admin whichever managed site they work on, and which one that is
comes from the path under it — `/Admin/ManagedSites/Content/alpha` — not from the address the content
itself is served at.

## Preview

A preview link is built from the managed site's own address, so the preview is composed by the
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
administrators, on a screen that reports why, and starts rendering again if the condition is
restored — no re-authoring.

| Reason | What happened |
| --- | --- |
| `EditScopeRemoved` | The managed site was removed from the item's edit scope |
| `SourceUnpublished` | The item it replaces was unpublished |
| `SourceDeleted` | The item it replaces was deleted |
| `CapabilityDetached` | Managed Content was detached from the content type |
| `ManagedSiteDisabled` | The managed site was switched off |

!!! warning "A draftable type that is not versionable unpublishes when a draft is saved"
    That is the platform's behaviour, not this feature's: a type marked draftable but not versionable
    keeps one version, so saving a draft of a published item sets that item unpublished. It stops
    rendering for everybody, leaves the managed site content list, and suppresses any override of it
    with `SourceUnpublished` — all of which looks like the item vanishing for no reason. Mark the
    content types a managed site may override **Versionable** as well as Draftable, and a draft sits
    beside the published version instead of replacing it.

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
- A `Managed Site Content Editor` role holding `EditManagedSiteContent`, `AccessAdminPanel` and
  `ManageOwnMediaContent` — what an editor needs, and deliberately not the permission to govern managed
  sites. It also holds reading on the `shared` folder, which every managed site may reuse.
- An `Alpha Assets`, `Beta Assets` and `Gamma Assets` role holding one managed site's folder each.
  `alphaUser` holds the first and `gammaUser` the third; `betaUser` holds the second and the third,
  because they work on two managed sites. One role for every editor would have handed each of them every
  folder.
- Three editors in that role, `alphaUser`, `betaUser` and `gammaUser`, all with the password
  `Password1!`. Each is cleared for the managed site they are named for; `betaUser` is also cleared for
  Gamma, so the screen that offers the choice is reachable without granting clearance first.
- A home page with three sections, a main menu with two entries, and a footer widget on a layer — one of
  each way content composes.
- Media folders to try the rules against: `alpha`, `beta` and `gamma`, made when their managed sites are;
  `shared`, the blueprint folder every editor may read; and `restricted`, a blueprint folder nobody is
  granted. Each holds one placeholder image, so every grant has something to prove.

The scopes are set so the content list has something to say in each direction:

| Item | Who may override it |
| --- | --- |
| `Home` page | Every managed site — overriding it replaces the page, its sections included |
| `What we do` section | Every managed site |
| `Where we are` section | Alpha only, so Beta and Gamma do not see it listed |
| `Not for anybody` section | Nobody, so the list has content it will not offer |
| `Home` menu entry | Every managed site |
| `About` menu entry | Nobody |
| `Footer note` widget | Every managed site |

**Managed Sites → Content** goes straight to Alpha for `alphaUser`, because a user cleared for one
managed site is not being offered a choice. For `betaUser` it asks which managed site to work on.

The media rules show themselves under **Content → Media** as that same `alphaUser`: `alpha` and `shared`
are there, and `beta`, `gamma` and `restricted` are not. Uploading into `alpha` works and uploading into
`shared` is refused, which is the pair worth seeing, because no arrangement of grants could have
expressed it.

!!! warning "For development only"
    The recipe creates accounts with a password written in it. Do not run it anywhere that matters.

## Credits

Built for VendallionCMS.
