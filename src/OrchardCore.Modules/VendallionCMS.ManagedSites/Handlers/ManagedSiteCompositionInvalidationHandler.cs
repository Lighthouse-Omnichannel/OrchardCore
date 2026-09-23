using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites.Handlers;

/// <summary>
/// Drops cached composition state when published content changes what a Managed Site would serve.
/// </summary>
/// <remarks>
/// Only published changes are acted on. A draft changes nothing a visitor receives, so invalidating on
/// every save would throw away caches to no effect, and repeatedly while somebody is typing.
///
/// Which Managed Site is affected depends on what changed. An override belongs to one Managed Site, so
/// only that one is dropped. Site Blueprint content is composed by all of them, so all of them are.
/// A change to an item's scopes is the same case as blueprint content: the set of Managed Sites that
/// may now see it is precisely what changed, so the old answer is no longer trustworthy for any of them.
/// </remarks>
public sealed class ManagedSiteCompositionInvalidationHandler : ContentHandlerBase
{
    private readonly IManagedSiteCompositionCacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteCompositionInvalidationHandler" /> class.
    /// </summary>
    /// <param name="cacheService">The composition cache service.</param>
    public ManagedSiteCompositionInvalidationHandler(IManagedSiteCompositionCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    /// <inheritdoc />
    public override Task PublishedAsync(PublishContentContext context) => InvalidateAsync(context.ContentItem);

    /// <inheritdoc />
    public override Task UnpublishedAsync(PublishContentContext context) => InvalidateAsync(context.ContentItem);

    /// <inheritdoc />
    public override Task RemovedAsync(RemoveContentContext context) => InvalidateAsync(context.ContentItem);

    private async Task InvalidateAsync(ContentItem contentItem)
    {
        if (contentItem is null)
        {
            return;
        }

        if (contentItem.TryGet<ManagedContentOverridePart>(out var overridePart)
            && !string.IsNullOrEmpty(overridePart.ManagedSiteId))
        {
            await _cacheService.InvalidateAsync(overridePart.ManagedSiteId, ManagedSiteCompositionArea.Override);

            return;
        }

        if (contentItem.Has(nameof(ManagedContentPart)))
        {
            // The item is customizable, so its scopes decide who sees it and its content decides what
            // they see. Both reach every Managed Site.
            await _cacheService.InvalidateAsync(
                managedSiteId: null,
                ManagedSiteCompositionArea.ManagedContentScope);
        }

        await _cacheService.InvalidateAsync(managedSiteId: null, ManagedSiteCompositionArea.Content);
    }
}
