namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Stores the active Managed Site selected by each portal user.
/// </summary>
public sealed class ManagedSiteSessionsDocument
{
    /// <summary>
    /// Gets the active Managed Site session scopes, keyed by user identifier.
    /// </summary>
    public Dictionary<string, ActiveManagedSiteSessionScope> ActiveScopes { get; set; } =
        new(StringComparer.Ordinal);
}
