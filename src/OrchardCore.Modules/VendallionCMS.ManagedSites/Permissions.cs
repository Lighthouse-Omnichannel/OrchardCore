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

    /// <summary>
    /// Allows granting users clearance to manage specific Managed Sites.
    /// </summary>
    /// <remarks>
    /// Assigning clearance decides who may change Managed Site content, so it is held separately from
    /// managing that content. A Managed Site editor must not be able to widen their own access.
    /// </remarks>
    public static readonly Permission ManageManagedSiteClearances = new(
        "ManageManagedSiteClearances",
        "Manage Managed Site clearances",
        [ManageSiteBlueprint],
        isSecurityCritical: true);

    private readonly IEnumerable<Permission> _allPermissions =
    [
        ManageSiteBlueprint,
        ManageManagedSites,
        ManageManagedSiteClearances,
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
