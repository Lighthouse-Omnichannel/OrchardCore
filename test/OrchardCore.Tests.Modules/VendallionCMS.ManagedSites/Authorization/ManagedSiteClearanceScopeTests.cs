using System.Threading.Tasks;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers what one Managed Site clearance scope grants on its own.
/// </summary>
/// <remarks>
/// Every scope that changes or previews a Managed Site's content implies being able to read it. A
/// clearance granting edit but not view listed nothing in the portal, so the user it was meant to
/// empower could not reach a single item to edit, and the first screen they were shown refused them.
///
/// The clearance editor ticks view for a grant with no action chosen, which reads as though view is the
/// baseline. It was not: choosing any action replaced that baseline rather than adding to it.
/// </remarks>
public class ManagedSiteClearanceScopeTests
{
    [Theory]
    [InlineData("edit")]
    [InlineData("publish")]
    [InlineData("preview")]
    public async Task AScopeThatActsOnContent_AlsoGrantsReadingIt(string scope)
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        Assert.True(await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User($"site-a:{scope}"),
            "site-a",
            "view"));
    }

    [Fact]
    public async Task ViewAlone_StillGrantsNothingFurther()
    {
        // Guards the implication: it must run one way only.
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        Assert.False(await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("site-a:view"),
            "site-a",
            "edit"));
    }

    [Fact]
    public async Task TheImplication_DoesNotReachAnotherManagedSite()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));

        Assert.False(await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("site-a:edit"),
            "site-b",
            "view"));
    }
}
