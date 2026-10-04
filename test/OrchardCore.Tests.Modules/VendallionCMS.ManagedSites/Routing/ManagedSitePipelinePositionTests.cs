using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Routing;

/// <summary>
/// Covers where in the tenant pipeline a request acquires its Managed Site.
/// </summary>
/// <remarks>
/// Resolving the Managed Site is not enough on its own: a Managed Site answering under a URL prefix
/// needs that prefix moved from the path onto the path base, and that has to happen before an endpoint
/// is chosen. OrchardCore calls <c>UseRouting</c> before it lets a module configure the pipeline, so
/// middleware added from a module's <c>Configure</c> runs after the endpoint has already been selected
/// from the original path. A prefixed Managed Site then matched and served a 404, because no content is
/// routed under the prefix, while host-named Managed Sites worked and hid the problem.
///
/// A startup filter is added ahead of <c>UseRouting</c>, which is the position OrchardCore itself
/// rebases a tenant's own prefix from. These tests pin that placement, because nothing about the
/// middleware's own behaviour reveals it: it rebases correctly either way.
/// </remarks>
public class ManagedSitePipelinePositionTests
{
    [Fact]
    public void TheRoutingFeature_AddsTheMiddlewareThroughAStartupFilter()
    {
        var services = new ServiceCollection();

        new RoutingStartup().ConfigureServices(services);

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IStartupFilter)
                && service.ImplementationType == typeof(ManagedSiteRequestStartupFilter));
    }

    [Fact]
    public void TheRoutingFeature_AddsNoMiddlewareOfItsOwn()
    {
        // Guards the test above. Adding the middleware from Configure as well would put a second copy
        // after routing, and the one that mattered would be whichever ran first.
        var configure = typeof(RoutingStartup).GetMethod(
            nameof(RoutingStartup.Configure),
            [typeof(IApplicationBuilder), typeof(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder), typeof(IServiceProvider)]);

        Assert.NotEqual(typeof(RoutingStartup), configure.DeclaringType);
    }

    [Fact]
    public async Task TheStartupFilter_RebasesAPrefixBeforeTheRestOfThePipelineRuns()
    {
        // The rest of the pipeline is where routing lives, so what it sees is what an endpoint is
        // chosen from.
        var seen = await RunAsync("fabrikam.com", "/shop/about");

        Assert.Equal("/shop", seen.PathBase);
        Assert.Equal("/about", seen.Path);
    }

    [Fact]
    public async Task TheStartupFilter_LeavesAPathNoManagedSiteClaimsAlone()
    {
        var seen = await RunAsync("fabrikam.com", "/elsewhere/about");

        Assert.Equal(string.Empty, seen.PathBase);
        Assert.Equal("/elsewhere/about", seen.Path);
    }

    private static async Task<(string PathBase, string Path)> RunAsync(string host, string path)
    {
        var services = new ServiceCollection()
            .AddSingleton<IManagedSiteUrlResolver>(new ManagedSiteUrlResolver(
                new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("site-a", urlPrefix: "shop"))))
            .AddSingleton<IManagedSiteCompositionContextAccessor, ManagedSiteCompositionContextAccessor>()
            .AddSingleton(Options.Create(new AdminOptions()))
            .BuildServiceProvider();

        (string PathBase, string Path) seen = default;

        // Exactly how OrchardCore applies a startup filter: the filter wraps everything that follows,
        // and everything that follows begins with routing.
        var builder = new ApplicationBuilder(services);
        new ManagedSiteRequestStartupFilter()
            .Configure(app => app.Run(context =>
            {
                seen = (context.Request.PathBase.Value, context.Request.Path.Value);

                return Task.CompletedTask;
            }))(builder);

        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = path;

        await builder.Build()(httpContext);

        return seen;
    }
}
