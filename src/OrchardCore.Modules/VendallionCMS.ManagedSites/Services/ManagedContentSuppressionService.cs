using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Decides whether an override still renders, and says why when it does not.
/// </summary>
public interface IManagedContentSuppressionService
{
    /// <summary>
    /// Evaluates an override against the current state of its Managed Site and source item.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site that owns the override.</param>
    /// <param name="sourceContentItemId">The Managed Content item the override replaces.</param>
    /// <param name="sourceContainerContentItemId">The stored item the source lives in, when known.</param>
    /// <returns>The suppression reason, or <see cref="ManagedContentOverrideSuppressionReason.None" />.</returns>
    ValueTask<ManagedContentOverrideSuppressionReason> EvaluateAsync(
        string managedSiteId,
        string sourceContentItemId,
        string sourceContainerContentItemId = null);

    /// <summary>
    /// Evaluates an override from state the caller already holds.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site that owns the override.</param>
    /// <param name="managedSite">The owning Managed Site, or <see langword="null" /> when it is gone.</param>
    /// <param name="publishedSource">The published source item, or <see langword="null" /> when it is not published.</param>
    /// <param name="sourceExists">Whether the source content item still exists in any version.</param>
    /// <returns>The suppression reason, or <see cref="ManagedContentOverrideSuppressionReason.None" />.</returns>
    ManagedContentOverrideSuppressionReason Evaluate(
        string managedSiteId,
        ManagedSite managedSite,
        ContentItem publishedSource,
        bool sourceExists);
}

/// <summary>
/// Derives suppression from the current state of the Managed Site and the source content item.
/// </summary>
/// <remarks>
/// Suppression is computed on read rather than stored, because everything that causes it happens
/// somewhere other than the override: a blueprint administrator narrows an edit scope, unpublishes the
/// source, or detaches the capability. A stored flag would only be as fresh as the last time somebody
/// saved the override, so a revoked scope could keep rendering.
///
/// Reasons are reported most specific first. An editor can act on a source item or an edit scope;
/// the state of the Managed Site as a whole is the fallback explanation.
/// </remarks>
public sealed class ManagedContentSuppressionService : IManagedContentSuppressionService
{
    private readonly IManagedContentLocator _locator;
    private readonly IManagedSiteService _managedSiteService;
    private readonly IManagedContentScopeService _scopeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentSuppressionService" /> class.
    /// </summary>
    /// <param name="locator">The Managed Content locator.</param>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    public ManagedContentSuppressionService(
        IManagedContentLocator locator,
        IManagedSiteService managedSiteService,
        IManagedContentScopeService scopeService)
    {
        _locator = locator;
        _managedSiteService = managedSiteService;
        _scopeService = scopeService;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedContentOverrideSuppressionReason> EvaluateAsync(
        string managedSiteId,
        string sourceContentItemId,
        string sourceContainerContentItemId = null)
    {
        var managedSite = await _managedSiteService.GetAsync(managedSiteId);

        var published = await _locator.FindAsync(
            sourceContentItemId,
            sourceContainerContentItemId,
            VersionOptions.Published);

        // Only look for the draft when there is no published version, to tell a deleted item from an
        // unpublished one without a second load in the common case. A contained item is published
        // exactly when the item storing it is, because it has no version of its own.
        var sourceExists = published is not null
            || await _locator.FindAsync(sourceContentItemId, sourceContainerContentItemId, VersionOptions.Latest)
                is not null;

        return Evaluate(managedSiteId, managedSite, published?.ContentItem, sourceExists);
    }

    /// <inheritdoc />
    public ManagedContentOverrideSuppressionReason Evaluate(
        string managedSiteId,
        ManagedSite managedSite,
        ContentItem publishedSource,
        bool sourceExists)
    {
        if (!sourceExists)
        {
            return ManagedContentOverrideSuppressionReason.SourceDeleted;
        }

        if (publishedSource is null)
        {
            return ManagedContentOverrideSuppressionReason.SourceUnpublished;
        }

        if (!publishedSource.TryGet<ManagedContentPart>(out var part))
        {
            return ManagedContentOverrideSuppressionReason.CapabilityDetached;
        }

        if (!_scopeService.CanEdit(part, managedSiteId))
        {
            return ManagedContentOverrideSuppressionReason.EditScopeRemoved;
        }

        if (managedSite is null || managedSite.Status != ManagedSiteStatus.Enabled)
        {
            return ManagedContentOverrideSuppressionReason.ManagedSiteDisabled;
        }

        return ManagedContentOverrideSuppressionReason.None;
    }
}
