using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Resolves and records the Managed Site that is active for the caller's portal session.
/// </summary>
[Route("api/managed-sites/session")]
public sealed class ManagedSiteSessionApiController : ManagedSitesApiControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteSessionApiController" /> class.
    /// </summary>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="sessionService">The Managed Site session service.</param>
    public ManagedSiteSessionApiController(
        IManagedSiteClearanceService clearanceService,
        IManagedSiteSessionService sessionService)
        : base(clearanceService, sessionService)
    {
    }

    /// <summary>
    /// Gets the active Managed Site scope, auto-selecting when exactly one site is authorized.
    /// </summary>
    /// <returns>The active session scope, or the sites to choose from.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ManagedSiteSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Current()
    {
        var result = await SessionService.ResolveAsync(User);

        if (result.Status == ManagedSiteSessionStatus.NoClearance)
        {
            return ManagedSitesProblem(
                StatusCodes.Status403Forbidden,
                "Managed Site clearance required",
                "The caller holds no effective Managed Site clearance.",
                ManagedSitesConstants.ErrorCodes.NoClearance);
        }

        return Ok(ToResponse(result));
    }

    /// <summary>
    /// Selects the Managed Site that scopes the caller's session.
    /// </summary>
    /// <param name="request">The Managed Site to activate.</param>
    /// <returns>The active session scope, or a problem describing why selection failed.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ManagedSiteSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Select([FromBody] SelectManagedSiteRequest request)
    {
        var managedSiteId = request?.ManagedSiteId;

        // Reject a mismatched consistency header before touching session state.
        if (Request.Headers.TryGetValue(ManagedSitesConstants.Headers.ManagedSiteId, out var headerValues))
        {
            var headerManagedSiteId = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(headerManagedSiteId)
                && !string.Equals(headerManagedSiteId, managedSiteId, StringComparison.Ordinal))
            {
                return ManagedSitesProblem(
                    StatusCodes.Status409Conflict,
                    "Managed Site scope mismatch",
                    $"The {ManagedSitesConstants.Headers.ManagedSiteId} header does not match the requested Managed Site.",
                    ManagedSitesConstants.ErrorCodes.ScopeMismatch);
            }
        }

        var result = await SessionService.SelectAsync(User, managedSiteId);

        switch (result.Status)
        {
            case ManagedSiteSessionStatus.NoClearance:
                return ManagedSitesProblem(
                    StatusCodes.Status403Forbidden,
                    "Managed Site clearance required",
                    "The caller holds no effective Managed Site clearance.",
                    ManagedSitesConstants.ErrorCodes.NoClearance);

            case ManagedSiteSessionStatus.Forbidden:
                return ManagedSitesProblem(
                    StatusCodes.Status403Forbidden,
                    "Managed Site clearance required",
                    "The signed authorization clearance does not grant access to the requested Managed Site.",
                    ManagedSitesConstants.ErrorCodes.NoClearance);

            case ManagedSiteSessionStatus.Unavailable:
                return ManagedSitesProblem(
                    StatusCodes.Status409Conflict,
                    "Managed Site unavailable",
                    "The requested Managed Site is disabled or no longer available.",
                    ManagedSitesConstants.ErrorCodes.ManagedSiteUnavailable);

            default:
                return Ok(ToResponse(result));
        }
    }

    private static ManagedSiteSessionResponse ToResponse(ManagedSiteSessionResult result)
    {
        var response = new ManagedSiteSessionResponse
        {
            ManagedSiteId = result.Scope?.ManagedSiteId,
            SelectedAt = result.Scope?.SelectedAt,
            RequiresSelection = result.Status == ManagedSiteSessionStatus.RequiresSelection,
        };

        foreach (var managedSite in result.AuthorizedManagedSites)
        {
            response.AuthorizedManagedSites.Add(ManagedSiteSummary.From(managedSite));
        }

        return response;
    }
}
