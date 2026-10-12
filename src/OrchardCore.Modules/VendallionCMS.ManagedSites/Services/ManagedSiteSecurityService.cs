using Microsoft.AspNetCore.Identity;
using OrchardCore.Entities;
using OrchardCore.Users;
using OrchardCore.Users.Indexes;
using OrchardCore.Users.Models;
using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Models;
using YesSql;
using YesSql.Services;
using ISession = YesSql.ISession;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Who holds clearance for a Managed Site, and who could be given it.
/// </summary>
/// <remarks>
/// Clearance is stored on the user and administered from the Managed Site, so this reads it the way the
/// administration screen asks: by site rather than by person.
/// </remarks>
public interface IManagedSiteSecurityService
{
    /// <summary>
    /// Lists the users holding clearance for a Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site.</param>
    /// <param name="search">Part of a user name to narrow by, or nothing for all of them.</param>
    /// <returns>The users, and the clearance each holds.</returns>
    ValueTask<IReadOnlyList<ManagedSiteUserClearance>> ListClearedAsync(string managedSiteId, string search = null);

    /// <summary>
    /// Lists the users who hold no clearance for a Managed Site and could be given it.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site.</param>
    /// <param name="search">Part of a user name to narrow by, or nothing for all of them.</param>
    /// <param name="take">How many to return.</param>
    /// <returns>The users.</returns>
    ValueTask<IReadOnlyList<ManagedSiteUserClearance>> ListAvailableAsync(
        string managedSiteId,
        string search = null,
        int take = 50);

    /// <summary>
    /// Grants a user clearance for a Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site.</param>
    /// <param name="userId">The user.</param>
    /// <param name="scopes">What the clearance allows, or nothing for the default.</param>
    /// <returns><see langword="true" /> when the user was found and the grant written.</returns>
    ValueTask<bool> GrantAsync(string managedSiteId, string userId, IEnumerable<string> scopes = null);

    /// <summary>
    /// Takes a user's clearance for a Managed Site away.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site.</param>
    /// <param name="userId">The user.</param>
    /// <returns><see langword="true" /> when the user held clearance and it was taken away.</returns>
    ValueTask<bool> RevokeAsync(string managedSiteId, string userId);
}

/// <summary>
/// One user's clearance for one Managed Site.
/// </summary>
/// <param name="UserId">The user's identifier.</param>
/// <param name="UserName">The user's name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Scopes">What the clearance allows, empty when it allows everything.</param>
public sealed record ManagedSiteUserClearance(
    string UserId,
    string UserName,
    string Email,
    IReadOnlyList<string> Scopes);

