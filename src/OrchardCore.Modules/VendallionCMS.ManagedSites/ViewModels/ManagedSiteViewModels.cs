using System.ComponentModel.DataAnnotations;
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
    public ManagedSiteStatus Status { get; set; } = ManagedSiteStatus.Draft;

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
