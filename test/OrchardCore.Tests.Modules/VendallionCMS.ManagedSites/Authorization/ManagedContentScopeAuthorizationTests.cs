using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers who may configure Managed Content edit and display scopes.
/// </summary>
/// <remarks>
/// Scopes decide who may change the tenant's common content, so configuring them requires Site
/// Blueprint management access. A Managed Site administrator must never widen their own reach.
/// </remarks>
public class ManagedContentScopeAuthorizationTests
{
    [Fact]
    public async Task CanConfigureScopes_WithBlueprintAccess_IsAllowed()
    {
        var handler = CreateHandler(isAuthorized: true);

        Assert.True(await handler.CanConfigureScopesAsync());
    }

    [Fact]
    public async Task CanConfigureScopes_WithoutBlueprintAccess_IsDenied()
    {
        var handler = CreateHandler(isAuthorized: false);

        Assert.False(await handler.CanConfigureScopesAsync());
    }

    [Fact]
    public async Task CanConfigureScopes_ManagedSiteClearanceAlone_IsDenied()
    {
        // Clearance grants content authority inside a Managed Site, never the right to redraw the
        // boundary that granted it.
        var handler = CreateHandler(isAuthorized: false);

        Assert.False(await handler.CanConfigureScopesAsync(ManagedSitesTestData.User("site-a:edit,publish")));
    }

    [Fact]
    public async Task CanConfigureScopes_AnonymousUser_IsDenied()
    {
        var handler = CreateHandler(isAuthorized: false);

        Assert.False(await handler.CanConfigureScopesAsync(ManagedSitesTestData.AnonymousUser()));
    }

    [Fact]
    public async Task CanConfigureScopes_NoHttpContext_IsDenied()
    {
        var handler = new ManagedContentScopeAuthorizationHandler(
            new StubAuthorizationService(isAuthorized: true),
            new HttpContextAccessor());

        Assert.False(await handler.CanConfigureScopesAsync());
    }

    [Fact]
    public async Task CanConfigureScopes_NullUser_IsDenied()
    {
        var handler = CreateHandler(isAuthorized: true);

        Assert.False(await handler.CanConfigureScopesAsync(user: null));
    }

    private static ManagedContentScopeAuthorizationHandler CreateHandler(bool isAuthorized)
        => new(
            new StubAuthorizationService(isAuthorized),
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext { User = ManagedSitesTestData.User() },
            });
}
