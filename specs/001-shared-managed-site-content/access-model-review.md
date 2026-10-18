# Access to a Managed Site: how it works, and following the platform instead

A review of how a user is allowed to work on a Managed Site today, what the platform's own pattern is,
and what it would take to move onto it so that one grant covers a Managed Site's content **and** its
media folder.

## 1. How it works today

Access is decided in two unrelated ways depending on what is being reached.

### Content: clearance held on the user

Two static permissions exist, in `Permissions.cs`:

| Permission | Means |
| --- | --- |
| `ManageManagedSites` | Define Managed Sites, set edit and display scope, grant clearance. Security critical. |
| `EditManagedSiteContent` | Open the Managed Site content screens at all. |

Neither says *which* Managed Site. That comes from **clearance**, which is a separate mechanism:

1. **Stored on the user.** `ManagedSiteSecurityService.GrantAsync` writes a grant into
   `ManagedSiteClearanceSettings` on the `User` document — a code and a list of scopes
   (`edit`, `publish`, `preview`).
2. **Administered per Managed Site.** `AdminController.Access` is a screen of two searchable user
   lists, reachable with `ManageManagedSites`.
3. **Surfaced as claims.** `ManagedSiteClaimsProvider` (an `IUserClaimsProvider`) emits one
   `managed_site` claim per grant, valued `alpha`, `alpha:edit,publish` or `alpha:*`.
4. **Read back from claims.** `ManagedSiteAuthorizationService.GetClearancesAsync` parses them;
   `ManagedSiteClearanceService` drops clearance for Managed Sites that no longer exist or are disabled.
5. **Enforced twice.** `ManagedSiteContentController.ResolveScopeAsync` gates the screens, and
   `ManagedSiteContentAuthorizationHandler` grants the platform's content permissions
   (`EditContent`, `PublishContent`, `PreviewContent`, …) on any item carrying
   `ManagedContentOverridePart` whose `ManagedSiteCode` the caller holds.

Because the grant rides on claims, changing it has to reach sessions already open: `GrantAsync` and
`RevokeAsync` go through `UserManager.UpdateAsync` and `UpdateSecurityStampAsync`, and the Access screen
calls `SignInManager.RefreshSignInAsync` for the caller's own principal.

### Media: permissions held on a role

Media went the other way, because it had to:

- The platform's **Secure Media** feature generates `ViewMediaContent_{folder}` per root folder.
- A role holds `ViewMediaContent_alpha`, and that is what makes Alpha's folder visible.
- `ManageOwnMediaContent` opens the library (`ManageMedia` is implied by it).
- `ManagedSiteMediaAuthorizationHandler` refuses writes outside the caller's own site folders, because
  the platform cannot express "readable but not writable" in grants.

### The result

| | Managed Site content | Managed Site media |
| --- | --- | --- |
| Granted to | a **user** | a **role** |
| Stored in | user settings | role permissions |
| Administered from | Managed Sites → Access | Security → Roles |
| Carried as | a `managed_site` claim | a permission claim |
| Visible in the Roles screen | no | yes |
| Provisioned in a recipe by | a `ManagedSiteEditors` step | an ordinary `Roles` step |

Two systems answer one question — *may this person work on Alpha?* — and nothing keeps them in step.
The development recipe shows the cost plainly: `alphaUser` needs a clearance grant **and** membership of
an `Alpha Assets` role, and getting one without the other produces an editor who can change Alpha's
content but cannot see Alpha's images, or the reverse.

## 2. What the platform's pattern actually is

Secure Media is not a special case. The same four parts appear in `OrchardCore.Contents` for per-content-type
permissions, and they are the pattern to follow.

1. **A template permission** carrying a `{0}` and an implication chain.

   ```csharp
   // SecureMediaPermissions
   new("ViewMediaContent_{0}", "View media content in folder '{0}'", [MediaPermissions.ViewMedia]);

   // ContentTypePermissionsHelper
   new("Edit_{0}", "Edit {0} for others", [s_publishContent, CommonPermissions.EditContent]);
   ```

2. **A factory** that stamps the template with one thing's name and keeps the implication chain,
   caching the result: `CreateDynamicPermission(template, "alpha")` → `ViewMediaContent_alpha`.
   `ContentTypePermissionsHelper` also sets `Category`, which is what groups the permissions in the
   Roles screen.

3. **An `IPermissionProvider`** that enumerates the things — folders, content types — and yields their
   dynamic permissions, so an administrator sees them in Roles without anybody registering each one.
   `SecureMediaPermissions` caches that enumeration for five minutes and invalidates on a signal.

4. **An `AuthorizationHandler<PermissionRequirement>`** that turns a question about a *resource* into a
   question about a *permission*: take the required global permission, convert it to its template, stamp
   it with the resource's name, and ask the authorization service again.
   `ContentTypeAuthorizationHandler` and `ViewMediaFolderAuthorizationHandler` are the same twelve lines
   with different resources.

Granting is then ordinary. `DefaultPermissionGrantingService.IsGranted` walks the `ImpliedBy` graph, so
`Publish_Article` grants `Edit_Article`, and a role claim is all that is needed.

