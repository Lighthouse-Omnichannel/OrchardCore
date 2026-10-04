namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// What an override stands in for, shown on its editor.
/// </summary>
public class ManagedContentOverrideLinkViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site that owns the override.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the owning Managed Site, or <see langword="null" /> when it is gone.
    /// </summary>
    public string ManagedSiteName { get; set; }

    /// <summary>
    /// Gets or sets the content item this override replaces.
    /// </summary>
    public string SourceContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the stored content item holding the source.
    /// </summary>
    public string SourceContainerContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the display text of the source item.
    /// </summary>
    public string SourceDisplayText { get; set; }

    /// <summary>
    /// Gets or sets the content type of the source item.
    /// </summary>
    public string SourceContentType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the source is stored inside another item.
    /// </summary>
    public bool SourceIsContained { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the source could be reached at all.
    /// </summary>
    public bool IsLinked { get; set; }
}
