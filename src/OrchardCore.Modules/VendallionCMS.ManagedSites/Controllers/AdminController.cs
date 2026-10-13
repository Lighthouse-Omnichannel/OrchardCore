using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Users;
using OrchardCore.Users.Services;
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
    private readonly IManagedSiteSecurityService _securityService;
    private readonly SignInManager<IUser> _signInManager;
    private readonly IUserService _userService;
    private readonly IAuthorizationService _authorizationService;
    private readonly INotifier _notifier;

    internal readonly IStringLocalizer S;
    internal readonly IHtmlLocalizer H;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="securityService">The Managed Site security service.</param>
    /// <param name="signInManager">The sign-in manager, used to rebuild the caller's own principal.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="notifier">The admin notifier.</param>
    /// <param name="stringLocalizer">The string localizer.</param>
    /// <param name="htmlLocalizer">The HTML localizer used for admin notifications.</param>
    public AdminController(
        IManagedSiteService managedSiteService,
        IManagedSiteSecurityService securityService,
        SignInManager<IUser> signInManager,
        IUserService userService,
        IAuthorizationService authorizationService,
        INotifier notifier,
        IStringLocalizer<AdminController> stringLocalizer,
        IHtmlLocalizer<AdminController> htmlLocalizer)
    {
        _managedSiteService = managedSiteService;
        _securityService = securityService;
        _signInManager = signInManager;
        _userService = userService;
        _authorizationService = authorizationService;
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
    /// Clearance is administered from the Managed Site rather than from each user, because the question
    /// an administrator arrives with is who may work on this site, and answering it one user at a time
    /// meant opening every user to find out.
    ///
    /// Governing a Managed Site is what this needs, not editing its content: deciding who may work on a
    /// site is the same kind of decision as deciding what the site is.
    /// </remarks>
    /// <param name="code">The Managed Site code.</param>
    /// <param name="available">Part of a name to narrow the users without clearance by.</param>
    /// <param name="cleared">Part of a name to narrow the users with clearance by.</param>
    /// <returns>The access view, or a refusal.</returns>
    public async Task<IActionResult> Access(string code, string available, string cleared)
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

        return View(await BuildAccessAsync(managedSite, available, cleared));
    }

    /// <summary>
    /// Gives a user clearance for a Managed Site.
    /// </summary>
    /// <param name="code">The Managed Site code.</param>
    /// <param name="userId">The user to give it to.</param>
    /// <param name="available">The search the administrator had on the users without clearance.</param>
    /// <param name="cleared">The search the administrator had on the users with clearance.</param>
    /// <returns>A redirect back to the security view.</returns>
    [HttpPost]
    public async Task<IActionResult> Grant(string code, string userId, string available, string cleared)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        if (await _managedSiteService.GetAsync(code) is null)
        {
            return NotFound();
        }

        // Granted the run of the site to read, and nothing more until somebody says so on the row. The
        // least a clearance can carry is what it starts with.
        if (await _securityService.GrantAsync(code, userId))
        {
            await _notifier.SuccessAsync(H["That user can now work on this managed site."]);

            await RefreshOwnPrincipalAsync(userId);
        }
        else
        {
            await _notifier.WarningAsync(H["That user could not be found."]);
        }

        return RedirectToAction(nameof(Access), new { code, available, cleared });
    }

    /// <summary>
    /// Takes a user's clearance for a Managed Site away.
    /// </summary>
    /// <param name="code">The Managed Site code.</param>
    /// <param name="userId">The user to take it from.</param>
    /// <param name="available">The search the administrator had on the users without clearance.</param>
    /// <param name="cleared">The search the administrator had on the users with clearance.</param>
    /// <returns>A redirect back to the security view.</returns>
    [HttpPost]
    public async Task<IActionResult> Revoke(string code, string userId, string available, string cleared)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        if (await _managedSiteService.GetAsync(code) is null)
        {
            return NotFound();
        }

        if (await _securityService.RevokeAsync(code, userId))
        {
            await _notifier.SuccessAsync(H["That user can no longer work on this managed site."]);

            await RefreshOwnPrincipalAsync(userId);
        }
        else
        {
            await _notifier.WarningAsync(H["That user held no clearance for this managed site."]);
        }

        return RedirectToAction(nameof(Access), new { code, available, cleared });
    }

    /// <summary>
    /// Changes what a user's clearance for a Managed Site allows.
    /// </summary>
    /// <param name="code">The Managed Site code.</param>
    /// <param name="userId">The user whose clearance to change.</param>
    /// <param name="scopes">What it should allow.</param>
    /// <param name="available">The search the administrator had on the users without clearance.</param>
    /// <param name="cleared">The search the administrator had on the users with clearance.</param>
    /// <returns>A redirect back to the security view.</returns>
    [HttpPost]
    public async Task<IActionResult> Scopes(
        string code,
        string userId,
        string[] scopes,
        string available,
        string cleared)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        if (await _managedSiteService.GetAsync(code) is null)
        {
            return NotFound();
        }

        if (await _securityService.GrantAsync(code, userId, scopes))
        {
            await _notifier.SuccessAsync(H["What that user may do on this managed site was changed."]);

            await RefreshOwnPrincipalAsync(userId);
        }
        else
        {
            await _notifier.WarningAsync(H["That user could not be found."]);
        }

        return RedirectToAction(nameof(Access), new { code, available, cleared });
    }

    /// <summary>
    /// Rebuilds the caller's principal when they changed their own clearance.
    /// </summary>
    /// <remarks>
    /// Clearance is carried as claims on the signed principal, settled when the session was signed in,
    /// so changing the record alone leaves the session believing what it believed before. The platform
    /// does the same wherever an administrator edits their own user.
    ///
    /// Only for the caller: somebody else's session is reached by the security stamp instead, which the
    /// security service moves when a change takes something away.
    /// </remarks>
    /// <param name="userId">The user whose clearance changed.</param>
    private async Task RefreshOwnPrincipalAsync(string userId)
    {
        if (!string.Equals(User.FindFirstValue(ClaimTypes.NameIdentifier), userId, StringComparison.Ordinal))
        {
            return;
        }

        if (await _userService.GetUserByUniqueIdAsync(userId) is IUser user)
        {
            await _signInManager.RefreshSignInAsync(user);
        }
    }

    private async Task<ManagedSiteAccessViewModel> BuildAccessAsync(
        ManagedSite managedSite,
        string available,
        string cleared)
    {
        const int Take = 25;

        // One more than the screen shows, so it can say the list was cut short without counting every
        // user in the tenant.
        var candidates = await _securityService.ListAvailableAsync(managedSite.Code, available, Take);

        return new ManagedSiteAccessViewModel
        {
            ManagedSite = managedSite,
            Cleared = await _securityService.ListClearedAsync(managedSite.Code, cleared),
            Available = [.. candidates.Take(Take)],
            MoreAvailable = candidates.Count > Take,
            AvailableSearch = available,
            ClearedSearch = cleared,
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
