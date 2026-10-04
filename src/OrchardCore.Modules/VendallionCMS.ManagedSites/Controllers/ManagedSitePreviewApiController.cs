using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Hands an editor the address that shows their Managed Site's composed output.
/// </summary>
/// <remarks>
/// Preview is its own clearance scope. An editor may be trusted to look at what their Managed Site
/// would serve without being trusted to change it, and asking to see unpublished work is a further
/// step again, which the request itself is authorized for when it arrives.
/// </remarks>
[Route("api/managed-sites/{managedSiteId}/preview")]
public sealed class ManagedSitePreviewApiController : ManagedSitesApiControllerBase
{
    private readonly IManagedSitePreviewService _previewService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSitePreviewApiController" /> class.
    /// </summary>
    /// <param name="previewService">The Managed Site preview service.</param>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="sessionService">The Managed Site session service.</param>
    public ManagedSitePreviewApiController(
        IManagedSitePreviewService previewService,
        IManagedSiteClearanceService clearanceService,
        IManagedSiteSessionService sessionService)
        : base(clearanceService, sessionService)
    {
        _previewService = previewService;
    }

    /// <summary>
    /// Builds the preview address for one path on the Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier from the route.</param>
    /// <param name="request">The path to preview.</param>
    /// <returns>The preview address, or a problem describing why it was rejected.</returns>
    [HttpPost("")]
    [ProducesResponseType(typeof(ManagedSitePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Post(string managedSiteId, [FromBody] ManagedSitePreviewRequest request)
    {
        var validation = await ValidateManagedSiteScopeAsync(managedSiteId, ManagedSitesConstants.Scopes.Preview);

        if (!validation.Succeeded)
        {
            return validation.Failure;
        }

        var preview = await _previewService.CreateAsync(
            managedSiteId,
            request?.Url,
            request?.IncludeDrafts == true);

        if (preview is null)
        {
            return ManagedSitesProblem(
                StatusCodes.Status409Conflict,
                "Managed Site unavailable",
                "The Managed Site is not serving requests, so it has no composed output to preview.",
                ManagedSitesConstants.ErrorCodes.ManagedSiteUnavailable);
        }

        return Ok(new ManagedSitePreviewResponse
        {
            PreviewUrl = preview.PreviewUrl,
            ManagedSiteId = preview.ManagedSiteId,
            CompositionMode = preview.CompositionMode,
        });
    }
}
