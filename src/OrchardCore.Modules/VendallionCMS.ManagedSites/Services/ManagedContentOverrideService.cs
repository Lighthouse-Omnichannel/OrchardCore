using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Models;
using YesSql;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Why an override could not be saved or removed.
/// </summary>
public enum ManagedContentOverrideError
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    None,

    /// <summary>
    /// The owning Managed Site does not exist, or no longer accepts editor changes.
    /// </summary>
    ManagedSiteUnavailable,

    /// <summary>
    /// The source content item does not exist or is not published.
    /// </summary>
    SourceNotFound,

    /// <summary>
    /// The source content item does not carry Managed Content.
    /// </summary>
    SourceNotManagedContent,

    /// <summary>
    /// The source item's edit scope does not cover the Managed Site.
    /// </summary>
    EditScopeExcluded,

    /// <summary>
    /// The content item named as the override does not exist.
    /// </summary>
    OverrideNotFound,

    /// <summary>
    /// The override content item is not of the source item's content type.
    /// </summary>
    ContentTypeMismatch,

    /// <summary>
    /// A different content item already overrides this item for this Managed Site.
    /// </summary>
    OverrideAlreadyExists,
}

/// <summary>
/// The outcome of writing or removing an override.
/// </summary>
public sealed class ManagedContentOverrideResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded => Error == ManagedContentOverrideError.None;

    /// <summary>
    /// Gets why the operation failed.
    /// </summary>
    public ManagedContentOverrideError Error { get; init; }

    /// <summary>
    /// Gets the resulting override, when the operation succeeded.
    /// </summary>
    public ManagedContentOverride Override { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="managedContentOverride">The resulting override.</param>
    /// <returns>The result.</returns>
    public static ManagedContentOverrideResult Success(ManagedContentOverride managedContentOverride)
        => new() { Override = managedContentOverride };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">Why the operation failed.</param>
    /// <returns>The result.</returns>
    public static ManagedContentOverrideResult Failed(ManagedContentOverrideError error)
        => new() { Error = error };
}

/// <summary>
/// Owns the Managed Site overrides of Managed Content items.
/// </summary>
public interface IManagedContentOverrideService
{
    /// <summary>
    /// Gets the override a Managed Site holds for one item.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <returns>The override, or <see langword="null" /> when the Managed Site holds none.</returns>
    ValueTask<ManagedContentOverride> GetAsync(string managedSiteId, string sourceContentItemId);

    /// <summary>
    /// Lists every override a Managed Site holds.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <returns>The overrides.</returns>
    ValueTask<IReadOnlyList<ManagedContentOverride>> ListAsync(string managedSiteId);

    /// <summary>
    /// Lists the overrides a Managed Site holds that do not render.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <returns>The suppressed overrides, each carrying its reason.</returns>
    ValueTask<IReadOnlyList<ManagedContentOverride>> ListSuppressedAsync(string managedSiteId);

    /// <summary>
    /// Finds the published content that overrides an item for a Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <returns>The published override content, or <see langword="null" /> when there is none.</returns>
    ValueTask<ContentItem> FindPublishedOverrideAsync(string managedSiteId, string sourceContentItemId);

    /// <summary>
    /// Registers a content item as a Managed Site's override of a Managed Content item.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <param name="overrideContentItemId">The content item holding the override content.</param>
    /// <param name="publish">Whether to publish the override, rather than keep it a draft.</param>
    /// <returns>The outcome.</returns>
    ValueTask<ManagedContentOverrideResult> SaveAsync(
        string managedSiteId,
        string sourceContentItemId,
        string overrideContentItemId,
        bool publish);

    /// <summary>
    /// Removes a Managed Site's override, restoring the original content for that Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <returns><see langword="true" /> when an override was removed; otherwise, <see langword="false" />.</returns>
    ValueTask<bool> DeleteAsync(string managedSiteId, string sourceContentItemId);
}

