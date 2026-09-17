namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Managed Site clearance granted to a single user, stored on the user entity.
/// </summary>
/// <remarks>
/// This is the durable grant record. It is the source the claims provider reads when building the
/// signed principal; whether a grant is currently effective is decided later against Managed Site
/// definitions, so revoking or disabling a Managed Site takes effect without rewriting grants.
/// </remarks>
public sealed class ManagedSiteClearanceSettings
{
    /// <summary>
    /// Gets the Managed Site grants held by the user.
    /// </summary>
    public List<ManagedSiteClearanceGrant> Grants { get; set; } = [];
}

/// <summary>
/// A grant of Managed Site clearance covering one Managed Site.
/// </summary>
public sealed class ManagedSiteClearanceGrant
{
    /// <summary>
    /// Gets or sets the Managed Site the grant covers.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets the action scopes the grant allows.
    /// </summary>
    /// <remarks>
    /// An empty set grants every scope, matching the wildcard form of the clearance claim.
    /// </remarks>
    public List<string> Scopes { get; set; } = [];
}
