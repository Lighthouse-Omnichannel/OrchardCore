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
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers the promise that content which never opted in is untouched.
/// </summary>
/// <remarks>
/// FR-031 and SC-012. Managed content is opt-in per content type, so a site that has not attached the
/// part must behave exactly as it did before the feature existed, including on a Managed Site's own
/// host. The feature reaches into two places wide enough to break that on their own: it wraps the
/// content item display manager for every item the tenant renders, and it walks every content item as
/// it loads. Both are asserted here against content that carries nothing of ours.
/// </remarks>
public class UnattachedContentRegressionTests
{
    private const string BagName = "Widgets";

    [Fact]
    public async Task PageWithoutManagedContent_IsUnchangedOnAManagedSitesHost()
    {
        var before = Snapshot(PlainPage());

        var page = await LoadAsync(PlainPage(), "costis.localhost");

        Assert.Equal(before, Snapshot(page));
    }

    [Fact]
    public async Task PageWithoutManagedContent_IsUnchangedOnTheBlueprintHost()
    {
        var before = Snapshot(PlainPage());

        var page = await LoadAsync(PlainPage(), "localhost");

        Assert.Equal(before, Snapshot(page));
    }

    [Fact]
    public async Task ContainedItemsWithoutManagedContent_AreUnchanged()
    {
        // The traversal walks every contained item looking for the part. Finding none must leave the
        // page exactly as it was, not merely leave the text alone.
        var before = Snapshot(PlainPage());

        var page = await LoadAsync(PlainPage(), "costis.localhost");

        Assert.Equal(before, Snapshot(page));
    }

    [Fact]
    public async Task ItemWithoutManagedContent_IsNeverSubstitutedEvenWhenAnOverrideNamesIt()
    {
        // An override claiming an item that does not carry the part must not take effect. Detaching the
        // capability is how a Site Blueprint administrator withdraws a Managed Site's reach, and it has
        // to work even while the override still exists.
        var before = Snapshot(PlainPage());

        var page = await LoadAsync(PlainPage(), "costis.localhost", overrideNames: "widget");

        Assert.Equal(before, Snapshot(page));
    }

    [Fact]
    public void ItemWithoutManagedContent_RendersInEveryContext()
    {
        var service = new ManagedContentScopeService();
        var plain = ManagedContentTestContent.Item("widget");

        Assert.True(service.CanDisplay(plain, "costis"));
        Assert.True(service.CanDisplay(plain, managedSiteId: null));
    }

    [Fact]
    public void ItemWithoutManagedContent_IsEditableByNoManagedSite()
    {
        // Not opting in is not a way of opting everyone in.
        var service = new ManagedContentScopeService();

        Assert.False(service.CanEdit(part: null, "costis"));
    }

    [Fact]
    public async Task ResolutionHandsUntouchedContentStraightThrough()
    {
        var plain = ManagedContentTestContent.Item("widget");
        var overrides = new FakeManagedContentOverrideService();

        var resolution = await new ManagedContentResolutionService(new ManagedContentScopeService(), overrides)
            .ResolveAsync(plain, "costis");

        Assert.True(resolution.ShouldRender);
        Assert.False(resolution.IsOverride);
        Assert.Same(plain, resolution.Content);

        // Nothing was even asked about it, which is what keeps an untouched site paying nothing.
        Assert.Equal(0, overrides.PublishedLookups);
    }

    private static string Snapshot(ContentItem contentItem)
        => ((JsonObject)contentItem.Content).ToJsonString();

    private static ContentItem PlainPage()
    {
        var page = ManagedContentTestContent.Item("page", "LandingPage");
        var widget = ManagedContentTestContent.Item("widget", "Paragraph");

        widget.DisplayText = "Untouched";
        widget.Content["ParagraphPart"] = new JsonObject { ["Text"] = "Untouched" };

        ((JsonObject)page.Content)[BagName] = new JsonObject
        {
            ["ContentItems"] = new JsonArray(JObject.FromObject(widget, JOptions.Default)),
        };

        return page;
    }

    private static async Task<ContentItem> LoadAsync(ContentItem page, string host, string overrideNames = null)
    {
        var accessor = new ManagedSiteCompositionContextAccessor();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = "/";

        await new ManagedSiteRequestMiddleware(_ => Task.CompletedTask).InvokeAsync(
            httpContext,
            new ManagedSiteUrlResolver(
                new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("costis", hostname: "costis.localhost"))),
            accessor,
            Options.Create(new AdminOptions()));

        var overrides = new FakeManagedContentOverrideService();

        if (overrideNames is not null)
        {
            overrides.WithPublished("costis", overrideNames, ManagedContentTestContent.Override());
        }

        await new ManagedContentCompositionHandler(
            ServiceProvider(overrides),
            accessor,
            new ManagedContentScopeService())
            .LoadedAsync(new LoadContentContext(page));

        return page;
    }

    private static IServiceProvider ServiceProvider(IManagedContentOverrideService overrides)
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

        return services.BuildServiceProvider();
    }
}
