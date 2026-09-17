namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Represents a URL-scoped managed site within the tenant acting as the Site Blueprint.
/// </summary>
/// <remarks>
/// Addressed the way an OrchardCore tenant is: a <see cref="Hostname" /> holding one or more host names
/// and a single <see cref="UrlPrefix" />. A Managed Site expands to one address per host name, so the
/// matching and precedence rules an administrator already knows from tenants carry over unchanged.
/// </remarks>
public sealed class ManagedSite
{
    /// <summary>
    /// Gets or sets the stable managed-site identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the host names this Managed Site answers on, separated the same way a tenant
    /// separates its request hosts.
    /// </summary>
    /// <remarks>
    /// Empty answers on every host the tenant serves.
    /// </remarks>
    public string Hostname { get; set; }

    /// <summary>
    /// Gets or sets the URL path prefix this Managed Site answers under, empty for the root.
    /// </summary>
    public string UrlPrefix { get; set; }

    /// <summary>
    /// Gets or sets the managed-site lifecycle status.
    /// </summary>
    public ManagedSiteStatus Status { get; set; } = ManagedSiteStatus.Draft;
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
    /// The Managed Site is active and can resolve matching requests.
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
