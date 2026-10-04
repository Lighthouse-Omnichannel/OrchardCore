using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers finding the content items stored inside a content item.
/// </summary>
/// <remarks>
/// Containment is found structurally rather than by asking each part what it holds. The platform's
/// containment aspect is published by bag and taxonomy parts only, so a widget in a flow part, which is
/// how most pages are built, was invisible to everything that relied on it: never listed in the portal,
/// and never substituted when a Managed Site had overridden it.
///
/// The shapes here are the ones OrchardCore actually stores, taken from a real page rather than
/// imagined: a flow part keeps its children under <c>Widgets</c> and a bag part under
/// <c>ContentItems</c>.
/// </remarks>
public class ManagedContentContainmentTests
{
    [Fact]
    public void WidgetInAFlowPart_IsFound()
    {
        var page = Page(("FlowPart", "Widgets", "Paragraph", "widget"));

        var contained = ManagedContentContainment.ListContained(page);

        Assert.Equal("widget", Assert.Single(contained).ContentItem.ContentItemId);
    }

    [Fact]
    public void ItemInABagPart_IsFound()
    {
        var page = Page(("Services", "ContentItems", "Service", "section"));

        var contained = ManagedContentContainment.ListContained(page);

        Assert.Equal("section", Assert.Single(contained).ContentItem.ContentItemId);
    }

    [Fact]
    public void ItemsInSeveralContainers_AreAllFound()
    {
        // A page mixes a flow of widgets with named bags, and Managed Content can be on any of them.
        var page = Page(
            ("FlowPart", "Widgets", "Paragraph", "widget"),
            ("Services", "ContentItems", "Service", "section"));

        var found = ManagedContentContainment.ListContained(page)
            .Select(item => item.ContentItem.ContentItemId)
            .Order()
            .ToArray();

        Assert.Equal(["section", "widget"], found);
    }

    [Fact]
    public void ItemInsideAContainedItem_IsFound()
    {
        var page = Page(("FlowPart", "Widgets", "Container", "outer"));
        var outer = (JsonObject)((JsonArray)((JsonObject)page.Content)["FlowPart"]["Widgets"])[0];

        outer["FlowPart"] = new JsonObject
        {
            ["Widgets"] = new JsonArray(JObject.FromObject(
                ManagedContentTestContent.Item("inner", "Paragraph"),
                JOptions.Default)),
        };

        var found = ManagedContentContainment.ListContained(page)
            .Select(item => item.ContentItem.ContentItemId)
            .Order()
            .ToArray();

        Assert.Equal(["inner", "outer"], found);
    }

    [Fact]
    public void TheContainerItself_IsNotListedAsContained()
    {
        var page = Page(("FlowPart", "Widgets", "Paragraph", "widget"));

        Assert.DoesNotContain(
            ManagedContentContainment.ListContained(page),
            item => item.ContentItem.ContentItemId == "page");
    }

    [Fact]
    public void PageWithNoContainedItems_YieldsNone()
        => Assert.Empty(ManagedContentContainment.ListContained(ManagedContentTestContent.Item("page")));

    [Fact]
    public void ContentPickerIdentifiers_AreNotMistakenForContainedItems()
    {
        // A picker stores identifiers as strings. Only an object carrying both an identifier and a type
        // is a serialized content item.
        var page = ManagedContentTestContent.Item("page", "LandingPage");

        ((JsonObject)page.Content)["PickerPart"] = new JsonObject
        {
            ["ContentItemIds"] = new JsonArray("some-id", "another-id"),
        };

        Assert.Empty(ManagedContentContainment.ListContained(page));
    }

    [Fact]
    public void FindLocatesAContainedItemById()
    {
        var page = Page(("FlowPart", "Widgets", "Paragraph", "widget"));

        Assert.Equal("widget", ManagedContentContainment.Find(page, "widget").ContentItemId);
    }

    [Fact]
    public void FindLocatesTheContainerItself()
        => Assert.Equal(
            "page",
            ManagedContentContainment.Find(Page(("FlowPart", "Widgets", "Paragraph", "widget")), "page").ContentItemId);

    [Fact]
    public void FindReturnsNothingForAnItemTheContainerDoesNotHold()
        => Assert.Null(
            ManagedContentContainment.Find(Page(("FlowPart", "Widgets", "Paragraph", "widget")), "elsewhere"));

    [Fact]
    public void EachContainedItemCarriesThePathItSitsAt()
    {
        // The path is what tells an override where it was found, and it has to name the real location.
        var page = Page(("FlowPart", "Widgets", "Paragraph", "widget"));

        var contained = Assert.Single(ManagedContentContainment.ListContained(page));

        Assert.Contains("FlowPart", contained.JsonPath, System.StringComparison.Ordinal);
    }

    private static ContentItem Page(params (string Part, string Collection, string Type, string Id)[] children)
    {
        var page = ManagedContentTestContent.Item("page", "LandingPage");

        foreach (var child in children)
        {
            ((JsonObject)page.Content)[child.Part] = new JsonObject
            {
                [child.Collection] = new JsonArray(JObject.FromObject(
                    ManagedContentTestContent.Item(child.Id, child.Type),
                    JOptions.Default)),
            };
        }

        return page;
    }
}
