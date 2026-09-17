using VendallionCMS.ManagedSites.Models;
using YesSql.Indexes;

namespace VendallionCMS.ManagedSites.Indexes;

/// <summary>
/// Indexes Managed Site documents by address and status.
/// </summary>
public sealed class ManagedSiteIndex : MapIndex
{
    /// <summary>
    /// Gets or sets the Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets one host name the Managed Site answers on, empty when it answers on every host.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site URL prefix.
    /// </summary>
    public string UrlPrefix { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site status.
    /// </summary>
    public ManagedSiteStatus Status { get; set; }
}
