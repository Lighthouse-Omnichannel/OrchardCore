using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// What the active Managed Site may customize.
/// </summary>
public sealed class ManagedSitePortalListViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site this session is working in.
    /// </summary>
    public ManagedSite ManagedSite { get; set; }

    /// <summary>
    /// Gets or sets the items on this page.
    /// </summary>
    public IReadOnlyList<ManagedContentListItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets how many items the page was drawn from.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the one-based page number.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets how many items a page holds.
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the content type the list is narrowed to, if any.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the override status the list is narrowed to, if any.
    /// </summary>
    public string OverrideStatus { get; set; }

    /// <summary>
    /// Gets whether there is a page after this one.
    /// </summary>
    public bool HasNextPage => Page * PageSize < TotalCount;

    /// <summary>
    /// Gets whether there is a page before this one.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// The Managed Sites an editor may work in.
/// </summary>
public sealed class ManagedSitePortalSelectViewModel
{
    /// <summary>
    /// Gets or sets the Managed Sites the editor holds clearance for.
    /// </summary>
    public IReadOnlyList<ManagedSite> ManagedSites { get; set; } = [];

    /// <summary>
    /// Gets or sets the Managed Site already chosen, if any.
    /// </summary>
    public string SelectedManagedSiteId { get; set; }
}

/// <summary>
/// One item, and what the active Managed Site has done with it.
/// </summary>
public sealed class ManagedSitePortalDetailViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site this session is working in.
    /// </summary>
    public ManagedSite ManagedSite { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the item being customized.
    /// </summary>
    public string SourceContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the content type of the item being customized.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets what to call the item being customized.
    /// </summary>
    public string DisplayText { get; set; }

    /// <summary>
    /// Gets or sets whether this Managed Site renders the item at all.
    /// </summary>
    public bool DisplayScopeIncludesManagedSite { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site's own version, or <see langword="null" /> when it has none.
    /// </summary>
    public ManagedContentOverrideSummary Override { get; set; }
}

/// <summary>
/// The Managed Site's versions that exist but are not being served.
/// </summary>
public sealed class ManagedSitePortalSuppressedViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site this session is working in.
    /// </summary>
    public ManagedSite ManagedSite { get; set; }

    /// <summary>
    /// Gets or sets the versions that are not rendering, each carrying why.
    /// </summary>
    public IReadOnlyList<ManagedContentOverride> Items { get; set; } = [];
}

/// <summary>
/// A link that opens a path at the Managed Site's own address.
/// </summary>
public sealed class ManagedSitePortalPreviewViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site this session is working in.
    /// </summary>
    public ManagedSite ManagedSite { get; set; }

    /// <summary>
    /// Gets or sets the path to open.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets whether the link asks for unpublished work.
    /// </summary>
    public bool IncludeDrafts { get; set; }

    /// <summary>
    /// Gets or sets the link, once one has been built.
    /// </summary>
    public string PreviewUrl { get; set; }
}
