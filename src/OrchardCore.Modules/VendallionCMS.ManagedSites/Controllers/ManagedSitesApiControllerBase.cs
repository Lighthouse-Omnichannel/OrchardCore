using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Base controller for Managed Sites API endpoints.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = OrchardCoreConstants.AuthenticationSchemes.Api)]
public abstract class ManagedSitesApiControllerBase : ControllerBase
{
    /// <summary>
    /// Returns a consistent forbidden result for Managed Site scope failures.
    /// </summary>
    /// <returns>A challenge or forbid result for the API authentication scheme.</returns>
    protected IActionResult ChallengeOrForbidApi()
        => Forbid(OrchardCoreConstants.AuthenticationSchemes.Api);
}
