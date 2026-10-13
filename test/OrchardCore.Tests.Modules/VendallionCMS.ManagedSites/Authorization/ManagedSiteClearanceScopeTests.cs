using System.Threading.Tasks;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers what a Managed Site clearance allows on its own, and what a scope adds to it.
/// </summary>
/// <remarks>
/// Holding clearance for a Managed Site is permission to read it. There is no scope for reading, and
/// there was: a clearance could name <c>view</c> while naming nothing else, and every other scope had
/// to imply it or an editor granted only <c>edit</c> was shown a content list holding nothing. Reading moved
/// into the clearance itself, so a scope now says only what the holder may do beyond reading.
/// </remarks>
public class ManagedSiteClearanceScopeTests
{
    [Fact]
    public async Task AClearanceNamingNoScope_StillAllowsReading()
    {
        var context = new ManagedSiteContentTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        Assert.True(await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("sitea"),
            "sitea",
            null));
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("publish")]
    [InlineData("preview")]
    public async Task AClearanceNamingNoScope_AllowsNothingFurther(string scope)
    {
        var context = new ManagedSiteContentTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        Assert.False(await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("sitea"),
            "sitea",
            scope));
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("publish")]
    [InlineData("preview")]
    public async Task AScope_AllowsItselfAndReading(string scope)
    {
        var context = new ManagedSiteContentTestContext(ManagedSitesTestData.ManagedSite("sitea"));
        var user = ManagedSitesTestData.User($"sitea:{scope}");

        Assert.True(await context.ClearanceService.HasClearanceAsync(user, "sitea", scope));
        Assert.True(await context.ClearanceService.HasClearanceAsync(user, "sitea", null));
    }

    [Fact]
    public async Task OneScope_DoesNotCarryAnother()
    {
        var context = new ManagedSiteContentTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        Assert.False(await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("sitea:edit"),
            "sitea",
            "publish"));
    }

    [Fact]
    public async Task AClearance_DoesNotReachAnotherManagedSite()
    {
        var context = new ManagedSiteContentTestContext(
            ManagedSitesTestData.ManagedSite("sitea"),
            ManagedSitesTestData.ManagedSite("siteb"));

        var user = ManagedSitesTestData.User("sitea:edit");

        Assert.False(await context.ClearanceService.HasClearanceAsync(user, "siteb", "edit"));
        Assert.False(await context.ClearanceService.HasClearanceAsync(user, "siteb", null));
    }

    [Fact]
    public async Task TheWildcard_FromAnIssuerThatMeansEveryScope_IsHonoured()
    {
        // Nothing here writes one, but a signed claim from elsewhere may say it.
        var context = new ManagedSiteContentTestContext(ManagedSitesTestData.ManagedSite("sitea"));
        var user = ManagedSitesTestData.User("sitea:*");

        Assert.True(await context.ClearanceService.HasClearanceAsync(user, "sitea", "edit"));
        Assert.True(await context.ClearanceService.HasClearanceAsync(user, "sitea", "publish"));
    }
}
