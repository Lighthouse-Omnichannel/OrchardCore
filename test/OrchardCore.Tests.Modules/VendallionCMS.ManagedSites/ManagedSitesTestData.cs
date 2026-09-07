using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Tests;

/// <summary>
/// Provides test data builders for Managed Sites tests.
/// </summary>
public static class ManagedSitesTestData
{
    /// <summary>
    /// Creates a test Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <returns>A test Managed Site.</returns>
    public static ManagedSite ManagedSite(string managedSiteId = "managed-site") => new()
    {
        Id = managedSiteId,
        BlueprintId = "blueprint",
        Name = "Managed Site",
        Status = ManagedSiteStatus.Enabled,
    };
}
