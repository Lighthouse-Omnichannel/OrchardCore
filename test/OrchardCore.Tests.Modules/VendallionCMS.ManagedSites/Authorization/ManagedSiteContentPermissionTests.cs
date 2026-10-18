using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using OrchardCore.ContentManagement;
using OrchardCore.Contents;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.Tests.ManagedContent;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers what a Managed Site's clearance authorizes in content terms.
/// </summary>
/// <remarks>
/// Without this, authoring an override would need tenant-wide permission for its content type, and
/// anyone holding that could change Site Blueprint content too. The handler exists to make clearance
/// enough for a Managed Site's own content, and to make it reach nothing else.
///
/// It only ever grants. Every case that should be refused is a case where the handler stays silent and
/// the decision is left to whatever else the tenant has configured.
/// </remarks>
public class ManagedSiteContentPermissionTests
{
    [Fact]
    public async Task Clearance_AuthorizesEditingTheManagedSitesOwnOverride()
    {
        var context = await AuthorizeAsync(CommonPermissions.EditContent, Override("sitea"), "sitea:view,edit");

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Clearance_AuthorizesPublishingWhenItGrantsPublish()
    {
        var context = await AuthorizeAsync(CommonPermissions.PublishContent, Override("sitea"), "sitea:view,edit,publish");

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task EditClearanceAlone_DoesNotAuthorizePublishing()
    {
        // The scopes a clearance names are the whole of what it grants; edit does not imply publish.
        var context = await AuthorizeAsync(CommonPermissions.PublishContent, Override("sitea"), "sitea:view,edit");

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Clearance_DoesNotAuthorizeAnotherManagedSitesOverride()
    {
        var context = await AuthorizeAsync(CommonPermissions.EditContent, Override("siteb"), "sitea:view,edit,publish");

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Clearance_DoesNotAuthorizeSiteBlueprintContent()
    {
        // The whole point of the handler: it reaches a Managed Site's own content and nothing else.
        var blueprintItem = ManagedContentTestContent.Source("source-item", ManagedContentScope.All());

        var context = await AuthorizeAsync(CommonPermissions.EditContent, blueprintItem, "sitea:view,edit,publish");

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Clearance_DoesNotAuthorizeContentThatIsNotAnOverride()
    {
        var context = await AuthorizeAsync(
            CommonPermissions.EditContent,
            ManagedContentTestContent.Item("plain-item"),
            "sitea:view,edit,publish");

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task NoClearance_AuthorizesNothing()
    {
        var context = await AuthorizeAsync(CommonPermissions.EditContent, Override("sitea"));

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PerTypePermission_IsAnsweredLikeItsBasePermission()
    {
        // Content permissions also arrive as EditContent_Article. A Managed Site's clearance covers its
        // own content whatever type that content is, so the type half decides nothing.
        var context = await AuthorizeAsync(
            new Permission("EditContent_Article"),
            Override("sitea"),
            "sitea:view,edit");

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionOutsideContent_IsLeftAlone()
    {
        // Managing Managed Sites is Site Blueprint governance. Clearance to edit content for one of them
        // must never turn into authority to define them.
        var context = await AuthorizeAsync(
            Permissions.ManageManagedSites,
            Override("sitea"),
            "sitea:view,edit,publish");

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task AGrantAlreadyMade_IsLeftUntouched()
    {
        // The handler adds authority and never takes it away, so it does not re-decide what another
        // handler already allowed.
        var requirement = new PermissionRequirement(CommonPermissions.EditContent);
        var context = new AuthorizationHandlerContext(
            [requirement],
            ManagedSitesTestData.User(),
            Override("siteb"));

        context.Succeed(requirement);

        await CreateHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    private static async Task<AuthorizationHandlerContext> AuthorizeAsync(
        Permission permission,
        ContentItem resource,
        params string[] clearances)
    {
        var requirement = new PermissionRequirement(permission);
        var context = new AuthorizationHandlerContext(
            [requirement],
            ManagedSitesTestData.User(clearances),
            resource);

        await CreateHandler().HandleAsync(context);

        return context;
    }

    private static ManagedSiteContentAuthorizationHandler CreateHandler()
    {
        var scaffold = new ManagedSiteContentTestContext(
            ManagedSitesTestData.ManagedSite("sitea"),
            ManagedSitesTestData.ManagedSite("siteb"));

        return new ManagedSiteContentAuthorizationHandler(scaffold.ManagedSiteService, scaffold.Services);
    }

    private static ContentItem Override(string managedSiteCode)
        => ManagedContentTestContent.Override("override-item", managedSiteCode);
}
