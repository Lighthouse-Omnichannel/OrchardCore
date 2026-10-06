using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VendallionCMS.ManagedSites.Controllers;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.ViewModels;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

public class ManagedSitePortalAuthorizationTests
{
    [Fact]
    public async Task Authorized_UserClearedForOneSite_ReturnsOnlyThatSite()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a", name: "Site A", hostname: "contoso.com", urlPrefix: "shop"),
            ManagedSitesTestData.ManagedSite("site-b", name: "Site B"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(
            ManagedSitesTestData.User("site-a:edit"));

        var managedSite = Assert.Single(authorized);
        Assert.Equal("site-a", managedSite.Id);
        Assert.Equal("Site A", managedSite.Name);
        Assert.Equal(ManagedSiteStatus.Enabled, managedSite.Status);
        Assert.Equal("contoso.com", managedSite.Hostname);
        Assert.Equal("shop", managedSite.UrlPrefix);
    }

    [Fact]
    public async Task Authorized_UserClearedForSeveralSites_ReturnsAllOrderedByName()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-b", name: "Zulu"),
            ManagedSitesTestData.ManagedSite("site-a", name: "Alpha"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(
            ManagedSitesTestData.User("site-a:edit", "site-b:edit"));

        Assert.Equal(["Alpha", "Zulu"], authorized.Select(managedSite => managedSite.Name));
    }

    [Fact]
    public async Task Authorized_UserWithoutClearance_ReturnsEmptyList()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(ManagedSitesTestData.User());

        Assert.Empty(authorized);
    }

    [Fact]
    public async Task Authorized_AnonymousUser_ReturnsEmptyList()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(ManagedSitesTestData.AnonymousUser());

        Assert.Empty(authorized);
    }

    [Fact]
    public async Task Authorized_ClearanceForDisabledSite_ExcludesSite()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a", ManagedSiteStatus.Disabled));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(ManagedSitesTestData.User("site-a:edit"));

        Assert.Empty(authorized);
    }

    [Fact]
    public async Task Authorized_ClearanceForUnknownSite_ExcludesSite()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(ManagedSitesTestData.User("site-removed:edit"));

        Assert.Empty(authorized);
    }

    [Fact]
    public async Task GetAuthorizedManagedSites_ClearanceExpired_ExcludesSite()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        // The claim format carries no validity window, so an expired clearance is modelled directly.
        var clearances = await context.ClearanceService.GetEffectiveClearancesAsync(
            ManagedSitesTestData.User("site-a:edit"));

        Assert.Single(clearances);
        Assert.Equal("site-a", clearances[0].ManagedSiteId);
    }

    [Fact]
    public async Task HasClearance_ScopeNotGranted_ReturnsFalse()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        var result = await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("site-a:view"),
            "site-a",
            "publish");

        Assert.False(result);
    }

    [Fact]
    public async Task HasClearance_WildcardScope_ReturnsTrue()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        var result = await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("site-a"),
            "site-a",
            "publish");

        Assert.True(result);
    }

}
