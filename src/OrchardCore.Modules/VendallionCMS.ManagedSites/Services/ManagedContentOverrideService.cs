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
    /// Creates the content item a Managed Site will use to override a Managed Content item.
    /// </summary>
    /// <remarks>
    /// The new item starts as a copy of the source, so an editor changes the blueprint content rather
    /// than facing an empty form, and it is owned by the Managed Site from the moment it exists, which
    /// is what lets clearance alone authorize editing it.
    /// </remarks>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <returns>The outcome, carrying the new override when it succeeded.</returns>
    ValueTask<ManagedContentOverrideResult> CreateAsync(string managedSiteId, string sourceContentItemId);

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
    private readonly IManagedContentLocator _locator;
    private readonly IManagedSiteService _managedSiteService;
    private readonly IManagedContentScopeService _scopeService;
    private readonly IManagedContentSuppressionService _suppressionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentOverrideService" /> class.
    /// </summary>
    /// <param name="session">The document session.</param>
    /// <param name="contentManager">The content manager.</param>
    /// <param name="locator">The Managed Content locator.</param>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    /// <param name="suppressionService">The suppression evaluation service.</param>
    public ManagedContentOverrideService(
        ISession session,
        IContentManager contentManager,
        IManagedContentLocator locator,
        IManagedSiteService managedSiteService,
        IManagedContentScopeService scopeService,
        IManagedContentSuppressionService suppressionService)
    {
        _session = session;
        _contentManager = contentManager;
        _locator = locator;
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

        // Ordered rather than taking whichever row comes back first. At most one override should exist
        // per Managed Site and item, but content imported or recipe-deployed can break that, and an
        // arbitrary winner would mean the same request rendering differently on different machines.
        return await _session
            .Query<ContentItem, ManagedContentOverrideIndex>(index =>
                index.ManagedSiteId == managedSiteId
                && index.SourceContentItemId == sourceContentItemId
                && index.Published)
            .OrderBy(index => index.OverrideContentItemId)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async ValueTask<ManagedContentOverrideResult> CreateAsync(
        string managedSiteId,
        string sourceContentItemId)
    {
        var validation = await ValidateAsync(managedSiteId, sourceContentItemId);

        if (validation.Error != ManagedContentOverrideError.None)
        {
            return ManagedContentOverrideResult.Failed(validation.Error);
        }

        var existing = await GetAsync(managedSiteId, sourceContentItemId);

        if (existing is not null)
        {
            return ManagedContentOverrideResult.Failed(ManagedContentOverrideError.OverrideAlreadyExists);
        }

        var source = validation.Location.ContentItem;
        var overrideContentItem = await _contentManager.NewAsync(source.ContentType);

        // Start from the blueprint content. An override replaces the item rather than extending it, so
        // an empty form would make the editor retype everything they did not intend to change.
        overrideContentItem.Merge(source);
        overrideContentItem.DisplayText = source.DisplayText;

        // The copy must not inherit the source's Managed Content. An override is somebody's answer to a
        // scoped item, never a scoped item in its own right, and leaving the part on would list every
        // override back in the portal as something else to override.
        overrideContentItem.Remove(nameof(ManagedContentPart));

        var overridePart = overrideContentItem.GetOrCreate<ManagedContentOverridePart>();
        overridePart.ManagedSiteId = managedSiteId;
        overridePart.SourceContentItemId = sourceContentItemId;
        overridePart.SourceContainerContentItemId = validation.Location.Container.ContentItemId;
        overrideContentItem.Apply(nameof(ManagedContentOverridePart), overridePart);

        await _contentManager.CreateAsync(overrideContentItem, VersionOptions.Draft);

        return ManagedContentOverrideResult.Success(new ManagedContentOverride
        {
            ManagedSiteId = managedSiteId,
            SourceContentItemId = sourceContentItemId,
            OverrideContentItemId = overrideContentItem.ContentItemId,
            ContentType = overrideContentItem.ContentType,
            Status = ManagedContentOverrideStatus.Draft,
            SuppressionReason = ManagedContentOverrideSuppressionReason.None,
        });
    }

    /// <inheritdoc />
    public async ValueTask<ManagedContentOverrideResult> SaveAsync(
        string managedSiteId,
        string sourceContentItemId,
        string overrideContentItemId,
        bool publish)
    {
        var validation = await ValidateAsync(managedSiteId, sourceContentItemId);

        if (validation.Error != ManagedContentOverrideError.None)
        {
            return ManagedContentOverrideResult.Failed(validation.Error);
        }

        var location = validation.Location;
        var source = location.ContentItem;

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
        overridePart.SourceContainerContentItemId = location.Container.ContentItemId;
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

    /// <summary>
    /// Checks everything that must hold before a Managed Site may own an override of an item.
    /// </summary>
    private async ValueTask<(ManagedContentOverrideError Error, ManagedContentLocation Location)> ValidateAsync(
        string managedSiteId,
        string sourceContentItemId)
    {
        var managedSite = await _managedSiteService.GetAsync(managedSiteId);

        if (managedSite is null || managedSite.Status != ManagedSiteStatus.Enabled)
        {
            return (ManagedContentOverrideError.ManagedSiteUnavailable, null);
        }

        // The source may be a section stored inside a page rather than a content item of its own, so it
        // is resolved through its container.
        var location = await _locator.FindAsync(sourceContentItemId, options: VersionOptions.Published);

        if (location?.ContentItem is null)
        {
            return (ManagedContentOverrideError.SourceNotFound, null);
        }

        if (!location.ContentItem.TryGet<ManagedContentPart>(out var managedContentPart))
        {
            return (ManagedContentOverrideError.SourceNotManagedContent, null);
        }

        return _scopeService.CanEdit(managedContentPart, managedSiteId)
            ? (ManagedContentOverrideError.None, location)
            : (ManagedContentOverrideError.EditScopeExcluded, null);
    }

    private async ValueTask<ManagedContentOverride> DescribeAsync(ManagedContentOverrideIndex[] rows)
    {
        // The same rule rendering uses, so what an administrator is shown is what visitors receive.
        var served = rows
            .Select(candidate => candidate.OverrideContentItemId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .First();

        var superseded = rows
            .Select(candidate => candidate.OverrideContentItemId)
            .Distinct(StringComparer.Ordinal)
            .Where(candidate => !string.Equals(candidate, served, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        rows = [.. rows.Where(candidate =>
            string.Equals(candidate.OverrideContentItemId, served, StringComparison.Ordinal))];

        var row = rows.FirstOrDefault(candidate => candidate.Latest) ?? rows[0];

        // The override knows where its source lives, so suppression does not have to rediscover it
        // through an index row that is gone whenever the edit scope was withdrawn.
        var overrideContentItem = await _contentManager.GetAsync(
            row.OverrideContentItemId,
            VersionOptions.Latest);

        ManagedContentOverridePart overridePart = null;
        overrideContentItem?.TryGet(out overridePart);

        var reason = await _suppressionService.EvaluateAsync(
            row.ManagedSiteId,
            row.SourceContentItemId,
            overridePart?.SourceContainerContentItemId);

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
            SupersededOverrideContentItemIds = superseded,
        };
    }
}
