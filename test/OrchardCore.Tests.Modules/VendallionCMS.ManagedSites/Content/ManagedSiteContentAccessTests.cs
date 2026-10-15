using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Implementation;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.DisplayManagement.Shapes;
using OrchardCore.Navigation;
using VendallionCMS.ManagedSites;
using VendallionCMS.ManagedSites.Controllers;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.Tests.ManagedContent;
using VendallionCMS.ManagedSites.ViewModels;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Content;

/// <summary>
/// Covers who reaches a Managed Site's content, and which Managed Site's.
/// </summary>
/// <remarks>
/// These are the rules the HTTP API enforced before these screens became an admin surface, and the
/// reason they are written again rather than deleted with it: each one is a way a Managed Site editor
/// could otherwise reach content that is not theirs. What changed is the shape of a refusal, not the
/// fact of one. An API answered with a status code; an admin surface forbids, offers the choice, or
/// says they hold no clearance at all.
///
/// The Managed Site an action acts on is named in the address, which decides nothing on its own:
/// clearance is checked against it on every action, because clearance can be withdrawn between the
/// action that listed an item and the action that changes it.
/// </remarks>
public class ManagedSiteContentAccessTests
{
    [Fact]
    public async Task WithoutThePermission_NothingIsReachable()
    {
        var context = new ContentContext(authorized: false);

        Assert.IsType<ForbidResult>(await context.Controller.Index("sitea", null, null, null, new PagerParameters()));
    }

    [Fact]
    public async Task WithThePermissionButNoClearance_TheEditorIsToldSoRatherThanShownAnEmptyList()
    {
        // An empty list reads as "nothing to do here", which sends someone looking for the wrong fault.
        var context = new ContentContext(clearance: []);

        var result = Assert.IsType<ViewResult>(await context.Controller.Sites());

        Assert.Equal("NoClearance", result.ViewName);
    }

    [Fact]
    public async Task WithThePermissionButNoClearance_NamingAManagedSiteIsRefused()
    {
        var context = new ContentContext(clearance: []);

        Assert.IsType<ForbidResult>(
            await context.Controller.Index("sitea", null, null, null, new PagerParameters()));
    }

    [Fact]
    public async Task ClearedForOneManagedSite_TheEditorIsSentStraightToIt()
    {
        // A list of one is not a choice.
        var context = new ContentContext(clearance: ["sitea:edit"]);

        var result = Assert.IsType<RedirectToActionResult>(await context.Controller.Sites());

        Assert.Equal(nameof(ManagedSiteContentController.Index), result.ActionName);
        Assert.Equal("sitea", result.RouteValues["managedSiteCode"]);
    }

    [Fact]
    public async Task ClearedForSeveral_TheEditorIsOfferedTheChoice()
    {
        var context = new ContentContext(
            clearance: ["sitea:edit", "siteb:edit"],
            managedSites: [Site("sitea"), Site("siteb")]);

        var result = Assert.IsType<ViewResult>(await context.Controller.Sites());

        Assert.Equal(
            ["sitea", "siteb"],
            Assert.IsType<ManagedSiteContentSitesViewModel>(result.Model).ManagedSites.Select(site => site.Code));
    }

    [Fact]
    public async Task NamingAManagedSiteOutsideTheirClearance_IsRefused()
    {
        // The address says which Managed Site, and anyone can type one, so naming it decides nothing.
        var context = new ContentContext(
            clearance: ["sitea:edit"],
            managedSites: [Site("sitea"), Site("siteb")]);

        Assert.IsType<ForbidResult>(
            await context.Controller.Index("siteb", null, null, null, new PagerParameters()));
    }

    [Fact]
    public async Task NamingAManagedSiteTheyAreClearedFor_OpensThatOne()
    {
        var context = new ContentContext(
            clearance: ["sitea:edit", "siteb:edit"],
            managedSites: [Site("sitea"), Site("siteb")]);

        var result = Assert.IsType<ViewResult>(
            await context.Controller.Index("siteb", null, null, null, new PagerParameters()));

        Assert.Equal("siteb", Assert.IsType<ManagedSiteContentListViewModel>(result.Model).ManagedSite.Code);
    }

