using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Environment.Cache;
using OrchardCore.Environment.Shell.Removing;
using VendallionCMS.ManagedSites.Handlers;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.Tests.ManagedContent;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Composition;

/// <summary>
/// Covers what publishing drops from cached composition state.
/// </summary>
/// <remarks>
/// The cost of getting this wrong runs both ways: invalidate too little and a Managed Site keeps
/// serving content that was replaced, invalidate too much and every publish throws away every Managed
/// Site's cache.
/// </remarks>
public class CompositionCacheInvalidationTests
{
    [Fact]
    public async Task PublishingAnOverride_DropsOnlyItsOwnManagedSite()
    {
        var context = new CacheTestContext();

        await context.PublishAsync(Override("costis"));

        Assert.Contains(Tag("costis", ManagedSiteCompositionArea.Override), context.Dropped);
        Assert.DoesNotContain(Tag("michael", ManagedSiteCompositionArea.Override), context.Dropped);
    }

    [Fact]
    public async Task PublishingBlueprintContent_DropsTheAreaForEveryManagedSite()
    {
        // Every Managed Site composes from the Site Blueprint, so any of them may have been serving it.
        // Composed output carries the area tag alongside its own, so dropping the area reaches all.
        var context = new CacheTestContext();

        await context.PublishAsync(ManagedContentTestContent.Item("page"));

        Assert.Contains(Tag(managedSiteId: null, ManagedSiteCompositionArea.Content), context.Dropped);
        Assert.Contains(
            Tag(managedSiteId: null, ManagedSiteCompositionArea.Content),
            ManagedSiteCompositionCacheService.Tags("costis", ManagedSiteCompositionArea.Content));
        Assert.Contains(
            Tag(managedSiteId: null, ManagedSiteCompositionArea.Content),
            ManagedSiteCompositionCacheService.Tags("michael", ManagedSiteCompositionArea.Content));
    }

    [Fact]
    public async Task PublishingAScopedItem_DropsTheScopeAreaAsWell()
    {
        // The scopes decide who sees the item, so the previous answer is no longer trustworthy.
        var context = new CacheTestContext();

        await context.PublishAsync(ManagedContentTestContent.Source("section", ManagedContentScope.All()));

        Assert.Contains(
            Tag(managedSiteId: null, ManagedSiteCompositionArea.ManagedContentScope),
            context.Dropped);
        Assert.Contains(Tag(managedSiteId: null, ManagedSiteCompositionArea.Content), context.Dropped);
    }

    [Fact]
    public async Task UnpublishingAnOverride_DropsItTheSameWay()
    {
        var context = new CacheTestContext();

        await context.UnpublishAsync(Override("costis"));

        Assert.Contains(Tag("costis", ManagedSiteCompositionArea.Override), context.Dropped);
    }

    [Fact]
    public async Task RemovingAnOverride_DropsItTheSameWay()
    {
        var context = new CacheTestContext();

        await context.RemoveAsync(Override("costis"));

        Assert.Contains(Tag("costis", ManagedSiteCompositionArea.Override), context.Dropped);
    }

    [Fact]
    public async Task Invalidation_SignalsAndDropsTheTag()
    {
        // Both, because they serve different consumers: a signal reaches anything holding a change
        // token, a tag drops cached output. Doing one would leave the other stale.
        var context = new CacheTestContext();

        await context.PublishAsync(Override("costis"));

        Assert.Contains(Tag("costis", ManagedSiteCompositionArea.Override), context.Signalled);
        Assert.Contains(Tag("costis", ManagedSiteCompositionArea.Override), context.Dropped);
    }

    private static string Tag(string managedSiteId, ManagedSiteCompositionArea area)
        => ManagedSiteCompositionCacheService.Tag(managedSiteId, area);

    private static ContentItem Override(string managedSiteId)
        => ManagedContentTestContent.Override("override-item", managedSiteId);

    private sealed class CacheTestContext
    {
        private readonly ManagedSiteCompositionInvalidationHandler _handler;

        public CacheTestContext()
        {
            var signal = new RecordingSignal();
            var tagCache = new RecordingTagCache();

            Signalled = signal.Keys;
            Dropped = tagCache.Tags;

            _handler = new ManagedSiteCompositionInvalidationHandler(
                new ManagedSiteCompositionCacheService(signal, tagCache));
        }

        public List<string> Signalled { get; }

        public List<string> Dropped { get; }

        public Task PublishAsync(ContentItem contentItem)
            => _handler.PublishedAsync(new PublishContentContext(contentItem, previousContentItem: null));

        public Task UnpublishAsync(ContentItem contentItem)
            => _handler.UnpublishedAsync(new PublishContentContext(contentItem, previousContentItem: null));

        public Task RemoveAsync(ContentItem contentItem)
            => _handler.RemovedAsync(new RemoveContentContext(contentItem, noActiveVersionLeft: true));
    }

    private sealed class RecordingSignal : ISignal
    {
        public List<string> Keys { get; } = [];

        public IChangeToken GetToken(string key) => new CancellationChangeToken(default);

        public Task SignalTokenAsync(string key)
        {
            Keys.Add(key);

            return Task.CompletedTask;
        }

        public Task ActivatingAsync() => Task.CompletedTask;

        public Task ActivatedAsync() => Task.CompletedTask;

        public Task TerminatingAsync() => Task.CompletedTask;

        public Task TerminatedAsync() => Task.CompletedTask;

        public Task RemovingAsync(ShellRemovingContext context) => Task.CompletedTask;
    }

    private sealed class RecordingTagCache : ITagCache
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
