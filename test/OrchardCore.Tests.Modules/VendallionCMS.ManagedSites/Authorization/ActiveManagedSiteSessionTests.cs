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

    [Fact]
    public async Task SessionEndpoint_SingleAuthorizedSite_ReturnsAutoSelectedScope()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit"));

        var result = Assert.IsType<OkObjectResult>(await controller.Current());
        var response = Assert.IsType<ManagedSiteSessionResponse>(result.Value);

        Assert.Equal("site-a", response.ManagedSiteId);
        Assert.False(response.RequiresSelection);
    }

    [Fact]
    public async Task SessionEndpoint_SeveralAuthorizedSites_RequiresSelection()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit", "site-b:edit"));

        var result = Assert.IsType<OkObjectResult>(await controller.Current());
        var response = Assert.IsType<ManagedSiteSessionResponse>(result.Value);

        Assert.Null(response.ManagedSiteId);
        Assert.True(response.RequiresSelection);
        Assert.Equal(["site-a", "site-b"], response.AuthorizedManagedSites.Select(item => item.Id));
    }

    [Fact]
    public async Task SessionEndpoint_NoClearance_ReturnsForbiddenProblem()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));
        var controller = CreateController(context, ManagedSitesTestData.User());

        var problem = await AssertProblemAsync(await controller.Current(), StatusCodes.Status403Forbidden);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.NoClearance, problem.Code);
    }

    [Fact]
    public async Task SelectEndpoint_SiteOutsideClearance_ReturnsForbiddenProblem()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit"));

        var problem = await AssertProblemAsync(
            await controller.Select(new SelectManagedSiteRequest { ManagedSiteId = "site-b" }),
            StatusCodes.Status403Forbidden);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.NoClearance, problem.Code);
    }

    [Fact]
    public async Task SelectEndpoint_DisabledSite_ReturnsConflictProblem()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b", ManagedSiteStatus.Disabled));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit", "site-b:edit"));

        var problem = await AssertProblemAsync(
            await controller.Select(new SelectManagedSiteRequest { ManagedSiteId = "site-b" }),
            StatusCodes.Status409Conflict);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.ManagedSiteUnavailable, problem.Code);
    }

    [Fact]
    public async Task SelectEndpoint_HeaderDisagreesWithBody_ReturnsConflictProblem()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit", "site-b:edit"));
        controller.HttpContext.Request.Headers[ManagedSitesConstants.Headers.ManagedSiteId] = "site-a";

        var problem = await AssertProblemAsync(
            await controller.Select(new SelectManagedSiteRequest { ManagedSiteId = "site-b" }),
            StatusCodes.Status409Conflict);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.ScopeMismatch, problem.Code);
        Assert.Equal(0, context.SessionStore.SetCount);
    }

    private static ManagedSiteSessionApiController CreateController(
        ManagedSitePortalTestContext context,
        ClaimsPrincipal user)
        => new(context.ClearanceService, context.SessionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            },
        };

    private static Task<ManagedSitesApiProblem> AssertProblemAsync(IActionResult result, int expectedStatus)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);

        var problem = Assert.IsType<ManagedSitesApiProblem>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);

        return Task.FromResult(problem);
    }
}
