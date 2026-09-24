using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using OrchardCore.Admin;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentManagement.Routing;
using VendallionCMS.ManagedSites.Handlers;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.Tests.ManagedContent;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Composition;

/// <summary>
/// Covers a request arriving at a URL and the content that is then there to be rendered.
/// </summary>
/// <remarks>
/// The assertions read the page's own content tree, because that is what a Liquid template reads. An
/// earlier version of this feature substituted overrides through the display manager, which is correct
/// for shape rendering and invisible to a template walking
/// <c>Model.ContentItem.Content.Services.ContentItems</c> and taking fields off each child. Every unit
/// test passed while nothing on the site changed, so these tests deliberately inspect the content a
/// template would see rather than a resolution result.
/// </remarks>
public class ManagedSiteCompositionIntegrationTests
{
    private const string BagName = "Services";
    private const string PageId = "page";
    private const string SectionId = "section";

    [Fact]
    public async Task RequestOnAManagedSitesHost_FindsTheOverrideInThePageContent()
    {
        var page = await ComposeAsync("costis.localhost");

        Assert.Equal("E-Commerce Costis", TitleOfFirstSection(page));
    }

    [Fact]
    public async Task RequestOnAnotherHost_FindsTheBlueprintContent()
    {
        var page = await ComposeAsync("localhost");

        Assert.Equal("E-Commerce", TitleOfFirstSection(page));
    }

    [Fact]
    public async Task OverriddenSection_CarriesTheOverridesOwnIdentity()
    {
        // What renders is the Managed Site's item, not the blueprint's with different words in it.
        var page = await ComposeAsync("costis.localhost");

        Assert.Equal("override-item", FirstSection(page)["ContentItemId"].GetValue<string>());
    }

    [Fact]
    public async Task OverriddenSection_KeepsNothingTheOriginalHadAndTheOverrideDoesNot()
    {
        // An override stands in for the item rather than extending it, so a field the blueprint carried
        // and the override does not must not show through.
        var page = await ComposeAsync("costis.localhost");

        Assert.False(FirstSection(page).ContainsKey("BlueprintOnlyPart"));
    }

    [Fact]
    public async Task AdminRequest_FindsTheBlueprintContent()
    {
        // The guard that stops an editor saving a Managed Site's content over the Site Blueprint's.
        var page = await ComposeAsync("costis.localhost", "/Admin/Contents/ContentItems");

        Assert.Equal("E-Commerce", TitleOfFirstSection(page));
    }

    [Fact]
    public async Task ApiRequest_FindsTheBlueprintContent()
    {
        // Content is edited through the API, so it must load the original whatever host it arrives on.
        var page = await ComposeAsync("costis.localhost", "/api/managed-sites/costis/managed-content");

        Assert.Equal("E-Commerce", TitleOfFirstSection(page));
    }

    [Fact]
    public async Task ManagedSiteWithoutAnOverride_FindsTheBlueprintContent()
    {
        var page = await ComposeAsync("michael.localhost");

        Assert.Equal("E-Commerce", TitleOfFirstSection(page));
    }

    [Fact]
    public async Task SectionOutsideTheEditScope_IsNotSubstituted()
    {
        // An override withdrawn from the edit scope stops being served without being deleted.
        var page = await ComposeAsync("costis.localhost", editScope: ManagedContentScope.None());

        Assert.Equal("E-Commerce", TitleOfFirstSection(page));
    }

    private static JsonObject FirstSection(ContentItem page)
        => (JsonObject)((JsonArray)((JsonObject)page.Content)[BagName]["ContentItems"])[0];

    private static string TitleOfFirstSection(ContentItem page)
        => FirstSection(page)["TitlePart"]["Title"].GetValue<string>();

    private static async Task<ContentItem> ComposeAsync(
        string host,
        string path = "/",
        ManagedContentScope editScope = null)
    {
        var managedSites = new FakeManagedSiteService(
            ManagedSitesTestData.ManagedSite("costis", hostname: "costis.localhost"),
            ManagedSitesTestData.ManagedSite("michael", hostname: "michael.localhost"));

        // Step one, exactly as the pipeline runs it: the URL decides which Managed Site the request is.
        var accessor = new ManagedSiteCompositionContextAccessor();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = path;

        await new ManagedSiteRequestMiddleware(_ => Task.CompletedTask).InvokeAsync(
            httpContext,
            new ManagedSiteUrlResolver(managedSites),
            accessor,
            Options.Create(new AdminOptions()));

        // Step two: the page loads, and its sections are substituted before anything renders them.
        var page = Page(editScope ?? ManagedContentScope.All());
        var overrides = new FakeManagedContentOverrideService()
            .WithPublished("costis", SectionId, OverrideSection());

        var handler = new ManagedContentCompositionHandler(
            ServiceProvider(overrides),
            accessor,
            new ManagedContentScopeService());

        await handler.LoadedAsync(new LoadContentContext(page));

        return page;
    }

    private static IServiceProvider ServiceProvider(IManagedContentOverrideService overrides)
    {
        var contentManager = new Mock<IContentManager>();

        // Containment is resolved the way each container part resolves it, through the aspect. The
        // accessor mirrors how a bag stores its children, which is what the traversal walks.
        contentManager
            .Setup(manager => manager.PopulateAspectAsync(
                It.IsAny<IContent>(),
                It.IsAny<ContainedContentItemsAspect>()))
            .Returns((IContent _, ContainedContentItemsAspect aspect) =>
            {
                aspect.Accessors.Add(content => content[BagName]?["ContentItems"] as JsonArray ?? []);

                return Task.FromResult(aspect);
            });

        var services = new ServiceCollection();
        services.AddSingleton(contentManager.Object);
        services.AddSingleton(overrides);

        return services.BuildServiceProvider();
    }

    private static ContentItem Page(ManagedContentScope editScope)
    {
        var page = ManagedContentTestContent.Item(PageId, "LandingPage");
        var section = ManagedContentTestContent.Source(SectionId, editScope);

        section.DisplayText = "E-Commerce";
        section.Content["TitlePart"] = new JsonObject { ["Title"] = "E-Commerce" };
        section.Content["BlueprintOnlyPart"] = new JsonObject { ["Note"] = "only on the blueprint" };

        // Serialized whole, identity included, because that is how a bag stores a child. Storing only
        // its parts would leave the section with no identifier, and the override lookup nothing to
        // match on, which is a property of the fixture rather than of the feature.
        ((JsonObject)page.Content)[BagName] = new JsonObject
        {
            ["ContentItems"] = new JsonArray(JObject.FromObject(section, JOptions.Default)),
        };

        return page;
    }

    private static ContentItem OverrideSection()
    {
        var overrideSection = ManagedContentTestContent.Override("override-item", "costis", SectionId);

        overrideSection.DisplayText = "E-Commerce Costis";
        overrideSection.Content["TitlePart"] = new JsonObject { ["Title"] = "E-Commerce Costis" };

        return overrideSection;
    }
}
