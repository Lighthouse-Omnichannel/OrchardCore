using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VendallionCMS.ManagedSites.Controllers;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers the rule that the route scope, optional consistency header, signed clearance, and active
/// session scope must all refer to the same authorized Managed Site.
/// </summary>
public class ManagedSiteApiScopeMismatchTests
{
    [Fact]
    public async Task Validate_AllScopesAgree_Succeeds()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit"));
        controller.HttpContext.Request.Headers[ManagedSitesConstants.Headers.ManagedSiteId] = "site-a";

        var failure = await controller.ValidateAsync("site-a", ManagedSitesConstants.Scopes.Edit);

        Assert.Null(failure);
    }

    [Fact]
    public async Task Validate_NoHeaderSupplied_Succeeds()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit"));

        var failure = await controller.ValidateAsync("site-a", ManagedSitesConstants.Scopes.Edit);

        Assert.Null(failure);
    }

    [Fact]
    public async Task Validate_HeaderDisagreesWithRoute_ReturnsConflict()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit", "site-b:edit"));
        controller.HttpContext.Request.Headers[ManagedSitesConstants.Headers.ManagedSiteId] = "site-b";

        var problem = AssertProblem(await controller.ValidateAsync("site-a"), StatusCodes.Status409Conflict);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.ScopeMismatch, problem.Code);
    }

    [Fact]
    public async Task Validate_RouteOutsideClearance_ReturnsForbidden()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit"));

        var problem = AssertProblem(await controller.ValidateAsync("site-b"), StatusCodes.Status403Forbidden);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.NoClearance, problem.Code);
    }

    [Fact]
    public async Task Validate_ScopeNotGranted_ReturnsForbidden()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:view"));

        var problem = AssertProblem(
            await controller.ValidateAsync("site-a", ManagedSitesConstants.Scopes.Publish),
            StatusCodes.Status403Forbidden);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.NoClearance, problem.Code);
    }

    [Fact]
    public async Task Validate_AnonymousCaller_ReturnsForbidden()
    {
        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));
        var controller = CreateController(context, ManagedSitesTestData.AnonymousUser());

        var problem = AssertProblem(await controller.ValidateAsync("site-a"), StatusCodes.Status403Forbidden);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.NoClearance, problem.Code);
    }

    [Fact]
    public async Task Validate_MultiSiteUserWithNoActiveSelection_ReturnsSelectionRequired()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit", "site-b:edit"));

        var problem = AssertProblem(await controller.ValidateAsync("site-a"), StatusCodes.Status409Conflict);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.SelectionRequired, problem.Code);
    }

    [Fact]
    public async Task Validate_ActiveSessionScopeDiffersFromRoute_ReturnsConflict()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));
        context.SessionStore.Seed(ManagedSitesTestData.UserId, "site-b");
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit", "site-b:edit"));

        var problem = AssertProblem(await controller.ValidateAsync("site-a"), StatusCodes.Status409Conflict);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.SessionScopeMismatch, problem.Code);
    }

    [Fact]
    public async Task Validate_ClearanceRevokedAfterSelection_ReturnsForbidden()
    {
        var context = new ManagedSitePortalTestContext(
            ManagedSitesTestData.ManagedSite("site-a"),
            ManagedSitesTestData.ManagedSite("site-b"));
        context.SessionStore.Seed(ManagedSitesTestData.UserId, "site-b");

        // The session still points at site-b, but the current token no longer carries clearance for it.
        var controller = CreateController(context, ManagedSitesTestData.User("site-a:edit"));

        var problem = AssertProblem(await controller.ValidateAsync("site-b"), StatusCodes.Status403Forbidden);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.NoClearance, problem.Code);
    }

    private static TestScopedApiController CreateController(
        ManagedSitePortalTestContext context,
        ClaimsPrincipal user)
        => new(context.ClearanceService, context.SessionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            },
        };

    private static ManagedSitesApiProblem AssertProblem(IActionResult result, int expectedStatus)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);

        var problem = Assert.IsType<ManagedSitesApiProblem>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);

        return problem;
    }

    /// <summary>
    /// Exposes the protected scope validation so the shared rule can be tested once.
    /// </summary>
    private sealed class TestScopedApiController : ManagedSitesApiControllerBase
    {
        public TestScopedApiController(
            IManagedSiteClearanceService clearanceService,
            IManagedSiteSessionService sessionService)
            : base(clearanceService, sessionService)
        {
        }

        public async Task<IActionResult> ValidateAsync(string managedSiteId, string scope = null)
        {
            var validation = await ValidateManagedSiteScopeAsync(managedSiteId, scope);

            return validation.Succeeded ? null : validation.Failure;
        }
    }
}
