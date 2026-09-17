using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Creates and updates Managed Site definitions.
/// </summary>
/// <remarks>
/// Defining Managed Sites is Site Blueprint governance, not a scoped editor action, so this endpoint
/// authorizes on the Managed Sites permission rather than per-site clearance. Requiring clearance here
/// would be impossible to satisfy: nobody can hold clearance for a Managed Site that does not exist yet.
/// </remarks>
[Route("api/managed-sites")]
public sealed class ManagedSiteDefinitionsApiController : ManagedSitesApiControllerBase
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IAuthorizationService _authorizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteDefinitionsApiController" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="sessionService">The Managed Site session service.</param>
    public ManagedSiteDefinitionsApiController(
        IManagedSiteService managedSiteService,
        IAuthorizationService authorizationService,
        IManagedSiteClearanceService clearanceService,
        IManagedSiteSessionService sessionService)
        : base(clearanceService, sessionService)
    {
        _managedSiteService = managedSiteService;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Creates or replaces a Managed Site definition.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier from the route.</param>
    /// <param name="request">The definition to store.</param>
    /// <returns>The stored definition, or a problem describing why it was rejected.</returns>
    [HttpPut("{managedSiteId}")]
    [ProducesResponseType(typeof(ManagedSiteSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Put(string managedSiteId, [FromBody] ManagedSiteDefinitionRequest request)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return ManagedSitesProblem(
                StatusCodes.Status403Forbidden,
                "Managed Sites permission required",
                "Defining Managed Sites requires the Manage Managed Sites permission.",
                ManagedSitesConstants.ErrorCodes.NoClearance);
        }

        if (string.IsNullOrWhiteSpace(managedSiteId))
        {
            return ManagedSitesProblem(
                StatusCodes.Status400BadRequest,
                "Managed Site identifier required",
                "The route must name the Managed Site to create or update.",
                ManagedSitesConstants.ErrorCodes.ManagedSiteNotFound);
        }

        var status = ManagedSiteStatus.Draft;
        if (!string.IsNullOrWhiteSpace(request?.Status)
            && !Enum.TryParse(request.Status, ignoreCase: true, out status))
        {
            return ManagedSitesProblem(
                StatusCodes.Status400BadRequest,
                "Invalid Managed Site status",
                $"'{request.Status}' is not a valid Managed Site status.",
                ManagedSitesConstants.ErrorCodes.InvalidName);
        }


        var managedSite = new ManagedSite
        {
            Id = managedSiteId,
            Name = request?.Name,
            Status = status,
            Hostname = request?.Hostname,
            UrlPrefix = request?.UrlPrefix,
        };

        try
        {
            await _managedSiteService.SaveAsync(managedSite);
        }
        catch (ManagedSiteValidationException exception)
        {
            return ManagedSitesProblem(
                exception.IsConflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest,
                exception.IsConflict ? "Managed Site conflict" : "Invalid Managed Site definition",
                exception.Message,
                exception.Code);
        }

        return Ok(ManagedSiteSummary.From(managedSite));
    }

    /// <summary>
    /// Deletes a Managed Site definition and the URLs it owns.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier from the route.</param>
    /// <returns>No content when removed, or a problem describing why it was rejected.</returns>
    [HttpDelete("{managedSiteId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string managedSiteId)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageManagedSites))
        {
            return ManagedSitesProblem(
                StatusCodes.Status403Forbidden,
                "Managed Sites permission required",
                "Deleting Managed Sites requires the Manage Managed Sites permission.",
                ManagedSitesConstants.ErrorCodes.NoClearance);
        }

        if (!await _managedSiteService.DeleteAsync(managedSiteId))
        {
            return ManagedSitesProblem(
                StatusCodes.Status404NotFound,
                "Managed Site not found",
                "No Managed Site exists with the requested identifier.",
                ManagedSitesConstants.ErrorCodes.ManagedSiteNotFound);
        }

        return NoContent();
    }
}
