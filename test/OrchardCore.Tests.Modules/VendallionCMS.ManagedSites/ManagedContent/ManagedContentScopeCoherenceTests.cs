using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers the rule that a Managed Site allowed to override an item also renders it.
/// </summary>
/// <remarks>
/// The editor disables the display control the edit scope already decides, and a disabled control posts
/// nothing. The rule therefore has to be restored when the part is written, or saving the form would
/// quietly revoke the visibility the editor was showing as checked.
/// </remarks>
public class ManagedContentScopeCoherenceTests
{
    [Fact]
    public void OverrideByEverySite_WidensDisplayToEverySite()
    {
        var part = Part(ManagedContentScope.All(), ManagedContentScope.None());

        part.EnsureDisplayCoversEdit();

        Assert.Equal(ManagedContentScopeMode.All, part.DisplayScope.Mode);
    }

    [Fact]
    public void OverrideByEverySite_ReplacesANarrowerSelection()
    {
        var part = Part(ManagedContentScope.All(), ManagedContentScope.Selected("site-a"));

        part.EnsureDisplayCoversEdit();

        Assert.Equal(ManagedContentScopeMode.All, part.DisplayScope.Mode);
        Assert.True(new ManagedContentScopeService().CanDisplay(part, "site-b"));
    }

    [Fact]
    public void OverrideByNamedSites_AddsThemToAnEmptyDisplayScope()
    {
        var part = Part(ManagedContentScope.Selected("site-a", "site-b"), ManagedContentScope.None());

        part.EnsureDisplayCoversEdit();

        Assert.Equal(ManagedContentScopeMode.Selected, part.DisplayScope.Mode);
        Assert.Equal(["site-a", "site-b"], part.DisplayScope.ManagedSiteIds);
    }

    [Fact]
    public void OverrideByNamedSites_KeepsTheSitesDisplayAlreadyNamed()
    {
        // Visibility granted on its own is a separate decision, so widening must never take it away.
        var part = Part(ManagedContentScope.Selected("site-a"), ManagedContentScope.Selected("site-b"));

        part.EnsureDisplayCoversEdit();

        Assert.Equal(["site-b", "site-a"], part.DisplayScope.ManagedSiteIds);
    }

    [Fact]
    public void OverrideByNamedSites_DoesNotRepeatASiteDisplayAlreadyNames()
    {
        var part = Part(ManagedContentScope.Selected("site-a"), ManagedContentScope.Selected("site-a"));

        part.EnsureDisplayCoversEdit();

        Assert.Equal(["site-a"], part.DisplayScope.ManagedSiteIds);
    }

    [Fact]
    public void OverrideByNamedSites_LeavesAWiderDisplayScopeAlone()
    {
        var part = Part(ManagedContentScope.Selected("site-a"), ManagedContentScope.All());

        part.EnsureDisplayCoversEdit();

        Assert.Equal(ManagedContentScopeMode.All, part.DisplayScope.Mode);
        Assert.Empty(part.DisplayScope.ManagedSiteIds);
    }

    [Fact]
    public void OverrideByNobody_LeavesDisplayUntouched()
    {
        // Nothing is being granted, so an item nobody sees stays an item nobody sees.
        var hidden = Part(ManagedContentScope.None(), ManagedContentScope.None());
        var narrow = Part(ManagedContentScope.None(), ManagedContentScope.Selected("site-a"));

        hidden.EnsureDisplayCoversEdit();
        narrow.EnsureDisplayCoversEdit();

        Assert.Equal(ManagedContentScopeMode.None, hidden.DisplayScope.Mode);
        Assert.Equal(["site-a"], narrow.DisplayScope.ManagedSiteIds);
    }

    [Fact]
    public void OverrideByNoNamedSite_DoesNotChangeTheDisplayMode()
    {
        // Selected with nothing named grants nobody anything, so the display scope has no gap to close.
        var part = Part(ManagedContentScope.Selected(), ManagedContentScope.None());

        part.EnsureDisplayCoversEdit();

        Assert.Equal(ManagedContentScopeMode.None, part.DisplayScope.Mode);
    }

    [Fact]
    public void Widening_NeverTouchesTheEditScope()
    {
        // The edit scope is the authority being granted; only visibility follows it.
        var part = Part(ManagedContentScope.Selected("site-a"), ManagedContentScope.All());

        part.EnsureDisplayCoversEdit();

        Assert.Equal(ManagedContentScopeMode.Selected, part.EditScope.Mode);
        Assert.Equal(["site-a"], part.EditScope.ManagedSiteIds);
    }

    [Fact]
    public void Widening_LeavesTheBlueprintContextDecisionAlone()
    {
        // The blueprint context is not a Managed Site, so no edit scope can reach it.
        var part = Part(ManagedContentScope.All(), ManagedContentScope.None());
        part.DisplayInBlueprintContext = false;

        part.EnsureDisplayCoversEdit();

        Assert.False(part.DisplayInBlueprintContext);
    }

    [Fact]
    public void Widening_IsIdempotent()
    {
        var part = Part(ManagedContentScope.Selected("site-a"), ManagedContentScope.Selected("site-b"));

        part.EnsureDisplayCoversEdit();
        part.EnsureDisplayCoversEdit();

        Assert.Equal(["site-b", "site-a"], part.DisplayScope.ManagedSiteIds);
    }

    [Fact]
    public void Widening_SurvivesMissingScopes()
    {
        // A part deserialized from partial data can carry a null scope, and the rule runs on every save.
        var noEditScope = Part(editScope: null, ManagedContentScope.None());
        var noDisplayScope = Part(ManagedContentScope.Selected("site-a"), displayScope: null);

        noEditScope.EnsureDisplayCoversEdit();
        noDisplayScope.EnsureDisplayCoversEdit();

        Assert.Equal(ManagedContentScopeMode.None, noEditScope.DisplayScope.Mode);
        Assert.Equal(["site-a"], noDisplayScope.DisplayScope.ManagedSiteIds);
    }

    [Fact]
    public void WidenedPart_LetsEveryOverridingSiteSeeTheItem()
    {
        var service = new ManagedContentScopeService();
        var part = Part(ManagedContentScope.Selected("site-a"), ManagedContentScope.None());

        part.EnsureDisplayCoversEdit();

        Assert.True(service.CanEdit(part, "site-a"));
        Assert.True(service.CanDisplay(part, "site-a"));
        Assert.False(service.CanDisplay(part, "site-b"));
    }

    private static ManagedContentPart Part(ManagedContentScope editScope, ManagedContentScope displayScope)
        => new() { EditScope = editScope, DisplayScope = displayScope };
}