    [Fact]
    public async Task ClearanceThatDoesNotReachWhatTheActionDoes_IsRefused()
    {
        // Seeing a Managed Site's content and changing it are separate grants, and the action that
        // changes it says so rather than trusting the one that listed it.
        var context = new ContentContext(clearance: ["sitea"]);

        Assert.IsType<ForbidResult>(await context.Controller.Create("sitea", "source-item"));
    }

    [Fact]
    public async Task PublishingWithEditClearanceAlone_IsRefused()
    {
        var context = new ContentContext(clearance: ["sitea:edit"]);

        Assert.IsType<ForbidResult>(await context.Controller.Publish("sitea", "source-item", "override-item"));
    }

    [Fact]
    public async Task PublishingWithPublishClearance_IsAllowedThrough()
    {
        var context = new ContentContext(clearance: ["sitea:edit,publish"]);

        // Published, then handed back to where it was asked from rather than to a screen of its own.
        Assert.IsType<RedirectResult>(await context.Controller.Publish("sitea", "source-item", "override-item"));
    }

    [Fact]
    public async Task AnItemTheManagedSiteMayNotCustomize_IsNotShown()
    {
        // Checked against the item as it stands rather than trusted from the listing, because a
        // blueprint administrator can narrow the scope between an editor seeing an item and opening it.
        var context = new ContentContext(clearance: ["sitea:edit"]);
        context.WithSource(ManagedContentScope.Selected("siteb"));

        Assert.IsType<NotFoundResult>(await context.Controller.Contained("sitea", "source-item", null, null, null, new PagerParameters()));
    }

    [Fact]
    public async Task AnItemThatDoesNotExist_IsNotShownEither()
    {
        // The same answer as an item that is not theirs, so that guessing identifiers tells an editor
        // nothing about what the Site Blueprint holds.
        var context = new ContentContext(clearance: ["sitea:edit"]);

        Assert.IsType<NotFoundResult>(await context.Controller.Contained("sitea", "nothing-here", null, null, null, new PagerParameters()));
    }

    [Fact]
    public async Task AnItemTheManagedSiteMayCustomize_IsShown()
    {
        // Guards the two above: they would pass just as well if nothing were ever shown.
        var context = new ContentContext(clearance: ["sitea:edit"]);
        context.WithSource(ManagedContentScope.All());

        var result = Assert.IsType<ViewResult>(
            await context.Controller.Contained("sitea", "source-item", null, null, null, new PagerParameters()));

        Assert.Equal("source-item", Assert.IsType<ManagedSiteContentContainedViewModel>(result.Model).SourceContentItemId);
    }

    [Fact]
    public async Task ClearanceWithdrawnAfterOpening_StopsTheNextRequest()
    {
        // The address outlives the clearance -- it is in a bookmark, a link, an open tab -- so every
        // request asks again rather than trusting that the last one was allowed.
        var context = new ContentContext(
            clearance: ["sitea:edit", "siteb:edit"],
            managedSites: [Site("sitea"), Site("siteb")]);

        Assert.IsType<ViewResult>(await context.Controller.Index("siteb", null, null, null, new PagerParameters()));

        context.WithClearance("sitea:edit");

        Assert.IsType<ForbidResult>(await context.Controller.Index("siteb", null, null, null, new PagerParameters()));
    }

    [Fact]
    public async Task EveryScreenAnswersForTheSameManagedSite()
    {
        // One place decides which Managed Site an action acts on, so a screen cannot be reached for a
        // different one by arriving at it directly.
        var context = new ContentContext(clearance: ["sitea:edit"]);

        foreach (var result in new[]
        {
            await context.Controller.Index("sitea", null, null, null, new PagerParameters()),
            await context.Controller.Suppressed("sitea"),
            await context.Controller.Preview("sitea"),
        })
        {
            Assert.IsType<ViewResult>(result);
        }
    }

