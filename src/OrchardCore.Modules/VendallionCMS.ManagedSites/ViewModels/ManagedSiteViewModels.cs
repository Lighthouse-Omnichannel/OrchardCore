using System.ComponentModel.DataAnnotations;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

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
/// <remarks>
/// Mirrors the tenant editor: one hostname value holding one or more host names, and one URL prefix.
/// </remarks>
public class ManagedSiteEditViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site identifier, empty when creating.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site name.
    /// </summary>
    [Required(ErrorMessage = "A name is required.")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site status.
    /// </summary>
    public ManagedSiteStatus Status { get; set; } = ManagedSiteStatus.Enabled;

    /// <summary>
    /// Gets or sets the host names this Managed Site answers on.
    /// </summary>
    public string Hostname { get; set; }

    /// <summary>
    /// Gets or sets the URL prefix this Managed Site answers under.
    /// </summary>
    public string UrlPrefix { get; set; }

    /// <summary>
    /// Gets a value indicating whether the form creates a new Managed Site.
    /// </summary>
    public bool IsNew => string.IsNullOrEmpty(Id);

    /// <summary>
    /// Builds an edit model from a stored Managed Site.
    /// </summary>
    /// <param name="managedSite">The Managed Site.</param>
    /// <returns>The edit model.</returns>
    public static ManagedSiteEditViewModel From(ManagedSite managedSite) => new()
    {
        Id = managedSite.Id,
        Name = managedSite.Name,
        Status = managedSite.Status,
        Hostname = managedSite.Hostname,
        UrlPrefix = managedSite.UrlPrefix,
    };
}

/// <summary>
/// Who may work on one Managed Site, and who could be given leave to.
/// </summary>
public sealed class ManagedSiteSecurityViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site whose security is being administered.
    /// </summary>
    public ManagedSite ManagedSite { get; set; }

    /// <summary>
    /// Gets or sets the users holding clearance for it, and what each may do.
    /// </summary>
    public IReadOnlyList<ManagedSiteUserClearance> Cleared { get; set; } = [];

    /// <summary>
    /// Gets or sets the users holding none, who could be given it.
    /// </summary>
    public IReadOnlyList<ManagedSiteUserClearance> Available { get; set; } = [];

    /// <summary>
    /// Gets or sets whether more users matched than are listed.
    /// </summary>
    /// <remarks>
    /// A tenant can hold more users than anyone wants listed, so the list stops and says so rather than
    /// growing without end. Narrowing it is how the rest are reached.
    /// </remarks>
    public bool MoreAvailable { get; set; }

    /// <summary>
    /// Gets or sets what the users without clearance are narrowed by.
    /// </summary>
    public string AvailableSearch { get; set; }

    /// <summary>
    /// Gets or sets what the users with clearance are narrowed by.
    /// </summary>
    public string ClearedSearch { get; set; }
}
