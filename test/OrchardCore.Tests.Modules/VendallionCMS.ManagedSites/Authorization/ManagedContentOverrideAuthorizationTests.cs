using System.Threading.Tasks;
using Moq;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.Tests.ManagedContent;
using Xunit;
using ISession = YesSql.ISession;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers what a Managed Site must satisfy before it may override an item.
/// </summary>
/// <remarks>
/// Every one of these checks runs before anything is read from or written to storage. The document
/// session handed to the service is strict and unconfigured, so a rejected request that reached it
/// would fail the test: a Managed Site outside the edit scope must not learn anything either.
///
/// Only refusals are covered here. A save that gets past these checks goes on to query stored
/// overrides, which needs a real session rather than a double.
/// </remarks>
public class ManagedContentOverrideAuthorizationTests
{
    [Fact]
    public async Task UnknownManagedSite_IsRejected()
    {
        var context = ContextWithoutManagedSite();

        Assert.Equal(ManagedContentOverrideError.ManagedSiteUnavailable, await context.SaveAsync());
    }

    [Theory]
    [InlineData(ManagedSiteStatus.Disabled)]
    [InlineData(ManagedSiteStatus.Archived)]
    public async Task WithdrawnManagedSite_IsRejected(ManagedSiteStatus status)
    {
        var context = Context(ManagedSitesTestData.ManagedSite("site-a", status));

        Assert.Equal(ManagedContentOverrideError.ManagedSiteUnavailable, await context.SaveAsync());
    }

    [Fact]
    public async Task UnpublishedSource_IsRejected()
    {
        // There is nothing to stand in for until the Site Blueprint has published the item.
        var context = Context();

        Assert.Equal(ManagedContentOverrideError.SourceNotFound, await context.SaveAsync());
    }

    [Fact]
    public async Task SourceWithoutManagedContent_IsRejected()
    {
        var context = Context();
        context.WithPlainSource();

        Assert.Equal(ManagedContentOverrideError.SourceNotManagedContent, await context.SaveAsync());
    }

    [Fact]
    public async Task ManagedSiteOutsideTheEditScope_IsRejected()
    {
        var context = Context();
        context.WithSource(ManagedContentScope.Selected("site-b"));

        Assert.Equal(ManagedContentOverrideError.EditScopeExcluded, await context.SaveAsync());
    }

    [Fact]
    public async Task EmptyEditScope_IsRejected()
    {
        var context = Context();
        context.WithSource(ManagedContentScope.None());

        Assert.Equal(ManagedContentOverrideError.EditScopeExcluded, await context.SaveAsync());
    }

    [Fact]
    public async Task MissingOverrideContentItem_IsRejected()
    {
        var context = Context();
        context.WithSource(ManagedContentScope.Selected("site-a"));

        Assert.Equal(ManagedContentOverrideError.OverrideNotFound, await context.SaveAsync());
    }

    [Fact]
    public async Task OverrideOfAnotherContentType_IsRejected()
    {
        // An override stands in for its source, so the platform has to be able to validate and render
        // it as the same type.
        var context = Context();
        context.WithSource(ManagedContentScope.Selected("site-a"));
        context.WithOverrideContentItem(contentType: "Article");

        Assert.Equal(ManagedContentOverrideError.ContentTypeMismatch, await context.SaveAsync());
    }

    [Fact]
    public async Task ScopeIsCheckedBeforeTheOverrideContentItemIsLookedUp()
    {
        // Order matters for what a refusal leaks. A Managed Site with no right to override must be
        // turned away on the scope, not told whether the content item it named exists.
        var context = Context();
        context.WithSource(ManagedContentScope.Selected("site-b"));
        context.WithOverrideContentItem(contentType: "Article");

        Assert.Equal(ManagedContentOverrideError.EditScopeExcluded, await context.SaveAsync());
    }

    /// <summary>
    /// Builds a context whose Managed Site is enabled, so the refusal under test is the one reached.
    /// </summary>
    /// <param name="managedSite">The Managed Site, defaulting to an enabled one.</param>
    /// <returns>The context.</returns>
    private static OverrideAuthorizationContext Context(ManagedSite managedSite = null)
        => new(managedSite ?? ManagedSitesTestData.ManagedSite("site-a"));

    /// <summary>
    /// Builds a context where the requested Managed Site was never defined.
    /// </summary>
    /// <returns>The context.</returns>
    private static OverrideAuthorizationContext ContextWithoutManagedSite() => new(managedSite: null);

    /// <summary>
    /// Assembles the override service with only the collaborators a refused save may reach.
    /// </summary>
    private sealed class OverrideAuthorizationContext
    {
        private readonly Mock<IContentManager> _contentManager = new(MockBehavior.Strict);
        private readonly ManagedContentOverrideService _service;

        public OverrideAuthorizationContext(ManagedSite managedSite)
        {
            _contentManager
                .Setup(manager => manager.GetAsync("source-item", VersionOptions.Published))
                .ReturnsAsync((ContentItem)null);

            _contentManager
                .Setup(manager => manager.GetAsync("override-item", VersionOptions.Latest))
                .ReturnsAsync((ContentItem)null);

            _service = new ManagedContentOverrideService(
                new Mock<ISession>(MockBehavior.Strict).Object,
                _contentManager.Object,
                managedSite is null ? new FakeManagedSiteService() : new FakeManagedSiteService(managedSite),
                new ManagedContentScopeService(),
                new Mock<IManagedContentSuppressionService>(MockBehavior.Strict).Object);
        }

        public void WithSource(ManagedContentScope editScope)
            => _contentManager
                .Setup(manager => manager.GetAsync("source-item", VersionOptions.Published))
                .ReturnsAsync(ManagedContentTestContent.Source("source-item", editScope));

        public void WithPlainSource()
            => _contentManager
                .Setup(manager => manager.GetAsync("source-item", VersionOptions.Published))
                .ReturnsAsync(ManagedContentTestContent.Item("source-item"));

        public void WithOverrideContentItem(string contentType = ManagedContentTestContent.ContentType)
            => _contentManager
                .Setup(manager => manager.GetAsync("override-item", VersionOptions.Latest))
                .ReturnsAsync(ManagedContentTestContent.Item("override-item", contentType));

        public async Task<ManagedContentOverrideError> SaveAsync()
        {
            var result = await _service.SaveAsync("site-a", "source-item", "override-item", publish: false);

            return result.Error;
        }
    }
}
