using OrchardCore;
using OrchardCore.Security.Permissions;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites;

/// <summary>
/// Defines the two permissions the feature uses.
/// </summary>
/// <remarks>
/// The split is between governing Managed Sites and working inside one. Governing covers everything that
/// decides what a Managed Site is and what it may reach: defining the Managed Sites themselves, choosing
/// which Site Blueprint content they may override, and granting people clearance to them. Working inside
/// one covers editing that Managed Site's own content, which is bounded further by the clearance the
/// person holds, Managed Site by Managed Site.
///
/// Keeping the line there is what stops a Managed Site editor widening their own reach: all three ways
/// of doing so, defining a new Managed Site, putting more content in scope, and granting clearance, sit
/// on the governing side, and none of them is reachable with the editing permission alone.
/// </remarks>
public sealed class Permissions : IPermissionProvider
{
    /// <summary>
    /// Allows governing Managed Sites: defining them, deciding which content they may override, and
    /// granting users clearance to them.
    /// </summary>
    /// <remarks>
    /// Security critical, because granting clearance decides who may change Managed Site content.
    /// </remarks>
    public static readonly Permission ManageManagedSites = new(
        "ManageManagedSites",
        "Manage Managed Sites",
        isSecurityCritical: true);

    /// <summary>
    /// Allows editing the content a Managed Site overrides, through its content screens in the admin.
    /// </summary>
    /// <remarks>
    /// Opens the content screens and nothing more. Which Managed Sites they open onto, and what may be
    /// done in each, comes from the permissions generated per Managed Site, exactly as the media
    /// library is opened by one permission and its folders decided by others.
    ///
    /// Implied by <see cref="ManageManagedSites" />, because whoever grants clearance can grant it to
    /// themselves; withholding the screens from them would be a formality rather than a restriction.
    /// </remarks>
    public static readonly Permission EditManagedSiteContent = new(
        "EditManagedSiteContent",
        "Edit Managed Site content",
        [ManageManagedSites]);

    private static readonly Permission[] _allPermissions =
    [
        ManageManagedSites,
        EditManagedSiteContent,
    ];

    private readonly IManagedSiteService _managedSiteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="Permissions" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site definition service.</param>
    public Permissions(IManagedSiteService managedSiteService)
    {
        _managedSiteService = managedSiteService;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The defined Managed Sites are listed so each contributes its own permissions, which is how they
    /// reach the roles screen without anybody registering them one at a time. Disabled Managed Sites
    /// are included: a site that is switched off still has content somebody may be given authority over
    /// before it is switched back on, and dropping its permissions would quietly revoke them.
    /// </remarks>
    public async Task<IEnumerable<Permission>> GetPermissionsAsync()
    {
        var managedSites = await _managedSiteService.ListAsync();

        if (managedSites.Count == 0)
        {
            return _allPermissions;
        }

        var permissions = new List<Permission>(_allPermissions);

        foreach (var managedSite in managedSites)
        {
            permissions.AddRange(
                ManagedSiteContentPermissions.ForManagedSite(managedSite.Code, managedSite.Name));
        }

        return permissions;
    }

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
