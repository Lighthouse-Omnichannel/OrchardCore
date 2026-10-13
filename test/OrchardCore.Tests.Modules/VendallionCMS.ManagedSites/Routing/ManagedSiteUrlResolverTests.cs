using System.Threading.Tasks;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Routing;

/// <summary>
/// Covers which Managed Site answers an incoming public request.
/// </summary>
/// <remarks>
/// The URL is the whole of the input. A request that matches nothing resolves to no Managed Site, which
/// every consumer reads as the Site Blueprint context rather than as a failure.
/// </remarks>
public class ManagedSiteUrlResolverTests
{
    [Fact]
    public async Task HostName_ResolvesTheManagedSiteThatClaimsIt()
    {
        var resolver = Resolver(ManagedSitesTestData.ManagedSite("sitea", hostname: "contoso.com"));

        var context = await resolver.ResolveAsync("contoso.com", "/");

        Assert.Equal("sitea", context.ManagedSiteCode);
    }

    [Fact]
    public async Task UnclaimedHostName_ResolvesToTheSiteBlueprint()
    {
        var resolver = Resolver(ManagedSitesTestData.ManagedSite("sitea", hostname: "contoso.com"));

        var context = await resolver.ResolveAsync("fabrikam.com", "/");

        Assert.Null(context.ManagedSiteCode);
    }

    [Fact]
    public async Task UrlPrefix_ResolvesOnAnyHostWhenNoHostNameIsClaimed()
    {
        // An empty Hostname answers on every host the tenant serves, exactly as it does for a tenant.
        var resolver = Resolver(ManagedSitesTestData.ManagedSite("sitea", urlPrefix: "shop"));

        Assert.Equal("sitea", (await resolver.ResolveAsync("contoso.com", "/shop")).ManagedSiteCode);
        Assert.Equal("sitea", (await resolver.ResolveAsync("fabrikam.com", "/shop/basket")).ManagedSiteCode);
    }

    [Fact]
    public async Task UrlPrefix_DoesNotMatchAPathThatMerelyStartsWithIt()
    {
        var resolver = Resolver(ManagedSitesTestData.ManagedSite("sitea", urlPrefix: "shop"));

        Assert.Null((await resolver.ResolveAsync("contoso.com", "/shopping")).ManagedSiteCode);
    }

    [Fact]
    public async Task HostSpecificManagedSite_WinsOverOneAnsweringOnEveryHost()
    {
        // FR-010a. Both could answer; the one that names the host is the more specific claim.
        var resolver = Resolver(
            ManagedSitesTestData.ManagedSite("everywhere", urlPrefix: "shop"),
            ManagedSitesTestData.ManagedSite("named", hostname: "contoso.com", urlPrefix: "shop"));

        Assert.Equal("named", (await resolver.ResolveAsync("contoso.com", "/shop")).ManagedSiteCode);
        Assert.Equal("everywhere", (await resolver.ResolveAsync("fabrikam.com", "/shop")).ManagedSiteCode);
    }

    [Fact]
    public async Task LongerPrefix_WinsOverAShorterOne()
    {
        var resolver = Resolver(
            ManagedSitesTestData.ManagedSite("shallow", urlPrefix: "shop"),
            ManagedSitesTestData.ManagedSite("deep", urlPrefix: "shop/outlet"));

        Assert.Equal("deep", (await resolver.ResolveAsync("contoso.com", "/shop/outlet/socks")).ManagedSiteCode);
        Assert.Equal("shallow", (await resolver.ResolveAsync("contoso.com", "/shop/socks")).ManagedSiteCode);
    }

    [Fact]
    public async Task DisabledManagedSite_AnswersNothing()
    {
        var resolver = Resolver(
            ManagedSitesTestData.ManagedSite("sitea", ManagedSiteStatus.Disabled, hostname: "contoso.com"));

        Assert.Null((await resolver.ResolveAsync("contoso.com", "/")).ManagedSiteCode);
    }

    [Fact]
    public async Task ResolvedContext_CarriesTheAddressItWasResolvedFrom()
    {
        // Kept so a consumer can say why it decided as it did, without re-reading the request.
        var resolver = Resolver(ManagedSitesTestData.ManagedSite("sitea", hostname: "contoso.com"));

        var context = await resolver.ResolveAsync("contoso.com", "/about");

        Assert.Equal("contoso.com", context.Host);
        Assert.Equal("/about", context.Path);
    }

    [Fact]
    public async Task NoManagedSitesDefined_ResolvesToTheSiteBlueprint()
    {
        Assert.Null((await Resolver().ResolveAsync("contoso.com", "/")).ManagedSiteCode);
    }

    private static ManagedSiteUrlResolver Resolver(params ManagedSite[] managedSites)
        => new(new FakeManagedSiteService(managedSites));
}
