namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Represents signed authorization clearance assigning a user to a Managed Site.
/// </summary>
public sealed class ManagedSiteClearance
{
    /// <summary>
    /// Gets or sets the user identity reference.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets the permitted action scopes.
    /// </summary>
    public ISet<string> Scopes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the optional time from which this clearance is effective.
    /// </summary>
    public DateTimeOffset? EffectiveFrom { get; set; }

    /// <summary>
    /// Gets or sets the optional time after which this clearance expires.
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; set; }
}
