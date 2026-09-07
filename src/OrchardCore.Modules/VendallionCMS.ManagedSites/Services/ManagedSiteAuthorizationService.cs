using System.Security.Claims;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Validates token-derived Managed Site clearance.
/// </summary>
public sealed class ManagedSiteAuthorizationService : IManagedSiteAuthorizationService
{
    /// <summary>
    /// Claim type used to carry Managed Site clearance values.
    /// </summary>
    public const string ManagedSiteClaimType = "managed_site";

    /// <inheritdoc />
    public ValueTask<bool> AuthorizeAsync(ClaimsPrincipal user, string managedSiteId, string scope)
    {
        if (user?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(managedSiteId))
        {
            return ValueTask.FromResult(false);
        }

        var isAuthorized = user.FindAll(ManagedSiteClaimType)
            .Select(ParseClearance)
            .Any(clearance => clearance.ManagedSiteId == managedSiteId && HasScope(clearance, scope));

        return ValueTask.FromResult(isAuthorized);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ManagedSiteClearance>> GetClearancesAsync(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return ValueTask.FromResult<IReadOnlyList<ManagedSiteClearance>>([]);
        }

        var clearances = user.FindAll(ManagedSiteClaimType)
            .Select(ParseClearance)
            .Where(clearance => !string.IsNullOrEmpty(clearance.ManagedSiteId))
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<ManagedSiteClearance>>(clearances);
    }

    private static bool HasScope(ManagedSiteClearance clearance, string scope)
        => string.IsNullOrWhiteSpace(scope) || clearance.Scopes.Contains(scope) || clearance.Scopes.Contains("*");

    private static ManagedSiteClearance ParseClearance(Claim claim)
    {
        var parts = claim.Value.Split(':', 2, StringSplitOptions.TrimEntries);
        var clearance = new ManagedSiteClearance
        {
            UserId = claim.Subject?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            ManagedSiteId = parts[0],
        };

        if (parts.Length == 1)
        {
            clearance.Scopes.Add("*");
            return clearance;
        }

        foreach (var scope in parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            clearance.Scopes.Add(scope);
        }

        return clearance;
    }
}
