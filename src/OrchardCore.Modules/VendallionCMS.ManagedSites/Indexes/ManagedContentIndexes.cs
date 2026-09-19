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

                var part = contentItem.As<ManagedContentPart>();
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
