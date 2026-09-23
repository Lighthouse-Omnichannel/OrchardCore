using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using VendallionCMS.ManagedSites.Controllers;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.Tests.ManagedContent;
using VendallionCMS.ManagedSites.ViewModels;
using Xunit;
using ISession = YesSql.ISession;

namespace VendallionCMS.ManagedSites.Tests.Contracts;

/// <summary>
/// Checks the Managed Content endpoints against the statuses and bodies the API contract promises.
/// </summary>
/// <remarks>
/// Only the endpoints that read one item or write an override are covered. The listing endpoint queries
/// the edit scope index, which needs a real document session rather than a double.
/// </remarks>
public class ManagedContentApiContractTests
{
    [Fact]
    public async Task Detail_EditableItem_ReturnsTheItemAndItsOverride()
    {
        var context = new ManagedContentApiTestContext();
        context.WithSource(ManagedContentScope.Selected("site-a"));
        context.Overrides.WithPublished("site-a", "source-item", ManagedContentTestContent.Override());

        var ok = Assert.IsType<OkObjectResult>(await context.Controller.Detail("site-a", "source-item"));
        var detail = Assert.IsType<ManagedContentDetailResponse>(ok.Value);

        Assert.Equal("source-item", detail.SourceContentItemId);
        Assert.Equal(ManagedContentTestContent.ContentType, detail.ContentType);
        Assert.True(detail.EditScopeIncludesManagedSite);
        Assert.True(detail.DisplayScopeIncludesManagedSite);
        Assert.Equal("override-item", detail.Override.OverrideContentItemId);
        Assert.Equal("Published", detail.Override.Status);
        Assert.Null(detail.Override.SuppressionReason);
    }

    [Fact]
    public async Task Detail_ItemWithoutAnOverride_ReportsNone()
    {
        var context = new ManagedContentApiTestContext();
        context.WithSource(ManagedContentScope.All());

        var ok = Assert.IsType<OkObjectResult>(await context.Controller.Detail("site-a", "source-item"));

        Assert.Null(Assert.IsType<ManagedContentDetailResponse>(ok.Value).Override);
    }

