using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Resolves a Managed Site from an incoming public request.
/// </summary>
public interface IManagedSiteUrlResolver
{
    /// <summary>
    /// Resolves the Managed Site request context for a host and path.
    /// </summary>
    /// <param name="host">The request host.</param>
    /// <param name="path">The request path.</param>
    /// <returns>The resolved request context.</returns>
    ValueTask<ManagedSiteRequestContext> ResolveAsync(string host, string path);
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
/// Resolves one content item to the content that should render for a request context.
/// </summary>
/// <remarks>
/// Resolution is per item and needs no ordering between items, which is what removed the precedence
/// rules the earlier multi-mechanism design required. An item either does not render, renders its
/// Managed Site override, or renders its own original content.
/// </remarks>
public interface IManagedContentResolutionService
{
    /// <summary>
    /// Resolves the content to render for a content item.
    /// </summary>
    /// <param name="contentItem">The source content item.</param>
    /// <param name="managedSiteId">The resolved Managed Site, or <see langword="null" /> for the Site Blueprint context.</param>
    /// <returns>The resolution outcome.</returns>
    ValueTask<ManagedContentResolution> ResolveAsync(ContentItem contentItem, string managedSiteId);
}

/// <summary>
/// The outcome of resolving one content item for a request context.
/// </summary>
public sealed class ManagedContentResolution
{
    /// <summary>
    /// Gets a value indicating whether the item renders at all.
    /// </summary>
    public required bool ShouldRender { get; init; }

    /// <summary>
    /// Gets the content to render, which is the Managed Site override when one applies and the original
    /// content otherwise.
    /// </summary>
    public ContentItem Content { get; init; }

    /// <summary>
    /// Gets a value indicating whether the resolved content is a Managed Site override.
    /// </summary>
    public bool IsOverride { get; init; }

    /// <summary>
    /// Creates a resolution that renders nothing.
    /// </summary>
    /// <returns>The resolution.</returns>
    public static ManagedContentResolution Hidden() => new() { ShouldRender = false };

    /// <summary>
    /// Creates a resolution that renders the original content.
    /// </summary>
    /// <param name="contentItem">The original content item.</param>
    /// <returns>The resolution.</returns>
    public static ManagedContentResolution Original(ContentItem contentItem)
        => new() { ShouldRender = true, Content = contentItem };

    /// <summary>
    /// Creates a resolution that renders a Managed Site override.
    /// </summary>
    /// <param name="contentItem">The override content item.</param>
    /// <returns>The resolution.</returns>
    public static ManagedContentResolution Override(ContentItem contentItem)
        => new() { ShouldRender = true, Content = contentItem, IsOverride = true };
}

/// <summary>
/// Tracks composition cache dependencies for Managed Site rendering.
/// </summary>
public interface IManagedSiteCompositionCacheService
{
    /// <summary>
    /// Invalidates cached composition state for a Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier, or <see langword="null" /> for every Managed Site.</param>
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
    /// Gets or sets the resolved Managed Site identifier, or <see langword="null" /> when the request
    /// matched no Managed Site and is served by the Site Blueprint context.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the URL prefix the resolved Managed Site answers under, empty when it answers at
    /// the root.
    /// </summary>
    /// <remarks>
    /// Carried so the request pipeline can move it out of the path, the way a tenant's own prefix is
    /// moved, and so the content routes underneath it resolve unchanged.
    /// </remarks>
    public string UrlPrefix { get; set; }

    /// <summary>
    /// Gets or sets the request host used for resolution.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// Gets or sets the request path used for resolution.
    /// </summary>
    public string Path { get; set; }
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
    /// A Managed Site address was changed.
    /// </summary>
    Address,

    /// <summary>
    /// A Managed Content edit or display scope was changed.
    /// </summary>
    ManagedContentScope,

    /// <summary>
    /// A Managed Content override was changed.
    /// </summary>
    Override,
}
