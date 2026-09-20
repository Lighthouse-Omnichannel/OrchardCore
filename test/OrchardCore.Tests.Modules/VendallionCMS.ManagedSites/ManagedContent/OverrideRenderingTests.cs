using System.Threading.Tasks;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers which content a request context receives for one Managed Content item.
/// </summary>
/// <remarks>
/// An override belongs to exactly one Managed Site. Every other context, including the Site Blueprint
/// and every Managed Site without its own version, receives the original content.
/// </remarks>
public class OverrideRenderingTests
{
    [Fact]
    public async Task OwningManagedSite_ReceivesTheOverride()
    {
        var source = ManagedContentTestContent.Source(editScope: ManagedContentScope.Selected("site-a"));
        var overrideContent = ManagedContentTestContent.Override();
        var service = Resolution(new FakeManagedContentOverrideService()
            .WithPublished("site-a", source.ContentItemId, overrideContent));

        var resolution = await service.ResolveAsync(source, "site-a");

        Assert.True(resolution.ShouldRender);
        Assert.True(resolution.IsOverride);
        Assert.Same(overrideContent, resolution.Content);
    }

    [Fact]
    public async Task OtherManagedSite_ReceivesTheOriginal()
    {
        // Two Managed Sites may both override an item; only the one that did receives its own version.
        var source = ManagedContentTestContent.Source(
            editScope: ManagedContentScope.Selected("site-a", "site-b"));

        var service = Resolution(new FakeManagedContentOverrideService()
            .WithPublished("site-a", source.ContentItemId, ManagedContentTestContent.Override()));

        var resolution = await service.ResolveAsync(source, "site-b");

        Assert.True(resolution.ShouldRender);
        Assert.False(resolution.IsOverride);
        Assert.Same(source, resolution.Content);
    }

    [Fact]
    public async Task BlueprintContext_ReceivesTheOriginal()
    {
        var source = ManagedContentTestContent.Source(editScope: ManagedContentScope.All());
        var service = Resolution(new FakeManagedContentOverrideService()
            .WithPublished("site-a", source.ContentItemId, ManagedContentTestContent.Override()));

        var resolution = await service.ResolveAsync(source, managedSiteId: null);

        Assert.True(resolution.ShouldRender);
        Assert.False(resolution.IsOverride);
        Assert.Same(source, resolution.Content);
    }

    [Fact]
    public async Task ManagedSiteWithoutAnOverride_ReceivesTheOriginal()
    {
        var source = ManagedContentTestContent.Source(editScope: ManagedContentScope.All());

        var resolution = await Resolution().ResolveAsync(source, "site-a");

        Assert.True(resolution.ShouldRender);
        Assert.False(resolution.IsOverride);
        Assert.Same(source, resolution.Content);
    }

    [Fact]
    public async Task ItemWithoutManagedContent_IsHandedThroughUntouched()
    {
        // The guarantee the whole capability rests on: a type that never opted in is unaffected.
        var plain = ManagedContentTestContent.Item();
        var overrides = new FakeManagedContentOverrideService();

        var resolution = await Resolution(overrides).ResolveAsync(plain, "site-a");

        Assert.True(resolution.ShouldRender);
        Assert.False(resolution.IsOverride);
        Assert.Same(plain, resolution.Content);
        Assert.Equal(0, overrides.PublishedLookups);
    }

    [Fact]
    public async Task NoContentItem_RendersNothing()
    {
        var resolution = await Resolution().ResolveAsync(contentItem: null, "site-a");

        Assert.False(resolution.ShouldRender);
        Assert.Null(resolution.Content);
    }

    private static ManagedContentResolutionService Resolution(FakeManagedContentOverrideService overrides = null)
        => new(new ManagedContentScopeService(), overrides ?? new FakeManagedContentOverrideService());
}
