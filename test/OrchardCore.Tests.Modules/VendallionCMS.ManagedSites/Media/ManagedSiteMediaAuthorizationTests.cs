using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Media;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Media;

/// <summary>
/// Covers the one rule in this feature that refuses instead of granting.
/// </summary>
/// <remarks>
/// The platform asks the same question of the same path whether it is about to read it or write it, so
/// everything here turns on telling those apart and on who the rule is allowed to reach. A refusal is
/// final once made, which is why a mistake in either direction matters: too eager and a Site Blueprint
/// administrator cannot manage their own library, too shy and one Managed Site rewrites another's
/// assets.
/// </remarks>
public class ManagedSiteMediaAuthorizationTests
{
    [Fact]
    public async Task WritingOutsideTheirOwnFolder_IsRefused()
        => Assert.True(await RefusedAsync(Write("/blueprint/logo.png"), clearedFor: ["alpha"]));

    [Fact]
    public async Task WritingAnotherManagedSitesFolder_IsRefused()
        => Assert.True(await RefusedAsync(Write("beta/logo.png"), clearedFor: ["alpha"]));

    [Fact]
    public async Task WritingAFolderOfTheirOwnAtTheRoot_IsRefused()
    {
        // The root is the Site Blueprint's to arrange. A Managed Site's folder is made when the site is.
        Assert.True(await RefusedAsync(Write("alpha-extra"), clearedFor: ["alpha"]));
    }

    [Fact]
    public async Task WritingTheirOwnFolder_IsLeftToThePlatform()
    {
        // This handler only refuses. Whether the write is allowed is still decided by the permissions
        // the editor holds, which is not this handler's business to grant.
        Assert.False(await RefusedAsync(Write("alpha/logo.png"), clearedFor: ["alpha"]));
    }

    [Fact]
    public async Task WritingTheirOwnFolderItself_IsLeftToThePlatform()
        => Assert.False(await RefusedAsync(Write("alpha"), clearedFor: ["alpha"]));

    [Theory]
    [InlineData("ALPHA/logo.png")]
    [InlineData("Alpha\\logo.png")]
    [InlineData("/alpha/nested/deep/logo.png")]
    public async Task TheirOwnFolder_IsRecognisedHoweverThePathIsWritten(string path)
    {
        // A path arrives as it was written rather than as the store would normalize it.
        Assert.False(await RefusedAsync(Write(path), clearedFor: ["alpha"]));
    }

    [Theory]
    [InlineData("alpha/logo.png")]
    [InlineData("beta/logo.png")]
    public async Task AnEditorClearedForSeveral_WritesInEachOfThem(string path)
        => Assert.False(await RefusedAsync(Write(path), clearedFor: ["alpha", "beta"]));

    [Fact]
    public async Task Reading_IsNeverRefused()
    {
        // Reading a Site Blueprint folder is the point of granting one, and it asks this same question
        // of this same path. Only the request method tells the two apart.
        Assert.False(await RefusedAsync(Read("/blueprint/logo.png"), clearedFor: ["alpha"]));
    }

    [Fact]
    public async Task ADecisionOutsideAnyRequest_IsNotRefused()
    {
        // A folder subscription over the hub asks the same question and changes nothing.
        Assert.False(await RefusedAsync(
            new Call(MediaPermissions.ManageMediaFolder, "/blueprint", Method: null),
            clearedFor: ["alpha"]));
    }

    [Fact]
    public async Task SomebodyHoldingNoClearance_IsNotTouched()
    {
        // Not a Managed Site editor, so not this feature's business either way.
        Assert.False(await RefusedAsync(Write("/blueprint/logo.png"), clearedFor: []));
    }

    [Fact]
    public async Task SomebodyGoverningManagedSites_IsNotConfinedByARuleAboutThem()
        => Assert.False(await RefusedAsync(
            Write("/blueprint/logo.png"),
            clearedFor: ["alpha"],
            holding: [Permissions.ManageManagedSites.Name]));

