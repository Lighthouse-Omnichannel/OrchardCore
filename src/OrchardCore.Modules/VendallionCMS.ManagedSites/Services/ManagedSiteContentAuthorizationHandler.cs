using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.Contents;
using OrchardCore.Security;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Lets a Managed Site's own permissions authorize content actions on that Managed Site's content.
/// </summary>
/// <remarks>
/// Without this, authoring an override would require tenant-wide content permissions for its content
/// type, and anyone holding those could equally change Site Blueprint content and every other Managed
/// Site's overrides. FR-011 forbids exactly that, so authority over a Managed Site has to be a content
/// authorization decision rather than something only the content endpoints consult.
///
/// The shape is the platform's own: take the content permission being asked for, work out which
/// authority over this Managed Site would answer it, and ask the authorization service again for that.
/// It is what <c>ContentTypeAuthorizationHandler</c> does with a content type, and what the media
/// handlers do with a folder.
///
/// The handler only ever grants. It succeeds for content a Managed Site owns and stays silent for
/// everything else, so it can widen what an authorized editor may do and can never narrow what anyone
/// else already had. Content that is not an override is never touched, which is what keeps Site
/// Blueprint content and other Managed Sites out of reach.
/// </remarks>
public sealed class ManagedSiteContentAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <summary>
    /// The scope each content permission corresponds to.
    /// </summary>
    /// <remarks>
    /// Deleting is grouped with editing: an editor who may replace the content of their override may
    /// equally withdraw it, and nothing else in the tenant is reachable either way.
    /// </remarks>
    private static readonly Dictionary<string, string> _scopesByPermission = new(StringComparer.Ordinal)
    {
        // Reading asks for no scope: authority over the Managed Site at all is what allows it.
        [CommonPermissions.ViewContent.Name] = null,
        [CommonPermissions.ViewOwnContent.Name] = null,
        [CommonPermissions.ListContent.Name] = null,
        [CommonPermissions.EditContent.Name] = ManagedSitesConstants.Scopes.Edit,
        [CommonPermissions.EditOwnContent.Name] = ManagedSitesConstants.Scopes.Edit,
        [CommonPermissions.DeleteContent.Name] = ManagedSitesConstants.Scopes.Edit,
        [CommonPermissions.DeleteOwnContent.Name] = ManagedSitesConstants.Scopes.Edit,
        [CommonPermissions.CloneContent.Name] = ManagedSitesConstants.Scopes.Edit,
        [CommonPermissions.CloneOwnContent.Name] = ManagedSitesConstants.Scopes.Edit,
        [CommonPermissions.PublishContent.Name] = ManagedSitesConstants.Scopes.Publish,
        [CommonPermissions.PublishOwnContent.Name] = ManagedSitesConstants.Scopes.Publish,
        [CommonPermissions.PreviewContent.Name] = ManagedSitesConstants.Scopes.Preview,
        [CommonPermissions.PreviewOwnContent.Name] = ManagedSitesConstants.Scopes.Preview,
    };

    private readonly IManagedSiteService _managedSiteService;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteContentAuthorizationHandler" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site definition service.</param>
    /// <param name="serviceProvider">
    /// The service provider the authorization service is read from, late, because asking it for a
    /// decision from inside a handler it owns would otherwise be a cycle.
    /// </param>
    public ManagedSiteContentAuthorizationHandler(
        IManagedSiteService managedSiteService,
        IServiceProvider serviceProvider)
    {
        _managedSiteService = managedSiteService;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Another handler already allowed this. Re-deciding it could only take a grant away, which is
        // not this handler's business.
        if (context.HasSucceeded)
        {
            return;
        }

        if (context.Resource is not ContentItem contentItem
            || !contentItem.TryGet<ManagedContentOverridePart>(out var part)
            || string.IsNullOrEmpty(part.ManagedSiteCode))
        {
            return;
        }

        if (!TryGetScope(requirement.Permission?.Name, out var scope))
        {
            return;
        }

        var permission = ManagedSiteContentPermissions.For(part.ManagedSiteCode, scope);

        if (permission is null)
        {
            return;
        }

        // A Managed Site that has been removed or switched off authorizes nothing, however its
        // permissions were granted. Its content is kept rather than served, so reaching it through a
        // permission nobody got round to revoking would be the one way back in.
        var managedSite = await _managedSiteService.GetAsync(part.ManagedSiteCode);

        if (managedSite is null || managedSite.Status != ManagedSiteStatus.Enabled)
        {
            return;
        }

        var authorizationService = _serviceProvider.GetService<IAuthorizationService>();

        if (authorizationService is not null
            && await authorizationService.AuthorizeAsync(context.User, permission))
        {
            context.Succeed(requirement);
        }
    }

    private static bool TryGetScope(string permissionName, out string scope)
    {
        scope = null;

        if (string.IsNullOrEmpty(permissionName))
        {
            return false;
        }

        if (_scopesByPermission.TryGetValue(permissionName, out scope))
        {
            return true;
        }

        // Content permissions also appear per content type, as EditContent_Article. The authority a
        // Managed Site editor holds is over that Managed Site's content whatever type it happens to be,
        // so the type half carries no decision here.
        var separator = permissionName.IndexOf('_');

        return separator > 0
            && _scopesByPermission.TryGetValue(permissionName[..separator], out scope);
    }
}
