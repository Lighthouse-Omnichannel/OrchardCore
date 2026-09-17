using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Hosts the React-based Managed Site Admin Portal.
/// </summary>
[Admin("ManagedSites/Portal", "ManagedSites.Portal")]
public sealed class PortalController : Controller
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAntiforgery _antiforgery;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortalController" /> class.
    /// </summary>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="antiforgery">The antiforgery token provider.</param>
    public PortalController(IAuthorizationService authorizationService, IAntiforgery antiforgery)
    {
        _authorizationService = authorizationService;
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Renders the Managed Site Admin Portal host page.
    /// </summary>
    /// <remarks>
    /// The page only hosts the client application. Every scoped action the portal performs goes through
    /// the Managed Sites API, which re-checks clearance and active session scope on each request.
    /// </remarks>
    /// <returns>The portal host view.</returns>
    public async Task<IActionResult> Index()
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return Forbid();
        }

        return View(new ManagedSitePortalViewModel
        {
            ApiBaseUrl = Url.Content("~/api/managed-sites"),
            AntiforgeryToken = _antiforgery.GetAndStoreTokens(HttpContext).RequestToken,
        });
    }
}
