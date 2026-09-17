using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Base controller for Managed Sites API endpoints.
/// </summary>
/// <remarks>
/// The authorization policy accepts the bearer API scheme and the admin cookie scheme, so the portal
/// works both as a token client and as an admin page. Managed Site clearance is always taken from the
/// authenticated principal's claims, never from client-supplied scope metadata.
/// </remarks>
[ApiController]
[Authorize(Policy = ManagedSitesConstants.AuthorizationPolicies.ManagedSitesApi)]
public abstract class ManagedSitesApiControllerBase : ControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSitesApiControllerBase" /> class.
    /// </summary>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="sessionService">The Managed Site session service.</param>
    protected ManagedSitesApiControllerBase(
        IManagedSiteClearanceService clearanceService,
        IManagedSiteSessionService sessionService)
    {
        ClearanceService = clearanceService;
        SessionService = sessionService;
    }

    /// <summary>
    /// Gets the Managed Site clearance service.
    /// </summary>
    protected IManagedSiteClearanceService ClearanceService { get; }

    /// <summary>
    /// Gets the Managed Site session service.
    /// </summary>
    protected IManagedSiteSessionService SessionService { get; }

    /// <summary>
    /// Outcome of validating the requested Managed Site scope against clearance and session state.
    /// </summary>
    protected sealed class ManagedSiteScopeValidation
    {
        /// <summary>
        /// Gets a value indicating whether the requested scope is valid.
        /// </summary>
        public bool Succeeded => Failure is null;

        /// <summary>
        /// Gets the result to return when validation failed.
        /// </summary>
        public IActionResult Failure { get; init; }

        /// <summary>
        /// Gets the validated active scope.
        /// </summary>
        public ActiveManagedSiteSessionScope Scope { get; init; }
    }

    /// <summary>
    /// Validates that the route scope, optional consistency header, signed clearance, and active
    /// portal session scope all refer to the same authorized Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier taken from the route.</param>
    /// <param name="scope">The action scope the operation requires, or <see langword="null" /> for any.</param>
    /// <returns>The validation outcome.</returns>
    protected async Task<ManagedSiteScopeValidation> ValidateManagedSiteScopeAsync(string managedSiteId, string scope = null)
    {
        // Optional consistency metadata must agree with the operation scope before anything else runs.
        if (Request.Headers.TryGetValue(ManagedSitesConstants.Headers.ManagedSiteId, out var headerValues))
        {
            var headerManagedSiteId = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(headerManagedSiteId)
                && !string.Equals(headerManagedSiteId, managedSiteId, StringComparison.Ordinal))
            {
                return Failed(ManagedSitesProblem(
                    StatusCodes.Status409Conflict,
                    "Managed Site scope mismatch",
                    $"The {ManagedSitesConstants.Headers.ManagedSiteId} header does not match the requested Managed Site.",
                    ManagedSitesConstants.ErrorCodes.ScopeMismatch));
            }
        }

        if (!await ClearanceService.HasClearanceAsync(User, managedSiteId, scope))
        {
            return Failed(ManagedSitesProblem(
                StatusCodes.Status403Forbidden,
                "Managed Site clearance required",
                "The signed authorization clearance does not grant access to the requested Managed Site.",
                ManagedSitesConstants.ErrorCodes.NoClearance));
        }

        var session = await SessionService.ResolveAsync(User);

        if (session.Status == ManagedSiteSessionStatus.NoClearance)
        {
            return Failed(ManagedSitesProblem(
                StatusCodes.Status403Forbidden,
                "Managed Site clearance required",
                "The caller holds no effective Managed Site clearance.",
                ManagedSitesConstants.ErrorCodes.NoClearance));
        }

        if (session.Status == ManagedSiteSessionStatus.RequiresSelection)
        {
            return Failed(ManagedSitesProblem(
                StatusCodes.Status409Conflict,
                "Active Managed Site required",
                "Select an active Managed Site for this session before performing scoped actions.",
                ManagedSitesConstants.ErrorCodes.SelectionRequired));
        }

        if (!string.Equals(session.Scope?.ManagedSiteId, managedSiteId, StringComparison.Ordinal))
        {
            return Failed(ManagedSitesProblem(
                StatusCodes.Status409Conflict,
                "Active Managed Site scope mismatch",
                "The requested Managed Site differs from the Managed Site active for this session.",
                ManagedSitesConstants.ErrorCodes.SessionScopeMismatch));
        }

        return new ManagedSiteScopeValidation { Scope = session.Scope };
    }

    /// <summary>
    /// Builds the consistent problem response used across the Managed Sites API.
    /// </summary>
    /// <param name="status">The HTTP status code.</param>
    /// <param name="title">The short summary.</param>
    /// <param name="detail">The occurrence-specific explanation.</param>
    /// <param name="code">The stable machine-readable code.</param>
    /// <returns>An object result carrying the problem body.</returns>
    protected IActionResult ManagedSitesProblem(int status, string title, string detail, string code)
        => StatusCode(status, new ManagedSitesApiProblem
        {
            Status = status,
            Title = title,
            Detail = detail,
            Code = code,
        });

    private static ManagedSiteScopeValidation Failed(IActionResult failure)
        => new() { Failure = failure };
}
