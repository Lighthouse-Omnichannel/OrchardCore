using System.Threading.Tasks;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers the order the two scope questions are asked in.
/// </summary>
/// <remarks>
/// Display scope comes first and is decisive. An item a request context must not see renders nothing
/// there, whatever content a Managed Site has prepared for it, and withdrawing the right to override
/// withdraws the override that right produced.
/// </remarks>
public class DisplayScopePrecedenceTests
{
    [Fact]
    public async Task ExcludedManagedSite_RendersNothingEvenWithAPublishedOverride()
    {
        var source = ManagedContentTestContent.Source(
            editScope: ManagedContentScope.Selected("site-a"),
            displayScope: ManagedContentScope.None());

        var overrides = new FakeManagedContentOverrideService()
            .WithPublished("site-a", source.ContentItemId, ManagedContentTestContent.Override());

        var resolution = await Resolution(overrides).ResolveAsync(source, "site-a");

        Assert.False(resolution.ShouldRender);
        Assert.Null(resolution.Content);
    }

    [Fact]
    public async Task ExcludedManagedSite_IsAnsweredWithoutLookingForAnOverride()
    {
        // Display scope decides on its own, so the override store is never consulted for an item the
        // context must not see. Asking would be work that cannot change the answer.
        var source = ManagedContentTestContent.Source(
            editScope: ManagedContentScope.Selected("site-a"),
            displayScope: ManagedContentScope.None());

        var overrides = new FakeManagedContentOverrideService();

        await Resolution(overrides).ResolveAsync(source, "site-a");

        Assert.Equal(0, overrides.PublishedLookups);
    }

    [Fact]
    public async Task ExcludedBlueprintContext_RendersNothing()
    {
        var source = ManagedContentTestContent.Source(displayInBlueprintContext: false);

        var resolution = await Resolution().ResolveAsync(source, managedSiteId: null);

        Assert.False(resolution.ShouldRender);
    }

    [Fact]
    public async Task IncludedBlueprintContext_RendersTheOriginal()
    {
        var source = ManagedContentTestContent.Source(
            displayScope: ManagedContentScope.None(),
            displayInBlueprintContext: true);

        var resolution = await Resolution().ResolveAsync(source, managedSiteId: null);

        Assert.True(resolution.ShouldRender);
        Assert.Same(source, resolution.Content);
    }

    [Fact]
    public async Task ManagedSiteRemovedFromTheEditScope_FallsBackToTheOriginal()
    {
        // The override content still exists and stays recoverable, but it no longer renders: the right
        // that produced it is gone.
        var source = ManagedContentTestContent.Source(
            editScope: ManagedContentScope.None(),
            displayScope: ManagedContentScope.All());

        var overrides = new FakeManagedContentOverrideService()
            .WithPublished("site-a", source.ContentItemId, ManagedContentTestContent.Override());

        var resolution = await Resolution(overrides).ResolveAsync(source, "site-a");

        Assert.True(resolution.ShouldRender);
        Assert.False(resolution.IsOverride);
        Assert.Same(source, resolution.Content);
    }

    [Fact]
    public async Task DisplayScopeNamingOtherSites_RendersNothingForThisOne()
    {
        var source = ManagedContentTestContent.Source(
            editScope: ManagedContentScope.Selected("site-b"),
            displayScope: ManagedContentScope.Selected("site-b"));

        var resolution = await Resolution().ResolveAsync(source, "site-a");

        Assert.False(resolution.ShouldRender);
    }

    private static ManagedContentResolutionService Resolution(FakeManagedContentOverrideService overrides = null)
        => new(new ManagedContentScopeService(), overrides ?? new FakeManagedContentOverrideService());
}
