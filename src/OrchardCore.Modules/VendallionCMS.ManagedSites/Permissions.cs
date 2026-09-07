using OrchardCore;
using OrchardCore.Security.Permissions;

namespace VendallionCMS.ManagedSites;

/// <summary>
/// Defines permissions for Site Blueprint and Managed Site management.
/// </summary>
public sealed class Permissions : IPermissionProvider
{
    /// <summary>
    /// Allows management of Site Blueprint configuration and content.
    /// </summary>
    public static readonly Permission ManageSiteBlueprint = new("ManageSiteBlueprint", "Manage Site Blueprint");

    /// <summary>
    /// Allows management of Managed Site content and contributions.
    /// </summary>
    public static readonly Permission ManageManagedSites = new("ManageManagedSites", "Manage Managed Sites");

    private readonly IEnumerable<Permission> _allPermissions =
    [
        ManageSiteBlueprint,
        ManageManagedSites,
    ];

    /// <inheritdoc />
    public Task<IEnumerable<Permission>> GetPermissionsAsync() => Task.FromResult(_allPermissions);

    /// <inheritdoc />
    public IEnumerable<PermissionStereotype> GetDefaultStereotypes() =>
    [
        new PermissionStereotype
        {
            Name = OrchardCoreConstants.Roles.Administrator,
            Permissions = _allPermissions,
        },
    ];
}
