using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Resolves a Managed Site from an incoming public URL.
/// </summary>
public interface IManagedSiteUrlResolver
{
    /// <summary>
    /// Resolves the Managed Site request context for the specified URL.
    /// </summary>
    /// <param name="url">The incoming URL.</param>
    /// <returns>The resolved request context.</returns>
    ValueTask<ManagedSiteRequestContext> ResolveAsync(string url);
}

/// <summary>
/// Provides access to the current Managed Site composition context.
/// </summary>
public interface IManagedSiteCompositionContextAccessor
{
    /// <summary>
    /// Gets or sets the current request context.
    /// </summary>
    ManagedSiteRequestContext Current { get; set; }
}

/// <summary>
/// Tracks composition cache dependencies for Managed Site rendering.
/// </summary>
public interface IManagedSiteCompositionCacheService
{
    /// <summary>
    /// Invalidates cached composition state for a Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <param name="affectedArea">The affected composition area.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask InvalidateAsync(string managedSiteId, ManagedSiteCompositionArea affectedArea);
}

/// <summary>
/// Represents the Managed Site context resolved for a public request.
/// </summary>
public sealed class ManagedSiteRequestContext
{
    /// <summary>
    /// Gets or sets the resolved Site Blueprint identifier.
    /// </summary>
    public string BlueprintId { get; set; }

    /// <summary>
    /// Gets or sets the resolved Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the URL used for resolution.
    /// </summary>
    public string Url { get; set; }
}

/// <summary>
/// Defines areas that can affect composed Managed Site output.
/// </summary>
public enum ManagedSiteCompositionArea
{
    /// <summary>
    /// Content was changed.
    /// </summary>
    Content,

    /// <summary>
    /// URL registration was changed.
    /// </summary>
    UrlRegistration,

    /// <summary>
    /// Navigation contribution was changed.
    /// </summary>
    Navigation,

    /// <summary>
    /// Page override was changed.
    /// </summary>
    PageOverride,

    /// <summary>
    /// Layer contribution was changed.
    /// </summary>
    LayerContribution,

    /// <summary>
    /// Placeholder assignment was changed.
    /// </summary>
    PlaceholderAssignment,
}
