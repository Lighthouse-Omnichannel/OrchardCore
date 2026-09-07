namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// Represents Site Blueprint settings in the admin editor.
/// </summary>
public class SiteBlueprintSettingsViewModel
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