    [Fact]
    public async Task EveryScreenIsRefusedTheSameWay()
    {
        var context = new ContentContext(authorized: false);

        Assert.IsType<ForbidResult>(await context.Controller.Index("sitea", null, null, null, new PagerParameters()));
        Assert.IsType<ForbidResult>(await context.Controller.Suppressed("sitea"));
        Assert.IsType<ForbidResult>(await context.Controller.Preview("sitea"));
        Assert.IsType<ForbidResult>(await context.Controller.Contained("sitea", "source-item", null, null, null, new PagerParameters()));
        Assert.IsType<ForbidResult>(await context.Controller.Create("sitea", "source-item"));
        Assert.IsType<ForbidResult>(await context.Controller.Remove("sitea", "source-item"));
    }

    [Fact]
    public async Task CreatingAVersion_OpensItForEditing()
    {
        // Creating a version is the beginning of changing it, and the copy handed over is the blueprint
        // content until the editor does, so there is nothing to read on a screen in between.
        var context = new ContentContext(clearance: ["sitea:edit"]);
        context.WithSource(ManagedContentScope.All());

        var result = Assert.IsType<RedirectToActionResult>(await context.Controller.Create("sitea", "source-item"));

        Assert.Equal("Edit", result.ActionName);
        Assert.Equal("Admin", result.ControllerName);
        Assert.Equal("OrchardCore.Contents", result.RouteValues["area"]);
    }

    [Fact]
    public async Task CreatingAVersion_ReturnsToWhereItWasAskedFor()
    {
        var context = new ContentContext(clearance: ["sitea:edit"]);
        context.WithSource(ManagedContentScope.All());

        var result = Assert.IsType<RedirectToActionResult>(
            await context.Controller.Create("sitea", "source-item", "/Admin/ManagedSites/Content/sitea"));

        Assert.Equal("/Admin/ManagedSites/Content/sitea", result.RouteValues["returnUrl"]);
    }

    [Fact]
    public async Task CreatingAVersion_WillNotBounceTheEditorOffTheSite()
    {
        // The value arrives in the query string, where anyone can put anything.
        var context = new ContentContext(clearance: ["sitea:edit"]);
        context.WithSource(ManagedContentScope.All());

        var result = Assert.IsType<RedirectToActionResult>(
            await context.Controller.Create("sitea", "source-item", "https://elsewhere.example/"));

        Assert.DoesNotContain("elsewhere.example", result.RouteValues["returnUrl"]?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task TheList_OffersNoWayToChangeAnythingWithoutEditClearance()
    {
        var context = new ContentContext(clearance: ["sitea"]);

        var result = Assert.IsType<ViewResult>(await context.Controller.Index("sitea", null, null, null, new PagerParameters()));

        Assert.False(Assert.IsType<ManagedSiteContentListViewModel>(result.Model).CanEdit);
    }

    [Fact]
    public async Task TheList_OffersTheWayToChangeThingsWithEditClearance()
    {
        var context = new ContentContext(clearance: ["sitea:edit"]);

        var result = Assert.IsType<ViewResult>(await context.Controller.Index("sitea", null, null, null, new PagerParameters()));

        Assert.True(Assert.IsType<ManagedSiteContentListViewModel>(result.Model).CanEdit);
    }

    [Fact]
    public async Task AnItem_OffersPublishingOnlyToSomebodyWhoMayPublish()
    {
        var context = new ContentContext(clearance: ["sitea:edit"]);
        context.WithSource(ManagedContentScope.All());

        var result = Assert.IsType<ViewResult>(await context.Controller.Index("sitea", null, null, null, new PagerParameters()));
        var model = Assert.IsType<ManagedSiteContentListViewModel>(result.Model);

        Assert.True(model.CanEdit);
        Assert.False(model.CanPublish);
    }

    /// <summary>
    /// Enough of a URL helper to decide where to hand an editor back to.
    /// </summary>
    /// <remarks>
    /// <see cref="IsLocalUrl" /> follows the platform's own rule: a path rooted at the site, and not one
    /// that starts a host of its own. The rule is what the controller relies on to refuse a return
    /// address pointing somewhere else, so a stub that said yes to everything would test nothing.
    /// </remarks>
    private sealed class StubUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();

        public string Action(UrlActionContext actionContext)
            => $"/Admin/ManagedSites/Content/{actionContext.Action}";

        public string Content(string contentPath) => contentPath;

        public bool IsLocalUrl(string url)
            => !string.IsNullOrEmpty(url)
                && url[0] == '/'
                && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));

