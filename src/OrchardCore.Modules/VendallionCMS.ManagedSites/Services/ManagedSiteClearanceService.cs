using System.Security.Claims;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Resolves the Managed Sites a user may operate on from signed authorization claims and scopes.
/// </summary>
public interface IManagedSiteClearanceService
{
    /// <summary>
    /// Gets the effective clearances carried by the user principal.
    /// </summary>
    /// <param name="user">The user principal to evaluate.</param>
    /// <returns>Clearances that are currently effective, ordered by Managed Site identifier.</returns>
    ValueTask<IReadOnlyList<ManagedSiteClearance>> GetEffectiveClearancesAsync(ClaimsPrincipal user);

    /// <summary>
    /// Gets the Managed Sites the user is authorized to manage.
    /// </summary>
    /// <remarks>
    /// Only Managed Sites that exist under the active Site Blueprint and are enabled are returned,
    /// so clearance for a removed or disabled Managed Site never reaches the portal.
    /// </remarks>
    /// <param name="user">The user principal to evaluate.</param>
    /// <returns>The authorized Managed Sites, ordered by name.</returns>
    ValueTask<IReadOnlyList<ManagedSite>> GetAuthorizedManagedSitesAsync(ClaimsPrincipal user);

    /// <summary>
    /// Determines whether the user holds effective clearance for a Managed Site and scope.
    /// </summary>
    /// <param name="user">The user principal to evaluate.</param>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <param name="scope">The requested action scope, or <see langword="null" /> for any scope.</param>
    /// <returns><see langword="true" /> when the user holds effective clearance; otherwise, <see langword="false" />.</returns>
    ValueTask<bool> HasClearanceAsync(ClaimsPrincipal user, string managedSiteId, string scope);
}

/// <summary>
/// Extracts Managed Site clearance from token claims and reconciles it with Managed Site definitions.
/// </summary>
public sealed class ManagedSiteClearanceService : IManagedSiteClearanceService
{
    private readonly IManagedSiteAuthorizationService _authorizationService;
    private readonly IManagedSiteService _managedSiteService;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteClearanceService" /> class.
    /// </summary>
    /// <param name="authorizationService">The claim-based authorization service.</param>
    /// <param name="managedSiteService">The Managed Site definition service.</param>
    /// <param name="timeProvider">The time provider used to evaluate clearance validity windows.</param>
    public ManagedSiteClearanceService(
        IManagedSiteAuthorizationService authorizationService,
        IManagedSiteService managedSiteService,
        TimeProvider timeProvider)
    {
        _authorizationService = authorizationService;
        _managedSiteService = managedSiteService;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ManagedSiteClearance>> GetEffectiveClearancesAsync(ClaimsPrincipal user)
    {
        var clearances = await _authorizationService.GetClearancesAsync(user);
        if (clearances.Count == 0)
        {
            return [];
        }

        var now = _timeProvider.GetUtcNow();

        return clearances
            .Where(clearance => IsEffective(clearance, now))
            .GroupBy(clearance => clearance.ManagedSiteId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(clearance => clearance.ManagedSiteId, StringComparer.Ordinal)
            .ToArray();
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ManagedSite>> GetAuthorizedManagedSitesAsync(ClaimsPrincipal user)
    {
        var clearances = await GetEffectiveClearancesAsync(user);
        if (clearances.Count == 0)
        {
            return [];
        }

        var clearedIds = clearances
            .Select(clearance => clearance.ManagedSiteId)
            .ToHashSet(StringComparer.Ordinal);

        var managedSites = await _managedSiteService.ListAsync();

        return managedSites
            .Where(managedSite => clearedIds.Contains(managedSite.Id))
            .Where(managedSite => managedSite.Status == ManagedSiteStatus.Enabled)
            .OrderBy(managedSite => managedSite.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasClearanceAsync(ClaimsPrincipal user, string managedSiteId, string scope)
    {
        if (string.IsNullOrWhiteSpace(managedSiteId))
        {
            return false;
        }

        var clearances = await GetEffectiveClearancesAsync(user);

        return clearances.Any(clearance =>
            string.Equals(clearance.ManagedSiteId, managedSiteId, StringComparison.Ordinal)
            && HasScope(clearance, scope));
    }

    private bool IsEffective(ManagedSiteClearance clearance, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(clearance.ManagedSiteId))
        {
            return false;
        }

        if (clearance.EffectiveFrom.HasValue && clearance.EffectiveFrom.Value > now)
        {
            return false;
        }

        return !clearance.EffectiveTo.HasValue || clearance.EffectiveTo.Value > now;
    }

    private static bool HasScope(ManagedSiteClearance clearance, string scope)
        => string.IsNullOrWhiteSpace(scope) || clearance.Scopes.Contains(scope) || clearance.Scopes.Contains("*");
}
