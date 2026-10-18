using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Media;
using OrchardCore.Security;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Keeps a Managed Site editor from changing media outside their own Managed Site's folder.
/// </summary>
/// <remarks>
/// This is the one place in the feature that refuses rather than grants, and it exists because the
/// platform cannot express what is wanted here in grants alone. Permission to manage media is implied
/// by permission to manage one's own, and a write is authorized by that permission together with
/// permission to view the folder being written to. Read access to a folder therefore carries write
/// access to it, so withholding a grant can hide a folder from an editor but cannot hand them one to
/// read without also handing it to them to change. Blueprint folders are meant to be read and reused,
/// not rewritten by whichever Managed Site happened to open one.
///
/// Reading and writing ask the same question of the same path, so the request method is what tells
/// them apart: every media endpoint that reads is a GET and every endpoint that changes something is a
/// POST. A decision made outside a request, such as a folder subscription over the hub, changes
/// nothing and is left alone.
///
/// Two people are not confined by this. Whoever governs Managed Sites is not subject to a rule about
/// them, and whoever has been trusted with other people's media has been told explicitly that the
/// library is theirs. Everybody else holding clearance is an editor of their own Managed Sites and
/// nothing more.
/// </remarks>
public sealed class ManagedSiteMediaAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IManagedSiteClearanceService _clearanceService;
    private readonly IManagedSiteMediaFolderService _folderService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteMediaAuthorizationHandler" /> class.
    /// </summary>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="folderService">The Managed Site media folder service.</param>
    /// <param name="httpContextAccessor">The accessor the request method is read from.</param>
    /// <param name="serviceProvider">
    /// The service provider the authorization service is read from, late, because asking it for a
    /// decision from inside a handler it owns would otherwise be a cycle.
    /// </param>
    public ManagedSiteMediaAuthorizationHandler(
        IManagedSiteClearanceService clearanceService,
        IManagedSiteMediaFolderService folderService,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _clearanceService = clearanceService;
        _folderService = folderService;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (requirement.Permission?.Name != MediaPermissions.ManageMediaFolder.Name)
        {
            return;
        }

        if (context.Resource is not string path)
        {
            return;
        }

        var request = _httpContextAccessor.HttpContext?.Request;

        // Reading asks this same question of this same path, so a decision that is not a request to
        // change something is none of this handler's business.
        if (request is null || HttpMethods.IsGet(request.Method))
        {
            return;
        }

        var managedSites = await _clearanceService.GetAuthorizedManagedSitesAsync(context.User);

        if (managedSites.Count == 0)
        {
            return;
        }

        var folder = _folderService.GetOwningFolder(path);

        if (folder is not null
            && managedSites.Any(managedSite =>
                string.Equals(managedSite.Code, folder, StringComparison.OrdinalIgnoreCase)))
        {
            // Their own Managed Site's folder. Whether they may write in it is still the platform's
            // decision, made from the permissions they hold; this handler only refuses.
            return;
        }

        if (await IsTrustedWithTheLibraryAsync(context))
        {
            return;
        }

        context.Fail();
    }

    /// <summary>
    /// Answers whether the caller is somebody a rule about Managed Site editors should not reach.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <returns><see langword="true" /> when the caller is not confined to a Managed Site's folder.</returns>
    private async Task<bool> IsTrustedWithTheLibraryAsync(AuthorizationHandlerContext context)
    {
        var authorizationService = _serviceProvider.GetService<IAuthorizationService>();

        if (authorizationService is null)
        {
            return false;
        }

        return await authorizationService.AuthorizeAsync(context.User, Permissions.ManageManagedSites)
            || await authorizationService.AuthorizeAsync(context.User, MediaPermissions.ManageOthersMedia);
    }
}
