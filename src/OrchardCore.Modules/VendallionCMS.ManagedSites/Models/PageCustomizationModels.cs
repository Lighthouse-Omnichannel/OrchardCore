namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Describes a Site Blueprint page and its managed-site customization policy.
/// </summary>
public sealed class BlueprintPage
{
    /// <summary>
    /// Gets or sets the blueprint page content item identifier.
    /// </summary>
    public string ContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the owning Site Blueprint identifier.
    /// </summary>
    public string BlueprintId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether managed-site page overrides are allowed.
    /// </summary>
    public bool AllowManagedSiteOverride { get; set; }
}

/// <summary>
/// Represents managed-site-owned content that replaces an overrideable blueprint page.
/// </summary>
public sealed class ManagedSitePageOverride
{
    /// <summary>
    /// Gets or sets the stable override identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the owning Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the target blueprint page content item identifier.
    /// </summary>
    public string BlueprintPageContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the managed-site content item used as the override.
    /// </summary>
    public string OverrideContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the override lifecycle status.
    /// </summary>
    public ManagedSiteCustomizationStatus Status { get; set; } = ManagedSiteCustomizationStatus.Draft;
}

/// <summary>
/// Represents a blueprint-defined location where Managed Sites can add layout-rendered content.
/// </summary>
public sealed class LayerContributionPoint
{
    /// <summary>
    /// Gets or sets the stable contribution point identifier.
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
    /// Gets or sets the target layer name.
    /// </summary>
    public string LayerName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the contribution point is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Represents managed-site-owned content assigned to a layer contribution point.
/// </summary>
public sealed class ManagedSiteLayerContribution
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
    /// Gets or sets the target contribution point identifier.
    /// </summary>
    public string ContributionPointId { get; set; }

    /// <summary>
    /// Gets the ordered content item identifiers.
    /// </summary>
    public IList<string> ContentItemIds { get; } = [];

    /// <summary>
    /// Gets or sets the contribution lifecycle status.
    /// </summary>
    public ManagedSiteCustomizationStatus Status { get; set; } = ManagedSiteCustomizationStatus.Draft;
}

/// <summary>
/// Represents a blueprint content area intended to be filled by Managed Site content.
/// </summary>
public sealed class ManagedSitePlaceholder
{
    /// <summary>
    /// Gets or sets the stable placeholder identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the owning blueprint page content item identifier.
    /// </summary>
    public string BlueprintPageContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the blueprint fallback content item identifiers.
    /// </summary>
    public IList<string> FallbackContentItemIds { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the placeholder is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Represents managed-site content assigned to a blueprint placeholder.
/// </summary>
public sealed class PlaceholderAssignment
{
    /// <summary>
    /// Gets or sets the stable assignment identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the owning Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the target placeholder identifier.
    /// </summary>
    public string PlaceholderId { get; set; }

    /// <summary>
    /// Gets the ordered content item identifiers.
    /// </summary>
    public IList<string> ContentItemIds { get; } = [];

    /// <summary>
    /// Gets or sets the assignment lifecycle status.
    /// </summary>
    public ManagedSiteCustomizationStatus Status { get; set; } = ManagedSiteCustomizationStatus.Draft;
}

/// <summary>
/// Defines lifecycle states shared by managed-site customization records.
/// </summary>
public enum ManagedSiteCustomizationStatus
{
    /// <summary>
    /// The customization is being prepared.
    /// </summary>
    Draft,

    /// <summary>
    /// The customization is active for rendering.
    /// </summary>
    Published,

    /// <summary>
    /// The customization is retained but not rendered.
    /// </summary>
    Disabled,

    /// <summary>
    /// The customization is retained for review, reassignment, cleanup, or recovery.
    /// </summary>
    Recoverable,
}
