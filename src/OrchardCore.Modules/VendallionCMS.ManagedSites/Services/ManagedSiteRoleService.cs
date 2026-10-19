using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Media;
using OrchardCore.Security.Services;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Gives a Managed Site a role that carries it, so granting somebody the site is one step.
/// </summary>
/// <remarks>
/// Authority over a Managed Site is a permission, and permissions are held by roles, so somebody had
/// to assemble a role before anybody could be given a Managed Site at all. Assembling it is the same
/// every time, so the feature does it when the Managed Site is defined rather than leaving an
/// administrator to find four permissions and a media folder and remember the admin panel.
///
/// The role is an ordinary role from then on. It is not owned, tracked or rewritten: an administrator
/// may widen it, narrow it, rename it or delete it, and nothing here will argue.
/// </remarks>
public interface IManagedSiteRoleService
{
    /// <summary>
    /// Gets the name of the role created for a Managed Site.
    /// </summary>
    /// <param name="managedSite">The Managed Site.</param>
    /// <returns>The role name.</returns>
    string GetEditorRoleName(ManagedSite managedSite);

    /// <summary>
    /// Creates the Managed Site's editor role when it is not there already.
    /// </summary>
    /// <param name="managedSite">The Managed Site.</param>
    /// <returns>The role name when one was created, or <see langword="null" /> when nothing was done.</returns>
    ValueTask<string> EnsureEditorRoleAsync(ManagedSite managedSite);

    /// <summary>
    /// Lists the permissions a Managed Site's role is created holding.
    /// </summary>
    /// <param name="managedSite">The Managed Site.</param>
    /// <returns>The permission names.</returns>
    IReadOnlyList<string> GetEditorPermissions(ManagedSite managedSite);

    /// <summary>
    /// Lists every role in the tenant, by name.
    /// </summary>
    /// <returns>The role names.</returns>
    ValueTask<IReadOnlyList<string>> ListAllRoleNamesAsync();

    /// <summary>
    /// Lists the roles that carry a Managed Site, whichever of its permissions they hold.
    /// </summary>
    /// <remarks>
    /// Found by what a role holds rather than by what it is called, so a role an administrator made
    /// themselves counts exactly as much as the one made with the site.
    /// </remarks>
    /// <param name="managedSite">The Managed Site.</param>
    /// <returns>The role names, ordered.</returns>
    ValueTask<IReadOnlyList<string>> ListCarryingRolesAsync(ManagedSite managedSite);
}

/// <summary>
/// Creates a Managed Site's editor role through the platform's own role manager.
/// </summary>
/// <remarks>
/// The role manager is resolved when it is wanted rather than injected, so that defining a Managed Site
/// works on a tenant not running the Roles feature. There, a Managed Site simply has no role made for
/// it, which is not a reason to refuse to define it.
/// </remarks>
public sealed class ManagedSiteRoleService : IManagedSiteRoleService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IManagedSiteMediaFolderService _mediaFolderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteRoleService" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider the role manager is read from.</param>
    /// <param name="mediaFolderService">The Managed Site media folder service.</param>
    public ManagedSiteRoleService(
        IServiceProvider serviceProvider,
        IManagedSiteMediaFolderService mediaFolderService)
    {
        _serviceProvider = serviceProvider;
        _mediaFolderService = mediaFolderService;
    }

    /// <inheritdoc />
    public string GetEditorRoleName(ManagedSite managedSite)
    {
        var name = managedSite?.Name?.Trim();

        return string.IsNullOrEmpty(name) ? null : $"{name} Editor";
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetEditorPermissions(ManagedSite managedSite)
    {
        if (string.IsNullOrEmpty(managedSite?.Code))
        {
            return [];
        }

        var folder = _mediaFolderService.GetFolder(managedSite.Code);

        return
        [
            // Everything one person needs, so holding this role alone is enough to work on the site:
            // the admin panel, the screens, authority over this Managed Site, and its own media folder.
            "AccessAdminPanel",
            Permissions.EditManagedSiteContent.Name,
            ManagedSiteContentPermissions.For(managedSite.Code, ManagedSitesConstants.Scopes.Edit).Name,
            ManagedSiteContentPermissions.For(managedSite.Code, ManagedSitesConstants.Scopes.Publish).Name,
            ManagedSiteContentPermissions.For(managedSite.Code, ManagedSitesConstants.Scopes.Preview).Name,
            MediaPermissions.ManageOwnMedia.Name,
            $"ViewMediaContent_{folder}",
        ];
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<string>> ListAllRoleNamesAsync()
    {
        var roleService = _serviceProvider.GetService<IRoleService>();

        if (roleService is null)
        {
            return [];
        }

        return [.. (await roleService.GetRolesAsync()).Select(role => role.RoleName)];
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<string>> ListCarryingRolesAsync(ManagedSite managedSite)
    {
        if (string.IsNullOrEmpty(managedSite?.Code))
        {
            return [];
        }

        var roleService = _serviceProvider.GetService<IRoleService>();

        if (roleService is null)
        {
            return [];
        }

        var carried = ManagedSiteContentPermissions.Templates
            .Select(template => ManagedSiteContentPermissions.Create(template, managedSite.Code).Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var carrying = new List<string>();

        foreach (var role in await roleService.GetRolesAsync())
        {
            var claims = await roleService.GetRoleClaimsAsync(role.RoleName);

            if (claims.Any(claim =>
                string.Equals(claim.Type, Permission.ClaimType, StringComparison.Ordinal)
                && carried.Contains(claim.Value)))
            {
                carrying.Add(role.RoleName);
            }
        }

        return [.. carrying.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <inheritdoc />
    public async ValueTask<string> EnsureEditorRoleAsync(ManagedSite managedSite)
    {
        var roleName = GetEditorRoleName(managedSite);

        if (roleName is null)
        {
            return null;
        }

        var roleManager = _serviceProvider.GetService<RoleManager<IRole>>();

        if (roleManager is null)
        {
            return null;
        }

        if (await roleManager.FindByNameAsync(roleManager.NormalizeKey(roleName)) is not null)
        {
            // Already there, and whatever it holds now is the administrator's business rather than
            // something to be put back the way this feature would have made it.
            return null;
        }

        var role = new Role
        {
            RoleName = roleName,
            RoleDescription = $"Works on the {managedSite.Name} managed site.",
            RoleClaims =
            [
                .. GetEditorPermissions(managedSite)
                    .Select(permission => new RoleClaim(Permission.ClaimType, permission)),
            ],
        };

        var result = await roleManager.CreateAsync(role);

        return result.Succeeded ? roleName : null;
    }
}
