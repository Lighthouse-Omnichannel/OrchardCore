using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Manages Site Blueprint state.
/// </summary>
public interface ISiteBlueprintService
{
    /// <summary>
    /// Gets the active Site Blueprint.
    /// </summary>
    /// <returns>The active Site Blueprint, or <see langword="null" /> when one has not been configured.</returns>
    ValueTask<SiteBlueprint> GetAsync();

    /// <summary>
    /// Saves the active Site Blueprint.
    /// </summary>
    /// <param name="blueprint">The Site Blueprint to save.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask SaveAsync(SiteBlueprint blueprint);
}

/// <summary>
/// Manages Managed Site definitions and URL registrations.
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
    /// Lists Managed Sites for the active Site Blueprint.
    /// </summary>
    /// <returns>The Managed Sites for the active Site Blueprint.</returns>
    ValueTask<IReadOnlyList<ManagedSite>> ListAsync();

    /// <summary>
    /// Saves a Managed Site.
    /// </summary>
    /// <param name="managedSite">The Managed Site to save.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask SaveAsync(ManagedSite managedSite);
}

/// <summary>
/// Manages URL registrations for Site Blueprints and Managed Sites.
/// </summary>
public interface IUrlRegistrationService
{
    /// <summary>
    /// Finds the active registration matching a URL.
    /// </summary>
    /// <param name="url">The URL to resolve.</param>
    /// <returns>The active URL registration, or <see langword="null" /> when no match exists.</returns>
    ValueTask<UrlRegistration> FindByUrlAsync(string url);

    /// <summary>
    /// Saves a URL registration after validation.
    /// </summary>
    /// <param name="registration">The URL registration to save.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask SaveAsync(UrlRegistration registration);
}
