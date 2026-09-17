using System.Security.Claims;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Describes the outcome of resolving or selecting the active Managed Site for a portal session.
/// </summary>
public enum ManagedSiteSessionStatus
{
    /// <summary>
    /// The user holds no effective Managed Site clearance.
    /// </summary>
    NoClearance,

    /// <summary>
    /// The user is authorized for more than one Managed Site and must choose one.
    /// </summary>
    RequiresSelection,

    /// <summary>
    /// An active Managed Site is selected for the session.
    /// </summary>
    Selected,

    /// <summary>
    /// The requested Managed Site is not covered by the user's clearance.
    /// </summary>
    Forbidden,

    /// <summary>
    /// The requested Managed Site does not exist or is not enabled.
    /// </summary>
    Unavailable,
}

/// <summary>
/// Represents the result of a Managed Site session operation.
/// </summary>
public sealed class ManagedSiteSessionResult
{
    /// <summary>
    /// Gets the outcome status.
    /// </summary>
    public required ManagedSiteSessionStatus Status { get; init; }

    /// <summary>
    /// Gets the active scope when one is selected.
    /// </summary>
    public ActiveManagedSiteSessionScope Scope { get; init; }

    /// <summary>
    /// Gets the Managed Sites the user is authorized to manage.
    /// </summary>
    public IReadOnlyList<ManagedSite> AuthorizedManagedSites { get; init; } = [];
}

/// <summary>
/// Resolves and records the active Managed Site scope for a portal session.
/// </summary>
public interface IManagedSiteSessionService
{
    /// <summary>
    /// Resolves the active Managed Site for the user, auto-selecting when exactly one is authorized.
    /// </summary>
    /// <param name="user">The user principal.</param>
    /// <returns>The session resolution result.</returns>
    ValueTask<ManagedSiteSessionResult> ResolveAsync(ClaimsPrincipal user);

    /// <summary>
    /// Selects a Managed Site as the active scope for the user's session.
    /// </summary>
    /// <param name="user">The user principal.</param>
    /// <param name="managedSiteId">The Managed Site to select.</param>
    /// <returns>The session selection result.</returns>
    ValueTask<ManagedSiteSessionResult> SelectAsync(ClaimsPrincipal user, string managedSiteId);
}

/// <summary>
/// Applies the active-scope rules: auto-select a single authorized Managed Site, require an explicit
/// choice when several are authorized, and invalidate a stored selection that clearance no longer covers.
/// </summary>
public sealed class ManagedSiteSessionService : IManagedSiteSessionService
{
    private readonly IManagedSiteClearanceService _clearanceService;
    private readonly IManagedSiteService _managedSiteService;
    private readonly IManagedSiteSessionStore _sessionStore;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteSessionService" /> class.
    /// </summary>
    /// <param name="clearanceService">The clearance service.</param>
    /// <param name="managedSiteService">The Managed Site definition service.</param>
    /// <param name="sessionStore">The active scope store.</param>
    /// <param name="timeProvider">The time provider used to stamp selections.</param>
    public ManagedSiteSessionService(
        IManagedSiteClearanceService clearanceService,
        IManagedSiteService managedSiteService,
        IManagedSiteSessionStore sessionStore,
        TimeProvider timeProvider)
    {
        _clearanceService = clearanceService;
        _managedSiteService = managedSiteService;
        _sessionStore = sessionStore;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedSiteSessionResult> ResolveAsync(ClaimsPrincipal user)
    {
        var authorized = await _clearanceService.GetAuthorizedManagedSitesAsync(user);
        if (authorized.Count == 0)
        {
            return new ManagedSiteSessionResult { Status = ManagedSiteSessionStatus.NoClearance };
        }

        var userId = GetUserId(user);
        var stored = await _sessionStore.GetAsync(userId);

        // A stored selection stays valid only while clearance still covers it and the site is enabled.
        if (stored != null && authorized.Any(managedSite => string.Equals(managedSite.Id, stored.ManagedSiteId, StringComparison.Ordinal)))
        {
            return new ManagedSiteSessionResult
            {
                Status = ManagedSiteSessionStatus.Selected,
                Scope = stored,
                AuthorizedManagedSites = authorized,
            };
        }

        if (stored != null)
        {
            await _sessionStore.ClearAsync(userId);
        }

        if (authorized.Count == 1)
        {
            var scope = await RecordAsync(userId, authorized[0].Id);

            return new ManagedSiteSessionResult
            {
                Status = ManagedSiteSessionStatus.Selected,
                Scope = scope,
                AuthorizedManagedSites = authorized,
            };
        }

        return new ManagedSiteSessionResult
        {
            Status = ManagedSiteSessionStatus.RequiresSelection,
            AuthorizedManagedSites = authorized,
        };
    }

    /// <inheritdoc />
    public async ValueTask<ManagedSiteSessionResult> SelectAsync(ClaimsPrincipal user, string managedSiteId)
    {
        var clearances = await _clearanceService.GetEffectiveClearancesAsync(user);
        if (clearances.Count == 0)
        {
            return new ManagedSiteSessionResult { Status = ManagedSiteSessionStatus.NoClearance };
        }

        var authorized = await _clearanceService.GetAuthorizedManagedSitesAsync(user);

        if (string.IsNullOrWhiteSpace(managedSiteId)
            || !clearances.Any(clearance => string.Equals(clearance.ManagedSiteId, managedSiteId, StringComparison.Ordinal)))
        {
            return new ManagedSiteSessionResult
            {
                Status = ManagedSiteSessionStatus.Forbidden,
                AuthorizedManagedSites = authorized,
            };
        }

        var managedSite = await _managedSiteService.GetAsync(managedSiteId);
        if (managedSite == null || managedSite.Status != ManagedSiteStatus.Enabled)
        {
            return new ManagedSiteSessionResult
            {
                Status = ManagedSiteSessionStatus.Unavailable,
                AuthorizedManagedSites = authorized,
            };
        }

        var scope = await RecordAsync(GetUserId(user), managedSiteId);

        return new ManagedSiteSessionResult
        {
            Status = ManagedSiteSessionStatus.Selected,
            Scope = scope,
            AuthorizedManagedSites = authorized,
        };
    }

    private async ValueTask<ActiveManagedSiteSessionScope> RecordAsync(string userId, string managedSiteId)
    {
        var scope = new ActiveManagedSiteSessionScope
        {
            UserId = userId,
            ManagedSiteId = managedSiteId,
            SelectedAt = _timeProvider.GetUtcNow(),
        };

        await _sessionStore.SetAsync(scope);

        return scope;
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
