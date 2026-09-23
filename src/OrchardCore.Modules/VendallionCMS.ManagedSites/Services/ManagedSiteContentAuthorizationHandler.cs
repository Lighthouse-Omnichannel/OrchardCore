using Microsoft.AspNetCore.Authorization;
using OrchardCore.ContentManagement;
using OrchardCore.Contents;
using OrchardCore.Security;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Lets a Managed Site's clearance authorize content actions on that Managed Site's own override content.
/// </summary>
/// <remarks>
/// Without this, authoring an override would require tenant-wide content permissions for its content
/// type, and anyone holding those could equally change Site Blueprint content and every other Managed
/// Site's overrides. FR-011 forbids exactly that, so clearance has to be a content authorization
/// decision rather than something only the portal endpoints consult.
///
/// The handler only ever grants. It succeeds for content a Managed Site owns and stays silent for
/// everything else, so it can widen what a cleared editor may do and can never narrow what anyone else
/// already had. Content that is not an override is never touched, which is what keeps Site Blueprint
/// content and other Managed Sites out of reach.
/// </remarks>
public sealed class ManagedSiteContentAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <summary>
    /// The clearance scope each content permission corresponds to.
    /// </summary>
    /// <remarks>
    /// Deleting is grouped with editing: an editor who may replace the content of their override may
    /// equally withdraw it, and nothing else in the tenant is reachable either way.
    /// </remarks>
    private static readonly Dictionary<string, string> _scopesByPermission = new(StringComparer.Ordinal)
    {
        [CommonPermissions.ViewContent.Name] = ManagedSitesConstants.Scopes.View,
        [CommonPermissions.ViewOwnContent.Name] = ManagedSitesConstants.Scopes.View,
        [CommonPermissions.ListContent.Name] = ManagedSitesConstants.Scopes.View,
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

    private readonly IManagedSiteClearanceService _clearanceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteContentAuthorizationHandler" /> class.
    /// </summary>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    public ManagedSiteContentAuthorizationHandler(IManagedSiteClearanceService clearanceService)
    {
        _clearanceService = clearanceService;
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
            || string.IsNullOrEmpty(part.ManagedSiteId))
        {
            return;
        }

        if (!TryGetScope(requirement.Permission?.Name, out var scope))
        {
            return;
        }

        if (await _clearanceService.HasClearanceAsync(context.User, part.ManagedSiteId, scope))
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

        // Content permissions also appear per content type, as EditContent_Article. The clearance a
        // Managed Site holds is over its own content whatever type that content happens to be, so the
        // type half carries no decision here.
        var separator = permissionName.IndexOf('_');

        return separator > 0
            && _scopesByPermission.TryGetValue(permissionName[..separator], out scope);
    }
}
