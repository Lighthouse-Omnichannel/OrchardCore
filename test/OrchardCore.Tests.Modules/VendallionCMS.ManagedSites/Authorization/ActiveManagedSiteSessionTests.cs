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
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        var result = await context.SessionService.ResolveAsync(ManagedSitesTestData.User("sitea:edit"));

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("sitea", result.Scope.ManagedSiteCode);
        Assert.Equal(ManagedSitesTestData.UserId, result.Scope.UserId);
        Assert.Equal(1, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task ResolveAsync_SeveralAuthorizedSites_RequiresSelection()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("sitea"),
            ManagedSitesTestData.ManagedSite("siteb"));

        var result = await context.SessionService.ResolveAsync(
            ManagedSitesTestData.User("sitea:edit", "siteb:edit"));

        Assert.Equal(ManagedSiteSessionStatus.RequiresSelection, result.Status);
        Assert.Null(result.Scope);
        Assert.Equal(2, result.AuthorizedManagedSites.Count);
        Assert.Equal(0, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task ResolveAsync_NoClearance_ReturnsNoClearance()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        var result = await context.SessionService.ResolveAsync(ManagedSitesTestData.User());

        Assert.Equal(ManagedSiteSessionStatus.NoClearance, result.Status);
        Assert.Empty(result.AuthorizedManagedSites);
    }

    [Fact]
    public async Task ResolveAsync_StoredScopeStillAuthorized_KeepsSelectionWithoutRewriting()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("sitea"),
            ManagedSitesTestData.ManagedSite("siteb"));
        context.SessionStore.Seed(ManagedSitesTestData.UserId, "siteb");

        var result = await context.SessionService.ResolveAsync(
            ManagedSitesTestData.User("sitea:edit", "siteb:edit"));

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("siteb", result.Scope.ManagedSiteCode);
        Assert.Equal(0, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task ResolveAsync_StoredScopeNoLongerCleared_ClearsAndRequiresSelection()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("sitea"),
            ManagedSitesTestData.ManagedSite("siteb"),
            ManagedSitesTestData.ManagedSite("sitec"));
        context.SessionStore.Seed(ManagedSitesTestData.UserId, "sitec");

        var result = await context.SessionService.ResolveAsync(
            ManagedSitesTestData.User("sitea:edit", "siteb:edit"));

        Assert.Equal(ManagedSiteSessionStatus.RequiresSelection, result.Status);
        Assert.Equal(1, context.SessionStore.ClearCount);
    }

    [Fact]
    public async Task ResolveAsync_StoredScopeDisabled_FallsBackToRemainingSite()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("sitea"),
            ManagedSitesTestData.ManagedSite("siteb", ManagedSiteStatus.Disabled));
        context.SessionStore.Seed(ManagedSitesTestData.UserId, "siteb");

        var result = await context.SessionService.ResolveAsync(
            ManagedSitesTestData.User("sitea:edit", "siteb:edit"));

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("sitea", result.Scope.ManagedSiteCode);
    }

    [Fact]
    public async Task SelectAsync_AuthorizedSite_RecordsActiveScope()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("sitea"),
            ManagedSitesTestData.ManagedSite("siteb"));

        var result = await context.SessionService.SelectAsync(
            ManagedSitesTestData.User("sitea:edit", "siteb:edit"),
            "siteb");

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("siteb", result.Scope.ManagedSiteCode);

        var stored = await context.SessionStore.GetAsync(ManagedSitesTestData.UserId);
        Assert.Equal("siteb", stored.ManagedSiteCode);
    }

    [Fact]
    public async Task SelectAsync_SiteOutsideClearance_ReturnsForbidden()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("sitea"),
            ManagedSitesTestData.ManagedSite("siteb"));

        var result = await context.SessionService.SelectAsync(ManagedSitesTestData.User("sitea:edit"), "siteb");

        Assert.Equal(ManagedSiteSessionStatus.Forbidden, result.Status);
        Assert.Equal(0, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task SelectAsync_DisabledSite_ReturnsUnavailable()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("sitea"),
            ManagedSitesTestData.ManagedSite("siteb", ManagedSiteStatus.Disabled));

        var result = await context.SessionService.SelectAsync(
            ManagedSitesTestData.User("sitea:edit", "siteb:edit"),
            "siteb");

        Assert.Equal(ManagedSiteSessionStatus.Unavailable, result.Status);
        Assert.Equal(0, context.SessionStore.SetCount);
    }

    [Fact]
    public async Task SelectAsync_NoClearance_ReturnsNoClearance()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        var result = await context.SessionService.SelectAsync(ManagedSitesTestData.User(), "sitea");

        Assert.Equal(ManagedSiteSessionStatus.NoClearance, result.Status);
    }
}