    [Fact]
    public async Task SomebodyTrustedWithOtherPeoplesMedia_IsNotConfined()
    {
        // Holding that is having been told the library is theirs, which is a decision already made.
        Assert.False(await RefusedAsync(
            Write("/blueprint/logo.png"),
            clearedFor: ["alpha"],
            holding: [MediaPermissions.ManageOthersMedia.Name]));
    }

    [Fact]
    public async Task APermissionThatIsNotAboutAMediaFolder_IsNotTouched()
        => Assert.False(await RefusedAsync(
            new Call(Permissions.EditManagedSiteContent, "/blueprint", HttpMethods.Post),
            clearedFor: ["alpha"]));

    [Fact]
    public async Task ARequestCarryingNoPath_IsNotTouched()
    {
        // The library's own index asks with no path at all.
        Assert.False(await RefusedAsync(
            new Call(MediaPermissions.ManageMediaFolder, Resource: null, HttpMethods.Post),
            clearedFor: ["alpha"]));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("/", null)]
    [InlineData("alpha", "alpha")]
    [InlineData("/alpha/", "alpha")]
    [InlineData("alpha/one/two.png", "alpha")]
    [InlineData("\\alpha\\one.png", "alpha")]
    public void TheOwningFolder_IsTheFirstSegmentOfThePath(string path, string expected)
        => Assert.Equal(expected, ManagedSiteMediaFolderService.GetOwningFolderOf(path));

    private sealed record Call(Permission Permission, object Resource, string Method);

    private static Call Write(string path)
        => new(MediaPermissions.ManageMediaFolder, path, HttpMethods.Post);

    private static Call Read(string path)
        => new(MediaPermissions.ManageMediaFolder, path, HttpMethods.Get);

    private static async Task<bool> RefusedAsync(Call call, string[] clearedFor, string[] holding = null)
    {
        var accessor = new HttpContextAccessor();

        if (call.Method is not null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = call.Method;
            accessor.HttpContext = httpContext;
        }

        var services = new ServiceCollection();
        services.AddSingleton<IAuthorizationService>(new StubAuthorizationService(holding ?? []));

        var handler = new ManagedSiteMediaAuthorizationHandler(
            new StubClearanceService(),
            new ManagedSiteMediaFolderService(new ServiceCollection().BuildServiceProvider()),
            accessor,
            services.BuildServiceProvider());

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(call.Permission)],
            ManagedSitesTestData.User(clearedFor),
            call.Resource);

        await handler.HandleAsync(context);

        return context.HasFailed;
    }

    /// <summary>
    /// Answers which Managed Sites the principal may work on, from the permissions it holds.
    /// </summary>
    private sealed class StubClearanceService : IManagedSiteClearanceService
    {
        private static readonly string[] s_codes = ["alpha", "beta"];

        public ValueTask<IReadOnlyList<ManagedSite>> GetAuthorizedManagedSitesAsync(ClaimsPrincipal user)
        {
            var held = user.FindAll(Permission.ClaimType).Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);

            var managedSites = s_codes
                .Where(code => held.Contains(ManagedSiteContentPermissions.For(code, scope: null).Name)
                    || ManagedSitesConstants.Scopes.All.Any(scope =>
                        held.Contains(ManagedSiteContentPermissions.For(code, scope).Name)))
                .Select(code => ManagedSitesTestData.ManagedSite(code))
                .ToArray();

            return ValueTask.FromResult<IReadOnlyList<ManagedSite>>(managedSites);
        }

        public ValueTask<bool> HasClearanceAsync(ClaimsPrincipal user, string managedSiteCode, string scope)
            => ValueTask.FromResult(false);
    }

    private sealed class StubAuthorizationService : IAuthorizationService
    {
        private readonly HashSet<string> _holds;

        public StubAuthorizationService(string[] holds)
            => _holds = new HashSet<string>(holds, StringComparer.Ordinal);

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object resource,
            IEnumerable<IAuthorizationRequirement> requirements)
        {
            var held = requirements
                .OfType<PermissionRequirement>()
                .All(requirement => _holds.Contains(requirement.Permission.Name));

            return Task.FromResult(held ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        }

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
            => Task.FromResult(AuthorizationResult.Failed());
    }
}
