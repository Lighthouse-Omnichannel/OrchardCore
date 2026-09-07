namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Represents a URL-scoped managed site within a Site Blueprint.
/// </summary>
public sealed class ManagedSite
{
    /// <summary>
    /// Gets or sets the stable managed-site identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the parent Site Blueprint identifier.
    /// </summary>
    public string BlueprintId { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the managed-site lifecycle status.
    /// </summary>
    public ManagedSiteStatus Status { get; set; } = ManagedSiteStatus.Draft;

    /// <summary>
    /// Gets the URLs mapped to this Managed Site.
    /// </summary>
    public IList<UrlRegistration> UrlRegistrations { get; } = [];
}

/// <summary>
/// Describes the lifecycle state of a Managed Site.
/// </summary>
public enum ManagedSiteStatus
{
    /// <summary>
    /// The Managed Site is being prepared and should not receive public traffic.
    /// </summary>
    Draft,

    /// <summary>
    /// The Managed Site is active and can resolve matching URLs.
    /// </summary>
    Enabled,

    /// <summary>
    /// The Managed Site is temporarily disabled and should reject editor mutations.
    /// </summary>
    Disabled,

    /// <summary>
    /// The Managed Site is retained for audit or recovery but no longer participates in rendering.
    /// </summary>
    Archived,
}
