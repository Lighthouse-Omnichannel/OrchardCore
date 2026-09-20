using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Builds the content items Managed Content tests resolve.
/// </summary>
public static class ManagedContentTestContent
{
    /// <summary>
    /// The content type used by tests that do not care which type an item has.
    /// </summary>
    public const string ContentType = "Page";

    /// <summary>
    /// Creates a source content item carrying Managed Content.
    /// </summary>
    /// <param name="contentItemId">The content item identifier.</param>
    /// <param name="editScope">Who may override the item, defaulting to nobody.</param>
    /// <param name="displayScope">Who renders the item, defaulting to every Managed Site.</param>
    /// <param name="displayInBlueprintContext">Whether the item renders when no Managed Site matches.</param>
    /// <param name="contentType">The content type.</param>
    /// <returns>The content item.</returns>
    public static ContentItem Source(
        string contentItemId = "source-item",
        ManagedContentScope editScope = null,
        ManagedContentScope displayScope = null,
        bool displayInBlueprintContext = true,
        string contentType = ContentType)
    {
        var contentItem = Item(contentItemId, contentType);

        contentItem.Apply(
            nameof(ManagedContentPart),
            new ManagedContentPart
            {
                EditScope = editScope ?? ManagedContentScope.None(),
                DisplayScope = displayScope ?? ManagedContentScope.All(),
                DisplayInBlueprintContext = displayInBlueprintContext,
            });

        return contentItem;
    }

    /// <summary>
    /// Creates a content item that stands in for a source item on one Managed Site.
    /// </summary>
    /// <param name="contentItemId">The content item identifier.</param>
    /// <param name="managedSiteId">The owning Managed Site.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <param name="contentType">The content type, which must match the source.</param>
    /// <returns>The content item.</returns>
    public static ContentItem Override(
        string contentItemId = "override-item",
        string managedSiteId = "site-a",
        string sourceContentItemId = "source-item",
        string contentType = ContentType)
    {
        var contentItem = Item(contentItemId, contentType);

        contentItem.Apply(
            nameof(ManagedContentOverridePart),
            new ManagedContentOverridePart
            {
                ManagedSiteId = managedSiteId,
                SourceContentItemId = sourceContentItemId,
            });

        return contentItem;
    }

    /// <summary>
    /// Creates a content item that never opted into Managed Content.
    /// </summary>
    /// <param name="contentItemId">The content item identifier.</param>
    /// <param name="contentType">The content type.</param>
    /// <returns>The content item.</returns>
    public static ContentItem Item(string contentItemId = "plain-item", string contentType = ContentType)
        => new()
        {
            ContentItemId = contentItemId,
            ContentType = contentType,
            Published = true,
            Latest = true,
        };
}

/// <summary>
/// In-memory override store that records whether rendering consulted it.
/// </summary>
/// <remarks>
/// The call count is part of the contract under test: display scope is evaluated before override
/// resolution, so an excluded item must be answered without the override lookup happening at all.
/// </remarks>
public sealed class FakeManagedContentOverrideService : IManagedContentOverrideService
{
    private readonly Dictionary<string, ContentItem> _published = new(StringComparer.Ordinal);
    private readonly List<ManagedContentOverride> _suppressed = [];
    private ManagedContentOverrideError _error = ManagedContentOverrideError.None;

    /// <summary>
    /// Gets how many times rendering asked whether a published override exists.
    /// </summary>
    public int PublishedLookups { get; private set; }

    /// <summary>
    /// Registers a published override.
    /// </summary>
    /// <param name="managedSiteId">The owning Managed Site.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <param name="content">The override content.</param>
    /// <returns>This instance, for chaining.</returns>
    public FakeManagedContentOverrideService WithPublished(
        string managedSiteId,
        string sourceContentItemId,
        ContentItem content)
    {
        _published[Key(managedSiteId, sourceContentItemId)] = content;

        return this;
    }

