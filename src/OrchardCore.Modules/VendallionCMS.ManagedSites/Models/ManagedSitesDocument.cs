namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Stores Managed Sites configuration for the tenant acting as the Site Blueprint.
/// </summary>
public sealed class ManagedSitesDocument
{
    /// <summary>
    /// Gets or sets the Managed Sites defined in the tenant.
    /// </summary>
    public List<ManagedSite> ManagedSites { get; set; } = [];

    /// <summary>
    /// Gets or sets the host names this module last wrote into the tenant hostname setting.
    /// </summary>
    /// <remarks>
    /// Tracked so synchronization can withdraw exactly the host names it previously added, leaving host
    /// names an operator configured on the tenant untouched.
    /// </remarks>
    public List<string> AppliedShellHosts { get; set; } = [];
}
