using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Persists the active Managed Site selected for a portal session.
/// </summary>
/// <remarks>
/// Portal API requests can arrive on a bearer token without an HTTP session, so the active scope is
/// stored server-side per user rather than in session state.
/// </remarks>
public interface IManagedSiteSessionStore
{
    /// <summary>
    /// Gets the active scope recorded for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The active scope, or <see langword="null" /> when the user has not selected one.</returns>
    ValueTask<ActiveManagedSiteSessionScope> GetAsync(string userId);

    /// <summary>
    /// Records the active scope for a user, replacing any previous selection.
    /// </summary>
    /// <param name="scope">The active scope to record.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask SetAsync(ActiveManagedSiteSessionScope scope);

    /// <summary>
    /// Clears the active scope recorded for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask ClearAsync(string userId);
}
