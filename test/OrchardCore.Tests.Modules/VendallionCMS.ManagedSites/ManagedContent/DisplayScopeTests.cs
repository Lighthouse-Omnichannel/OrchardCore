using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers which request contexts render a Managed Content item.
/// </summary>
/// <remarks>
/// The Site Blueprint context is the request that matched no Managed Site. It is answered by its own
/// flag rather than by the display scope, because the scope names Managed Sites and the blueprint is
/// not one of them.
/// </remarks>
public class DisplayScopeTests
{
    [Fact]
    public void ScopeAll_RendersForEveryManagedSite()
    {
        Assert.True(Service.CanDisplay(Part(ManagedContentScope.All()), "site-a"));
        Assert.True(Service.CanDisplay(Part(ManagedContentScope.All()), "site-b"));
    }

    [Fact]
    public void ScopeNone_RendersForNoManagedSite()
        => Assert.False(Service.CanDisplay(Part(ManagedContentScope.None()), "site-a"));

    [Fact]
    public void ScopeSelected_RendersOnlyForTheNamedManagedSites()
    {
        var part = Part(ManagedContentScope.Selected("site-a"));

        Assert.True(Service.CanDisplay(part, "site-a"));
        Assert.False(Service.CanDisplay(part, "site-b"));
    }

    [Fact]
    public void BlueprintContext_FollowsItsOwnFlagNotTheScope()
    {
        var hidden = new ManagedContentPart
        {
            DisplayScope = ManagedContentScope.All(),
            DisplayInBlueprintContext = false,
        };

        var shown = new ManagedContentPart
        {
            DisplayScope = ManagedContentScope.None(),
            DisplayInBlueprintContext = true,
        };

        Assert.False(Service.CanDisplay(hidden, managedSiteId: null));
        Assert.True(Service.CanDisplay(hidden, "site-a"));

        Assert.True(Service.CanDisplay(shown, managedSiteId: null));
        Assert.False(Service.CanDisplay(shown, "site-a"));
    }

    [Fact]
    public void BlueprintContext_TreatsAnEmptyIdentifierAsNoManagedSite()
    {
        var part = new ManagedContentPart { DisplayInBlueprintContext = false };

        Assert.False(Service.CanDisplay(part, string.Empty));
    }

    [Fact]
    public void ItemWithoutThePart_RendersEverywhere()
    {
        Assert.True(Service.CanDisplay(part: null, "site-a"));
        Assert.True(Service.CanDisplay(part: null, managedSiteId: null));
    }

    [Fact]
    public void DisplayScope_IsIndependentOfEditScope()
    {
        // An item can be visible to everyone while nobody is allowed to override it.
        var part = new ManagedContentPart
        {
            EditScope = ManagedContentScope.None(),
            DisplayScope = ManagedContentScope.All(),
        };

        Assert.True(Service.CanDisplay(part, "site-a"));
        Assert.False(Service.CanEdit(part, "site-a"));
    }

    private static ManagedContentScopeService Service => new();

    private static ManagedContentPart Part(ManagedContentScope displayScope)
        => new() { DisplayScope = displayScope };
}
