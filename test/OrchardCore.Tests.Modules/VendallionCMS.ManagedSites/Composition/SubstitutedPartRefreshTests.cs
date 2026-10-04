using System.Text.Json;
using System.Text.Json.Nodes;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Handlers;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Composition;

/// <summary>
/// Covers making a load-time substitution visible to whatever reads the item afterwards.
/// </summary>
/// <remarks>
/// A content item keeps each part it has been asked for, and the platform asks for all of them while
/// loading, to hand them to the part handlers. A handler runs after that, so editing the stored JSON
/// changes nothing for anything that reads a part: the menu went on handing out the Site Blueprint's
/// entries however the JSON beneath them had been rewritten, which is why an override of a menu entry
/// was listed, published, reported as rendering, and never appeared.
///
/// Page sections hid this, because they are drawn through the display manager, where a separate
/// decorator substitutes them. Only consumers reading parts straight off the item were affected, and
/// menus are the main one.
/// </remarks>
public class SubstitutedPartRefreshTests
{
    [Fact]
    public void APartAlreadyRead_StillShowsTheOldContentWhenTheJsonIsRewritten()
    {
        // The problem, stated. Nothing below is worth anything if this ever stops being true.
        var item = ItemWithPartAlreadyRead();

        Rewrite(item, "overridden");

        Assert.Equal("blueprint", item.As<ProbePart>().Value);
    }

    [Fact]
    public void RefreshingThePart_MakesTheRewrittenContentVisible()
    {
        var item = ItemWithPartAlreadyRead();
        Rewrite(item, "overridden");

        ManagedContentCompositionHandler.Refresh(item, [nameof(ProbePart)]);

        Assert.Equal("overridden", item.As<ProbePart>().Value);
    }

    [Fact]
    public void RefreshingLeavesTheContentItself_Alone()
    {
        var item = ItemWithPartAlreadyRead();
        Rewrite(item, "overridden");

        ManagedContentCompositionHandler.Refresh(item, [nameof(ProbePart)]);

        Assert.Equal("overridden", (string)((JsonObject)item.Content)[nameof(ProbePart)]["Value"]);
    }

    [Fact]
    public void RefreshingAPartTheItemDoesNotHave_DoesNothing()
    {
        var item = ItemWithPartAlreadyRead();

        ManagedContentCompositionHandler.Refresh(item, ["NotAPartHere"]);

        Assert.Equal("blueprint", item.As<ProbePart>().Value);
    }

    [Theory]
    [InlineData("$.MenuItemsListPart.MenuItems[0]", "MenuItemsListPart")]
    [InlineData("$.Services.ContentItems[2]", "Services")]
    [InlineData("$.FlowPart.Widgets[0].FlowPart.Widgets[1]", "FlowPart")]
    [InlineData("$.OnlyAPart", "OnlyAPart")]
    [InlineData("$", null)]
    [InlineData(null, null)]
    public void ThePartAContainedItemSitsUnder_IsReadFromItsPath(string jsonPath, string expected)
        => Assert.Equal(expected, ManagedContentCompositionHandler.PartNameOf(jsonPath));

    private static ContentItem ItemWithPartAlreadyRead()
    {
        var item = new ContentItem { ContentType = "Menu", ContentItemId = "menu" };

        ((JsonObject)item.Content)[nameof(ProbePart)] = new JsonObject { ["Value"] = "blueprint" };

        // Reading it once is what the platform does while loading, and what makes the copy stick.
        Assert.Equal("blueprint", item.As<ProbePart>().Value);

        return item;
    }

    private static void Rewrite(ContentItem item, string value)
        => ((JsonObject)item.Content)[nameof(ProbePart)]["Value"] = value;

    private sealed class ProbePart : ContentPart
    {
        public string Value { get; set; }
    }
}
