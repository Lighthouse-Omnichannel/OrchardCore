using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Provides admin screens for Managed Site definitions.
/// </summary>
[Admin]
public sealed class ManagedSitesAdminController : Controller
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IAuthorizationService _authorizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSitesAdminController" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="authorizationService">The authorization service.</param>
    public ManagedSitesAdminController(
        IManagedSiteService managedSiteService,
        IAuthorizationService authorizationService)
    {
        _managedSiteService = managedSiteService;
        _authorizationService = authorizationService;
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
        foreach (var managedSite in await _managedSiteService.ListAsync())
        {
            model.ManagedSites.Add(managedSite);
        }

        return View(model);
    }
}
