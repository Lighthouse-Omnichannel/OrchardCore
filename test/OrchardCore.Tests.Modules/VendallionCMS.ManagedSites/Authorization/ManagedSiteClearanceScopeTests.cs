using System.Threading.Tasks;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers what a Managed Site clearance allows on its own, and what a scope adds to it.
/// </summary>
/// <remarks>
/// Holding clearance for a Managed Site is permission to read it. There is no scope for reading, and
/// there was: a clearance could name <c>view</c> while naming nothing else, and every other scope had
/// to imply it or an editor granted only <c>edit</c> was shown a portal listing nothing. Reading moved
/// into the clearance itself, so a scope now says only what the holder may do beyond reading.
/// </remarks>
public class ManagedSiteClearanceScopeTests
{
    [Fact]
    public async Task AClearanceNamingNoScope_StillAllowsReading()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        Assert.True(await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("site-a"),
            "site-a",
            null));
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("publish")]
    [InlineData("preview")]
    public async Task AClearanceNamingNoScope_AllowsNothingFurther(string scope)
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        Assert.False(await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("site-a"),
            "site-a",
            scope));
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("publish")]
    [InlineData("preview")]
    public async Task AScope_AllowsItselfAndReading(string scope)
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));
        var user = ManagedSitesTestData.User($"site-a:{scope}");

        Assert.True(await context.ClearanceService.HasClearanceAsync(user, "site-a", scope));
        Assert.True(await context.ClearanceService.HasClearanceAsync(user, "site-a", null));
    }

    [Fact]
    public async Task OneScope_DoesNotCarryAnother()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        Assert.False(await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("site-a:edit"),
            "site-a",
            "publish"));
    }

    [Fact]
    public async Task AClearance_DoesNotReachAnotherManagedSite()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));

        var user = ManagedSitesTestData.User("site-a:edit");

        Assert.False(await context.ClearanceService.HasClearanceAsync(user, "site-b", "edit"));
        Assert.False(await context.ClearanceService.HasClearanceAsync(user, "site-b", null));
    }

    [Fact]
    public async Task TheWildcard_FromAnIssuerThatMeansEveryScope_IsHonoured()
    {
        // Nothing here writes one, but a signed claim from elsewhere may say it.
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));
        var user = ManagedSitesTestData.User("site-a:*");

        Assert.True(await context.ClearanceService.HasClearanceAsync(user, "site-a", "edit"));
        Assert.True(await context.ClearanceService.HasClearanceAsync(user, "site-a", "publish"));
    }
}
