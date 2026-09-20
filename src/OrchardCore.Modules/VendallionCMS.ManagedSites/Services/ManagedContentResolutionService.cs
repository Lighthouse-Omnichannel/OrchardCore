using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Resolves one content item to the content that renders for a request context.
/// </summary>
/// <remarks>
/// The order of the questions is the whole rule. Display scope is asked first, so an item excluded from
/// a Managed Site renders nothing there even when that Managed Site holds a published override. Only
/// then does the override question arise, and only for a Managed Site still allowed to override the
/// item, so withdrawing the edit scope withdraws the override with it.
///
/// Every item is resolved on its own. No ordering between items is needed, which is what removed the
/// precedence rules the earlier multi-mechanism design carried.
/// </remarks>
public sealed class ManagedContentResolutionService : IManagedContentResolutionService
{
    private readonly IManagedContentScopeService _scopeService;
    private readonly IManagedContentOverrideService _overrideService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentResolutionService" /> class.
    /// </summary>
    /// <param name="scopeService">The Managed Content scope service.</param>
    /// <param name="overrideService">The Managed Content override service.</param>
    public ManagedContentResolutionService(
        IManagedContentScopeService scopeService,
        IManagedContentOverrideService overrideService)
    {
        _scopeService = scopeService;
        _overrideService = overrideService;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedContentResolution> ResolveAsync(ContentItem contentItem, string managedSiteId)
    {
        if (contentItem is null)
        {
            return ManagedContentResolution.Hidden();
        }

        // An item that never got the capability is outside the feature entirely.
        if (!contentItem.TryGet<ManagedContentPart>(out var part))
        {
            return ManagedContentResolution.Original(contentItem);
        }

        if (!_scopeService.CanDisplay(part, managedSiteId))
        {
            return ManagedContentResolution.Hidden();
        }

        // The Site Blueprint context is the original content by definition; only a Managed Site holds
        // an override.
        if (string.IsNullOrEmpty(managedSiteId) || !_scopeService.CanEdit(part, managedSiteId))
        {
            return ManagedContentResolution.Original(contentItem);
        }

        var overrideContent = await _overrideService.FindPublishedOverrideAsync(
            managedSiteId,
            contentItem.ContentItemId);

        return overrideContent is null
            ? ManagedContentResolution.Original(contentItem)
            : ManagedContentResolution.Override(overrideContent);
    }
}

/// <summary>
/// Holds the Managed Site resolved for the current request.
/// </summary>
/// <remarks>
/// Scoped, so it lives exactly as long as the request that resolved it. Outside a request, and before
/// the request pipeline has resolved anything, <see cref="Current" /> is null, which every consumer
/// reads as the Site Blueprint context.
/// </remarks>
public sealed class ManagedSiteCompositionContextAccessor : IManagedSiteCompositionContextAccessor
{
    /// <inheritdoc />
    public ManagedSiteRequestContext Current { get; set; }
}
