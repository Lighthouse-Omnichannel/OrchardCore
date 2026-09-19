using OrchardCore.ContentManagement;

namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Marks a content item as customizable per Managed Site.
/// </summary>
/// <remarks>
/// This is the single mechanism for Managed Site customization. Pages, navigation entries, layer
/// widgets, and page sections are all content items, so attaching this part to a content type covers
/// every case that would otherwise need a dedicated model, driver, and editor.
///
/// The part carries two independent scopes. <see cref="EditScope" /> answers who may override the item;
/// <see cref="DisplayScope" /> answers who renders it. Keeping them apart allows an item that is visible
/// everywhere but editable by nobody, and an item shown only to the Managed Sites that may edit it.
///
/// The defaults make attachment observably inert: nothing renders differently and no Managed Site gains
/// authority until a Site Blueprint administrator configures a scope.
/// </remarks>
public class ManagedContentPart : ContentPart
{
    /// <summary>
    /// Gets or sets the Managed Sites that may override this item.
    /// </summary>
    /// <remarks>
    /// Defaults to none, so attaching the part never grants write access to blueprint content.
    /// </remarks>
    public ManagedContentScope EditScope { get; set; } = ManagedContentScope.None();

    /// <summary>
    /// Gets or sets the Managed Sites that render this item.
    /// </summary>
    /// <remarks>
    /// Defaults to every Managed Site, so attaching the part never hides content that was visible.
    /// </remarks>
    public ManagedContentScope DisplayScope { get; set; } = ManagedContentScope.All();

    /// <summary>
    /// Gets or sets a value indicating whether the item renders when no Managed Site is resolved.
    /// </summary>
    /// <remarks>
    /// Defaults to true, so a request that matches no Managed Site keeps rendering the item as before.
    /// </remarks>
    public bool DisplayInBlueprintContext { get; set; } = true;

    /// <summary>
    /// Widens the display scope so every Managed Site allowed to override the item also renders it.
    /// </summary>
    /// <remarks>
    /// An override nobody can see is a trap: the Managed Site administrator edits content that never
    /// reaches their visitors. The editor disables the matching display control for the same reason;
    /// this keeps the rule true no matter how the part is written.
    ///
    /// Only the display scope moves. The edit scope is the authority being granted, so it is never
    /// narrowed to satisfy the rule.
    /// </remarks>
    public void EnsureDisplayCoversEdit()
    {
        var editScope = EditScope;

        if (editScope is null || editScope.Mode == ManagedContentScopeMode.None)
        {
            return;
        }

        if (editScope.Mode == ManagedContentScopeMode.All)
        {
            DisplayScope = ManagedContentScope.All();

            return;
        }

        if (editScope.ManagedSiteIds.Count == 0 || DisplayScope?.Mode == ManagedContentScopeMode.All)
        {
            return;
        }

        var managedSiteIds = new List<string>();

        if (DisplayScope?.Mode == ManagedContentScopeMode.Selected)
        {
            managedSiteIds.AddRange(DisplayScope.ManagedSiteIds);
        }

        foreach (var managedSiteId in editScope.ManagedSiteIds)
        {
            if (!string.IsNullOrEmpty(managedSiteId) && !managedSiteIds.Contains(managedSiteId, StringComparer.Ordinal))
            {
                managedSiteIds.Add(managedSiteId);
            }
        }

        DisplayScope = new ManagedContentScope
        {
            Mode = ManagedContentScopeMode.Selected,
            ManagedSiteIds = managedSiteIds,
        };
    }
}
