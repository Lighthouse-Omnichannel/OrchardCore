using System.Security.Claims;
using Microsoft.Extensions.Logging;
using OrchardCore.Entities;
using OrchardCore.Users;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Adds Managed Site clearance claims to the signed principal.
/// </summary>
/// <remarks>
/// The claims are produced by the platform claims principal factory, so they reach both the admin
/// cookie and the issued access token from the same grant record. Custom claims carry no explicit
/// destination, so the OpenID access controller places them in the access token only, which is where
/// an authorization concern belongs.
///
/// Each grant becomes one <c>managed_site</c> claim, so a user cleared for several Managed Sites
/// carries several claims and the token serializes them as an array.
/// </remarks>
public sealed class ManagedSiteClaimsProvider : IUserClaimsProvider
{
    private readonly ILogger<ManagedSiteClaimsProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteClaimsProvider" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ManagedSiteClaimsProvider(ILogger<ManagedSiteClaimsProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task GenerateAsync(IUser user, ClaimsIdentity claims)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(claims);

        if (user is not User entity || !entity.TryGet<ManagedSiteClearanceSettings>(out var settings))
        {
            return Task.CompletedTask;
        }

        var emitted = new HashSet<string>(StringComparer.Ordinal);

        foreach (var grant in settings.Grants)
        {
            var value = BuildClaimValue(grant);
            if (value is not null && emitted.Add(value))
            {
                claims.AddClaim(new Claim(ManagedSiteAuthorizationService.ManagedSiteClaimType, value));
            }
        }

        return Task.CompletedTask;
    }

    private string BuildClaimValue(ManagedSiteClearanceGrant grant)
    {
        var managedSiteId = grant?.ManagedSiteId?.Trim();

        if (string.IsNullOrEmpty(managedSiteId))
        {
            return null;
        }

        // The claim packs the Managed Site and its scopes into one value separated by a colon, so an
        // identifier containing a separator would silently widen or misdirect the grant when parsed back.
        if (managedSiteId.Contains(':', StringComparison.Ordinal) || managedSiteId.Contains(',', StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Skipping Managed Site clearance claim because the Managed Site identifier contains a reserved separator.");

            return null;
        }

        var scopes = grant.Scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Where(scope => !scope.Contains(',', StringComparison.Ordinal) && !scope.Contains(':', StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // No scopes means every scope, which the clearance parser represents as the wildcard form.
        return scopes.Length == 0
            ? managedSiteId
            : $"{managedSiteId}:{string.Join(',', scopes)}";
    }
}