/// <inheritdoc />
public sealed class ManagedSiteSecurityService : IManagedSiteSecurityService
{
    private readonly ISession _session;
    private readonly UserManager<IUser> _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteSecurityService" /> class.
    /// </summary>
    /// <param name="session">The document session.</param>
    /// <param name="userManager">The user manager, which owns writing a user back.</param>
    public ManagedSiteSecurityService(ISession session, UserManager<IUser> userManager)
    {
        _session = session;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ManagedSiteUserClearance>> ListClearedAsync(
        string managedSiteId,
        string search = null)
    {
        if (string.IsNullOrEmpty(managedSiteId))
        {
            return [];
        }

        var rows = await _session
            .QueryIndex<ManagedSiteClearanceIndex>(index => index.ManagedSiteId == managedSiteId)
            .ListAsync();

        var userIds = rows.Select(row => row.UserId).ToArray();

        if (userIds.Length == 0)
        {
            return [];
        }

        var users = await _session
            .Query<User, UserIndex>(index => index.UserId.IsIn(userIds))
            .ListAsync();

        return Describe(users, managedSiteId, search);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ManagedSiteUserClearance>> ListAvailableAsync(
        string managedSiteId,
        string search = null,
        int take = 50)
    {
        var cleared = await _session
            .QueryIndex<ManagedSiteClearanceIndex>(index => index.ManagedSiteId == managedSiteId)
            .ListAsync();

        var exclude = cleared.Select(row => row.UserId).ToHashSet(StringComparer.Ordinal);

        var query = _session.Query<User, UserIndex>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();

            query = query.Where(index =>
                index.NormalizedUserName.Contains(normalized) || index.NormalizedEmail.Contains(normalized));
        }

        // One more than asked for, so the caller can say the list was cut short without counting every
        // user in the tenant.
        var users = await query
            .OrderBy(index => index.NormalizedUserName)
            .Take(take + exclude.Count + 1)
            .ListAsync();

        return
        [
            .. users
                .Where(user => !exclude.Contains(user.UserId))
                .Take(take + 1)
                .Select(user => new ManagedSiteUserClearance(user.UserId, user.UserName, user.Email, [])),
        ];
    }

    /// <inheritdoc />
    public async ValueTask<bool> GrantAsync(string managedSiteId, string userId, IEnumerable<string> scopes = null)
    {
        var user = await FindAsync(userId);

        if (user is null || string.IsNullOrEmpty(managedSiteId))
        {
            return false;
        }

        var settings = user.GetOrCreate<ManagedSiteClearanceSettings>();
        var narrowed = Narrows(ScopesOf(user, managedSiteId), scopes);

        ApplyGrant(settings, managedSiteId, scopes);

        user.Put(settings);
        await _userManager.UpdateAsync(user);

        // Taking something away has to reach a session already signed in, the way removing a role does.
        // A session carries what its clearance allowed when it was signed in, so without this somebody
        // would keep a scope that had just been taken from them until they happened to sign in again.
        if (narrowed)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return true;
    }

    /// <inheritdoc />
    public async ValueTask<bool> RevokeAsync(string managedSiteId, string userId)
    {
        var user = await FindAsync(userId);

        if (user is null || !user.TryGet<ManagedSiteClearanceSettings>(out var settings))
        {
            return false;
        }

        if (!RemoveGrant(settings, managedSiteId))
        {
            return false;
        }

        user.Put(settings);
        await _userManager.UpdateAsync(user);

        // The same as removing a role: a clearance taken away is worth nothing if the session that held
        // it carries on regardless.
        await _userManager.UpdateSecurityStampAsync(user);

        return true;
    }

    /// <summary>
    /// Decides whether a change takes something a clearance previously allowed.
    /// </summary>
    /// <remarks>
    /// An empty set is the wildcard, so replacing it with anything named takes scopes away, and
    /// replacing anything named with it only ever adds.
    /// </remarks>
    /// <param name="before">What the clearance allowed.</param>
    /// <param name="after">What it is being asked to allow.</param>
    /// <returns><see langword="true" /> when something is being taken.</returns>
    internal static bool Narrows(IReadOnlyList<string> before, IEnumerable<string> after)
    {
        var requested = Normalize(after);

        if (requested.Count == 0)
        {
            return false;
        }

        var allowed = before.Count == 0 ? ManagedSitesConstants.Scopes.All : [.. before];

        return allowed.Any(scope => !requested.Contains(scope, StringComparer.Ordinal));
    }

    /// <summary>
    /// Writes a grant into a user's clearance, replacing what it allowed before.
    /// </summary>
    /// <remarks>
    /// One grant per Managed Site. Granting again says what the clearance allows now rather than adding
    /// a second grant beside the first, which would leave two answers to one question.
    /// </remarks>
    /// <param name="settings">The user's clearance.</param>
    /// <param name="managedSiteId">The Managed Site the grant covers.</param>
    /// <param name="scopes">What it allows, or nothing for every scope.</param>
    internal static void ApplyGrant(
        ManagedSiteClearanceSettings settings,
        string managedSiteId,
        IEnumerable<string> scopes)
    {
        var grant = settings.Grants.Find(candidate =>
            string.Equals(candidate.ManagedSiteId, managedSiteId, StringComparison.Ordinal));

        if (grant is null)
        {
            grant = new ManagedSiteClearanceGrant { ManagedSiteId = managedSiteId };
            settings.Grants.Add(grant);
        }

        grant.Scopes = Normalize(scopes);
    }

    /// <summary>
    /// Takes a Managed Site's grant out of a user's clearance.
    /// </summary>
    /// <param name="settings">The user's clearance.</param>
    /// <param name="managedSiteId">The Managed Site to take it for.</param>
    /// <returns><see langword="true" /> when a grant was there to take.</returns>
    internal static bool RemoveGrant(ManagedSiteClearanceSettings settings, string managedSiteId)
        => settings.Grants.RemoveAll(grant =>
            string.Equals(grant.ManagedSiteId, managedSiteId, StringComparison.Ordinal)) > 0;

    /// <summary>
    /// Keeps the scopes that mean something, in a stable order.
    /// </summary>
    /// <remarks>
    /// An empty set is the wildcard: it grants every scope, which is what a grant made without saying
    /// otherwise should do.
    /// </remarks>
    /// <param name="scopes">What the caller asked for.</param>
    /// <returns>The scopes to store.</returns>
    internal static List<string> Normalize(IEnumerable<string> scopes)
        => scopes is null
            ? []
            : [.. scopes
                .Where(scope => !string.IsNullOrWhiteSpace(scope))
                .Select(scope => scope.Trim().ToLowerInvariant())
                .Where(ManagedSitesConstants.Scopes.All.Contains)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(scope => Array.IndexOf(ManagedSitesConstants.Scopes.All, scope))];

    private static IReadOnlyList<ManagedSiteUserClearance> Describe(
        IEnumerable<User> users,
        string managedSiteId,
        string search)
    {
        var normalized = search?.Trim();

        return
        [
            .. users
                .Where(user => string.IsNullOrEmpty(normalized)
                    || (user.UserName?.Contains(normalized, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (user.Email?.Contains(normalized, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(user => new ManagedSiteUserClearance(
                    user.UserId,
                    user.UserName,
                    user.Email,
                    ScopesOf(user, managedSiteId)))
                .OrderBy(entry => entry.UserName, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static List<string> ScopesOf(User user, string managedSiteId)
    {
        if (!user.TryGet<ManagedSiteClearanceSettings>(out var settings))
        {
            return [];
        }

        return settings.Grants
            .Find(grant => string.Equals(grant.ManagedSiteId, managedSiteId, StringComparison.Ordinal))
            ?.Scopes ?? [];
    }

    private ValueTask<User> FindAsync(string userId)
        => string.IsNullOrEmpty(userId)
            ? ValueTask.FromResult<User>(null)
            : new ValueTask<User>(_session.Query<User, UserIndex>(index => index.UserId == userId).FirstOrDefaultAsync());
}
