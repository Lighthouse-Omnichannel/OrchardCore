using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Models;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers what reaches the edit scope index, which is what the portal offers a Managed Site.
/// </summary>
/// <remarks>
/// An override must contribute nothing, and neither must anything stored inside it. An override of a
/// container is a copy of that container, so it carries copies of its children, and each copy still
/// holds the scopes the blueprint gave it. Indexed, those copies were offered to every other Managed
/// Site named in those scopes: signing in to one Managed Site showed an item belonging to another's
/// override. Worse, a copied child keeps the identifier of the item it was copied from, so the listing,
/// which keeps one row per identifier, could show the copy in the blueprint item's place.
/// </remarks>
public class EditScopeIndexTests
{
    [Fact]
    public void AnOverrideOfAContainer_OffersNothingInsideItToAnyoneElse()
    {
        var rows = ManagedContentEditScopeIndexProvider.BuildRows(
            Container("costis-override", isOverride: true));

        Assert.Null(rows);
    }

    [Fact]
    public void TheBlueprintContainer_StillOffersWhatIsInsideIt()
    {
        // Guards the test above: it would pass just as well if nothing were ever indexed.
        var rows = ManagedContentEditScopeIndexProvider.BuildRows(Container("page", isOverride: false));

        var row = Assert.Single(rows);
        Assert.Equal("section", row.ContentItemId);
        Assert.Equal("page", row.ContainerContentItemId);
        Assert.True(row.AllManagedSites);
    }

    [Fact]
    public void AnOverrideThatHoldsNothing_IsStillNotOffered()
    {
        var overrideItem = ManagedContentTestContent.Item("override-item");
        overrideItem.Weld(new ManagedContentOverridePart
        {
            ManagedSiteId = "costis",
            SourceContentItemId = "section",
        });
        overrideItem.Alter<ManagedContentPart>(part => part.EditScope = ManagedContentScope.All());

        Assert.Null(ManagedContentEditScopeIndexProvider.BuildRows(overrideItem));
    }

    [Fact]
    public void ADraftThatIsNeitherLatestNorPublished_IsNotOffered()
    {
        var page = Container("page", isOverride: false);
        page.Latest = false;
        page.Published = false;

        Assert.Null(ManagedContentEditScopeIndexProvider.BuildRows(page));
    }

    /// <summary>
    /// Builds a container holding one section the blueprint opened to every Managed Site.
    /// </summary>
    private static ContentItem Container(string contentItemId, bool isOverride)
    {
        var container = ManagedContentTestContent.Item(contentItemId, "LandingPage");
        container.Latest = true;
        container.Published = true;

        var section = ManagedContentTestContent.Source("section", ManagedContentScope.All());

        ((JsonObject)container.Content)["Services"] = new JsonObject
        {
            ["ContentItems"] = new JsonArray(JObject.FromObject(section, JOptions.Default)),
        };

        if (isOverride)
        {
            container.Weld(new ManagedContentOverridePart
            {
                ManagedSiteId = "costis",
                SourceContentItemId = "blueprint-page",
            });
        }

        return container;
    }
}
