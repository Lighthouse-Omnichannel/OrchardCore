using System.Text.Json.Nodes;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Models;
using YesSql;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// One content item found inside another.
/// </summary>
/// <param name="Content">The contained item's own JSON, which is where it is edited in place.</param>
/// <param name="ContentItem">The contained item.</param>
/// <param name="JsonPath">Where it sits inside the stored container.</param>
public readonly record struct ManagedContentContainedItem(
    JsonObject Content,
    ContentItem ContentItem,
    string JsonPath);

/// <summary>
/// Walks the content items stored inside another content item.
/// </summary>
/// <remarks>
/// A content item is not always a document. A page section is a content item stored inside its page, so
/// anything that looks for Managed Content only among stored documents finds none of the items that
/// most often carry it.
///
/// Containment is found structurally rather than by asking each part what it holds. The platform's
/// <c>ContainedContentItemsAspect</c> is the obvious mechanism and reaches too little: only bag and
/// taxonomy parts publish it, so a widget in a flow part, which is how most pages are built, is
/// invisible to anything that relies on it. A serialized content item is recognisable on its own terms,
/// carrying both an identifier and a type, and that holds for every container part there is or will be.
/// </remarks>
public static class ManagedContentContainment
{
    private const string ContentItemIdProperty = "ContentItemId";
    private const string ContentTypeProperty = "ContentType";

    /// <summary>
    /// Lists every content item stored inside a content item, at any depth.
    /// </summary>
    /// <param name="container">The stored content item to walk.</param>
    /// <returns>The contained items, each with the path at which it sits.</returns>
    public static IReadOnlyList<ManagedContentContainedItem> ListContained(ContentItem container)
    {
        var results = new List<ManagedContentContainedItem>();

        if (container is not null)
        {
            Collect((JsonObject)container.Content, results);
        }

        return results;
    }

    /// <summary>
    /// Finds one content item inside a stored content item, or the container itself.
    /// </summary>
    /// <param name="container">The stored content item to search.</param>
    /// <param name="contentItemId">The content item identifier to find.</param>
    /// <returns>The item, or <see langword="null" /> when the container does not hold it.</returns>
    public static ContentItem Find(ContentItem container, string contentItemId)
    {
        if (container is null || string.IsNullOrEmpty(contentItemId))
        {
            return null;
        }

        if (string.Equals(container.ContentItemId, contentItemId, StringComparison.Ordinal))
        {
            return container;
        }

        return ListContained(container)
            .FirstOrDefault(item =>
                string.Equals(item.ContentItem.ContentItemId, contentItemId, StringComparison.Ordinal))
            .ContentItem;
    }

    private static void Collect(JsonNode node, List<ManagedContentContainedItem> results)
    {
        switch (node)
        {
            case JsonArray array:
                foreach (var element in array)
                {
                    Collect(element, results);
                }

                break;

            case JsonObject item when IsContentItem(item):
                results.Add(new ManagedContentContainedItem(
                    item,
                    item.ToObject<ContentItem>(),
                    item.GetNormalizedPath()));

                // A container can hold containers, and a section inside a section is still something a
                // Managed Site may be given the right to override.
                foreach (var property in item)
                {
                    Collect(property.Value, results);
                }

                break;

            case JsonObject other:
                foreach (var property in other)
                {
                    Collect(property.Value, results);
                }

                break;
        }
    }

    // A serialized content item carries both an identifier and a type. A content picker stores only
    // identifiers, as strings, so it is not mistaken for one.
    private static bool IsContentItem(JsonObject candidate)
        => candidate[ContentItemIdProperty] is JsonValue id
            && candidate[ContentTypeProperty] is JsonValue type
            && !string.IsNullOrEmpty(id.GetValue<string>())
            && !string.IsNullOrEmpty(type.GetValue<string>());
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

        containerContentItemId ??= await FindContainerIdAsync(contentItemId, options);

        if (containerContentItemId is null)
        {
            return null;
        }

        var container = await _contentManager.GetAsync(containerContentItemId, options ?? VersionOptions.Published);

        if (container is null)
        {
            return null;
        }

        var contentItem = ManagedContentContainment.Find(container, contentItemId);

        return contentItem is null
            ? null
            : new ManagedContentLocation { ContentItem = contentItem, Container = container };
    }

    private async Task<string> FindContainerIdAsync(string contentItemId, VersionOptions options)
    {
        var rows = await _session
            .QueryIndex<ManagedContentEditScopeIndex>(index => index.ContentItemId == contentItemId)
            .ListAsync();

        // A Managed Site's override of a container holds copies of that container's children, and a
        // copy keeps the identifier of what it was copied from. So an item can be named by more than
        // one row, and answering with the override's would resolve the Site Blueprint's item to another
        // Managed Site's copy of it. The index no longer writes those rows; this skips the ones already
        // written, which stay until the override they describe is saved again.
        //
        // Loading here costs nothing twice over: the caller loads the container it is given, and the
        // content manager answers the second request from the same scope.
        foreach (var candidate in rows
            .Select(row => row.ContainerContentItemId ?? contentItemId)
            .Distinct(StringComparer.Ordinal))
        {
            var container = await _contentManager.GetAsync(candidate, options ?? VersionOptions.Published);

            if (container is not null && !container.Has(nameof(ManagedContentOverridePart)))
            {
                return candidate;
            }
        }

        // An item with no edit scope row is either not customizable or stored in its own right, and the
        // second case still resolves by loading it directly.
        return contentItemId;
    }
}
