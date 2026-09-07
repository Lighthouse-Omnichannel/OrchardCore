namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Represents the primary site context that owns common content, navigation structure, placeholders, and composition rules.
/// </summary>
public sealed class SiteBlueprint
{
    /// <summary>
    /// Gets or sets the stable blueprint identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name shown to administrators.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether blueprint composition is active.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets the URL registrations owned by the blueprint context.
    /// </summary>
    public IList<UrlRegistration> UrlRegistrations { get; } = [];
}
