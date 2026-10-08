using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.ViewModels;
using YesSql;
using ISession = YesSql.ISession;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// What a Managed Site may customize, and what it has done with it so far.
/// </summary>
/// <remarks>
/// This is the portal's content list, and what it answers is not a query anyone can write against the
/// store: an item's edit scope is read from the item as it stands rather than trusted from the index,
/// a contained item is found inside the container that holds it, and an item's override comes from the
/// Managed Site's own content, which cannot be joined to the rest.
/// </remarks>
public interface IManagedContentListService
{
    /// <summary>
    /// Lists what a Managed Site may customize, as items stored in their own right.
    /// </summary>
    /// <remarks>
    /// Items stored inside another are left to <see cref="ListContainedAsync" />. Listing both together
    /// put a page and each of its sections side by side as though they were peers, which is not how
    /// either is reached or what overriding one does to the other.
    /// </remarks>
    /// <param name="managedSiteId">The Managed Site.</param>
    /// <param name="query">What to list, and which page of it.</param>
    /// <returns>The page, and how many items it was drawn from.</returns>
    ValueTask<ManagedContentListing> ListAsync(string managedSiteId, ManagedContentListQuery query);

    /// <summary>
    /// Lists what a Managed Site may customize inside one container.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site.</param>
    /// <param name="containerContentItemId">The item the listed items are stored inside.</param>
    /// <param name="query">What to list, and which page of it.</param>
    /// <returns>The page, and how many items it was drawn from.</returns>
    ValueTask<ManagedContentListing> ListContainedAsync(
        string managedSiteId,
        string containerContentItemId,
        ManagedContentListQuery query);
}

/// <summary>
/// What to list, and which page of it.
/// </summary>
/// <param name="ContentType">One content type, or <see langword="null" /> for all of them.</param>
/// <param name="OverrideStatus">One override status, or <see langword="null" /> for all of them.</param>
/// <param name="Page">The one-based page number.</param>
/// <param name="PageSize">How many items a page holds, clamped to between one and two hundred.</param>
/// <remarks>
/// A record rather than a record struct, so that the defaults named here are the ones an unspecified
/// query gets. A struct is always constructible with no arguments at all, which would zero every value
/// and ask for nothing, from the first page, of a page holding nothing.
/// </remarks>
public sealed record ManagedContentListQuery(
    string ContentType = null,
    ManagedContentOverrideStatus? OverrideStatus = null,
    int Page = 1,
    int PageSize = 50);

/// <summary>
/// One page of what a Managed Site may customize.
/// </summary>
/// <param name="Items">The page.</param>
/// <param name="TotalCount">How many items the page was drawn from, before paging.</param>
public sealed record ManagedContentListing(
    IReadOnlyList<ManagedContentListItem> Items,
    int TotalCount);

/// <summary>
/// Lists what a Managed Site may customize.
/// </summary>
/// <remarks>
/// A service rather than part of whatever renders it, because none of this is presentation: which
/// items a Managed Site may customize, whether each is still in scope, and what its override is, are
/// answers about the feature. The surface that shows them should not be the one that works them out.
/// </remarks>
public sealed class ManagedContentListService : IManagedContentListService
{
    /// <summary>
    /// Parts that make a content item hold children of its own.
    /// </summary>
    /// <remarks>
    /// Named rather than referenced, so discovering that an item is a container does not make this
    /// module depend on the modules that supply those parts.
    /// </remarks>
    private static readonly string[] s_containerParts = ["ListPart", "BagPart", "FlowPart"];

