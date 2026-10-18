using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Users;
using OrchardCore.Users.Indexes;
using OrchardCore.Users.Services;
using YesSql;
using ISession = YesSql.ISession;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Provides admin screens for Managed Site definitions.
/// </summary>
[Admin("ManagedSites/{action}/{code?}", "ManagedSites{action}")]
public sealed class AdminController : Controller
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IManagedSiteRoleService _roleService;
    private readonly UserManager<IUser> _userManager;
    private readonly ISession _session;
    private readonly INotifier _notifier;

    internal readonly IStringLocalizer S;
    internal readonly IHtmlLocalizer H;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="notifier">The admin notifier.</param>
    /// <param name="stringLocalizer">The string localizer.</param>
    /// <param name="htmlLocalizer">The HTML localizer used for admin notifications.</param>
    public AdminController(
        IManagedSiteService managedSiteService,
        IAuthorizationService authorizationService,
        IManagedSiteRoleService roleService,
        UserManager<IUser> userManager,
        ISession session,
        INotifier notifier,
        IStringLocalizer<AdminController> stringLocalizer,
        IHtmlLocalizer<AdminController> htmlLocalizer)
    {
        _managedSiteService = managedSiteService;
        _authorizationService = authorizationService;
        _roleService = roleService;
        _userManager = userManager;
        _session = session;
        _notifier = notifier;
        S = stringLocalizer;
        H = htmlLocalizer;
    }

    /// <summary>
    /// Lists Managed Sites.
    /// </summary>
    /// <returns>The Managed Sites admin index view.</returns>
    public async Task<IActionResult> Index()
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        var model = new ManagedSiteIndexViewModel();
        foreach (var managedSite in (await _managedSiteService.ListAsync()).OrderBy(managedSite => managedSite.Name, StringComparer.OrdinalIgnoreCase))
        {
            model.ManagedSites.Add(managedSite);
        }

        return View(model);
    }

    /// <summary>
    /// Shows the form for a new Managed Site.
    /// </summary>
    /// <returns>The Managed Site editor view.</returns>
    public async Task<IActionResult> Create()
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        return View(nameof(Edit), new ManagedSiteEditViewModel { IsNew = true });
    }

    /// <summary>
    /// Creates a Managed Site.
    /// </summary>
    /// <param name="model">The submitted definition.</param>
    /// <returns>A redirect to the index on success, or the editor with validation errors.</returns>
    [HttpPost]
    [ActionName(nameof(Create))]
    public async Task<IActionResult> CreatePost(ManagedSiteEditViewModel model)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        // Saving under a code that already exists is how an edit is expressed, so creating has to say
        // that the code is taken rather than quietly replacing the Managed Site that holds it.
        if (!string.IsNullOrWhiteSpace(model.Code)
            && await _managedSiteService.GetAsync(model.Code.Trim()) is not null)
        {
            ModelState.AddModelError(
                nameof(ManagedSiteEditViewModel.Code),
                S["Another managed site already has that code."]);
        }

        if (!await TrySaveAsync(model))
        {
            model.IsNew = true;

            return View(nameof(Edit), model);
        }

        await _notifier.SuccessAsync(H["Managed site created successfully."]);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows the form for an existing Managed Site.
    /// </summary>
    /// <param name="code">The Managed Site code.</param>
    /// <returns>The Managed Site editor view.</returns>
    public async Task<IActionResult> Edit(string code)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        var managedSite = await _managedSiteService.GetAsync(code);
        if (managedSite == null)
        {
            return NotFound();
        }

        return View(ManagedSiteEditViewModel.From(managedSite));
    }

    /// <summary>
    /// Updates an existing Managed Site.
    /// </summary>
    /// <param name="code">The Managed Site code.</param>
    /// <param name="model">The submitted definition.</param>
    /// <returns>A redirect to the index on success, or the editor with validation errors.</returns>
    [HttpPost]
    [ActionName(nameof(Edit))]
    public async Task<IActionResult> EditPost(string code, ManagedSiteEditViewModel model)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        if (await _managedSiteService.GetAsync(code) == null)
        {
            return NotFound();
        }

        model.Code = code;

        if (!await TrySaveAsync(model))
        {
            model.IsNew = false;

            return View(nameof(Edit), model);
        }

        await _notifier.SuccessAsync(H["Managed site updated successfully."]);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Deletes a Managed Site and the URLs it owns.
    /// </summary>
    /// <param name="code">The Managed Site code.</param>
    /// <returns>A redirect to the index.</returns>
    [HttpPost]
    public async Task<IActionResult> Delete(string code)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        if (!await _managedSiteService.DeleteAsync(code))
        {
            return NotFound();
        }

        await _notifier.SuccessAsync(H["Managed site deleted successfully."]);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows who may work on a Managed Site, and lets that be changed.
    /// </summary>
    /// <remarks>
    /// Authority over a Managed Site is a permission and permissions are held by roles, so this screen
    /// is about roles. It is reached from the Managed Site because that is the question an
    /// administrator arrives with; the roles screen answers the other one, and answering this from
    /// there meant opening every role to find out which carried this site.
    ///
    /// Giving somebody access adds them to the role made with the Managed Site. Taking it away removes
    /// them from the role it is in. Neither is anything the roles screen could not also do.
    /// </remarks>
    /// <param name="code">The Managed Site code.</param>
    /// <param name="search">Part of a name to narrow the users who could be given access by.</param>
    /// <returns>The access view, or a refusal.</returns>
    public async Task<IActionResult> Access(string code, string search)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        var managedSite = await _managedSiteService.GetAsync(code);

        if (managedSite is null)
        {
            return NotFound();
        }

        return View(await BuildAccessAsync(managedSite, search));
    }

    /// <summary>
    /// Adds a user to the role made with the Managed Site.
    /// </summary>
    /// <param name="code">The Managed Site code.</param>
    /// <param name="userId">The user to give it to.</param>
    /// <param name="search">The search the administrator had.</param>
    /// <returns>The access screen, carrying what happened.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GiveAccess(string code, string userId, string search)
    {
        var refusal = await RefuseUnlessGoverningAsync(code);

        if (refusal is not null)
        {
            return refusal;
        }

        var managedSite = await _managedSiteService.GetAsync(code);
        var roleName = _roleService.GetEditorRoleName(managedSite);

        // Made when the Managed Site was defined, but an administrator may have removed it since.
        await _roleService.EnsureEditorRoleAsync(managedSite);

        if (!await CanAssignAsync(roleName))
        {
            return Forbid();
        }

        if (await _userManager.FindByIdAsync(userId) is { } user)
        {
            await _userManager.AddToRoleAsync(user, roleName);
            await _userManager.UpdateSecurityStampAsync(user);

            await _notifier.SuccessAsync(H["{0} can now work on this managed site.", await _userManager.GetUserNameAsync(user)]);
        }
        else
        {
            await _notifier.WarningAsync(H["That user could not be found."]);
        }

        return RedirectToAction(nameof(Access), new { code, search });
    }

    /// <summary>
    /// Removes a user from one of the roles carrying the Managed Site.
    /// </summary>
    /// <param name="code">The Managed Site code.</param>
    /// <param name="userId">The user to take it from.</param>
    /// <param name="roleName">The role to take them out of.</param>
    /// <param name="search">The search the administrator had.</param>
    /// <returns>The access screen, carrying what happened.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TakeAccess(string code, string userId, string roleName, string search)
    {
        var refusal = await RefuseUnlessGoverningAsync(code);

        if (refusal is not null)
        {
            return refusal;
        }

        if (!await CanAssignAsync(roleName))
        {
            return Forbid();
        }

        if (await _userManager.FindByIdAsync(userId) is { } user)
        {
            await _userManager.RemoveFromRoleAsync(user, roleName);

            // The role is gone from their principal only once it is rebuilt, and an open session keeps
            // the one it signed in with until something moves the stamp.
            await _userManager.UpdateSecurityStampAsync(user);

            await _notifier.SuccessAsync(H["{0} no longer holds {1}.", await _userManager.GetUserNameAsync(user), roleName]);
        }

        return RedirectToAction(nameof(Access), new { code, search });
    }

    /// <summary>
    /// Refuses anybody who may not govern Managed Sites, or a Managed Site that is not there.
    /// </summary>
    /// <param name="code">The Managed Site code.</param>
    /// <returns>The refusal, or <see langword="null" /> when there is none.</returns>
    private async Task<IActionResult> RefuseUnlessGoverningAsync(string code)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        return await _managedSiteService.GetAsync(code) is null ? NotFound() : null;
    }

    /// <summary>
    /// Answers whether the caller may put people into one role.
    /// </summary>
    /// <remarks>
    /// Governing Managed Sites is not authority over the tenant's users. Assigning a role is the
    /// platform's decision, asked here so that this screen cannot hand out a role its own permission
    /// screen would refuse.
    /// </remarks>
    /// <param name="roleName">The role.</param>
    /// <returns><see langword="true" /> when the caller may assign it.</returns>
    private async Task<bool> CanAssignAsync(string roleName)
        => !string.IsNullOrEmpty(roleName)
            && await _authorizationService.AuthorizeAsync(
                User,
                UsersPermissions.CreateAssignRoleToUsersPermission(roleName));

    /// <summary>
    /// Reads who carries the Managed Site, and who could be given it.
    /// </summary>
    private async Task<ManagedSiteAccessViewModel> BuildAccessAsync(ManagedSite managedSite, string search)
    {
        const int Take = 25;

        var roleNames = await _roleService.ListCarryingRolesAsync(managedSite);
        var editorRoleName = _roleService.GetEditorRoleName(managedSite);

        var roles = new List<ManagedSiteAccessRole>();
        var holders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roleNames)
        {
            var users = (await _userManager.GetUsersInRoleAsync(roleName))
                .OfType<OrchardCore.Users.Models.User>()
                .OrderBy(user => user.UserName, StringComparer.OrdinalIgnoreCase)
                .Select(user => new ManagedSiteAccessUser(user.UserId, user.UserName, user.Email))
                .ToArray();

            foreach (var user in users)
            {
                holders.Add(user.UserId);
            }

            roles.Add(new ManagedSiteAccessRole(
                roleName,
                string.Equals(roleName, editorRoleName, StringComparison.OrdinalIgnoreCase),
                users));
        }

        // One more than the screen shows, so it can say the list was cut short without counting every
        // user in the tenant. Narrowed before paging, because the users already holding the site are
        // dropped afterwards and paging first would give short pages.
        var query = _session.Query<OrchardCore.Users.Models.User, UserIndex>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();

            query = query.Where(index =>
                index.NormalizedUserName.Contains(normalized) || index.NormalizedEmail.Contains(normalized));
        }

        var candidates = (await query
            .OrderBy(index => index.NormalizedUserName)
            .Take((Take * 2) + holders.Count)
            .ListAsync())
            .Where(user => !holders.Contains(user.UserId))
            .Select(user => new ManagedSiteAccessUser(user.UserId, user.UserName, user.Email))
            .ToArray();

        return new ManagedSiteAccessViewModel
        {
            ManagedSite = managedSite,
            EditorRoleName = editorRoleName,
            EditorRoleExists = roleNames.Any(name =>
                string.Equals(name, editorRoleName, StringComparison.OrdinalIgnoreCase)),
            Roles = roles,
            Available = [.. candidates.Take(Take)],
            MoreAvailable = candidates.Length > Take,
            Search = search,
        };
    }

    private async Task<bool> TrySaveAsync(ManagedSiteEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return false;
        }


        var managedSite = new ManagedSite
        {
            Code = model.Code,
            Name = model.Name,
            Status = model.Status,
            Hostname = model.Hostname,
            UrlPrefix = model.UrlPrefix,
        };

        try
        {
            await _managedSiteService.SaveAsync(managedSite);

            return true;
        }
        catch (ManagedSiteValidationException exception)
        {
            ModelState.AddModelError(
                exception.Code switch
                {
                    ManagedSitesConstants.ErrorCodes.InvalidCode => nameof(ManagedSiteEditViewModel.Code),
                    ManagedSitesConstants.ErrorCodes.CodeConflict => nameof(ManagedSiteEditViewModel.Code),
                    ManagedSitesConstants.ErrorCodes.InvalidName => nameof(ManagedSiteEditViewModel.Name),
                    ManagedSitesConstants.ErrorCodes.NameConflict => nameof(ManagedSiteEditViewModel.Name),
                    _ => nameof(ManagedSiteEditViewModel.Hostname),
                },
                Describe(exception));

            return false;
        }
    }

    /// <summary>
    /// Says in the reader's language why a Managed Site could not be saved.
    /// </summary>
    /// <remarks>
    /// The service raises a code and a message, and only the code is a contract. Wording belongs here,
    /// where there is a reader and a language to write it in; the message the service carries is a
    /// developer's description and reaches an administrator only when a refusal arrives that this
    /// screen does not know about.
    /// </remarks>
    /// <param name="exception">The refusal.</param>
    /// <returns>The message to show.</returns>
    private string Describe(ManagedSiteValidationException exception) => exception.Code switch
    {
        ManagedSitesConstants.ErrorCodes.InvalidCode =>
            S["A managed site code is lowercase letters and digits, beginning with a letter, up to {0} characters.",
                ManagedSitesConstants.Code.MaximumLength],
        ManagedSitesConstants.ErrorCodes.CodeConflict =>
            S["Another managed site already has that code."],
        ManagedSitesConstants.ErrorCodes.InvalidName =>
            S["A managed site needs a name."],
        ManagedSitesConstants.ErrorCodes.NameConflict =>
            S["Another managed site is already called that."],
        ManagedSitesConstants.ErrorCodes.InvalidUrl =>
            S["That address cannot be used. A host name goes without a scheme or a path, and a URL prefix is a single segment without slashes."],
        ManagedSitesConstants.ErrorCodes.UrlConflict =>
            S["Another managed site already answers on that address."],
        ManagedSitesConstants.ErrorCodes.ManagedSiteNotFound =>
            S["That managed site no longer exists."],
        _ => exception.Message,
    };
}
