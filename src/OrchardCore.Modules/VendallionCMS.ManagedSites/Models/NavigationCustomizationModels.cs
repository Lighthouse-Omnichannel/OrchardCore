namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Represents a Site Blueprint navigation placeholder that Managed Sites can populate.
/// </summary>
public sealed class MenuPlaceholder
{
    /// <summary>
    /// Gets or sets the stable placeholder identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the owning Site Blueprint identifier.
    /// </summary>
    public string BlueprintId { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the placeholder type.
    /// </summary>
    public MenuPlaceholderType Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the placeholder is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Represents managed-site-owned navigation entries assigned to a Site Blueprint placeholder.
/// </summary>
public sealed class ManagedNavigationContribution
{
    /// <summary>
    /// Gets or sets the stable contribution identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the owning Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the target menu placeholder identifier.
    /// </summary>
    public string MenuPlaceholderId { get; set; }

    /// <summary>
    /// Gets the ordered menu item identifiers.
    /// </summary>
    public IList<string> MenuItemIds { get; } = [];

    /// <summary>
    /// Gets or sets the contribution lifecycle status.
    /// </summary>
    public ManagedSiteCustomizationStatus Status { get; set; } = ManagedSiteCustomizationStatus.Draft;
}

/// <summary>
/// Defines whether a navigation placeholder replaces a menu or a menu item.
/// </summary>
public enum MenuPlaceholderType
{
    /// <summary>
    /// The placeholder represents an entire menu.
    /// </summary>
    PlaceholderMenu,

    /// <summary>
    /// The placeholder represents an item within a menu.
    /// </summary>
    PlaceholderMenuItem,
}
