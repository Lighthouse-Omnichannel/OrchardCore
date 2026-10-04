using OrchardCore;
using OrchardCore.Security.Permissions;

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
    /// Allows editing the content a Managed Site overrides, through the Managed Site Admin Portal.
    /// </summary>
    /// <remarks>
    /// Granted alongside clearance rather than instead of it. This permission opens the portal; which
    /// Managed Sites it opens onto, and what may be done in each, comes from the holder's clearance.
    ///
    /// Implied by <see cref="ManageManagedSites" />, because whoever grants clearance can grant it to
    /// themselves; withholding the portal from them would be a formality rather than a restriction.
    /// </remarks>
    public static readonly Permission EditManagedSiteContent = new(
        "EditManagedSiteContent",
        "Edit Managed Site content",
        [ManageManagedSites]);

    private readonly IEnumerable<Permission> _allPermissions =
    [
        ManageManagedSites,
        EditManagedSiteContent,
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
