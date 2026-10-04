using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.Handlers;
using VendallionCMS.ManagedSites.Drivers;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers what an editor is shown when the content item they opened is a Managed Site's override.
/// </summary>
/// <remarks>
/// An override is a content item of the same type as the item it replaces, so it inherits that type's
/// parts, Managed Content included, and opens in the same editor. Two things follow: the scopes must
/// not be offered on it, because being able to grant a Managed Site the right to override an override
/// means nothing, and something must say what it stands in for, because otherwise nothing on the page
/// does.
/// </remarks>
public class OverrideEditorTests
{
    [Fact]
    public void ScopeEditor_IsNotShownOnAnOverride()
    {
        var driver = ScopeDriver(isAuthorized: true);

        Assert.Null(driver.Edit(PartOf(Override()), EditorContext()));
    }

    [Fact]
    public void ScopeEditor_IsShownOnAnItemThatIsNotAnOverride()
    {
        // Guards the test above: it would pass just as well if the editor were never shown at all.
        var driver = ScopeDriver(isAuthorized: true);

        Assert.NotNull(driver.Edit(PartOf(Source()), EditorContext()));
    }

    [Fact]
    public async Task ScopeUpdate_OnAnOverride_ChangesNothing()
    {
        // The editor is not rendered, so nothing is posted; an update must not read defaults back over
        // scopes the item never offered.
        var driver = ScopeDriver(isAuthorized: true);
        var overrideItem = Override();
        var part = PartOf(overrideItem);

        part.EditScope = ManagedContentScope.Selected("costis");
        part.DisplayScope = ManagedContentScope.Selected("costis");

        await driver.UpdateAsync(part, PartUpdateContext());

        Assert.Equal(ManagedContentScopeMode.Selected, part.EditScope.Mode);
        Assert.Equal(["costis"], part.EditScope.ManagedSiteIds);
    }

    [Fact]
    public void OverrideLink_IsShownOnAnOverride()
    {
        var driver = new ManagedContentOverrideDisplayDriver(
            new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("costis")),
            new FakeManagedContentLocator());

        Assert.NotNull(driver.Edit(Override(), EditorContext()));
    }

    [Fact]
    public void OverrideLink_IsNotShownOnAnItemThatIsNotAnOverride()
    {
        var driver = new ManagedContentOverrideDisplayDriver(
            new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("costis")),
            new FakeManagedContentLocator());

        Assert.Null(driver.Edit(Source(), EditorContext()));
    }

    [Fact]
    public async Task OverrideUpdate_CompletesALinkThatDoesNotRecordItsContainer()
    {
        // An override written before the container was recorded would otherwise leave suppression
        // unable to reach its source once the edit scope that created it is withdrawn.
        var page = ManagedContentTestContent.Item("page", "LandingPage");
        var section = ManagedContentTestContent.Source("section", ManagedContentScope.All());

        var locator = new FakeManagedContentLocator().WithPublished(section, page);
        var driver = new ManagedContentOverrideDisplayDriver(
            new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("costis")),
            locator);

        var overrideItem = ManagedContentTestContent.Override("override-item", "costis", "section");
        overrideItem.TryGet<ManagedContentOverridePart>(out var before);
        Assert.True(string.IsNullOrEmpty(before.SourceContainerContentItemId));

        await driver.UpdateAsync(overrideItem, UpdateContext());

        overrideItem.TryGet<ManagedContentOverridePart>(out var after);
        Assert.Equal("page", after.SourceContainerContentItemId);
    }

    [Fact]
    public async Task OverrideUpdate_LeavesAnAlreadyRecordedContainerAlone()
    {
        var driver = new ManagedContentOverrideDisplayDriver(
            new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("costis")),
            new FakeManagedContentLocator());

        var overrideItem = Override();
        overrideItem.TryGet<ManagedContentOverridePart>(out var part);
        part.SourceContainerContentItemId = "recorded-page";
        overrideItem.Apply(nameof(ManagedContentOverridePart), part);

        await driver.UpdateAsync(overrideItem, UpdateContext());

        overrideItem.TryGet<ManagedContentOverridePart>(out var after);
        Assert.Equal("recorded-page", after.SourceContainerContentItemId);
    }

    private static ManagedContentPartDisplayDriver ScopeDriver(bool isAuthorized)
        => new(
            new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("costis")),
            new ManagedContentScopeAuthorizationHandler(
                new StubAuthorizationService(isAuthorized),
                new HttpContextAccessor
                {
                    HttpContext = new DefaultHttpContext { User = ManagedSitesTestData.User() },
                }));

    private static ContentItem Override()
        => ManagedContentTestContent.Override("override-item", "costis", "section");

    private static ContentItem Source()
        => ManagedContentTestContent.Source("section", ManagedContentScope.All());

    private static ManagedContentPart PartOf(ContentItem contentItem)
    {
        var part = contentItem.GetOrCreate<ManagedContentPart>();
        part.ContentItem = contentItem;

        return part;
    }

    private static BuildPartEditorContext EditorContext()
        => new(PartDefinition(), Context());

    private static UpdatePartEditorContext PartUpdateContext()
        => new(PartDefinition(), UpdateContext());

    private static ContentTypePartDefinition PartDefinition()
        => new(
            nameof(ManagedContentPart),
            new ContentPartDefinition(nameof(ManagedContentPart)),
            []);

    private static UpdateEditorContext UpdateContext()
        => new(null, string.Empty, false, string.Empty, null, null, null);

    private static BuildEditorContext Context()
        => new(null, string.Empty, false, string.Empty, null, null, null);
}
