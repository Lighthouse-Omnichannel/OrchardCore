using System.Security.Claims;
using System.Threading.Tasks;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

public class ManagedSiteContentAuthorizationTests
{
    [Fact]
    public async Task AuthorizeAsync_MatchingClaimAndScope_ReturnsTrue()
    {
        var service = new ManagedSiteAuthorizationService();
        var user = CreateUser("site-a:edit,publish");

        var result = await service.AuthorizeAsync(user, "site-a", "edit");

        Assert.True(result);
    }

    [Fact]
    public async Task AuthorizeAsync_MissingManagedSite_ReturnsFalse()
    {
        var service = new ManagedSiteAuthorizationService();
        var user = CreateUser("site-a:edit");

        var result = await service.AuthorizeAsync(user, "site-b", "edit");

        Assert.False(result);
    }

    private static ClaimsPrincipal CreateUser(string managedSiteClaim)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim(ManagedSiteAuthorizationService.ManagedSiteClaimType, managedSiteClaim),
            ],
            "Test");

        return new ClaimsPrincipal(identity);
    }
}
