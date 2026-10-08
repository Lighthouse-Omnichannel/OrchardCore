using OrchardCore.DisplayManagement;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// What the active Managed Site may customize.
/// </summary>
public class ManagedSitePortalListViewModel
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
    /// Gets or sets the one-based position of the first item on this page, or zero when there are none.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets the one-based position of the last item on this page, or zero when there are none.
    /// </summary>
    public int EndIndex => StartIndex == 0 ? 0 : StartIndex + Items.Count - 1;

    /// <summary>
    /// Gets or sets the content type the list is narrowed to, if any.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the override status the list is narrowed to, if any.
    /// </summary>
    public string OverrideStatus { get; set; }

    /// <summary>
    /// Gets or sets the pager shape, which draws the page links the admin draws everywhere else.
    /// </summary>
    public IShape Pager { get; set; }

    /// <summary>
    /// Gets or sets whether the editor may change this Managed Site's content.
    /// </summary>
    /// <remarks>
    /// Clearance to see a Managed Site's content does not carry clearance to change it, so the actions
    /// that would change it are not offered to somebody holding only the first. The actions check for
    /// themselves as well; this is so an editor is not invited to do something they will be refused.
    /// </remarks>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Gets or sets whether the editor may publish this Managed Site's content.
    /// </summary>
    /// <remarks>
    /// Separate from editing, so somebody who may draft a version but not put it in front of visitors
    /// is not offered the button that would.
    /// </remarks>
    public bool CanPublish { get; set; }
}

/// <summary>
/// What the active Managed Site may customize inside one item.
/// </summary>
public sealed class ManagedSitePortalContainedViewModel : ManagedSitePortalListViewModel
{
    /// <summary>
    /// Gets or sets the identifier of the item these are stored inside.
    /// </summary>
    public string SourceContentItemId { get; set; }

    /// <summary>
    /// Gets or sets what to call the item these are stored inside.
    /// </summary>
    public string ContainerDisplayText { get; set; }

    /// <summary>
    /// Gets or sets whether the Managed Site has its own version of the container.
    /// </summary>
    /// <remarks>
    /// A version of a container carries its own contents, so while one exists nothing listed here is
    /// being served, whatever its own version says. The screen says so rather than letting an editor
    /// work on items that cannot reach the site.
    /// </remarks>
    public bool ContainerIsOverridden { get; set; }
}

/// <summary>
/// One row's worth of what an editor may do to an item.
/// </summary>
/// <remarks>
/// Carried to the partial that draws the actions, so the list and the contents of a container offer the
/// same ones rather than two sets that drift apart.
/// </remarks>
public sealed class ManagedContentRowActionsViewModel
{
    /// <summary>
    /// Gets or sets the item the actions act on.
    /// </summary>
    public ManagedContentListItem Item { get; set; }

    /// <summary>
    /// Gets or sets whether the editor may change this Managed Site's content.
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Gets or sets whether the editor may publish this Managed Site's content.
    /// </summary>
    public bool CanPublish { get; set; }

    /// <summary>
    /// Gets or sets where an action should hand the editor back to.
    /// </summary>
    public string ReturnUrl { get; set; }
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

    /// <summary>
    /// Gets or sets whether the link opens a host the editor is not signed in on.
    /// </summary>
    /// <remarks>
    /// A sign-in reaches only the host that issued it, so a preview on a Managed Site that names a
    /// different host arrives as nobody, and is served published content however the link asked for
    /// drafts. Saying so is the difference between a limitation and an editor concluding their draft
    /// did not save.
    /// </remarks>
    public bool OpensAnotherHost { get; set; }
}
