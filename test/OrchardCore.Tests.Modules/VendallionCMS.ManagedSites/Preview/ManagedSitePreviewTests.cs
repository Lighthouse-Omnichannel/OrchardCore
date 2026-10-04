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

namespace VendallionCMS.ManagedSites.Tests.Preview;

/// <summary>
/// Covers what an editor is sent to look at, and what they are shown when they get there.
/// </summary>
/// <remarks>
/// Preview points at the Managed Site's own address rather than at a separate rendering path, so a
/// preview cannot drift from what is served: it is composed by the same middleware and the same
/// substitution as a visitor's request.
///
/// Unpublished work is the one difference, and it is asked for in the address and granted by clearance
/// when the request arrives. A query value anyone can type must never be what puts drafts in front of
/// visitors, so these tests exercise the grant as well as the ask.
/// </remarks>
public class ManagedSitePreviewTests
{
    private const string BagName = "Services";
    private const string SectionId = "section";

    [Fact]
    public async Task Preview_PointsAtTheManagedSitesOwnHost()
    {
        var preview = await PreviewService().CreateAsync("costis", "/about", includeDrafts: false);

        Assert.Equal("//costis.localhost/about", preview.PreviewUrl);
        Assert.Equal("costis", preview.ManagedSiteId);
        Assert.Equal(ManagedSitesConstants.Preview.ManagedSiteMode, preview.CompositionMode);
    }

    [Fact]
    public async Task Preview_OfAHostAgnosticManagedSite_CarriesItsPrefix()
    {
        var service = new ManagedSitePreviewService(
            new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("shop", urlPrefix: "shop")));

        var preview = await service.CreateAsync("shop", "/basket", includeDrafts: false);

