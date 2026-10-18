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

/// <summary>
/// Who may work on one Managed Site, and who could be given leave to.
/// </summary>
/// <remarks>
/// Authority over a Managed Site is a permission, and permissions are held by roles, so this screen is
/// about roles. It is still reached from the Managed Site, because the question an administrator
/// arrives with is who may work on this site, and the roles screen answers the other question.
/// </remarks>
public sealed class ManagedSiteAccessViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site whose access is being administered.
    /// </summary>
    public ManagedSite ManagedSite { get; set; }

    /// <summary>
    /// Gets or sets the name of the role created with the Managed Site, which giving access adds to.
    /// </summary>
    public string EditorRoleName { get; set; }

    /// <summary>
    /// Gets or sets whether that role still exists.
    /// </summary>
    public bool EditorRoleExists { get; set; }

    /// <summary>
    /// Gets or sets the roles carrying this Managed Site, and who is in each.
    /// </summary>
    public IReadOnlyList<ManagedSiteAccessRole> Roles { get; set; } = [];

    /// <summary>
    /// Gets or sets the users holding none of those roles, who could be given one.
    /// </summary>
    public IReadOnlyList<ManagedSiteAccessUser> Available { get; set; } = [];

    /// <summary>
    /// Gets or sets whether more users matched than are listed.
    /// </summary>
    public bool MoreAvailable { get; set; }

    /// <summary>
    /// Gets or sets what the users who could be given access are narrowed by.
    /// </summary>
    public string Search { get; set; }
}

/// <summary>
/// One role carrying a Managed Site, and the users in it.
/// </summary>
/// <param name="Name">The role name.</param>
/// <param name="IsEditorRole">Whether this is the role created with the Managed Site.</param>
/// <param name="Users">The users holding it.</param>
public sealed record ManagedSiteAccessRole(
    string Name,
    bool IsEditorRole,
    IReadOnlyList<ManagedSiteAccessUser> Users);

/// <summary>
/// One user, as this screen shows them.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="UserName">The user name.</param>
/// <param name="Email">The email address, if any.</param>
public sealed record ManagedSiteAccessUser(string UserId, string UserName, string Email);
