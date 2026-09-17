using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Manages Managed Site definitions and the addresses they answer on.
/// </summary>
public interface IManagedSiteService
{
    /// <summary>
    /// Gets a Managed Site by identifier.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <returns>The Managed Site, or <see langword="null" /> when no match exists.</returns>
    ValueTask<ManagedSite> GetAsync(string managedSiteId);

    /// <summary>
    /// Lists the Managed Sites defined in the tenant.
    /// </summary>
    /// <returns>The Managed Sites.</returns>
    ValueTask<IReadOnlyList<ManagedSite>> ListAsync();

    /// <summary>
    /// Finds the enabled Managed Site answering a request host and path.
    /// </summary>
    /// <remarks>
    /// A Managed Site naming the request host wins over one answering on every host, and a longer prefix
    /// wins over a shorter one.
    /// </remarks>
    /// <param name="host">The request host.</param>
    /// <param name="path">The request path.</param>
    /// <returns>The matching Managed Site, or <see langword="null" /> when none answers.</returns>
    ValueTask<ManagedSite> FindByAddressAsync(string host, string path);

    /// <summary>
    /// Creates or replaces a Managed Site.
    /// </summary>
    /// <param name="managedSite">The Managed Site to save, normalized in place.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ManagedSiteValidationException">Thrown when the definition or address is invalid or collides.</exception>
    ValueTask SaveAsync(ManagedSite managedSite);

    /// <summary>
    /// Deletes a Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <returns><see langword="true" /> when a Managed Site was removed; otherwise, <see langword="false" />.</returns>
    ValueTask<bool> DeleteAsync(string managedSiteId);
}
