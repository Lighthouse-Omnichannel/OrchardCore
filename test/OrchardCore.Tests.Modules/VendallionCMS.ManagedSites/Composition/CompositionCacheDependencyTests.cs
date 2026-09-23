using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using OrchardCore.Environment.Cache;
using OrchardCore.Environment.Shell.Removing;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Composition;

/// <summary>
/// Covers what a cached composed response depends on, expressed as the tag it is filed under.
/// </summary>
/// <remarks>
/// The tag is the dependency. Two Managed Sites serving the same item must file it separately or one
/// publish would serve the other's content, and the areas must stay apart or a URL change would throw
/// away cached content that had nothing to do with addressing.
/// </remarks>
public class CompositionCacheDependencyTests
{
    [Fact]
    public void TagsSeparateOneManagedSiteFromAnother()
    {
        Assert.NotEqual(
            ManagedSiteCompositionCacheService.Tag("costis", ManagedSiteCompositionArea.Override),
            ManagedSiteCompositionCacheService.Tag("michael", ManagedSiteCompositionArea.Override));
    }

    [Fact]
    public void TagsSeparateOneAreaFromAnother()
    {
        Assert.NotEqual(
            ManagedSiteCompositionCacheService.Tag("costis", ManagedSiteCompositionArea.Override),
            ManagedSiteCompositionCacheService.Tag("costis", ManagedSiteCompositionArea.Content));
    }

    [Fact]
    public void TagIsStableForTheSameDependency()
    {
        // Cached output is filed under the tag and dropped by it, so an unstable tag would silently
        // cache forever.
        Assert.Equal(
            ManagedSiteCompositionCacheService.Tag("costis", ManagedSiteCompositionArea.Override),
            ManagedSiteCompositionCacheService.Tag("costis", ManagedSiteCompositionArea.Override));
    }

    [Fact]
    public void BlueprintTagIsDistinctFromEveryManagedSiteTag()
    {
        var blueprint = ManagedSiteCompositionCacheService.Tag(null, ManagedSiteCompositionArea.Content);

        Assert.NotEqual(
            blueprint,
            ManagedSiteCompositionCacheService.Tag("costis", ManagedSiteCompositionArea.Content));
        Assert.StartsWith(ManagedSiteCompositionCacheService.CachePrefix, blueprint);
    }

    [Fact]
    public async Task AddressChange_DropsAddressingWithoutDroppingContent_Area()
    {
        var context = new DependencyContext();

        await context.Cache.InvalidateAsync(managedSiteId: null, ManagedSiteCompositionArea.Address);

        Assert.Contains(
            ManagedSiteCompositionCacheService.Tag(null, ManagedSiteCompositionArea.Address),
            context.Dropped);
        Assert.DoesNotContain(
            ManagedSiteCompositionCacheService.Tag(null, ManagedSiteCompositionArea.Content),
            context.Dropped);
    }

    [Fact]
    public async Task ScopedInvalidation_LeavesOtherManagedSitesAlone()
    {
        var context = new DependencyContext();

        await context.Cache.InvalidateAsync("costis", ManagedSiteCompositionArea.Override);

        Assert.Single(context.Dropped);
        Assert.Contains(
            ManagedSiteCompositionCacheService.Tag("costis", ManagedSiteCompositionArea.Override),
            context.Dropped);
    }

    [Fact]
    public async Task BlueprintInvalidation_ReachesEveryManagedSiteThroughTheAreaTag()
    {
        // One drop rather than one per Managed Site, which is what lets this service stay independent
        // of the store that tells it when addresses change.
        var context = new DependencyContext();

        await context.Cache.InvalidateAsync(managedSiteId: null, ManagedSiteCompositionArea.Content);

        var areaTag = ManagedSiteCompositionCacheService.Tag(null, ManagedSiteCompositionArea.Content);

        Assert.Equal([areaTag], context.Dropped);
        Assert.Contains(areaTag, ManagedSiteCompositionCacheService.Tags("costis", ManagedSiteCompositionArea.Content));
        Assert.Contains(areaTag, ManagedSiteCompositionCacheService.Tags("michael", ManagedSiteCompositionArea.Content));
    }

    [Fact]
    public void ComposedOutputCarriesItsOwnTagAndTheAreaTag()
    {
        var tags = ManagedSiteCompositionCacheService.Tags("costis", ManagedSiteCompositionArea.Override);

        Assert.Contains(
            ManagedSiteCompositionCacheService.Tag("costis", ManagedSiteCompositionArea.Override),
            tags);
        Assert.Contains(
            ManagedSiteCompositionCacheService.Tag(null, ManagedSiteCompositionArea.Override),
            tags);
    }

    private sealed class DependencyContext
    {
        public DependencyContext()
        {
            var signal = new CollectingSignal();
            var tagCache = new CollectingTagCache();

            Dropped = tagCache.Tags;
            Cache = new ManagedSiteCompositionCacheService(signal, tagCache);
        }

        public IManagedSiteCompositionCacheService Cache { get; }

        public List<string> Dropped { get; }
    }

    private sealed class CollectingSignal : ISignal
    {
        public IChangeToken GetToken(string key) => new CancellationChangeToken(default);

        public Task SignalTokenAsync(string key) => Task.CompletedTask;

        public Task ActivatingAsync() => Task.CompletedTask;

        public Task ActivatedAsync() => Task.CompletedTask;

        public Task TerminatingAsync() => Task.CompletedTask;

        public Task TerminatedAsync() => Task.CompletedTask;

        public Task RemovingAsync(ShellRemovingContext context) => Task.CompletedTask;
    }

    private sealed class CollectingTagCache : ITagCache
    {
        public List<string> Tags { get; } = [];

        public Task TagAsync(string key, params string[] tags) => Task.CompletedTask;

        public Task<IEnumerable<string>> GetTaggedItemsAsync(string tag)
            => Task.FromResult(Enumerable.Empty<string>());

        public Task RemoveTagAsync(string tag)
        {
            Tags.Add(tag);

            return Task.CompletedTask;
        }
    }
}
