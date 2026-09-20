using System.Threading.Tasks;
using Moq;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers why an override that exists stops rendering.
/// </summary>
/// <remarks>
/// Suppression is derived from the current state of the Managed Site and the source item rather than
/// stored, so a scope narrowed a moment ago takes effect on the next read instead of the next save.
/// </remarks>
public class OverrideSuppressionTests
{
    [Fact]
    public void HealthyOverride_IsNotSuppressed()
    {
        var reason = Evaluate(Enabled(), Source(ManagedContentScope.Selected("site-a")), sourceExists: true);

        Assert.Equal(ManagedContentOverrideSuppressionReason.None, reason);
    }

    [Fact]
    public void DeletedSource_ReportsSourceDeleted()
    {
        var reason = Evaluate(Enabled(), publishedSource: null, sourceExists: false);

        Assert.Equal(ManagedContentOverrideSuppressionReason.SourceDeleted, reason);
    }

    [Fact]
    public void UnpublishedSource_ReportsSourceUnpublished()
    {
        // The draft still exists, so this is recoverable by publishing the source again.
        var reason = Evaluate(Enabled(), publishedSource: null, sourceExists: true);

        Assert.Equal(ManagedContentOverrideSuppressionReason.SourceUnpublished, reason);
    }

    [Fact]
    public void SourceWithoutTheCapability_ReportsCapabilityDetached()
    {
        var reason = Evaluate(Enabled(), ManagedContentTestContent.Item("source-item"), sourceExists: true);

        Assert.Equal(ManagedContentOverrideSuppressionReason.CapabilityDetached, reason);
    }

    [Fact]
    public void ManagedSiteOutsideTheEditScope_ReportsEditScopeRemoved()
    {
        var reason = Evaluate(Enabled(), Source(ManagedContentScope.Selected("site-b")), sourceExists: true);

        Assert.Equal(ManagedContentOverrideSuppressionReason.EditScopeRemoved, reason);
    }

    [Theory]
    [InlineData(ManagedSiteStatus.Disabled)]
    [InlineData(ManagedSiteStatus.Archived)]
    public void WithdrawnManagedSite_ReportsManagedSiteDisabled(ManagedSiteStatus status)
    {
        var managedSite = ManagedSitesTestData.ManagedSite("site-a", status);

        var reason = Evaluate(managedSite, Source(ManagedContentScope.Selected("site-a")), sourceExists: true);

        Assert.Equal(ManagedContentOverrideSuppressionReason.ManagedSiteDisabled, reason);
    }

    [Fact]
    public void MissingManagedSite_ReportsManagedSiteDisabled()
    {
        var reason = Evaluate(managedSite: null, Source(ManagedContentScope.Selected("site-a")), sourceExists: true);

        Assert.Equal(ManagedContentOverrideSuppressionReason.ManagedSiteDisabled, reason);
    }

    [Fact]
    public void ManagedSiteStillBeingPrepared_IsNotSuppressed()
    {
        // Draft means not live yet, not withdrawn. The override waits for the Managed Site to open
        // rather than being reported as broken.
        var managedSite = ManagedSitesTestData.ManagedSite("site-a", ManagedSiteStatus.Draft);

        var reason = Evaluate(managedSite, Source(ManagedContentScope.Selected("site-a")), sourceExists: true);

        Assert.Equal(ManagedContentOverrideSuppressionReason.None, reason);
    }

    [Fact]
    public void SeveralCausesAtOnce_ReportTheOneClosestToTheItem()
    {
        // A withdrawn Managed Site and a deleted source both apply; the source is what an administrator
        // looks at first, and nothing about the Managed Site would explain a missing item.
        var managedSite = ManagedSitesTestData.ManagedSite("site-a", ManagedSiteStatus.Archived);

        var reason = Evaluate(managedSite, publishedSource: null, sourceExists: false);

        Assert.Equal(ManagedContentOverrideSuppressionReason.SourceDeleted, reason);
    }

    [Fact]
    public async Task EvaluateAsync_LoadsTheCurrentStateOfTheSource()
    {
        var source = Source(ManagedContentScope.Selected("site-a"));
        var contentManager = new Mock<IContentManager>(MockBehavior.Strict);

        contentManager
            .Setup(manager => manager.GetAsync("source-item", VersionOptions.Published))
            .ReturnsAsync(source);

        var service = new ManagedContentSuppressionService(
            contentManager.Object,
            new FakeManagedSiteService(Enabled()),
            new ManagedContentScopeService());

        Assert.Equal(
            ManagedContentOverrideSuppressionReason.None,
            await service.EvaluateAsync("site-a", "source-item"));
    }

    [Fact]
    public async Task EvaluateAsync_AsksForTheDraftOnlyWhenNothingIsPublished()
    {
        // Telling a deleted item from an unpublished one costs a second load, so it is only paid when
        // the first load came back empty.
        var contentManager = new Mock<IContentManager>(MockBehavior.Strict);

        contentManager
            .Setup(manager => manager.GetAsync("source-item", VersionOptions.Published))
            .ReturnsAsync((ContentItem)null);

        contentManager
            .Setup(manager => manager.GetAsync("source-item", VersionOptions.Latest))
            .ReturnsAsync(Source(ManagedContentScope.Selected("site-a")));

        var service = new ManagedContentSuppressionService(
            contentManager.Object,
            new FakeManagedSiteService(Enabled()),
            new ManagedContentScopeService());

        Assert.Equal(
            ManagedContentOverrideSuppressionReason.SourceUnpublished,
            await service.EvaluateAsync("site-a", "source-item"));

        contentManager.Verify(manager => manager.GetAsync("source-item", VersionOptions.Latest), Times.Once);
    }

    private static ManagedContentOverrideSuppressionReason Evaluate(
        ManagedSite managedSite,
        ContentItem publishedSource,
        bool sourceExists)
    {
        // The content manager and the Managed Site service are strict and unconfigured on purpose: this
        // overload answers from state the caller already holds and must load nothing.
        var service = new ManagedContentSuppressionService(
            new Mock<IContentManager>(MockBehavior.Strict).Object,
            new Mock<IManagedSiteService>(MockBehavior.Strict).Object,
            new ManagedContentScopeService());

        return service.Evaluate("site-a", managedSite, publishedSource, sourceExists);
    }

    private static ManagedSite Enabled() => ManagedSitesTestData.ManagedSite("site-a");

    private static ContentItem Source(ManagedContentScope editScope)
        => ManagedContentTestContent.Source("source-item", editScope);
}
