namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Maps an incoming URL to either a Site Blueprint or a Managed Site.
/// </summary>
public sealed class UrlRegistration
{
    /// <summary>
    /// Gets or sets the stable URL registration identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the owner type for this registration.
    /// </summary>
    public UrlRegistrationOwnerType OwnerType { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the owning blueprint or managed site.
    /// </summary>
    public string OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the normalized URL path or host/path entry.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the registration status.
    /// </summary>
    public UrlRegistrationStatus Status { get; set; } = UrlRegistrationStatus.Active;
}

/// <summary>
/// Defines the owner type for a URL registration.
/// </summary>
public enum UrlRegistrationOwnerType
{
    /// <summary>
    /// The URL belongs to the Site Blueprint context.
    /// </summary>
    SiteBlueprint,

    /// <summary>
    /// The URL belongs to a Managed Site context.
    /// </summary>
    ManagedSite,
}

/// <summary>
/// Defines whether a URL registration participates in request resolution.
/// </summary>
public enum UrlRegistrationStatus
{
    /// <summary>
    /// The URL participates in request resolution.
    /// </summary>
    Active,

    /// <summary>
    /// The URL is retained but ignored during request resolution.
    /// </summary>
    Disabled,
}
