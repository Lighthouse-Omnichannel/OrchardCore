namespace VendallionCMS.ManagedSites.Settings;

/// <summary>
/// Stores Site Blueprint configuration for the current site.
/// </summary>
public sealed class SiteBlueprintSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the current site is a Site Blueprint.
    /// </summary>
    public bool IsSiteBlueprint { get; set; }

    /// <summary>
    /// Gets or sets the display name for the Site Blueprint.
    /// </summary>
    public string Name { get; set; }
}
