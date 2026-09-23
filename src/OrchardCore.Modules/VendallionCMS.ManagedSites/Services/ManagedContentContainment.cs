using System.Text.Json.Nodes;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Routing;
using VendallionCMS.ManagedSites.Indexes;
using YesSql;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// One content item found inside another.
/// </summary>
/// <param name="ContentItem">The contained item.</param>
/// <param name="JsonPath">Where it sits inside the stored container.</param>
public readonly record struct ManagedContentContainedItem(ContentItem ContentItem, string JsonPath);

/// <summary>
/// Walks the content items stored inside another content item.
/// </summary>
/// <remarks>
/// A content item is not always a document. A page section is a content item stored inside its page, so
/// anything that looks for Managed Content only among stored documents finds none of the items that
/// most often carry it.
///
/// Containment is resolved through <see cref="ContainedContentItemsAspect" />, which each container part
/// supplies for itself, so bag, flow, and anything else that holds children are all covered without
/// this code knowing their names.
/// </remarks>
public static class ManagedContentContainment
{
    /// <summary>
    /// Lists every content item stored inside a content item, at any depth.
    /// </summary>
    /// <param name="contentManager">The content manager, used to resolve containment.</param>
    /// <param name="container">The stored content item to walk.</param>
    /// <returns>The contained items, each with the path at which it sits.</returns>
    public static async Task<IReadOnlyList<ManagedContentContainedItem>> ListContainedAsync(
        IContentManager contentManager,
        ContentItem container)
    {
        var results = new List<ManagedContentContainedItem>();

        await CollectAsync(contentManager, container, (JsonObject)container.Content, results);

        return results;
    }

    /// <summary>
    /// Finds one content item inside a stored content item, or the container itself.
    /// </summary>
    /// <param name="contentManager">The content manager, used to resolve containment.</param>
    /// <param name="container">The stored content item to search.</param>
    /// <param name="contentItemId">The content item identifier to find.</param>
    /// <returns>The item, or <see langword="null" /> when the container does not hold it.</returns>
    public static async Task<ContentItem> FindAsync(
        IContentManager contentManager,
        ContentItem container,
        string contentItemId)
    {
        if (container is null || string.IsNullOrEmpty(contentItemId))
        {
            return null;
        }

        if (string.Equals(container.ContentItemId, contentItemId, StringComparison.Ordinal))
        {
            return container;
        }

        var contained = await ListContainedAsync(contentManager, container);

        return contained
            .FirstOrDefault(item =>
                string.Equals(item.ContentItem.ContentItemId, contentItemId, StringComparison.Ordinal))
            .ContentItem;
    }

    private static async Task CollectAsync(
        IContentManager contentManager,
        ContentItem owner,
        JsonObject content,
        List<ManagedContentContainedItem> results)
    {
        var aspect = await contentManager.PopulateAspectAsync<ContainedContentItemsAspect>(owner);

        foreach (var accessor in aspect.Accessors)
        {
            foreach (var jItem in accessor.Invoke(content).Cast<JsonObject>())
            {
                var contained = jItem.ToObject<ContentItem>();

                results.Add(new ManagedContentContainedItem(contained, jItem.GetNormalizedPath()));

                // A container can hold containers, and a section inside a section is still a content
                // item a Managed Site may be given the right to override.
                await CollectAsync(contentManager, contained, jItem, results);
            }
        }
    }
}

/// <summary>
/// Where a Managed Content item lives.
/// </summary>
public sealed class ManagedContentLocation
{
    /// <summary>
    /// Gets the item carrying Managed Content.
    /// </summary>
    public required ContentItem ContentItem { get; init; }

    /// <summary>
    /// Gets the stored content item that had to be loaded to reach it.
    /// </summary>
    public required ContentItem Container { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item is stored inside another rather than in its own right.
    /// </summary>
    public bool IsContained
        => !string.Equals(ContentItem.ContentItemId, Container.ContentItemId, StringComparison.Ordinal);
}

/// <summary>
/// Resolves a Managed Content item by identifier, whether it is stored in its own right or inside another.
/// </summary>
public interface IManagedContentLocator
{
    /// <summary>
    /// Finds a Managed Content item, discovering its container when one is not known.
    /// </summary>
    /// <param name="contentItemId">The content item identifier.</param>
    /// <param name="containerContentItemId">The stored container, when the caller already knows it.</param>
    /// <param name="options">The version to load, defaulting to the published one.</param>
    /// <returns>The location, or <see langword="null" /> when nothing matches.</returns>
    ValueTask<ManagedContentLocation> FindAsync(
        string contentItemId,
        string containerContentItemId = null,
        VersionOptions options = null);
}

/// <summary>
/// Finds Managed Content items through their container.
/// </summary>
/// <remarks>
/// A caller that already knows the container, from an index row or from an override that recorded it,
/// says so and the lookup is a single load. Otherwise the edit scope index answers where to look, which
/// covers every item a Managed Site could be asked to override, because an item nobody may override has
/// no reason to be located.
/// </remarks>
public sealed class ManagedContentLocator : IManagedContentLocator
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentLocator" /> class.
    /// </summary>
    /// <param name="session">The document session.</param>
    /// <param name="contentManager">The content manager.</param>
    public ManagedContentLocator(ISession session, IContentManager contentManager)
    {
        _session = session;
        _contentManager = contentManager;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedContentLocation> FindAsync(
        string contentItemId,
        string containerContentItemId = null,
        VersionOptions options = null)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return null;
        }

        containerContentItemId ??= await FindContainerIdAsync(contentItemId);

        if (containerContentItemId is null)
        {
            return null;
        }

        var container = await _contentManager.GetAsync(containerContentItemId, options ?? VersionOptions.Published);

        if (container is null)
        {
            return null;
        }

        var contentItem = await ManagedContentContainment.FindAsync(_contentManager, container, contentItemId);

        return contentItem is null
            ? null
            : new ManagedContentLocation { ContentItem = contentItem, Container = container };
    }

    private async Task<string> FindContainerIdAsync(string contentItemId)
    {
        var row = await _session
            .QueryIndex<ManagedContentEditScopeIndex>(index => index.ContentItemId == contentItemId)
            .FirstOrDefaultAsync();

        // An item with no edit scope row is either not customizable or stored in its own right, and the
        // second case still resolves by loading it directly.
        return row?.ContainerContentItemId ?? contentItemId;
    }
}