        public string Link(string routeName, object values) => null;

        public string RouteUrl(UrlRouteContext routeContext) => null;
    }

    /// <summary>
    /// Builds a bare shape, which is all the pager needs here: these tests are about who the list is
    /// shown to, not what it looks like.
    /// </summary>
    private sealed class StubShapeFactory : IShapeFactory
    {
        public dynamic New => null;

        public ValueTask<IShape> CreateAsync(
            string shapeType,
            Func<ValueTask<IShape>> shapeFactory,
            Action<ShapeCreatingContext> creating,
            Action<ShapeCreatedContext> created)
            => ValueTask.FromResult<IShape>(new Shape());
    }

    private static ManagedSite Site(string id) => ManagedSitesTestData.ManagedSite(id);

    /// <summary>
    /// Assembles the controller over the real clearance service, so the refusals under test are the
    /// ones the feature makes rather than ones a double was told to make.
    /// </summary>
    private sealed class ContentContext
    {
        private readonly FakeManagedContentLocator _locator = new();
        private readonly StubAuthorizationService _authorization;

        private ClaimsPrincipal _user;

        public ContentContext(
            string[] clearance = null,
            bool authorized = true,
            ManagedSite[] managedSites = null)
        {
            Scaffold = new ManagedSiteContentTestContext(managedSites ?? [Site("sitea")]);
            _authorization = new StubAuthorizationService(authorized);
            _user = ManagedSitesTestData.User(clearance ?? ["sitea:edit,publish"]);

            Controller = new ManagedSiteContentController(
                _authorization,
                Scaffold.ClearanceService,
                new StubListService(),
                new FakeManagedContentOverrideService(),
                _locator,
                new ManagedContentScopeService(),
                new ManagedSitePreviewService(Scaffold.ManagedSiteService),
                Mock.Of<INotifier>(),
                new StubShapeFactory(),
                Options.Create(new PagerOptions()),
                new StubStringLocalizer<ManagedSiteContentController>(),
                new StubHtmlLocalizer<ManagedSiteContentController>())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = _user },
                },
                Url = new StubUrlHelper(),
            };
        }

        public ManagedSiteContentTestContext Scaffold { get; }

        public ManagedSiteContentController Controller { get; }

        public void WithSource(ManagedContentScope editScope)
            => _locator.WithPublished(ManagedContentTestContent.Source("source-item", editScope));

        public void WithClearance(params string[] clearance)
        {
            _user = ManagedSitesTestData.User(clearance);
            Controller.ControllerContext.HttpContext = new DefaultHttpContext { User = _user };
        }
    }

    private sealed class StubListService : IManagedContentListService
    {
        public ValueTask<ManagedContentListing> ListAsync(string managedSiteCode, ManagedContentListQuery query)
            => ValueTask.FromResult(new ManagedContentListing([], 0));

        public ValueTask<ManagedContentListing> ListContainedAsync(
            string managedSiteCode,
            string containerContentItemId,
            ManagedContentListQuery query)
            => ValueTask.FromResult(new ManagedContentListing([], 0));
    }
}
