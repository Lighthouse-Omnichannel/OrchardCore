using System.Linq;
using System.Threading.Tasks;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Routing;

/// <summary>
/// Covers addressing a Managed Site the way a tenant is addressed, by host name and URL prefix.
/// </summary>
public class ManagedSiteAddressTests
{
    [Theory]
    [InlineData("contoso.com", "contoso.com")]
    [InlineData("  Contoso.COM  ", "contoso.com")]
    [InlineData("https://contoso.com", "contoso.com")]
    [InlineData("http://contoso.com/shop", "contoso.com")]
    [InlineData("//contoso.com", "contoso.com")]
    [InlineData("localhost:5000", "localhost:5000")]
    [InlineData("  ", "")]
    public void NormalizeHost_StripsSchemeAndPath(string value, string expected)
        => Assert.Equal(expected, ManagedSiteAddressValidator.NormalizeHost(value));

    [Theory]
    [InlineData("/shop/", "shop")]
    [InlineData("Shop", "shop")]
    [InlineData("/a/b/", "a/b")]
    [InlineData("", "")]
    [InlineData("/", "")]
    public void NormalizePrefix_TrimsSlashesAndLowercases(string value, string expected)
        => Assert.Equal(expected, ManagedSiteAddressValidator.NormalizePrefix(value));

    [Fact]
    public void SplitHostnames_AcceptsTenantSeparators()
    {
        var hosts = ManagedSiteAddressValidator.SplitHostnames("contoso.com, www.contoso.com fabrikam.com");

        Assert.Equal(["contoso.com", "www.contoso.com", "fabrikam.com"], hosts);
    }

    [Fact]
    public void SplitHostnames_DropsDuplicates()
        => Assert.Equal(["contoso.com"], ManagedSiteAddressValidator.SplitHostnames("contoso.com, CONTOSO.com"));

    [Fact]
    public void Expand_OneAddressPerHostName()
    {
        var managedSite = ManagedSitesTestData.ManagedSite(hostname: "contoso.com,fabrikam.com", urlPrefix: "shop");

        var addresses = ManagedSiteAddressValidator.Expand(managedSite);

        Assert.Equal(
            [new ManagedSiteAddress("contoso.com", "shop"), new ManagedSiteAddress("fabrikam.com", "shop")],
            addresses);
    }

    [Fact]
    public void Expand_NoHostName_YieldsOneHostAgnosticAddress()
    {
        var addresses = ManagedSiteAddressValidator.Expand(ManagedSitesTestData.ManagedSite(urlPrefix: "shop"));

        Assert.Equal([new ManagedSiteAddress(string.Empty, "shop")], addresses);
    }

    [Fact]
    public void Overlaps_SameHostAndPrefix_IsTrue()
        => Assert.True(ManagedSiteAddressValidator.Overlaps(
            new ManagedSiteAddress("contoso.com", "shop"),
            new ManagedSiteAddress("contoso.com", "shop")));

    [Fact]
    public void Overlaps_DifferentHosts_IsFalse()
        => Assert.False(ManagedSiteAddressValidator.Overlaps(
            new ManagedSiteAddress("contoso.com", "shop"),
            new ManagedSiteAddress("fabrikam.com", "shop")));

    [Fact]
    public void Overlaps_DifferentPrefixes_IsFalse()
        => Assert.False(ManagedSiteAddressValidator.Overlaps(
            new ManagedSiteAddress("contoso.com", "shop"),
            new ManagedSiteAddress("contoso.com", "news")));

    [Fact]
    public void Overlaps_HostAgnosticAgainstHostSpecific_IsTrue()
    {
        // An empty host name answers on every host, so both would claim the same request.
        Assert.True(ManagedSiteAddressValidator.Overlaps(
            new ManagedSiteAddress(string.Empty, "shop"),
            new ManagedSiteAddress("contoso.com", "shop")));
    }

