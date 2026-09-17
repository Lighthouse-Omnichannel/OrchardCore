using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Entities;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Provides admin screens for Managed Site definitions.
/// </summary>
[Admin("ManagedSites/{action}/{id?}", "ManagedSites{action}")]
public sealed class AdminController : Controller
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IIdGenerator _idGenerator;
    private readonly INotifier _notifier;

    internal readonly IStringLocalizer S;
    internal readonly IHtmlLocalizer H;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="idGenerator">The identifier generator.</param>
    /// <param name="notifier">The admin notifier.</param>
    /// <param name="stringLocalizer">The string localizer.</param>
    /// <param name="htmlLocalizer">The HTML localizer used for admin notifications.</param>
    public AdminController(
        IManagedSiteService managedSiteService,
        IAuthorizationService authorizationService,
        IIdGenerator idGenerator,
        INotifier notifier,
        IStringLocalizer<AdminController> stringLocalizer,
        IHtmlLocalizer<AdminController> htmlLocalizer)
    {
        _managedSiteService = managedSiteService;
        _authorizationService = authorizationService;
        _idGenerator = idGenerator;
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

        return View(nameof(Edit), new ManagedSiteEditViewModel());
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

        model.Id = _idGenerator.GenerateUniqueId();

        if (!await TrySaveAsync(model))
        {
            // The generated identifier is not shown to the editor and must not survive a failed attempt,
            // otherwise a retry would look like an edit of a Managed Site that was never stored.
            model.Id = null;

            return View(nameof(Edit), model);
        }

        await _notifier.SuccessAsync(H["Managed site created successfully."]);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows the form for an existing Managed Site.
    /// </summary>
    /// <param name="id">The Managed Site identifier.</param>
    /// <returns>The Managed Site editor view.</returns>
    public async Task<IActionResult> Edit(string id)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        var managedSite = await _managedSiteService.GetAsync(id);
        if (managedSite == null)
        {
            return NotFound();
        }

        return View(ManagedSiteEditViewModel.From(managedSite));
    }

    /// <summary>
    /// Updates an existing Managed Site.
    /// </summary>
    /// <param name="id">The Managed Site identifier.</param>
    /// <param name="model">The submitted definition.</param>
    /// <returns>A redirect to the index on success, or the editor with validation errors.</returns>
    [HttpPost]
    [ActionName(nameof(Edit))]
    public async Task<IActionResult> EditPost(string id, ManagedSiteEditViewModel model)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        if (await _managedSiteService.GetAsync(id) == null)
        {
            return NotFound();
        }

        model.Id = id;

        if (!await TrySaveAsync(model))
        {
            return View(nameof(Edit), model);
        }

        await _notifier.SuccessAsync(H["Managed site updated successfully."]);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Deletes a Managed Site and the URLs it owns.
    /// </summary>
    /// <param name="id">The Managed Site identifier.</param>
    /// <returns>A redirect to the index.</returns>
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        if (!await _managedSiteService.DeleteAsync(id))
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
            Id = model.Id,
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
                exception.Code == ManagedSitesConstants.ErrorCodes.InvalidName
                || exception.Code == ManagedSitesConstants.ErrorCodes.NameConflict
                    ? nameof(ManagedSiteEditViewModel.Name)
                    : nameof(ManagedSiteEditViewModel.Hostname),
                exception.Message);

            return false;
        }
    }
}