    [Fact]
    public async Task Detail_UnknownItem_ReturnsNotFound()
    {
        var context = new ManagedContentApiTestContext();

        var problem = AssertProblem(
            await context.Controller.Detail("site-a", "source-item"),
            StatusCodes.Status404NotFound);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.SourceNotFound, problem.Code);
    }

    [Fact]
    public async Task Detail_ItemOutsideTheEditScope_ReturnsForbidden()
    {
        var context = new ManagedContentApiTestContext();
        context.WithSource(ManagedContentScope.Selected("site-b"));

        var problem = AssertProblem(
            await context.Controller.Detail("site-a", "source-item"),
            StatusCodes.Status403Forbidden);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.EditScopeExcluded, problem.Code);
    }

    [Fact]
    public async Task Detail_WithoutClearanceForTheManagedSite_ReturnsForbidden()
    {
        var context = new ManagedContentApiTestContext(clearance: "site-b:view");

        AssertProblem(
            await context.Controller.Detail("site-a", "source-item"),
            StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task PutOverride_Draft_ReturnsTheStoredOverride()
    {
        var context = new ManagedContentApiTestContext();

        var ok = Assert.IsType<OkObjectResult>(
            await context.Controller.PutOverride("site-a", "source-item", Request("override-item", "Draft")));

        var summary = Assert.IsType<ManagedContentOverrideSummary>(ok.Value);
        Assert.Equal("override-item", summary.OverrideContentItemId);
        Assert.Equal("Draft", summary.Status);
        Assert.Null(summary.SuppressionReason);
    }

    [Fact]
    public async Task PutOverride_WithoutAStatus_DefaultsToDraft()
    {
        // Defaulting to published would put unreviewed content in front of visitors on a bare request.
        var context = new ManagedContentApiTestContext();

        var ok = Assert.IsType<OkObjectResult>(
            await context.Controller.PutOverride("site-a", "source-item", Request("override-item", status: null)));

        Assert.Equal("Draft", Assert.IsType<ManagedContentOverrideSummary>(ok.Value).Status);
    }

    [Fact]
    public async Task PutOverride_WithoutAContentItem_ReturnsBadRequest()
    {
        var context = new ManagedContentApiTestContext();

        AssertProblem(
            await context.Controller.PutOverride("site-a", "source-item", Request(null, "Draft")),
            StatusCodes.Status400BadRequest);
    }

    [Theory]
    [InlineData("Whatever")]
    [InlineData("Suppressed")]
    public async Task PutOverride_WithAStatusAnOverrideCannotBeSavedWith_ReturnsBadRequest(string status)
    {
        // Suppressed is a state the system derives, never one an editor can ask for.
        var context = new ManagedContentApiTestContext();

        var problem = AssertProblem(
            await context.Controller.PutOverride("site-a", "source-item", Request("override-item", status)),
            StatusCodes.Status400BadRequest);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.InvalidOverrideStatus, problem.Code);
    }

    [Fact]
    public async Task PutOverride_PublishingWithoutPublishClearance_ReturnsForbidden()
    {
        var context = new ManagedContentApiTestContext(clearance: "site-a:view,edit");

        AssertProblem(
            await context.Controller.PutOverride("site-a", "source-item", Request("override-item", "Published")),
            StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task PutOverride_DraftWithEditClearanceAlone_IsAllowed()
    {
        var context = new ManagedContentApiTestContext(clearance: "site-a:view,edit");

        Assert.IsType<OkObjectResult>(
            await context.Controller.PutOverride("site-a", "source-item", Request("override-item", "Draft")));
    }

    [Theory]
    [InlineData(ManagedContentOverrideError.ManagedSiteUnavailable, StatusCodes.Status409Conflict)]
    [InlineData(ManagedContentOverrideError.SourceNotFound, StatusCodes.Status404NotFound)]
    [InlineData(ManagedContentOverrideError.SourceNotManagedContent, StatusCodes.Status409Conflict)]
    [InlineData(ManagedContentOverrideError.EditScopeExcluded, StatusCodes.Status403Forbidden)]
    [InlineData(ManagedContentOverrideError.OverrideNotFound, StatusCodes.Status404NotFound)]
    [InlineData(ManagedContentOverrideError.ContentTypeMismatch, StatusCodes.Status400BadRequest)]
    [InlineData(ManagedContentOverrideError.OverrideAlreadyExists, StatusCodes.Status409Conflict)]
    public async Task PutOverride_RefusedSave_MapsToTheContractStatus(
        ManagedContentOverrideError error,
        int expectedStatus)
    {
        var context = new ManagedContentApiTestContext();
        context.Overrides.FailWith(error);

        AssertProblem(
            await context.Controller.PutOverride("site-a", "source-item", Request("override-item", "Draft")),
            expectedStatus);
    }

    [Fact]
    public async Task DeleteOverride_ExistingOverride_ReturnsNoContent()
    {
        var context = new ManagedContentApiTestContext();
        context.Overrides.WithPublished("site-a", "source-item", ManagedContentTestContent.Override());

        Assert.IsType<NoContentResult>(await context.Controller.DeleteOverride("site-a", "source-item"));
    }

    [Fact]
    public async Task DeleteOverride_WithoutAnOverride_ReturnsNotFound()
    {
        var context = new ManagedContentApiTestContext();

        var problem = AssertProblem(
            await context.Controller.DeleteOverride("site-a", "source-item"),
            StatusCodes.Status404NotFound);

        Assert.Equal(ManagedSitesConstants.ErrorCodes.OverrideNotFound, problem.Code);
    }

    [Fact]
    public async Task Suppressed_ReturnsEveryOverrideWithItsReason()
    {
        var context = new ManagedContentApiTestContext();
        context.Overrides.WithSuppressed("source-item", ManagedContentOverrideSuppressionReason.EditScopeRemoved);

        var ok = Assert.IsType<OkObjectResult>(await context.Controller.Suppressed("site-a"));
        var response = Assert.IsType<SuppressedOverridesResponse>(ok.Value);

        var item = Assert.Single(response.Items);
        Assert.Equal("source-item", item.SourceContentItemId);
        Assert.Equal("Suppressed", item.Status);
        Assert.Equal("EditScopeRemoved", item.SuppressionReason);
    }

    [Fact]
    public async Task Suppressed_WithoutClearance_ReturnsForbidden()
    {
        var context = new ManagedContentApiTestContext(clearance: "site-b:view");

        AssertProblem(await context.Controller.Suppressed("site-a"), StatusCodes.Status403Forbidden);
    }

    private static ManagedContentOverrideRequest Request(string overrideContentItemId, string status)
        => new() { OverrideContentItemId = overrideContentItemId, Status = status };

    private static ManagedSitesApiProblem AssertProblem(IActionResult result, int expectedStatus)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);

        var problem = Assert.IsType<ManagedSitesApiProblem>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);

        return problem;
    }

    private sealed class ManagedContentApiTestContext
    {
        private readonly FakeManagedContentLocator _locator = new();

        public ManagedContentApiTestContext(string clearance = "site-a:view,edit,publish")
        {
            var portal = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

            Controller = new ManagedContentApiController(
                new Mock<ISession>(MockBehavior.Strict).Object,
                new Mock<IContentManager>(MockBehavior.Strict).Object,
                _locator,
                new Mock<IContentDefinitionManager>(MockBehavior.Strict).Object,
                new ManagedContentScopeService(),
                Overrides,
                portal.ClearanceService,
                portal.SessionService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = ManagedSitesTestData.User(clearance) },
                },
            };
        }

        public FakeManagedContentOverrideService Overrides { get; } = new();

        public ManagedContentApiController Controller { get; }

        public void WithSource(ManagedContentScope editScope)
            => _locator.WithPublished(ManagedContentTestContent.Source("source-item", editScope));
    }
}
