using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Routing;

/// <summary>
/// Covers the rule that a public request never gets to say which Managed Site it belongs to.
/// </summary>
/// <remarks>
/// FR-022. If a header could decide, any visitor could ask for another Managed Site's content by
/// sending one. The middleware is given the host and the path and nothing else, so the rule holds by
/// construction rather than by remembering to check.
/// </remarks>
public class PublicManagedSiteScopeMetadataTests
{
    [Fact]
    public async Task ScopeHeader_DoesNotChangeWhichManagedSiteIsResolved()
    {
        var context = Request("contoso.com", "/");
        context.Request.Headers[ManagedSitesConstants.Headers.ManagedSiteId] = "site-b";

        var accessor = await InvokeAsync(context);

        Assert.Equal("site-a", accessor.Current.ManagedSiteId);
    }

    [Fact]
    public async Task ScopeHeader_CannotInventAManagedSiteForAnUnclaimedHost()
    {
        var context = Request("fabrikam.com", "/");
        context.Request.Headers[ManagedSitesConstants.Headers.ManagedSiteId] = "site-a";

        var accessor = await InvokeAsync(context);

        Assert.Null(accessor.Current.ManagedSiteId);
    }

    [Fact]
    public async Task ScopeQueryValue_IsIgnoredToo()
    {
        var context = Request("fabrikam.com", "/", queryString: "?managedSiteId=site-a");

        var accessor = await InvokeAsync(context);

        Assert.Null(accessor.Current.ManagedSiteId);
    }

    [Fact]
    public async Task PublicRequest_ResolvesFromTheUrl()
    {
        var accessor = await InvokeAsync(Request("contoso.com", "/"));

        Assert.Equal("site-a", accessor.Current.ManagedSiteId);
    }

    [Fact]
    public async Task AdminRequest_IsLeftInTheSiteBlueprintContext()
    {
        // A blueprint administrator who happens to arrive on a Managed Site's host is still editing the
        // tenant's own content, and must not silently be switched into that Managed Site.
        var accessor = await InvokeAsync(Request("contoso.com", "/Admin/Contents/ContentItems"));

        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task RequestIsAlwaysPassedOn()
    {
        var context = Request("contoso.com", "/");
        var reached = false;

        await Middleware(_ =>
        {
            reached = true;

            return Task.CompletedTask;
        }).InvokeAsync(context, Resolver(), new ManagedSiteCompositionContextAccessor(), AdminOptions());

        Assert.True(reached);
    }

    [Fact]
    public async Task PrefixedManagedSite_HasItsPrefixMovedOntoThePathBase()
    {
        // Without this a prefixed Managed Site resolves only for URLs under its prefix, where no content
        // is routed, so it would match and then serve nothing.
        var context = Request("localhost", "/shop/about");

        await InvokeAsync(context, ManagedSitesTestData.ManagedSite("site-a", urlPrefix: "shop"));

        Assert.Equal("/shop", context.Request.PathBase);
        Assert.Equal("/about", context.Request.Path);
    }

    [Fact]
    public async Task PrefixRoot_BecomesTheSiteRoot()
    {
        // The whole path is the prefix, so nothing is left of it. An empty path is what a tenant's own
        // prefix leaves behind too, and it is what routes to the home page.
        var context = Request("localhost", "/shop");

        await InvokeAsync(context, ManagedSitesTestData.ManagedSite("site-a", urlPrefix: "shop"));

        Assert.Equal("/shop", context.Request.PathBase);
        Assert.Equal(string.Empty, context.Request.Path.ToString());
    }

    [Fact]
    public async Task PrefixIsAppendedToAnExistingPathBase()
    {
        // A tenant prefix or a hosting path may already have set one, so it is appended, not replaced.
        var context = Request("localhost", "/shop/about");
        context.Request.PathBase = "/tenant";

        await InvokeAsync(context, ManagedSitesTestData.ManagedSite("site-a", urlPrefix: "shop"));

        Assert.Equal("/tenant/shop", context.Request.PathBase);
    }

    [Fact]
    public async Task ManagedSiteWithoutAPrefix_LeavesThePathAlone()
    {
        var context = Request("contoso.com", "/about");

        await InvokeAsync(context);

        Assert.Equal(string.Empty, context.Request.PathBase.ToString());
        Assert.Equal("/about", context.Request.Path);
    }

    [Fact]
    public async Task UnmatchedRequest_LeavesThePathAlone()
    {
        var context = Request("fabrikam.com", "/shop/about");

        await InvokeAsync(context, ManagedSitesTestData.ManagedSite("site-a", hostname: "contoso.com", urlPrefix: "shop"));

        Assert.Equal(string.Empty, context.Request.PathBase.ToString());
        Assert.Equal("/shop/about", context.Request.Path);
    }

    private static async Task<IManagedSiteCompositionContextAccessor> InvokeAsync(HttpContext httpContext)
    {
        var accessor = new ManagedSiteCompositionContextAccessor();

        await Middleware(_ => Task.CompletedTask).InvokeAsync(httpContext, Resolver(), accessor, AdminOptions());

        return accessor;
    }

    private static async Task<IManagedSiteCompositionContextAccessor> InvokeAsync(
        HttpContext httpContext,
        params ManagedSite[] managedSites)
    {
        var accessor = new ManagedSiteCompositionContextAccessor();

        await Middleware(_ => Task.CompletedTask).InvokeAsync(
            httpContext,
            new ManagedSiteUrlResolver(new FakeManagedSiteService(managedSites)),
            accessor,
            AdminOptions());

        return accessor;
    }

    private static ManagedSiteRequestMiddleware Middleware(RequestDelegate next) => new(next);

    private static ManagedSiteUrlResolver Resolver()
        => new(new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("site-a", hostname: "contoso.com")));

    private static IOptions<AdminOptions> AdminOptions() => Options.Create(new AdminOptions());

    private static DefaultHttpContext Request(string host, string path, string queryString = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Request.Path = path;

        if (queryString is not null)
        {
            context.Request.QueryString = new QueryString(queryString);
        }

        return context;
    }
}
