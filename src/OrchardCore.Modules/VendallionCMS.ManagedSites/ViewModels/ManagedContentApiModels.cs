namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// One Managed Content item a Managed Site may override.
/// </summary>
public sealed class ManagedContentListItem
{
    /// <summary>
    /// Gets or sets the source content item identifier.
    /// </summary>
    public string SourceContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the content type of the source item.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the display text of the source item.
    /// </summary>
    public string DisplayText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item holds child content of its own.
    /// </summary>
    /// <remarks>
    /// Overriding a container replaces its children, so the portal warns before the editor commits to it.
    /// </remarks>
    public bool IsContainer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item's display scope reaches the Managed Site.
    /// </summary>
    /// <remarks>
    /// Always true for an item saved since the display scope was required to cover the edit scope. It
    /// stays on the contract because data written before that rule, or imported by a recipe, can still
    /// carry an override that would never render.
    /// </remarks>
    public bool DisplayScopeIncludesManagedSite { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site's override of the item, or <see langword="null" /> when it holds none.
    /// </summary>
    public ManagedContentOverrideSummary Override { get; set; }
}

/// <summary>
/// The state of one Managed Site's override.
/// </summary>
public sealed class ManagedContentOverrideSummary
{
    /// <summary>
    /// Gets or sets the content item holding the override content.
    /// </summary>
    public string OverrideContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the override lifecycle status.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets why the override does not render, or <see langword="null" /> when it does.
    /// </summary>
    public string SuppressionReason { get; set; }

    /// <summary>
    /// Gets or sets the other content items claiming to override the same item, which are not served.
    /// </summary>
    public List<string> SupersededOverrideContentItemIds { get; set; } = [];
}

/// <summary>
/// The Managed Content items a Managed Site may override.
/// </summary>
public sealed class ManagedContentListResponse
{
    /// <summary>
    /// Gets or sets the items on the requested page.
    /// </summary>
    public List<ManagedContentListItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets how many items match before paging.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// One Managed Content item in detail, for the Managed Site named in the route.
/// </summary>
public sealed class ManagedContentDetailResponse
{
    /// <summary>
    /// Gets or sets the source content item identifier.
    /// </summary>
    public string SourceContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the content type of the source item.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the display text of the source item.
    /// </summary>
    public string DisplayText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Managed Site may override the item.
    /// </summary>
    public bool EditScopeIncludesManagedSite { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item renders for the Managed Site.
    /// </summary>
    public bool DisplayScopeIncludesManagedSite { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site's override of the item, or <see langword="null" /> when it holds none.
    /// </summary>
    public ManagedContentOverrideSummary Override { get; set; }
}

/// <summary>
/// Registers a content item as a Managed Site's override.
/// </summary>
public sealed class ManagedContentOverrideRequest
{
    /// <summary>
    /// Gets or sets the content item holding the override content.
    /// </summary>
    /// <remarks>
    /// The item is authored through platform content services and named here, so the override uses the
    /// same editor, validation, and lifecycle as any other content of its type.
    /// </remarks>
    public string OverrideContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the requested status, either Draft or Published.
    /// </summary>
    public string Status { get; set; }
}

/// <summary>
/// One override that exists but does not render.
/// </summary>
public sealed class SuppressedOverrideItem
{
    /// <summary>
    /// Gets or sets the source content item identifier.
    /// </summary>
    public string SourceContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the content item holding the override content.
    /// </summary>
    public string OverrideContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the override lifecycle status.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets why the override does not render.
    /// </summary>
    public string SuppressionReason { get; set; }
}

/// <summary>
/// The overrides a Managed Site holds that do not render.
/// </summary>
public sealed class SuppressedOverridesResponse
{
    /// <summary>
    /// Gets or sets the suppressed overrides.
    /// </summary>
    public List<SuppressedOverrideItem> Items { get; set; } = [];
}
