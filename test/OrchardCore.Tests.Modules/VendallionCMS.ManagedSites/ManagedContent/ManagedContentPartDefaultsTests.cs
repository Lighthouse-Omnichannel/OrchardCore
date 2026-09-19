using System.Text.Json;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers the rule that attaching Managed Content changes nothing on its own.
/// </summary>
/// <remarks>
/// The constitution makes backward compatibility the default contract, so attaching the part to a
/// content type must not alter what any visitor sees nor grant any Managed Site authority it lacked.
/// </remarks>
public class ManagedContentPartDefaultsTests
{
    [Fact]
    public void NewPart_GrantsNoManagedSiteTheRightToOverride()
    {
        var part = new ManagedContentPart();

        Assert.Equal(ManagedContentScopeMode.None, part.EditScope.Mode);
    }

    [Fact]
    public void NewPart_IsVisibleToEveryManagedSite()
    {
        var part = new ManagedContentPart();

        Assert.Equal(ManagedContentScopeMode.All, part.DisplayScope.Mode);
    }

    [Fact]
    public void NewPart_IsVisibleWhenNoManagedSiteMatches()
    {
        var part = new ManagedContentPart();

        Assert.True(part.DisplayInBlueprintContext);
    }

    [Fact]
    public void NewPart_RendersEverywhereAndIsEditableNowhere()
    {
        var service = new ManagedContentScopeService();
        var part = new ManagedContentPart();

        Assert.True(service.CanDisplay(part, "site-a"));
        Assert.True(service.CanDisplay(part, managedSiteId: null));
        Assert.False(service.CanEdit(part, "site-a"));
    }

    [Fact]
    public void PartRoundTrippedThroughJson_KeepsItsDefaults()
    {
        // A content item that carries the part but was never edited deserializes from an empty object,
        // so the defaults have to survive that rather than come only from the constructor.
        var part = JsonSerializer.Deserialize<ManagedContentPart>("{}", JOptions.Default);

        Assert.Equal(ManagedContentScopeMode.None, part.EditScope.Mode);
        Assert.Equal(ManagedContentScopeMode.All, part.DisplayScope.Mode);
        Assert.True(part.DisplayInBlueprintContext);
    }

    [Fact]
    public void PartRoundTrippedThroughJson_KeepsConfiguredScopes()
    {
        var part = new ManagedContentPart
        {
            EditScope = ManagedContentScope.Selected("site-a", "site-b"),
            DisplayScope = ManagedContentScope.Selected("site-a"),
            DisplayInBlueprintContext = false,
        };

        var roundTripped = JsonSerializer.Deserialize<ManagedContentPart>(
            JsonSerializer.Serialize(part, JOptions.Default),
            JOptions.Default);

        Assert.Equal(["site-a", "site-b"], roundTripped.EditScope.ManagedSiteIds);
        Assert.Equal(["site-a"], roundTripped.DisplayScope.ManagedSiteIds);
        Assert.False(roundTripped.DisplayInBlueprintContext);
    }

    [Fact]
    public void ContentItemWithoutThePart_IsNeverRestricted()
    {
        var service = new ManagedContentScopeService();
        var contentItem = new ContentItem { ContentType = "Page" };

        Assert.True(service.CanDisplay(contentItem, "site-a"));
        Assert.True(service.CanDisplay(contentItem, managedSiteId: null));
    }
}
