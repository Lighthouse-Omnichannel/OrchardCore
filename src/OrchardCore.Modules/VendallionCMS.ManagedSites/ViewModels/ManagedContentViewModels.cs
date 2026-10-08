using VendallionCMS.ManagedSites.Models;

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
    /// Gets or sets the identifier of the item this one is stored inside, which is its own when it is
    /// stored in its own right.
    /// </summary>
    public string ContainerContentItemId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item holds child content of its own.
    /// </summary>
    /// <remarks>
    /// Overriding a container replaces its children, so the portal warns before the editor commits to it.
    /// </remarks>
    public bool IsContainer { get; set; }

    /// <summary>
    /// Gets or sets how many items stored inside this one the Managed Site may customize.
    /// </summary>
    /// <remarks>
    /// Counted over everything the Managed Site may customize rather than over whatever the list is
    /// currently narrowed to, because this describes the container and not the search.
    /// </remarks>
    public int ContainedItemCount { get; set; }

    /// <summary>
    /// Gets or sets how many of those the Managed Site has its own version of.
    /// </summary>
    public int ContainedOverriddenCount { get; set; }

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

    /// <summary>
    /// Describes an override, or nothing when the Managed Site has none.
    /// </summary>
    /// <remarks>
    /// Absent rather than empty when there is no override, because "no version of your own" and "a
    /// version of your own that is not rendering" are different things to show an editor.
    /// </remarks>
    /// <param name="managedContentOverride">The override, or <see langword="null" />.</param>
    /// <returns>The summary, or <see langword="null" />.</returns>
    public static ManagedContentOverrideSummary Of(ManagedContentOverride managedContentOverride)
        => managedContentOverride is null
            ? null
            : new ManagedContentOverrideSummary
            {
                OverrideContentItemId = managedContentOverride.OverrideContentItemId,
                Status = managedContentOverride.Status.ToString(),
                SuppressionReason =
                    managedContentOverride.SuppressionReason == ManagedContentOverrideSuppressionReason.None
                        ? null
                        : managedContentOverride.SuppressionReason.ToString(),
                SupersededOverrideContentItemIds = [.. managedContentOverride.SupersededOverrideContentItemIds],
            };
}
