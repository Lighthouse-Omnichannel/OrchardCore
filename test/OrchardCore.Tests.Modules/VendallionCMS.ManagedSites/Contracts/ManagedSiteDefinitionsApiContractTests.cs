using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VendallionCMS.ManagedSites.Controllers;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Contracts;

/// <summary>
/// Checks the Managed Site definitions endpoint against the statuses the API contract promises.
/// </summary>
public class ManagedSiteDefinitionsApiContractTests
{
    [Fact]
    public async Task Put_NewDefinition_ReturnsOkWithSummary()
    {
        var context = new DefinitionsApiTestContext();

        var result = await context.Controller.Put("site-a", Request("Site A", "Enabled", "contoso.com", "shop"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var summary = Assert.IsType<ManagedSiteSummary>(ok.Value);
        Assert.Equal("site-a", summary.Id);
        Assert.Equal("Site A", summary.Name);
        Assert.Equal("Enabled", summary.Status);
        Assert.Equal("contoso.com", summary.Hostname);
        Assert.Equal("shop", summary.UrlPrefix);
    }

    [Fact]
    public async Task Put_ExistingDefinition_ReplacesIt()
    {
        var context = new DefinitionsApiTestContext();
        await context.Controller.Put("site-a", Request("Site A", "Enabled", "contoso.com", "shop"));

        await context.Controller.Put("site-a", Request("Site A", "Disabled", "fabrikam.com", "news"));

        var stored = await context.ManagedSiteService.GetAsync("site-a");
        Assert.Equal("fabrikam.com", stored.Hostname);
        Assert.Equal("news", stored.UrlPrefix);
    }

    [Fact]
    public async Task Put_MissingName_ReturnsBadRequest()
    {
        var context = new DefinitionsApiTestContext();

        var problem = AssertProblem(
            await context.Controller.Put("site-a", Request(null, "Enabled")),
            StatusCodes.Status400BadRequest);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.InvalidName, problem.Code);
    }

    [Fact]
    public async Task Put_UnknownStatus_ReturnsBadRequest()
    {
        var context = new DefinitionsApiTestContext();

        AssertProblem(
            await context.Controller.Put("site-a", Request("Site A", "Whatever")),
            StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Put_UrlOwnedByAnotherManagedSite_ReturnsConflict()
    {
        var context = new DefinitionsApiTestContext();
        await context.Controller.Put("site-a", Request("Site A", "Enabled", "contoso.com", "shop"));

        var problem = AssertProblem(
            await context.Controller.Put("site-b", Request("Site B", "Enabled", "contoso.com", "shop")),
            StatusCodes.Status409Conflict);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.UrlConflict, problem.Code);
    }

    [Fact]
    public async Task Put_NameAlreadyUsed_ReturnsConflict()
    {
        var context = new DefinitionsApiTestContext();
        await context.Controller.Put("site-a", Request("Shared", "Enabled"));

        var problem = AssertProblem(
            await context.Controller.Put("site-b", Request("Shared", "Enabled")),
            StatusCodes.Status409Conflict);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.NameConflict, problem.Code);
    }

    [Fact]
    public async Task Put_WithoutPermission_ReturnsForbiddenAndStoresNothing()
    {
        var context = new DefinitionsApiTestContext(isAuthorized: false);

        AssertProblem(
            await context.Controller.Put("site-a", Request("Site A", "Enabled")),
            StatusCodes.Status403Forbidden);

        Assert.Null(await context.ManagedSiteService.GetAsync("site-a"));
    }

    [Fact]
    public async Task Delete_ExistingDefinition_ReturnsNoContent()
    {
        var context = new DefinitionsApiTestContext();
        await context.Controller.Put("site-a", Request("Site A", "Enabled", "contoso.com"));

        Assert.IsType<NoContentResult>(await context.Controller.Delete("site-a"));
        Assert.Null(await context.ManagedSiteService.GetAsync("site-a"));
    }

    [Fact]
    public async Task Delete_UnknownDefinition_ReturnsNotFound()
    {
        var context = new DefinitionsApiTestContext();

        AssertProblem(await context.Controller.Delete("missing"), StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Delete_WithoutPermission_ReturnsForbiddenAndKeepsDefinition()
    {
        var context = new DefinitionsApiTestContext();
        await context.Controller.Put("site-a", Request("Site A", "Enabled"));

        var denied = new DefinitionsApiTestContext(isAuthorized: false, siteService: context.SiteService);

        AssertProblem(await denied.Controller.Delete("site-a"), StatusCodes.Status403Forbidden);
        Assert.NotNull(await context.ManagedSiteService.GetAsync("site-a"));
    }

    private static ManagedSiteDefinitionRequest Request(
        string name,
        string status,
        string hostname = null,
        string urlPrefix = null)
        => new()
        {
            Name = name,
            Status = status,
            Hostname = hostname,
            UrlPrefix = urlPrefix,
        };

    private static ManagedSitesApiProblem AssertProblem(IActionResult result, int expectedStatus)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);

        var problem = Assert.IsType<ManagedSitesApiProblem>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);

        return problem;
    }

    private sealed class DefinitionsApiTestContext
    {
        public DefinitionsApiTestContext(bool isAuthorized = true, FakeSiteService siteService = null)
        {
            SiteService = siteService ?? new FakeSiteService();
            ShellSynchronization = new FakeShellUrlSynchronizationService();
            ManagedSiteService = new ManagedSiteService(SiteService, ShellSynchronization);

            var portal = new ManagedSitePortalTestContext();

            Controller = new ManagedSiteDefinitionsApiController(
                ManagedSiteService,
                new StubAuthorizationService(isAuthorized),
                portal.ClearanceService,
                portal.SessionService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = ManagedSitesTestData.User() },
                },
            };
        }

        public FakeSiteService SiteService { get; }

        public FakeShellUrlSynchronizationService ShellSynchronization { get; }

        public ManagedSiteService ManagedSiteService { get; }

        public ManagedSiteDefinitionsApiController Controller { get; }
    }
}
