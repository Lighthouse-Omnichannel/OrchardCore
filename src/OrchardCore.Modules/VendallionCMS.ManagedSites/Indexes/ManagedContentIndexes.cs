using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using YesSql.Indexes;

namespace VendallionCMS.ManagedSites.Indexes;

/// <summary>
/// Indexes one row per Managed Site that may override a content item.
/// </summary>
/// <remarks>
/// The portal lists everything the active Managed Site may customize, which is a query by Managed Site
/// rather than by content item. One row per pairing keeps that a single indexed lookup instead of a
/// scan over every item carrying the part.
///
/// An item whose edit scope covers every Managed Site produces one row with
/// <see cref="AllManagedSites" /> set, because the Managed Sites it covers are not known at index time.
///
/// A content item is not always a document. A page section lives inside its page, so the item that
/// carries Managed Content and the item that has to be loaded to reach it are different things, and the
/// index records both.
/// </remarks>
public sealed class ManagedContentEditScopeIndex : MapIndex
{
    /// <summary>
    /// Gets or sets the identifier of the item carrying Managed Content, which is the item a Managed
    /// Site overrides.
    /// </summary>
    public string ContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the stored content item to load in order to reach it. Equal to
    /// <see cref="ContentItemId" /> when the item is stored in its own right.
    /// </summary>
    public string ContainerContentItemId { get; set; }

    /// <summary>
    /// Gets or sets where the item sits inside its container, or <see langword="null" /> when it is not
    /// contained.
    /// </summary>
    public string JsonPath { get; set; }

    /// <summary>
    /// Gets or sets the content type of the item carrying Managed Content.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site this row covers, empty when the row covers every Managed Site.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the edit scope covers every Managed Site.
    /// </summary>
    public bool AllManagedSites { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the row maps the latest version of the container.
    /// </summary>
    public bool Latest { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the row maps the published version of the container.
    /// </summary>
    public bool Published { get; set; }
}

/// <summary>
/// Maps content items carrying Managed Content to the Managed Sites that may override them.
/// </summary>
/// <remarks>
/// Walks contained items as well as the document itself, because the sections of a page carry Managed
/// Content far more often than the page does, and a section is stored inside its page rather than on
/// its own. Containment is found structurally by <see cref="ManagedContentContainment" />, which covers
/// every container part rather than only those publishing a containment aspect.
/// </remarks>
public sealed class ManagedContentEditScopeIndexProvider : IndexProvider<ContentItem>
{
    /// <inheritdoc />
    public override void Describe(DescribeContext<ContentItem> context)
        => context.For<ManagedContentEditScopeIndex>().Map(BuildRows);

    /// <summary>
    /// Builds the edit scope rows a content item contributes.
    /// </summary>
    /// <remarks>
    /// Separated from <see cref="Describe" /> so the rule about overrides below can be tested without
    /// a store behind it.
    /// </remarks>
    /// <param name="contentItem">The content item being indexed.</param>
    /// <returns>The rows, or <see langword="null" /> when the item contributes none.</returns>
    internal static List<ManagedContentEditScopeIndex> BuildRows(ContentItem contentItem)
    {
        if (!contentItem.Latest && !contentItem.Published)
        {
            return null;
        }

        // An override contributes nothing, and neither does anything inside it. Its content is one
        // Managed Site's answer to an item, not an item any Managed Site may be offered. Indexing it
        // was worse than redundant: an override of a container carries copies of that container's
        // children, each still holding the scopes the blueprint gave it, so every other Managed Site
        // named in those scopes was shown an item belonging to somebody else's override, and the copy
        // shares its identifier with the blueprint's own, so it could be listed in its place.
        if (contentItem.Has(nameof(ManagedContentOverridePart)))
        {
            return null;
        }

        var results = new List<ManagedContentEditScopeIndex>();

        contentItem.TryGet<ManagedContentPart>(out var part);
        AddRows(
            results,
            contentItem,
            part,
            contentItem.ContentItemId,
            contentItem.ContentType,
            contentItem.ContentItemId,
            jsonPath: null);

        foreach (var contained in ManagedContentContainment.ListContained(contentItem))
        {
            contained.ContentItem.TryGet<ManagedContentPart>(out var containedPart);
            AddRows(
                results,
                contentItem,
                containedPart,
                contained.ContentItem.ContentItemId,
                contained.ContentItem.ContentType,
                contentItem.ContentItemId,
                contained.JsonPath);
        }

        return results;
    }

    private static void AddRows(
        List<ManagedContentEditScopeIndex> results,
        ContentItem container,
        ManagedContentPart part,
        string contentItemId,
        string contentType,
        string containerContentItemId,
        string jsonPath)
    {
        var scope = part?.EditScope;

        if (scope is null || scope.Mode == ManagedContentScopeMode.None)
        {
            return;
        }

        ManagedContentEditScopeIndex Row(string managedSiteId, bool allManagedSites) => new()
        {
            ContentItemId = contentItemId,
            ContainerContentItemId = containerContentItemId,
            JsonPath = jsonPath,
            ContentType = contentType,
            ManagedSiteId = managedSiteId,
            AllManagedSites = allManagedSites,

            // Versioning belongs to the stored container: a section has no version of its own.
            Latest = container.Latest,
            Published = container.Published,
        };

        if (scope.Mode == ManagedContentScopeMode.All)
        {
            results.Add(Row(string.Empty, allManagedSites: true));

            return;
        }

        foreach (var managedSiteId in scope.ManagedSiteIds
            .Where(managedSiteId => !string.IsNullOrEmpty(managedSiteId))
            .Distinct(StringComparer.Ordinal))
        {
            results.Add(Row(managedSiteId, allManagedSites: false));
        }
    }
}

/// <summary>
/// Indexes the content items that stand in for a Managed Content item on one Managed Site.
/// </summary>
/// <remarks>
/// Rendering asks one question very often: does this Managed Site hold a published override for this
/// item? Indexing the owning Managed Site and the source item together answers it with a single lookup
/// instead of loading candidates and filtering them.
/// </remarks>
public sealed class ManagedContentOverrideIndex : MapIndex
{
    /// <summary>
    /// Gets or sets the content item holding the override content.
    /// </summary>
    public string OverrideContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site that owns the override.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the Managed Content item the override replaces.
    /// </summary>
    public string SourceContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the content type shared by the override and its source.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the row maps the latest version.
    /// </summary>
    public bool Latest { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the row maps the published version.
    /// </summary>
    public bool Published { get; set; }
}

/// <summary>
/// Maps override content items to the Managed Site and source item they belong to.
/// </summary>
public sealed class ManagedContentOverrideIndexProvider : IndexProvider<ContentItem>
{
    /// <inheritdoc />
    public override void Describe(DescribeContext<ContentItem> context)
        => context.For<ManagedContentOverrideIndex>()
            .Map(contentItem =>
            {
                if (!contentItem.Latest && !contentItem.Published)
                {
                    return null;
                }

                if (!contentItem.TryGet<ManagedContentOverridePart>(out var part)
                    || string.IsNullOrEmpty(part.ManagedSiteId)
                    || string.IsNullOrEmpty(part.SourceContentItemId))
                {
                    return null;
                }

                return new ManagedContentOverrideIndex
                {
                    OverrideContentItemId = contentItem.ContentItemId,
                    ManagedSiteId = part.ManagedSiteId,
                    SourceContentItemId = part.SourceContentItemId,
                    ContentType = contentItem.ContentType,
                    Latest = contentItem.Latest,
                    Published = contentItem.Published,
                };
            });
}
