using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Exposes the Managed Sites a portal user is authorized to manage.
/// </summary>
[Route("api/managed-sites")]
public sealed class ManagedSitesApiController : ManagedSitesApiControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSitesApiController" /> class.
    /// </summary>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="sessionService">The Managed Site session service.</param>
    public ManagedSitesApiController(
        IManagedSiteClearanceService clearanceService,
        IManagedSiteSessionService sessionService)
        : base(clearanceService, sessionService)
    {
    }

    /// <summary>
    /// Lists the Managed Sites covered by the caller's signed clearance.
    /// </summary>
    /// <remarks>
    /// A caller with no clearance receives an empty list rather than an error, so the portal can render
    /// an explicit "no managed sites" state instead of failing.
    /// </remarks>
    /// <returns>The authorized Managed Sites.</returns>
    [HttpGet("authorized")]
    [ProducesResponseType(typeof(AuthorizedManagedSitesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Authorized()
    {
        var managedSites = await ClearanceService.GetAuthorizedManagedSitesAsync(User);

        var response = new AuthorizedManagedSitesResponse();
        foreach (var managedSite in managedSites)
        {
            response.Items.Add(ManagedSiteSummary.From(managedSite));
        }

        return Ok(response);
    }
}