    private readonly ISession _session;
    private readonly IContentManager _contentManager;
    private readonly IContentDefinitionManager _contentDefinitionManager;
    private readonly IManagedContentScopeService _scopeService;
    private readonly IManagedContentOverrideService _overrideService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentListService" /> class.
    /// </summary>
    /// <param name="session">The document session.</param>
    /// <param name="contentManager">The content manager.</param>
    /// <param name="contentDefinitionManager">The content definition manager.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    /// <param name="overrideService">The Managed Content override service.</param>
    public ManagedContentListService(
        ISession session,
        IContentManager contentManager,
        IContentDefinitionManager contentDefinitionManager,
        IManagedContentScopeService scopeService,
        IManagedContentOverrideService overrideService)
    {
        _session = session;
        _contentManager = contentManager;
        _contentDefinitionManager = contentDefinitionManager;
        _scopeService = scopeService;
        _overrideService = overrideService;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedContentListing> ListAsync(string managedSiteId, ManagedContentListQuery query)
        => ApplyQuery(InTheirOwnRight(await ListEditableAsync(managedSiteId)), query);

    /// <inheritdoc />
    public async ValueTask<ManagedContentListing> ListContainedAsync(
        string managedSiteId,
        string containerContentItemId,
        ManagedContentListQuery query)
    {
        var items = await ListEditableAsync(managedSiteId);

        return ApplyQuery(
            [.. items.Where(item => IsStoredInside(item, containerContentItemId))],
            query);
    }

    /// <summary>
    /// Picks out the items stored in their own right, and tells each how much it holds.
    /// </summary>
    /// <remarks>
    /// An item whose container the Managed Site may not customize is counted as being in its own right
    /// too. The container is not listed, so anything stored inside it would otherwise be reachable from
    /// nowhere: in scope, and invisible.
    ///
    /// The counts are taken over everything the Managed Site may customize, before any narrowing, so
    /// that a container still reports what it holds while the list is filtered to something else.
    /// </remarks>
    /// <param name="items">Everything the Managed Site may customize.</param>
    /// <returns>The items stored in their own right.</returns>
    internal static IReadOnlyList<ManagedContentListItem> InTheirOwnRight(IReadOnlyList<ManagedContentListItem> items)
    {
        var listed = items
            .Select(item => item.SourceContentItemId)
            .ToHashSet(StringComparer.Ordinal);

        var contained = items
            .Where(item => !IsInItsOwnRight(item, listed))
            .GroupBy(item => item.ContainerContentItemId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var own = new List<ManagedContentListItem>();

        foreach (var item in items.Where(candidate => IsInItsOwnRight(candidate, listed)))
        {
            if (contained.TryGetValue(item.SourceContentItemId, out var children))
            {
                item.ContainedItemCount = children.Length;
                item.ContainedOverriddenCount = children.Count(child => child.Override is not null);
            }

            own.Add(item);
        }

        return own;
    }

    private static bool IsInItsOwnRight(ManagedContentListItem item, HashSet<string> listed)
        => string.IsNullOrEmpty(item.ContainerContentItemId)
            || string.Equals(item.ContainerContentItemId, item.SourceContentItemId, StringComparison.Ordinal)
            || !listed.Contains(item.ContainerContentItemId);

    private static bool IsStoredInside(ManagedContentListItem item, string containerContentItemId)
        => string.Equals(item.ContainerContentItemId, containerContentItemId, StringComparison.Ordinal)
            && !string.Equals(item.SourceContentItemId, containerContentItemId, StringComparison.Ordinal);

    /// <summary>
    /// Narrows and pages what a Managed Site may customize.
    /// </summary>
    /// <remarks>
    /// Separated from reading it, because this half is arithmetic over a list and the other half needs
    /// a store behind it.
    ///
    /// Status is filtered here rather than in the query that found the items, because an item's
    /// override status lives on the Managed Site's own content and cannot be joined to the lookup that
    /// found the items. Paging therefore has to follow it: paging first would give short pages, and a
    /// total that counted items the caller asked not to see.
    ///
    /// Content type is narrowed here as well, rather than against the index rows, so that it narrows
    /// what is listed without also narrowing what a container reports holding. Filtering earlier made a
    /// page claim to hold nothing as soon as the list was narrowed to anything but its sections.
    /// </remarks>
    /// <param name="items">Everything the Managed Site may customize.</param>
    /// <param name="query">What to narrow it to, and which page of it.</param>
    /// <returns>The page, and how many items it was drawn from.</returns>
    internal static ManagedContentListing ApplyQuery(
        IReadOnlyList<ManagedContentListItem> items,
        ManagedContentListQuery query)
    {
        if (!string.IsNullOrEmpty(query.ContentType))
        {
            items = [.. items.Where(item =>
                string.Equals(item.ContentType, query.ContentType, StringComparison.OrdinalIgnoreCase))];
        }

        if (query.OverrideStatus is { } status)
        {
            items = [.. items.Where(item => StatusOf(item) == status)];
        }

        var take = Math.Clamp(query.PageSize, 1, 200);
        var skip = Math.Max(query.Page - 1, 0) * take;

        return new ManagedContentListing([.. items.Skip(skip).Take(take)], items.Count);
    }

    /// <summary>
    /// Reads an item's override status, which is absent rather than empty when it has none.
    /// </summary>
    /// <param name="item">The listed item.</param>
    /// <returns>The status.</returns>
    public static ManagedContentOverrideStatus StatusOf(ManagedContentListItem item)
        => item.Override is null
            ? ManagedContentOverrideStatus.None
            : Enum.Parse<ManagedContentOverrideStatus>(item.Override.Status);

    private async Task<List<ManagedContentListItem>> ListEditableAsync(string managedSiteId)
    {
        var rows = await _session
            .QueryIndex<ManagedContentEditScopeIndex>(index =>
                (index.ManagedSiteId == managedSiteId || index.AllManagedSites) && index.Published)
            .ListAsync();

        var candidates = rows.ToArray();

        if (candidates.Length == 0)
        {
            return [];
        }

        // Several sections can live in one page, so each container is loaded once and then searched,
        // rather than loaded again for every item it holds.
        var containerIds = candidates
            .Select(row => row.ContainerContentItemId ?? row.ContentItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var containers = (await _contentManager.GetAsync(containerIds, VersionOptions.Published))
            .ToDictionary(container => container.ContentItemId, StringComparer.Ordinal);

        // A row whose container is somebody's override describes that Managed Site's copy of an item,
        // not the Site Blueprint's. A copy keeps the identifier of what it was copied from, so the two
        // rows look alike and only one survives the grouping below: whichever happened to come first
        // decided which container was opened, and when that was another Managed Site's override, this
        // Managed Site was shown that Managed Site's content under an ordinary-looking name.
        //
        // The index no longer writes such rows. They are dropped here as well, because the ones already
        // written stay until the override they describe is saved again.
        var matching = candidates
            .Where(row => containers.TryGetValue(row.ContainerContentItemId ?? row.ContentItemId, out var container)
                && !container.Has(nameof(ManagedContentOverridePart)))
            .GroupBy(row => row.ContentItemId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();

        var overrides = (await _overrideService.ListAsync(managedSiteId))
            .ToDictionary(item => item.SourceContentItemId, StringComparer.Ordinal);

        var items = new List<ManagedContentListItem>();

        foreach (var row in matching)
        {
            if (!containers.TryGetValue(row.ContainerContentItemId ?? row.ContentItemId, out var container))
            {
                continue;
            }

            var source = ManagedContentContainment.Find(container, row.ContentItemId);

            // The edit scope is checked against the item as it stands now, never trusted from the index
            // row, because a blueprint administrator can narrow it at any moment.
            if (source is null
                || !source.TryGet<ManagedContentPart>(out var part)
                || !_scopeService.CanEdit(part, managedSiteId))
            {
                continue;
            }

            overrides.TryGetValue(source.ContentItemId, out var managedContentOverride);

            items.Add(new ManagedContentListItem
            {
                SourceContentItemId = source.ContentItemId,
                ContainerContentItemId = container.ContentItemId,
                ContentType = source.ContentType,
                DisplayText = Describe(source, container),
                IsContainer = await IsContainerAsync(source.ContentType),
                DisplayScopeIncludesManagedSite = _scopeService.CanDisplay(part, managedSiteId),
                Override = ManagedContentOverrideSummary.Of(managedContentOverride),
            });
        }

        return [.. items.OrderBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase)];
    }

    private static string Describe(ContentItem source, ContentItem container)
    {
        var text = source.DisplayText;

        if (string.IsNullOrWhiteSpace(text))
        {
            // A section often carries no display text of its own. The identifier alone would give an
            // editor nothing to recognise, so the type name stands in.
            text = source.ContentType;
        }

        return string.Equals(source.ContentItemId, container.ContentItemId, StringComparison.Ordinal)
            ? text
            : $"{text} ({container.DisplayText ?? container.ContentType})";
    }

    private async Task<bool> IsContainerAsync(string contentType)
    {
        var definition = await _contentDefinitionManager.GetTypeDefinitionAsync(contentType);

        return definition is not null
            && definition.Parts.Any(part => s_containerParts.Contains(part.PartDefinition?.Name, StringComparer.Ordinal));
    }
}
