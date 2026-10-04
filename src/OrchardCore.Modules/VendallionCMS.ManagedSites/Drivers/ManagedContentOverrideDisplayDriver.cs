using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Drivers;

/// <summary>
/// Shows an override what it stands in for.
/// </summary>
/// <remarks>
/// An override is a content item of the same type as the item it replaces, so its editor is the same
/// editor, and nothing on it would otherwise say that it is an override at all, which Managed Site it
/// belongs to, or which item it answers. That is the one thing an editor opening it most needs to know.
///
/// The link is shown rather than offered. Which item an override replaces is settled when it is
/// created, and letting it be retyped here would let one Managed Site's content be pointed at another
/// Managed Site's item.
///
/// Registered as a content display driver rather than a part display driver because the part is not in
/// any content type definition: it is welded on by the override service, and a part driver only runs
/// for parts the type declares.
/// </remarks>
public sealed class ManagedContentOverrideDisplayDriver : ContentDisplayDriver
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IManagedContentLocator _locator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentOverrideDisplayDriver" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="locator">The Managed Content locator.</param>
    public ManagedContentOverrideDisplayDriver(
        IManagedSiteService managedSiteService,
        IManagedContentLocator locator)
    {
        _managedSiteService = managedSiteService;
        _locator = locator;
    }

    /// <inheritdoc />
    public override IDisplayResult Edit(ContentItem contentItem, BuildEditorContext context)
    {
        if (!contentItem.TryGet<ManagedContentOverridePart>(out var part))
        {
            return null;
        }

        return Initialize<ManagedContentOverrideLinkViewModel>("ManagedContentOverrideLink", async model =>
        {
            var managedSite = await _managedSiteService.GetAsync(part.ManagedSiteId);

            model.ManagedSiteId = part.ManagedSiteId;
            model.ManagedSiteName = managedSite?.Name;
            model.SourceContentItemId = part.SourceContentItemId;
            model.SourceContainerContentItemId = part.SourceContainerContentItemId;

            var source = await _locator.FindAsync(
                part.SourceContentItemId,
                part.SourceContainerContentItemId);

            model.SourceDisplayText = source?.ContentItem?.DisplayText;
            model.SourceContentType = source?.ContentItem?.ContentType;
            model.SourceIsContained = source?.IsContained == true;
            model.IsLinked = source is not null;
        })
        .Location("Content:before");
    }

    /// <inheritdoc />
    public override async Task<IDisplayResult> UpdateAsync(ContentItem contentItem, UpdateEditorContext context)
    {
        if (!contentItem.TryGet<ManagedContentOverridePart>(out var part))
        {
            return null;
        }

        // Nothing is read back from the editor: the link is not the editor's to change. What is done
        // here is completing it, for an override written before the container was recorded, so
        // suppression can still reach its source once the edit scope that created it is withdrawn.
        if (string.IsNullOrEmpty(part.SourceContainerContentItemId)
            && !string.IsNullOrEmpty(part.SourceContentItemId))
        {
            var source = await _locator.FindAsync(part.SourceContentItemId);

            if (source is not null)
            {
                part.SourceContainerContentItemId = source.Container.ContentItemId;
                contentItem.Apply(nameof(ManagedContentOverridePart), part);
            }
        }

        return Edit(contentItem, context);
    }
}
