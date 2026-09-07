using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// Represents a list of Managed Sites in the admin UI.
/// </summary>
public class ManagedSiteIndexViewModel
{
    /// <summary>
    /// Gets the Managed Sites shown in the admin UI.
    /// </summary>
    public IList<ManagedSite> ManagedSites { get; } = [];
}

/// <summary>
/// Represents a Managed Site edit form.
/// </summary>
public class ManagedSiteEditViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site status.
    /// </summary>
    public ManagedSiteStatus Status { get; set; }

    /// <summary>
    /// Gets or sets line-separated URL registrations.
    /// </summary>
    public string Urls { get; set; }
}
