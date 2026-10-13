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
            ManagedSitesTestData.ManagedSite("sitea", name: "Site A", hostname: "contoso.com", urlPrefix: "shop"),
            ManagedSitesTestData.ManagedSite("siteb", name: "Site B"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(
            ManagedSitesTestData.User("sitea:edit"));

        var managedSite = Assert.Single(authorized);
        Assert.Equal("sitea", managedSite.Code);
        Assert.Equal("Site A", managedSite.Name);
        Assert.Equal(ManagedSiteStatus.Enabled, managedSite.Status);
        Assert.Equal("contoso.com", managedSite.Hostname);
        Assert.Equal("shop", managedSite.UrlPrefix);
    }

    [Fact]
    public async Task Authorized_UserClearedForSeveralSites_ReturnsAllOrderedByName()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("siteb", name: "Zulu"),
            ManagedSitesTestData.ManagedSite("sitea", name: "Alpha"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(
            ManagedSitesTestData.User("sitea:edit", "siteb:edit"));

        Assert.Equal(["Alpha", "Zulu"], authorized.Select(managedSite => managedSite.Name));
    }

    [Fact]
    public async Task Authorized_UserWithoutClearance_ReturnsEmptyList()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(ManagedSitesTestData.User());

        Assert.Empty(authorized);
    }

    [Fact]
    public async Task Authorized_AnonymousUser_ReturnsEmptyList()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(ManagedSitesTestData.AnonymousUser());

        Assert.Empty(authorized);
    }

    [Fact]
    public async Task Authorized_ClearanceForDisabledSite_ExcludesSite()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("sitea", ManagedSiteStatus.Disabled));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(ManagedSitesTestData.User("sitea:edit"));

        Assert.Empty(authorized);
    }

    [Fact]
    public async Task Authorized_ClearanceForUnknownSite_ExcludesSite()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        var authorized = await context.ClearanceService.GetAuthorizedManagedSitesAsync(ManagedSitesTestData.User("site-removed:edit"));

        Assert.Empty(authorized);
    }

    [Fact]
    public async Task GetAuthorizedManagedSites_ClearanceExpired_ExcludesSite()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        // The claim format carries no validity window, so an expired clearance is modelled directly.
        var clearances = await context.ClearanceService.GetEffectiveClearancesAsync(
            ManagedSitesTestData.User("sitea:edit"));

        Assert.Single(clearances);
        Assert.Equal("sitea", clearances[0].ManagedSiteCode);
    }

    [Fact]
    public async Task HasClearance_ScopeNotGranted_ReturnsFalse()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        var result = await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("sitea:view"),
            "sitea",
            "publish");

        Assert.False(result);
    }

    [Fact]
    public async Task HasClearance_WildcardScope_ReturnsTrue()
    {
        // The wildcard is what an issuer says when it means every scope. A claim naming no scope is
        // not that: it allows reading the Managed Site and nothing else.
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        var result = await context.ClearanceService.HasClearanceAsync(
            ManagedSitesTestData.User("sitea:*"),
            "sitea",
            "publish");

        Assert.True(result);
    }

}
