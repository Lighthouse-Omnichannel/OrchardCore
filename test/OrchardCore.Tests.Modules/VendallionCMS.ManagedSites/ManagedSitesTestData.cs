using System.Collections.Generic;
using System.Security.Claims;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites.Tests;

/// <summary>
/// Provides test data builders for Managed Sites tests.
/// </summary>
public static class ManagedSitesTestData
{
    /// <summary>
    /// The user identifier used by <see cref="User" />.
    /// </summary>
    public const string UserId = "user-id";

    /// <summary>
    /// Creates a test Managed Site addressed by host name and URL prefix.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <param name="status">The Managed Site lifecycle status.</param>
    /// <param name="name">The display name, defaulting to the identifier.</param>
    /// <param name="hostname">The host names it answers on, empty for every host.</param>
    /// <param name="urlPrefix">The URL prefix it answers under, empty for the root.</param>
    /// <returns>A test Managed Site.</returns>
    public static ManagedSite ManagedSite(
        string managedSiteId = "managed-site",
        ManagedSiteStatus status = ManagedSiteStatus.Enabled,
        string name = null,
        string hostname = null,
        string urlPrefix = null)
        => new()
        {
            Id = managedSiteId,
            Name = name ?? managedSiteId,
            Status = status,
            Hostname = hostname,
            UrlPrefix = urlPrefix,
        };

    /// <summary>
    /// Creates an authenticated principal carrying Managed Site clearance claims.
    /// </summary>
    /// <param name="managedSiteClaims">Clearance claim values such as <c>site-a:edit,publish</c>.</param>
    /// <returns>An authenticated principal.</returns>
    public static ClaimsPrincipal User(params string[] managedSiteClaims)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, UserId) };

        foreach (var managedSiteClaim in managedSiteClaims)
        {
            claims.Add(new Claim(ManagedSiteAuthorizationService.ManagedSiteClaimType, managedSiteClaim));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>
    /// Creates an anonymous principal.
    /// </summary>
    /// <returns>An unauthenticated principal.</returns>
    public static ClaimsPrincipal AnonymousUser() => new(new ClaimsIdentity());
}