        Assert.Equal("/shop/basket", preview.PreviewUrl);
    }

    [Fact]
    public async Task Preview_OfTheSiteRoot_IsTheSiteRoot()
    {
        var preview = await PreviewService().CreateAsync("costis", url: null, includeDrafts: false);

        Assert.Equal("//costis.localhost/", preview.PreviewUrl);
    }

    [Fact]
    public async Task Preview_AsksForDraftsInTheAddress()
    {
        var preview = await PreviewService().CreateAsync("costis", "/about", includeDrafts: true);

        Assert.Contains(ManagedSitesConstants.Preview.DraftsQueryKey, preview.PreviewUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preview_OfAnAbsoluteAddress_KeepsOnlyThePath()
    {
        // Otherwise a caller could point preview at another host and have the answer look like this
        // Managed Site's.
        var preview = await PreviewService().CreateAsync("costis", "https://elsewhere.example/about", false);

        Assert.Equal("//costis.localhost/about", preview.PreviewUrl);
    }

    [Fact]
    public async Task Preview_OfADisabledManagedSite_IsRefused()
    {
        // Its address resolves to the Site Blueprint, so a preview would show content that is not the
        // Managed Site's.
        var service = new ManagedSitePreviewService(new FakeManagedSiteService(
            ManagedSitesTestData.ManagedSite("costis", ManagedSiteStatus.Disabled, hostname: "costis.localhost")));

        Assert.Null(await service.CreateAsync("costis", "/", includeDrafts: false));
    }

    [Fact]
    public async Task Preview_OfAnUnknownManagedSite_IsRefused()
        => Assert.Null(await PreviewService().CreateAsync("missing", "/", includeDrafts: false));

    [Fact]
    public async Task PreviewingWithClearance_ShowsUnpublishedWork()
    {
        var page = await ComposeAsync(previewRequested: true, clearance: "costis:view,preview");

        Assert.Equal("Draft in progress", TitleOfFirstSection(page));
    }

    [Fact]
    public async Task PreviewingWithoutPreviewClearance_ShowsOnlyWhatIsPublished()
    {
        // Edit clearance is not preview clearance, and neither is typing the query value.
        var page = await ComposeAsync(previewRequested: true, clearance: "costis:view,edit");

        Assert.Equal("Published", TitleOfFirstSection(page));
    }

    [Fact]
    public async Task PreviewingAnonymously_ShowsOnlyWhatIsPublished()
    {
        var page = await ComposeAsync(previewRequested: true, clearance: null);

        Assert.Equal("Published", TitleOfFirstSection(page));
    }

    [Fact]
    public async Task AnOrdinaryRequest_ShowsOnlyWhatIsPublished()
    {
        // Even from someone who holds preview clearance: a visitor's address asks for nothing.
        var page = await ComposeAsync(previewRequested: false, clearance: "costis:view,preview");

        Assert.Equal("Published", TitleOfFirstSection(page));
    }

    private static ManagedSitePreviewService PreviewService()
        => new(new FakeManagedSiteService(
            ManagedSitesTestData.ManagedSite("costis", hostname: "costis.localhost")));

    private static string TitleOfFirstSection(ContentItem page)
        => ((JsonObject)((JsonArray)((JsonObject)page.Content)[BagName]["ContentItems"])[0])
            ["TitlePart"]["Title"].GetValue<string>();

    private static async Task<ContentItem> ComposeAsync(bool previewRequested, string clearance)
    {
        var managedSites = new FakeManagedSiteService(
            ManagedSitesTestData.ManagedSite("costis", hostname: "costis.localhost"));

        var accessor = new ManagedSiteCompositionContextAccessor();
        var httpContext = new DefaultHttpContext
        {
            User = clearance is null ? ManagedSitesTestData.AnonymousUser() : ManagedSitesTestData.User(clearance),
        };

        httpContext.Request.Host = new HostString("costis.localhost");
        httpContext.Request.Path = "/";

        if (previewRequested)
        {
            httpContext.Request.QueryString =
                new QueryString('?' + ManagedSitesConstants.Preview.DraftsQueryKey + "=1");
        }

        await new ManagedSiteRequestMiddleware(_ => Task.CompletedTask).InvokeAsync(
            httpContext,
            new ManagedSiteUrlResolver(managedSites),
            accessor,
            Options.Create(new AdminOptions()));

        var page = Page();
        var overrides = new FakeManagedContentOverrideService()
            .WithPublished("costis", SectionId, Section("Published", "published-override"))
            .WithDraft("costis", SectionId, Section("Draft in progress", "draft-override"));

        await new ManagedContentCompositionHandler(
            ServiceProvider(overrides, managedSites, httpContext),
            accessor,
            new ManagedContentScopeService())
            .LoadedAsync(new LoadContentContext(page));

        return page;
    }

    private static IServiceProvider ServiceProvider(
        IManagedContentOverrideService overrides,
        IManagedSiteService managedSites,
        HttpContext httpContext)
    {
        var contentManager = new Mock<IContentManager>();

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
        services.AddSingleton(managedSites);
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
        services.AddSingleton<IManagedSiteClearanceService>(new ManagedSiteClearanceService(
            new ManagedSiteAuthorizationService(),
            managedSites,
            TimeProvider.System));

        return services.BuildServiceProvider();
    }

    private static ContentItem Page()
    {
        var page = ManagedContentTestContent.Item("page", "LandingPage");
        var section = ManagedContentTestContent.Source(SectionId, ManagedContentScope.All());

        section.Content["TitlePart"] = new JsonObject { ["Title"] = "Blueprint" };

        ((JsonObject)page.Content)[BagName] = new JsonObject
        {
            ["ContentItems"] = new JsonArray(JObject.FromObject(section, JOptions.Default)),
        };

        return page;
    }

    private static ContentItem Section(string title, string contentItemId)
    {
        var section = ManagedContentTestContent.Override(contentItemId, "costis", SectionId);
        section.Content["TitlePart"] = new JsonObject { ["Title"] = title };

        return section;
    }
}
