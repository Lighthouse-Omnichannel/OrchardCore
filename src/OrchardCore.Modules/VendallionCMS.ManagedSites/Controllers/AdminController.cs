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
    private readonly IAuthorizationService _authorizationService;
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
        INotifier notifier,
        IStringLocalizer<AdminController> stringLocalizer,
        IHtmlLocalizer<AdminController> htmlLocalizer)
    {
        _managedSiteService = managedSiteService;
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
