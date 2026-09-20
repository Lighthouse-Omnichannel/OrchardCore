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
    /// <returns>The suppression reason, or <see cref="ManagedContentOverrideSuppressionReason.None" />.</returns>
    ValueTask<ManagedContentOverrideSuppressionReason> EvaluateAsync(string managedSiteId, string sourceContentItemId);

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
    private readonly IContentManager _contentManager;
    private readonly IManagedSiteService _managedSiteService;
    private readonly IManagedContentScopeService _scopeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentSuppressionService" /> class.
    /// </summary>
    /// <param name="contentManager">The content manager.</param>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    public ManagedContentSuppressionService(
        IContentManager contentManager,
        IManagedSiteService managedSiteService,
        IManagedContentScopeService scopeService)
    {
        _contentManager = contentManager;
        _managedSiteService = managedSiteService;
        _scopeService = scopeService;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedContentOverrideSuppressionReason> EvaluateAsync(
        string managedSiteId,
        string sourceContentItemId)
    {
        var managedSite = await _managedSiteService.GetAsync(managedSiteId);
        var publishedSource = await _contentManager.GetAsync(sourceContentItemId, VersionOptions.Published);

        // Only ask for the draft when there is no published version, to tell a deleted item from an
        // unpublished one without a second load in the common case.
        var sourceExists = publishedSource is not null
            || await _contentManager.GetAsync(sourceContentItemId, VersionOptions.Latest) is not null;

        return Evaluate(managedSiteId, managedSite, publishedSource, sourceExists);
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

        // A Managed Site still being prepared is not broken, it is simply not live yet, so its overrides
        // wait rather than suppress. Only withdrawal from service suppresses them.
        if (managedSite is null
            || managedSite.Status == ManagedSiteStatus.Disabled
            || managedSite.Status == ManagedSiteStatus.Archived)
        {
            return ManagedContentOverrideSuppressionReason.ManagedSiteDisabled;
        }

        return ManagedContentOverrideSuppressionReason.None;
    }
}
