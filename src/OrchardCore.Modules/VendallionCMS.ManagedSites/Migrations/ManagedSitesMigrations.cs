using OrchardCore.Data.Migration;

namespace VendallionCMS.ManagedSites.Migrations;

/// <summary>
/// Creates and evolves Managed Sites persistence structures.
/// </summary>
public sealed class ManagedSitesMigrations : DataMigration
{
    /// <summary>
    /// Creates the initial Managed Sites schema version.
    /// </summary>
    /// <returns>The next schema version.</returns>
    public static Task<int> CreateAsync() => Task.FromResult(1);
}