    [Theory]
    [InlineData("shop", "/shop", true)]
    [InlineData("shop", "/shop/basket", true)]
    [InlineData("shop", "/shopping", false)]
    [InlineData("shop", "/news", false)]
    [InlineData("", "/anything", true)]
    public void PathStartsWithPrefix_MatchesOnSegmentBoundaries(string prefix, string path, bool expected)
        => Assert.Equal(expected, ManagedSiteAddressValidator.PathStartsWithPrefix(path, prefix));

    [Fact]
    public async Task SaveAsync_SamePrefixOnDifferentHosts_IsAllowed()
    {
        var service = CreateService();

        await service.SaveAsync(ManagedSitesTestData.ManagedSite("a", name: "A", hostname: "contoso.com", urlPrefix: "shop"));
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("b", name: "B", hostname: "fabrikam.com", urlPrefix: "shop"));

        Assert.Equal(2, (await service.ListAsync()).Count);
    }

    [Fact]
    public async Task SaveAsync_SameHostAndPrefix_IsRejected()
    {
        var service = CreateService();
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("a", name: "A", hostname: "contoso.com", urlPrefix: "shop"));

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(
                ManagedSitesTestData.ManagedSite("b", name: "B", hostname: "contoso.com", urlPrefix: "shop")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.UrlConflict, exception.Code);
    }

    [Fact]
    public async Task SaveAsync_HostAgnosticPrefixAgainstHostSpecific_IsRejected()
    {
        var service = CreateService();
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("a", name: "A", urlPrefix: "shop"));

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(
                ManagedSitesTestData.ManagedSite("b", name: "B", hostname: "contoso.com", urlPrefix: "shop")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.UrlConflict, exception.Code);
    }

    [Fact]
    public async Task SaveAsync_DifferentPrefixesOnTheSameHost_IsAllowed()
    {
        var service = CreateService();

        await service.SaveAsync(ManagedSitesTestData.ManagedSite("a", name: "A", hostname: "contoso.com", urlPrefix: "shop"));
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("b", name: "B", hostname: "contoso.com", urlPrefix: "news"));

        Assert.Equal(2, (await service.ListAsync()).Count);
    }

    [Fact]
    public async Task SaveAsync_NormalizesTheAddressInPlace()
    {
        var service = CreateService();
        var managedSite = ManagedSitesTestData.ManagedSite("a", name: "A", hostname: " HTTPS://Contoso.com/x ", urlPrefix: "/Shop/");

        await service.SaveAsync(managedSite);

        var stored = await service.GetAsync("a");
        Assert.Equal("contoso.com", stored.Hostname);
        Assert.Equal("shop", stored.UrlPrefix);
    }

    [Fact]
    public async Task FindByAddressAsync_PrefersTheHostSpecificManagedSite()
    {
        var service = CreateService();
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("agnostic", name: "Agnostic", urlPrefix: "shop"));
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("specific", name: "Specific", hostname: "contoso.com", urlPrefix: "news"));

        var match = await service.FindByAddressAsync("contoso.com", "/news");

        Assert.Equal("specific", match.Id);
    }

    [Fact]
    public async Task FindByAddressAsync_PrefersTheLongerPrefix()
    {
        var service = CreateService();
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("root", name: "Root", hostname: "contoso.com"));
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("deep", name: "Deep", hostname: "contoso.com", urlPrefix: "shop"));

        var match = await service.FindByAddressAsync("contoso.com", "/shop/basket");

        Assert.Equal("deep", match.Id);
    }

    [Fact]
    public async Task FindByAddressAsync_UnknownHost_DoesNotMatchAHostSpecificManagedSite()
    {
        var service = CreateService();
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("a", name: "A", hostname: "contoso.com"));

        Assert.Null(await service.FindByAddressAsync("fabrikam.com", "/"));
    }

    [Fact]
    public async Task FindByAddressAsync_DisabledManagedSite_DoesNotAnswer()
    {
        var service = CreateService();
        await service.SaveAsync(ManagedSitesTestData.ManagedSite("a", ManagedSiteStatus.Disabled, "A", "contoso.com"));

        Assert.Null(await service.FindByAddressAsync("contoso.com", "/"));
    }

    private static ManagedSiteService CreateService()
        => new(new FakeSiteService(), new FakeShellUrlSynchronizationService());
}