/// <summary>
/// Stores overrides as content items carrying <see cref="ManagedContentOverridePart" />.
/// </summary>
/// <remarks>
/// The override is a content item of the source item's own type, so the platform draft and publish
/// lifecycle, validation, and editors all apply to it unchanged. This service only owns the link
/// between that item, the Managed Site, and the item it stands in for.
/// </remarks>
public sealed class ManagedContentOverrideService : IManagedContentOverrideService
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;
    private readonly IManagedSiteService _managedSiteService;
    private readonly IManagedContentScopeService _scopeService;
    private readonly IManagedContentSuppressionService _suppressionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentOverrideService" /> class.
    /// </summary>
    /// <param name="session">The document session.</param>
    /// <param name="contentManager">The content manager.</param>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    /// <param name="suppressionService">The suppression evaluation service.</param>
    public ManagedContentOverrideService(
        ISession session,
        IContentManager contentManager,
        IManagedSiteService managedSiteService,
        IManagedContentScopeService scopeService,
        IManagedContentSuppressionService suppressionService)
    {
        _session = session;
        _contentManager = contentManager;
        _managedSiteService = managedSiteService;
        _scopeService = scopeService;
        _suppressionService = suppressionService;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedContentOverride> GetAsync(string managedSiteId, string sourceContentItemId)
    {
        if (string.IsNullOrEmpty(managedSiteId) || string.IsNullOrEmpty(sourceContentItemId))
        {
            return null;
        }

        var rows = await _session
            .QueryIndex<ManagedContentOverrideIndex>(index =>
                index.ManagedSiteId == managedSiteId && index.SourceContentItemId == sourceContentItemId)
            .ListAsync();

        var candidates = rows.ToArray();

        return candidates.Length == 0
            ? null
            : await DescribeAsync(candidates);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ManagedContentOverride>> ListAsync(string managedSiteId)
    {
        if (string.IsNullOrEmpty(managedSiteId))
        {
            return [];
        }

        var rows = await _session
            .QueryIndex<ManagedContentOverrideIndex>(index => index.ManagedSiteId == managedSiteId)
            .ListAsync();

        var overrides = new List<ManagedContentOverride>();

        foreach (var group in rows.GroupBy(row => row.SourceContentItemId, StringComparer.Ordinal))
        {
            overrides.Add(await DescribeAsync([.. group]));
        }

        return overrides;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ManagedContentOverride>> ListSuppressedAsync(string managedSiteId)
    {
        var overrides = await ListAsync(managedSiteId);

        return [.. overrides.Where(item => item.Status == ManagedContentOverrideStatus.Suppressed)];
    }

    /// <inheritdoc />
    public async ValueTask<ContentItem> FindPublishedOverrideAsync(string managedSiteId, string sourceContentItemId)
    {
        if (string.IsNullOrEmpty(managedSiteId) || string.IsNullOrEmpty(sourceContentItemId))
        {
            return null;
        }

        return await _session
            .Query<ContentItem, ManagedContentOverrideIndex>(index =>
                index.ManagedSiteId == managedSiteId
                && index.SourceContentItemId == sourceContentItemId
                && index.Published)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async ValueTask<ManagedContentOverrideResult> SaveAsync(
        string managedSiteId,
        string sourceContentItemId,
        string overrideContentItemId,
        bool publish)
    {
        var managedSite = await _managedSiteService.GetAsync(managedSiteId);

        if (managedSite is null
            || managedSite.Status == ManagedSiteStatus.Disabled
            || managedSite.Status == ManagedSiteStatus.Archived)
        {
            return ManagedContentOverrideResult.Failed(ManagedContentOverrideError.ManagedSiteUnavailable);
        }

        var source = await _contentManager.GetAsync(sourceContentItemId, VersionOptions.Published);

        if (source is null)
        {
            return ManagedContentOverrideResult.Failed(ManagedContentOverrideError.SourceNotFound);
        }

        if (!source.TryGet<ManagedContentPart>(out var managedContentPart))
        {
            return ManagedContentOverrideResult.Failed(ManagedContentOverrideError.SourceNotManagedContent);
        }

        if (!_scopeService.CanEdit(managedContentPart, managedSiteId))
        {
            return ManagedContentOverrideResult.Failed(ManagedContentOverrideError.EditScopeExcluded);
        }

        var overrideContentItem = await _contentManager.GetAsync(overrideContentItemId, VersionOptions.Latest);

        if (overrideContentItem is null)
        {
            return ManagedContentOverrideResult.Failed(ManagedContentOverrideError.OverrideNotFound);
        }

        if (!string.Equals(overrideContentItem.ContentType, source.ContentType, StringComparison.Ordinal))
        {
            return ManagedContentOverrideResult.Failed(ManagedContentOverrideError.ContentTypeMismatch);
        }

        // Switching which content item overrides an item is a deliberate act with content on both sides,
        // so it is a remove followed by a save rather than a silent swap that orphans the old one.
        var existing = await GetAsync(managedSiteId, sourceContentItemId);

        if (existing is not null
            && !string.Equals(existing.OverrideContentItemId, overrideContentItemId, StringComparison.Ordinal))
        {
            return ManagedContentOverrideResult.Failed(ManagedContentOverrideError.OverrideAlreadyExists);
        }

        var overridePart = overrideContentItem.GetOrCreate<ManagedContentOverridePart>();
        overridePart.ManagedSiteId = managedSiteId;
        overridePart.SourceContentItemId = sourceContentItemId;
        overrideContentItem.Apply(nameof(ManagedContentOverridePart), overridePart);

        await _contentManager.UpdateAsync(overrideContentItem);

        if (publish)
        {
            await _contentManager.PublishAsync(overrideContentItem);
        }
        else
        {
            await _contentManager.SaveDraftAsync(overrideContentItem);
        }

        return ManagedContentOverrideResult.Success(new ManagedContentOverride
        {
            ManagedSiteId = managedSiteId,
            SourceContentItemId = sourceContentItemId,
            OverrideContentItemId = overrideContentItemId,
            ContentType = overrideContentItem.ContentType,
            Status = publish ? ManagedContentOverrideStatus.Published : ManagedContentOverrideStatus.Draft,
            SuppressionReason = ManagedContentOverrideSuppressionReason.None,
        });
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteAsync(string managedSiteId, string sourceContentItemId)
    {
        var existing = await GetAsync(managedSiteId, sourceContentItemId);

        if (existing is null)
        {
            return false;
        }

        var overrideContentItem = await _contentManager.GetAsync(
            existing.OverrideContentItemId,
            VersionOptions.Latest);

        if (overrideContentItem is null)
        {
            return false;
        }

        // Removing the content item removes the override with it: the item exists only to stand in for
        // the source, so leaving it behind unlinked would be litter the editor cannot reach.
        await _contentManager.RemoveAsync(overrideContentItem);

        return true;
    }

    private async ValueTask<ManagedContentOverride> DescribeAsync(ManagedContentOverrideIndex[] rows)
    {
        var row = rows.FirstOrDefault(candidate => candidate.Latest) ?? rows[0];
        var reason = await _suppressionService.EvaluateAsync(row.ManagedSiteId, row.SourceContentItemId);

        return new ManagedContentOverride
        {
            ManagedSiteId = row.ManagedSiteId,
            SourceContentItemId = row.SourceContentItemId,
            OverrideContentItemId = row.OverrideContentItemId,
            ContentType = row.ContentType,

            // A suppressed override reports why rather than how far it got, because the reason is what
            // an administrator has to act on. Whether it was a draft or published no longer decides
            // anything: neither renders while the cause stands.
            Status = reason != ManagedContentOverrideSuppressionReason.None
                ? ManagedContentOverrideStatus.Suppressed
                : rows.Any(candidate => candidate.Published)
                    ? ManagedContentOverrideStatus.Published
                    : ManagedContentOverrideStatus.Draft,
            SuppressionReason = reason,
        };
    }
}
