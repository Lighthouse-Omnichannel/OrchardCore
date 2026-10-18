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
    /// Gets or sets the code naming the Managed Site, asked for once and fixed thereafter.
    /// </summary>
    /// <remarks>
    /// Empty when creating, which is the only time it can be set. The editor shows it afterwards but
    /// does not offer it: everything referring to a Managed Site refers to it by this.
    /// </remarks>
    public string Code { get; set; }

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
    /// Gets or sets a value indicating whether the form creates a new Managed Site.
    /// </summary>
    /// <remarks>
    /// Set by whichever action rendered the form rather than worked out from the code. A create that
    /// was refused comes back with the code the editor typed, so reading it from there turned the form
    /// into an edit and took away the field the error was about.
    /// </remarks>
    public bool IsNew { get; set; }

    /// <summary>
    /// Builds an edit model from a stored Managed Site.
    /// </summary>
    /// <param name="managedSite">The Managed Site.</param>
    /// <returns>The edit model.</returns>
    public static ManagedSiteEditViewModel From(ManagedSite managedSite) => new()
    {
        IsNew = false,
        Code = managedSite.Code,
        Name = managedSite.Name,
        Status = managedSite.Status,
        Hostname = managedSite.Hostname,
        UrlPrefix = managedSite.UrlPrefix,
    };
}