    /// <inheritdoc />
    public ValueTask<ContentItem> FindPublishedOverrideAsync(string managedSiteId, string sourceContentItemId)
    {
        PublishedLookups++;
        _published.TryGetValue(Key(managedSiteId, sourceContentItemId), out var content);

        return ValueTask.FromResult(content);
    }

    /// <inheritdoc />
    public ValueTask<ManagedContentOverride> GetAsync(string managedSiteId, string sourceContentItemId)
        => ValueTask.FromResult(
            _published.TryGetValue(Key(managedSiteId, sourceContentItemId), out var content)
                ? new ManagedContentOverride
                {
                    ManagedSiteId = managedSiteId,
                    SourceContentItemId = sourceContentItemId,
                    OverrideContentItemId = content.ContentItemId,
                    ContentType = content.ContentType,
                    Status = ManagedContentOverrideStatus.Published,
                }
                : null);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ManagedContentOverride>> ListAsync(string managedSiteId)
        => ValueTask.FromResult<IReadOnlyList<ManagedContentOverride>>([.. _suppressed.Concat(Live(managedSiteId))]);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ManagedContentOverride>> ListSuppressedAsync(string managedSiteId)
        => ValueTask.FromResult<IReadOnlyList<ManagedContentOverride>>([.. _suppressed]);

    /// <inheritdoc />
    public ValueTask<ManagedContentOverrideResult> SaveAsync(
        string managedSiteId,
        string sourceContentItemId,
        string overrideContentItemId,
        bool publish)
    {
        if (_error != ManagedContentOverrideError.None)
        {
            return ValueTask.FromResult(ManagedContentOverrideResult.Failed(_error));
        }

        _published[Key(managedSiteId, sourceContentItemId)] =
            ManagedContentTestContent.Item(overrideContentItemId);

        return ValueTask.FromResult(ManagedContentOverrideResult.Success(new ManagedContentOverride
        {
            ManagedSiteId = managedSiteId,
            SourceContentItemId = sourceContentItemId,
            OverrideContentItemId = overrideContentItemId,
            ContentType = ManagedContentTestContent.ContentType,
            Status = publish ? ManagedContentOverrideStatus.Published : ManagedContentOverrideStatus.Draft,
        }));
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(string managedSiteId, string sourceContentItemId)
        => ValueTask.FromResult(_published.Remove(Key(managedSiteId, sourceContentItemId)));

    /// <summary>
    /// Makes the next save fail, so status mapping can be checked for every refusal.
    /// </summary>
    /// <param name="error">The refusal to report.</param>
    /// <returns>This instance, for chaining.</returns>
    public FakeManagedContentOverrideService FailWith(ManagedContentOverrideError error)
    {
        _error = error;

        return this;
    }

    /// <summary>
    /// Registers an override that exists but does not render.
    /// </summary>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <param name="reason">Why it does not render.</param>
    /// <returns>This instance, for chaining.</returns>
    public FakeManagedContentOverrideService WithSuppressed(
        string sourceContentItemId,
        ManagedContentOverrideSuppressionReason reason)
    {
        _suppressed.Add(new ManagedContentOverride
        {
            SourceContentItemId = sourceContentItemId,
            OverrideContentItemId = $"{sourceContentItemId}-override",
            Status = ManagedContentOverrideStatus.Suppressed,
            SuppressionReason = reason,
        });

        return this;
    }

    private IEnumerable<ManagedContentOverride> Live(string managedSiteId)
        => _published
            .Where(entry => entry.Key.StartsWith($"{managedSiteId}|", StringComparison.Ordinal))
            .Select(entry => new ManagedContentOverride
            {
                ManagedSiteId = managedSiteId,
                SourceContentItemId = entry.Key[(managedSiteId.Length + 1)..],
                OverrideContentItemId = entry.Value.ContentItemId,
                ContentType = entry.Value.ContentType,
                Status = ManagedContentOverrideStatus.Published,
            });

    private static string Key(string managedSiteId, string sourceContentItemId)
        => $"{managedSiteId}|{sourceContentItemId}";
}