## 3. What following it would look like

### The permissions

Four templates, shaped like the platform's content permissions so the implications read the same way:

| Template | Implied by | Allows |
| --- | --- | --- |
| `ViewManagedSiteContent_{0}` | the three below | Open that Managed Site and read its content list |
| `EditManagedSiteContent_{0}` | `PublishManagedSiteContent_{0}` | Create and change its overrides |
| `PublishManagedSiteContent_{0}` | — | Publish them |
| `PreviewManagedSiteContent_{0}` | — | See unpublished work, in the admin and through a preview link |

`EditManagedSiteContent` stays as the global gate that puts **Managed Sites → Content** in the menu,
exactly as `ManageMedia` gates the media library; the per-site permissions decide which sites are behind
it. `ManageManagedSites` is unchanged.

This maps one-to-one onto today's scopes, including the rule that holding clearance is permission to
read: `ViewManagedSiteContent_alpha` is that rule, written down as a permission instead of as the
absence of a scope.

### The code

| Today | Becomes |
| --- | --- |
| `Permissions.cs` returns two permissions | also enumerates Managed Sites and yields four per site, `Category = "Managed Site - Alpha"` |
| — | `ManagedSiteContentPermissions`: the templates and `CreateDynamicPermission`, mirroring `ContentTypePermissionsHelper` |
| `ManagedSiteContentAuthorizationHandler` reads clearance claims | converts the requirement plus `ManagedContentOverridePart.ManagedSiteCode` into the dynamic permission and re-asks — the shape of `ContentTypeAuthorizationHandler` |
| `ManagedSiteClearanceService` parses claims | asks `AuthorizeAsync(ViewManagedSiteContent_{code})` for each defined, enabled Managed Site |
| `ManagedSiteContentController.ResolveScopeAsync` calls `HasClearanceAsync` | calls `AuthorizeAsync` with the dynamic permission |
| `ManagedSiteMediaAuthorizationHandler` asks clearance for "their own folders" | asks the same authorization service, so content and folders agree by construction |

Deleted: `ManagedSiteClaimsProvider`, the claim parsing in `ManagedSiteAuthorizationService`,
`ManagedSiteSecurityService`, `ManagedSiteClearanceSettings`, the Access screen and its view models, and
the security-stamp and `RefreshSignInAsync` handling that exists only because clearance rides on claims.

### What a tenant looks like afterwards

One role per Managed Site, holding both halves:

```json
{
  "Name": "Alpha Editors",
  "Permissions": [
    "EditManagedSiteContent",
    "AccessAdminPanel",
    "ManageOwnMediaContent",
    "EditManagedSiteContent_alpha",
    "PublishManagedSiteContent_alpha",
    "PreviewManagedSiteContent_alpha",
    "ViewMediaContent_alpha",
    "ViewMediaContent_shared"
  ]
}
```

`betaUser`, who works on two Managed Sites, holds two such roles. That is already how the media half is
provisioned, so this removes a mechanism rather than adding one.

## 4. What it costs

**Per-user grants stop existing.** Everything becomes role membership. A tenant that assigns Managed
Sites ad hoc, person by person, would need a role per Managed Site — which the media half already
forces, so this is a cost already being paid once.

**The Access screen goes.** "Who may work on this site" becomes a Roles question, and Roles answers the
inverse — "what may this role do". If the per-site view is worth keeping, it can be rebuilt read-only as
a list of the roles holding `ViewManagedSiteContent_{code}` and their members, but it would report rather
than grant.

**External clearance moves to role mapping — decided, 2026-10-18.** The specification assumed clearance
could arrive from an external issuer as a signed claim, including the `alpha:*` wildcard. It now maps to
a role instead: an external identity reaches a Managed Site by being given the role that holds that
Managed Site's permissions. The platform already does this at sign-in through
`IExternalLoginEventHandler`, with `ScriptExternalLoginEventHandler` driving it from
`SyncPropertiesScript`, so no mechanism has to be built here. The spec's assumption and FR-017 were
updated to match.

**Tests move rather than disappear.** The clearance tests become permission tests; the handler tests keep
their shape, since the handler keeps its shape.

**No data migration.** There is no production data, so existing grants do not need converting.

**Enumeration cost is small.** Listing Managed Sites to build the permission list is a site-settings read,
cached as Secure Media caches its folder enumeration.

## 5. Recommendation

Move to the dynamic permission model. It removes a second authorization system that exists only for this
feature, makes one grant cover a Managed Site's content and its folder, and puts the answer in the screen
administrators already use for everything else. The spec's own wording — "managed-site-specific
permissions so only authorized users can manage that managed site's content" (FR-005) — describes
permissions more closely than it describes claims.

The external-issuer question is settled: role mapping, using the platform's own sign-in hook. Everything
else is mechanical.

A hybrid — keeping clearance claims and adding dynamic permissions that are honoured as well — is worth
naming only to rule out: two sources of truth for one decision is the problem being fixed, not a safer
way to fix it.
