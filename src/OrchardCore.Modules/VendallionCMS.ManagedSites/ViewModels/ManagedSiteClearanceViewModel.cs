namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// Editor model for the Managed Sites a user may manage.
/// </summary>
public class ManagedSiteClearanceViewModel
{
    /// <summary>
    /// Gets or sets the Managed Sites offered to the administrator, granted or not.
    /// </summary>
    public ManagedSiteClearanceEntryViewModel[] ManagedSites { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the current user may change these grants.
    /// </summary>
    public bool CanManage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any Managed Site has been defined yet.
    /// </summary>
    public bool HasManagedSites => ManagedSites.Length > 0;

    /// <summary>
    /// Gets or sets grants that reference a Managed Site which no longer exists.
    /// </summary>
    /// <remarks>
    /// These are surfaced rather than dropped so an administrator can see why a user lost access, and
    /// so a grant is not silently discarded when a Managed Site is temporarily removed.
    /// </remarks>
    public string[] OrphanedManagedSiteIds { get; set; } = [];
}

/// <summary>
/// One Managed Site row in the clearance editor.
/// </summary>
public sealed class ManagedSiteClearanceEntryViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site lifecycle status.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is cleared for this Managed Site.
    /// </summary>
    public bool IsGranted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user may read scoped content.
    /// </summary>
    public bool View { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user may change scoped content.
    /// </summary>
    public bool Edit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user may publish scoped content.
    /// </summary>
    public bool Publish { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user may preview composed output.
    /// </summary>
    public bool Preview { get; set; }
}
