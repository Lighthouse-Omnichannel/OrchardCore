using VendallionCMS.ManagedSites.Models;
using YesSql.Indexes;

namespace VendallionCMS.ManagedSites.Indexes;

/// <summary>
/// Indexes Managed Site documents by blueprint and status.
/// </summary>
public sealed class ManagedSiteIndex : MapIndex
{
    /// <summary>
    /// Gets or sets the Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the parent Site Blueprint identifier.
    /// </summary>
    public string BlueprintId { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site status.
    /// </summary>
    public ManagedSiteStatus Status { get; set; }
}

/// <summary>
/// Indexes URL registrations by normalized URL and owner.
/// </summary>
public sealed class UrlRegistrationIndex : MapIndex
{
    /// <summary>
    /// Gets or sets the URL registration identifier.
    /// </summary>
    public string UrlRegistrationId { get; set; }

    /// <summary>
    /// Gets or sets the normalized URL.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the owner type.
    /// </summary>
    public UrlRegistrationOwnerType OwnerType { get; set; }

    /// <summary>
    /// Gets or sets the owner identifier.
    /// </summary>
    public string OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the registration status.
    /// </summary>
    public UrlRegistrationStatus Status { get; set; }
}
