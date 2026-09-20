using System.Threading.Tasks;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers what a Managed Site receives when the item it overrode holds children.
/// </summary>
/// <remarks>
/// A Managed Site adds content by overriding the container the Site Blueprint placed and supplying its
/// own children inside that override. Resolution is what makes that work: the container the request
/// renders is the override, so the children reachable from it are the override's, and the original
/// container's children are never reached at all.
/// </remarks>
public class ContainerOverrideTests
{
    private const string ChildKey = "Children";

    [Fact]
    public async Task OverriddenContainer_RendersTheOverridesOwnChildren()
    {
        var source = Container("source-item", "blueprint-child");
        var overrideContainer = OverrideContainer("managed-child");

        var resolution = await Resolve(source, overrideContainer, "site-a");

        Assert.True(resolution.IsOverride);
        Assert.Equal("managed-child", ChildOf(resolution.Content));
    }

    [Fact]
    public async Task OverriddenContainer_DoesNotReachTheOriginalChildren()
    {
        var source = Container("source-item", "blueprint-child");
        var overrideContainer = OverrideContainer("managed-child");

        var resolution = await Resolve(source, overrideContainer, "site-a");

        Assert.NotSame(source, resolution.Content);
        Assert.NotEqual("blueprint-child", ChildOf(resolution.Content));
    }

    [Fact]
    public async Task ContainerWithoutAnOverride_KeepsItsOwnChildren()
    {
        var source = Container("source-item", "blueprint-child");

        var resolution = await Resolve(source, overrideContainer: null, "site-a");

        Assert.False(resolution.IsOverride);
        Assert.Equal("blueprint-child", ChildOf(resolution.Content));
    }

    [Fact]
    public async Task ContainerExcludedByDisplayScope_RendersNeitherItselfNorItsChildren()
    {
        var source = Container("source-item", "blueprint-child", displayScope: ManagedContentScope.None());
        var overrideContainer = OverrideContainer("managed-child");

        var resolution = await Resolve(source, overrideContainer, "site-a");

        Assert.False(resolution.ShouldRender);
        Assert.Null(resolution.Content);
    }

    private static async Task<ManagedContentResolution> Resolve(
        ContentItem source,
        ContentItem overrideContainer,
        string managedSiteId)
    {
        var overrides = new FakeManagedContentOverrideService();

        if (overrideContainer is not null)
        {
            overrides.WithPublished(managedSiteId, source.ContentItemId, overrideContainer);
        }

        var service = new ManagedContentResolutionService(new ManagedContentScopeService(), overrides);

        return await service.ResolveAsync(source, managedSiteId);
    }

    private static ContentItem Container(
        string contentItemId,
        string childId,
        ManagedContentScope displayScope = null)
    {
        var container = ManagedContentTestContent.Source(
            contentItemId,
            ManagedContentScope.Selected("site-a"),
            displayScope);

        container.Content[ChildKey] = childId;

        return container;
    }

    private static ContentItem OverrideContainer(string childId)
    {
        var container = ManagedContentTestContent.Override();
        container.Content[ChildKey] = childId;

        return container;
    }

    private static string ChildOf(ContentItem contentItem) => (string)contentItem.Content[ChildKey];
}
