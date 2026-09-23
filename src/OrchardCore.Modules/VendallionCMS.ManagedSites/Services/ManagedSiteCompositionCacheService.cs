using OrchardCore.Environment.Cache;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Invalidates cached composition state when something a composed response depends on changes.
/// </summary>
/// <remarks>
/// Composition is per Managed Site, so what has to be dropped is per Managed Site too. Publishing on
/// one Managed Site must not clear what every other one has cached, and a change to Site Blueprint
/// content must clear all of them, because any of them may have been serving it.
///
/// Both platform mechanisms are used because they serve different consumers. A signal invalidates
/// anything holding a change token, which is how the tenant's own caches are told; a tag drops cached
/// output that was tagged with it. Dropping only one would leave the other serving what was just
/// replaced.
///
/// Composed output is filed under two tags, and <see cref="Tags" /> returns both: one naming the
/// Managed Site it was composed for, one naming only the area. Publishing on one Managed Site drops
/// the first, and a Site Blueprint change drops the second, which reaches every Managed Site at once.
/// The alternative, holding only per-site tags, would make a blueprint change require the list of
/// Managed Sites, and this service would then depend on the store that in turn tells it when addresses
/// change.
/// </remarks>
public sealed class ManagedSiteCompositionCacheService : IManagedSiteCompositionCacheService
{
    /// <summary>
    /// Prefix for every cache key and tag this service owns.
    /// </summary>
    public const string CachePrefix = "VendallionCMS.ManagedSites.Composition";

    private readonly ISignal _signal;
    private readonly ITagCache _tagCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteCompositionCacheService" /> class.
    /// </summary>
    /// <param name="signal">The signal service.</param>
    /// <param name="tagCache">The tag cache.</param>
    public ManagedSiteCompositionCacheService(ISignal signal, ITagCache tagCache)
    {
        _signal = signal;
        _tagCache = tagCache;
    }

    /// <summary>
    /// Builds the cache tag naming one composition area of one Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier, or <see langword="null" /> for every Managed Site.</param>
    /// <param name="affectedArea">The affected composition area.</param>
    /// <returns>The tag.</returns>
    public static string Tag(string managedSiteId, ManagedSiteCompositionArea affectedArea)
        => string.IsNullOrEmpty(managedSiteId)
            ? $"{CachePrefix}:{affectedArea}"
            : $"{CachePrefix}:{affectedArea}:{managedSiteId}";

    /// <summary>
    /// Returns the tags composed output should be filed under.
    /// </summary>
    /// <remarks>
    /// Both are needed. Without the area tag a Site Blueprint change could not reach output composed for
    /// a Managed Site; without the Managed Site tag, publishing on one Managed Site would drop what
    /// every other one had cached.
    /// </remarks>
    /// <param name="managedSiteId">The Managed Site the output was composed for, or <see langword="null" />.</param>
    /// <param name="affectedArea">The composition area.</param>
    /// <returns>The tags to file the output under.</returns>
    public static string[] Tags(string managedSiteId, ManagedSiteCompositionArea affectedArea)
        => string.IsNullOrEmpty(managedSiteId)
            ? [Tag(managedSiteId: null, affectedArea)]
            : [Tag(managedSiteId, affectedArea), Tag(managedSiteId: null, affectedArea)];

    /// <inheritdoc />
    public async ValueTask InvalidateAsync(string managedSiteId, ManagedSiteCompositionArea affectedArea)
    {
        // A change with no Managed Site is Site Blueprint content, which every Managed Site composes
        // from. Dropping the area tag reaches all of them, because every composed response carries it
        // alongside its own.
        var tag = Tag(managedSiteId, affectedArea);

        await _signal.SignalTokenAsync(tag);
        await _tagCache.RemoveTagAsync(tag);
    }
}
