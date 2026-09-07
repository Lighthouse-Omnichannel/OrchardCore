namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Stores Managed Sites configuration for the Site Blueprint tenant.
/// </summary>
public sealed class ManagedSitesDocument
{
    /// <summary>
    /// Gets the Managed Sites defined for the Site Blueprint.
    /// </summary>
    public IList<ManagedSite> ManagedSites { get; } = [];

    /// <summary>
    /// Gets URL registrations owned by the Site Blueprint and Managed Sites.
    /// </summary>
    public IList<UrlRegistration> UrlRegistrations { get; } = [];
}
