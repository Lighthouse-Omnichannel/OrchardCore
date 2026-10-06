using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VendallionCMS.ManagedSites.Controllers;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

public class ActiveManagedSiteSessionTests
{
    [Fact]
    public async Task ResolveAsync_SingleAuthorizedSite_AutoSelectsIt()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        var result = await context.SessionService.ResolveAsync(ManagedSitesTestData.User("site-a:edit"));

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("site-a", result.Scope.ManagedSiteId);
        Assert.Equal(ManagedSitesTestData.UserId, result.Scope.UserId);
        Assert.Equal(1, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task ResolveAsync_SeveralAuthorizedSites_RequiresSelection()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));

        var result = await context.SessionService.ResolveAsync(
            ManagedSitesTestData.User("site-a:edit", "site-b:edit"));

        Assert.Equal(ManagedSiteSessionStatus.RequiresSelection, result.Status);
        Assert.Null(result.Scope);
        Assert.Equal(2, result.AuthorizedManagedSites.Count);
        Assert.Equal(0, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task ResolveAsync_NoClearance_ReturnsNoClearance()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        var result = await context.SessionService.ResolveAsync(ManagedSitesTestData.User());

        Assert.Equal(ManagedSiteSessionStatus.NoClearance, result.Status);
        Assert.Empty(result.AuthorizedManagedSites);
    }

    [Fact]
    public async Task ResolveAsync_StoredScopeStillAuthorized_KeepsSelectionWithoutRewriting()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));
        context.SessionStore.Seed(ManagedSitesTestData.UserId, "site-b");

        var result = await context.SessionService.ResolveAsync(
            ManagedSitesTestData.User("site-a:edit", "site-b:edit"));

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("site-b", result.Scope.ManagedSiteId);
        Assert.Equal(0, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task ResolveAsync_StoredScopeNoLongerCleared_ClearsAndRequiresSelection()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"),
            ManagedSitesTestData.ManagedSite("site-c"));
        context.SessionStore.Seed(ManagedSitesTestData.UserId, "site-c");

        var result = await context.SessionService.ResolveAsync(
            ManagedSitesTestData.User("site-a:edit", "site-b:edit"));

        Assert.Equal(ManagedSiteSessionStatus.RequiresSelection, result.Status);
        Assert.Equal(1, context.SessionStore.ClearCount);
    }

    [Fact]
    public async Task ResolveAsync_StoredScopeDisabled_FallsBackToRemainingSite()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b", ManagedSiteStatus.Disabled));
        context.SessionStore.Seed(ManagedSitesTestData.UserId, "site-b");

        var result = await context.SessionService.ResolveAsync(
            ManagedSitesTestData.User("site-a:edit", "site-b:edit"));

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("site-a", result.Scope.ManagedSiteId);
    }

    [Fact]
    public async Task SelectAsync_AuthorizedSite_RecordsActiveScope()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));

        var result = await context.SessionService.SelectAsync(
            ManagedSitesTestData.User("site-a:edit", "site-b:edit"),
            "site-b");

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("site-b", result.Scope.ManagedSiteId);

        var stored = await context.SessionStore.GetAsync(ManagedSitesTestData.UserId);
        Assert.Equal("site-b", stored.ManagedSiteId);
    }

    [Fact]
    public async Task SelectAsync_SiteOutsideClearance_ReturnsForbidden()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));

        var result = await context.SessionService.SelectAsync(ManagedSitesTestData.User("site-a:edit"), "site-b");

        Assert.Equal(ManagedSiteSessionStatus.Forbidden, result.Status);
        Assert.Equal(0, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task SelectAsync_DisabledSite_ReturnsUnavailable()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b", ManagedSiteStatus.Disabled));

        var result = await context.SessionService.SelectAsync(
            ManagedSitesTestData.User("site-a:edit", "site-b:edit"),
            "site-b");

        Assert.Equal(ManagedSiteSessionStatus.Unavailable, result.Status);
        Assert.Equal(0, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task SelectAsync_NoClearance_ReturnsNoClearance()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        var result = await context.SessionService.SelectAsync(ManagedSitesTestData.User(), "site-a");

        Assert.Equal(ManagedSiteSessionStatus.NoClearance, result.Status);
    }
}
