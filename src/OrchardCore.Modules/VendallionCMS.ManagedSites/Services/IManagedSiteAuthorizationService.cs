using System.Security.Claims;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Validates whether a user can operate within a Managed Site scope.
/// </summary>
public interface IManagedSiteAuthorizationService
{
    /// <summary>
    /// Checks whether the specified user is authorized for the Managed Site and scope.
    /// </summary>
    /// <param name="user">The user principal to evaluate.</param>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <param name="scope">The requested action scope.</param>
    /// <returns><see langword="true" /> when the user is authorized; otherwise, <see langword="false" />.</returns>
    ValueTask<bool> AuthorizeAsync(ClaimsPrincipal user, string managedSiteId, string scope);

    /// <summary>
    /// Gets Managed Site clearances represented by the specified user principal.
    /// </summary>
    /// <param name="user">The user principal to evaluate.</param>
    /// <returns>The clearances available to the user.</returns>
    ValueTask<IReadOnlyList<ManagedSiteClearance>> GetClearancesAsync(ClaimsPrincipal user);
}
