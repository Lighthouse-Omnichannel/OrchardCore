using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.Tests.ManagedContent;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Composition;

/// <summary>
/// Covers a request arriving at a URL and receiving that Managed Site's content.
/// </summary>
/// <remarks>
/// The two halves are built and tested apart: the middleware turns a URL into a Managed Site, and
/// resolution turns a Managed Site into content. These check that they meet, because each half was
/// correct on its own for an entire phase while nothing rendered differently.
/// </remarks>
public class ManagedSiteCompositionServiceTests
{
    private const string SourceId = "source-item";

    [Fact]
    public async Task RequestOnAManagedSitesHost_ReceivesItsOverride()
    {
        var context = await ComposeAsync("costis.localhost", "/");

        Assert.True(context.Resolution.IsOverride);
        Assert.Equal("override-item", context.Resolution.Content.ContentItemId);
    }

    [Fact]
    public async Task RequestOnAnotherHost_ReceivesTheOriginal()
    {
        var context = await ComposeAsync("localhost", "/");

        Assert.False(context.Resolution.IsOverride);
        Assert.Equal(SourceId, context.Resolution.Content.ContentItemId);
    }

    [Fact]
    public async Task RequestUnderAManagedSitesPrefix_ReceivesItsOverride()
    {
        var context = await ComposeAsync("localhost", "/costis/about", hostname: "", urlPrefix: "costis");

        Assert.True(context.Resolution.IsOverride);
    }

    [Fact]
    public async Task RequestOutsideTheManagedSitesPrefix_ReceivesTheOriginal()
    {
        var context = await ComposeAsync("localhost", "/about", hostname: "", urlPrefix: "costis");

        Assert.False(context.Resolution.IsOverride);
    }

    [Fact]
    public async Task ManagedSiteWithoutAnOverride_ReceivesTheOriginal()
    {
        var context = await ComposeAsync("costis.localhost", "/", withOverride: false);

        Assert.True(context.Resolution.ShouldRender);
        Assert.False(context.Resolution.IsOverride);
    }

    [Fact]
    public async Task AdminRequest_ComposesAsTheSiteBlueprint()
    {
        // Reached through the Managed Site's own host, and still the tenant's own content.
        var context = await ComposeAsync("costis.localhost", "/Admin/Contents/ContentItems");

        Assert.Null(context.ManagedSiteId);
        Assert.False(context.Resolution.IsOverride);
    }

    private static async Task<CompositionOutcome> ComposeAsync(
        string host,
        string path,
        string hostname = "costis.localhost",
        string urlPrefix = null,
        bool withOverride = true)
    {
        var managedSite = ManagedSitesTestData.ManagedSite(
            "costis",
            hostname: hostname,
            urlPrefix: urlPrefix);

        var accessor = new ManagedSiteCompositionContextAccessor();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = path;

        var middleware = new ManagedSiteRequestMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            httpContext,
            new ManagedSiteUrlResolver(new FakeManagedSiteService(managedSite)),
            accessor,
            Options.Create(new AdminOptions()));

        var overrides = new FakeManagedContentOverrideService();

        if (withOverride)
        {
            overrides.WithPublished("costis", SourceId, ManagedContentTestContent.Override());
        }

        var resolution = new ManagedContentResolutionService(new ManagedContentScopeService(), overrides);
        var source = ManagedContentTestContent.Source(SourceId, ManagedContentScope.All());

        return new CompositionOutcome(
            accessor.Current?.ManagedSiteId,
            await resolution.ResolveAsync(source, accessor.Current?.ManagedSiteId));
    }

    private sealed record CompositionOutcome(string ManagedSiteId, ManagedContentResolution Resolution);
}
