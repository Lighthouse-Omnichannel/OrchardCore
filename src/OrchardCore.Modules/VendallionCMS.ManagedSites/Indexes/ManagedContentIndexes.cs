using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;
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
/// </remarks>
public sealed class ManagedContentEditScopeIndex : MapIndex
{
    /// <summary>
    /// Gets or sets the source content item identifier.
    /// </summary>
    public string ContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the source content type.
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
    /// Gets or sets a value indicating whether the row maps the latest version.
    /// </summary>
    public bool Latest { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the row maps the published version.
    /// </summary>
    public bool Published { get; set; }
}

/// <summary>
/// Maps content items carrying Managed Content to the Managed Sites that may override them.
/// </summary>
public sealed class ManagedContentEditScopeIndexProvider : IndexProvider<ContentItem>
{
    /// <inheritdoc />
    public override void Describe(DescribeContext<ContentItem> context)
        => context.For<ManagedContentEditScopeIndex>()
            .Map(contentItem =>
            {
                if (!contentItem.Latest && !contentItem.Published)
                {
                    return [];
                }

                contentItem.TryGet<ManagedContentPart>(out var part);
                var scope = part?.EditScope;

                if (scope is null || scope.Mode == ManagedContentScopeMode.None)
                {
                    return [];
                }

                if (scope.Mode == ManagedContentScopeMode.All)
                {
                    return
                    [
                        new ManagedContentEditScopeIndex
                        {
                            ContentItemId = contentItem.ContentItemId,
                            ContentType = contentItem.ContentType,
                            ManagedSiteId = string.Empty,
                            AllManagedSites = true,
                            Latest = contentItem.Latest,
                            Published = contentItem.Published,
                        },
                    ];
                }

                return scope.ManagedSiteIds
                    .Where(managedSiteId => !string.IsNullOrEmpty(managedSiteId))
                    .Distinct(StringComparer.Ordinal)
                    .Select(managedSiteId => new ManagedContentEditScopeIndex
                    {
                        ContentItemId = contentItem.ContentItemId,
                        ContentType = contentItem.ContentType,
                        ManagedSiteId = managedSiteId,
                        AllManagedSites = false,
                        Latest = contentItem.Latest,
                        Published = contentItem.Published,
                    })
                    .ToArray();
            });
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
