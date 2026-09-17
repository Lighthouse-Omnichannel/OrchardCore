using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using OrchardCore.Entities;
using OrchardCore.Users.Models;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers turning stored Managed Site grants into the clearance claims carried by the signed principal.
/// </summary>
public class ManagedSiteClaimsProviderTests
{
    [Fact]
    public async Task GenerateAsync_GrantWithScopes_EmitsScopedClaim()
    {
        var identity = await GenerateAsync(Grant("site-a", "edit", "publish"));

        var claim = Assert.Single(ClearanceClaims(identity));
        Assert.Equal("site-a:edit,publish", claim);
    }

    [Fact]
    public async Task GenerateAsync_GrantWithoutScopes_EmitsWildcardClaim()
    {
        var identity = await GenerateAsync(Grant("site-a"));

        var claim = Assert.Single(ClearanceClaims(identity));
        Assert.Equal("site-a", claim);
    }

    [Fact]
    public async Task GenerateAsync_SeveralGrants_EmitsOneClaimEach()
    {
        var identity = await GenerateAsync(Grant("site-a", "edit"), Grant("site-b", "view"));

        Assert.Equal(["site-a:edit", "site-b:view"], ClearanceClaims(identity));
    }

    [Fact]
    public async Task GenerateAsync_NoClearanceRecorded_EmitsNoClaims()
    {
        var identity = await GenerateAsync();

        Assert.Empty(ClearanceClaims(identity));
    }

    [Fact]
    public async Task GenerateAsync_DuplicateGrants_EmitsClaimOnce()
    {
        var identity = await GenerateAsync(Grant("site-a", "edit"), Grant("site-a", "edit"));

        Assert.Single(ClearanceClaims(identity));
    }

    [Theory]
    [InlineData("site:a")]
    [InlineData("site,a")]
    public async Task GenerateAsync_IdentifierContainsSeparator_SkipsGrant(string managedSiteId)
    {
        // A separator inside the identifier would reparse as a different site, silently misdirecting access.
        var identity = await GenerateAsync(Grant(managedSiteId, "edit"));

        Assert.Empty(ClearanceClaims(identity));
    }

    [Fact]
    public async Task GenerateAsync_BlankIdentifier_SkipsGrant()
    {
        var identity = await GenerateAsync(Grant("  ", "edit"));

        Assert.Empty(ClearanceClaims(identity));
    }

    [Fact]
    public async Task GenerateAsync_ScopeContainsSeparator_DropsThatScopeOnly()
    {
        var identity = await GenerateAsync(Grant("site-a", "edit", "pub,lish"));

        var claim = Assert.Single(ClearanceClaims(identity));
        Assert.Equal("site-a:edit", claim);
    }

    [Fact]
    public async Task GenerateAsync_EmittedClaims_AreAcceptedByTheClearanceParser()
    {
        // The provider and the parser must agree on the claim format, or a granted user is still denied.
        var identity = await GenerateAsync(Grant("site-a", "edit"), Grant("site-b"));
        var principal = new ClaimsPrincipal(identity);
        var authorizationService = new ManagedSiteAuthorizationService();

        Assert.True(await authorizationService.AuthorizeAsync(principal, "site-a", "edit"));
        Assert.False(await authorizationService.AuthorizeAsync(principal, "site-a", "publish"));
        Assert.True(await authorizationService.AuthorizeAsync(principal, "site-b", "publish"));
        Assert.False(await authorizationService.AuthorizeAsync(principal, "site-c", "edit"));
    }

    [Fact]
    public async Task GenerateAsync_EmittedClaims_ResolveThroughTheSessionService()
    {
        var identity = await GenerateAsync(Grant("site-a", "edit"));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, ManagedSitesTestData.UserId));

        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("site-a"));

        var result = await context.SessionService.ResolveAsync(new ClaimsPrincipal(identity));

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("site-a", result.Scope.ManagedSiteId);
    }

    private static ManagedSiteClearanceGrant Grant(string managedSiteId, params string[] scopes)
    {
        var grant = new ManagedSiteClearanceGrant { ManagedSiteId = managedSiteId };

        foreach (var scope in scopes)
        {
            grant.Scopes.Add(scope);
        }

        return grant;
    }

    private static async Task<ClaimsIdentity> GenerateAsync(params ManagedSiteClearanceGrant[] grants)
    {
        var user = new User { UserName = "editor" };

        if (grants.Length > 0)
        {
            var settings = new ManagedSiteClearanceSettings();
            foreach (var grant in grants)
            {
                settings.Grants.Add(grant);
            }

            user.Put(settings);
        }

        var identity = new ClaimsIdentity("Test");
        var provider = new ManagedSiteClaimsProvider(NullLogger<ManagedSiteClaimsProvider>.Instance);

        await provider.GenerateAsync(user, identity);

        return identity;
    }

    private static string[] ClearanceClaims(ClaimsIdentity identity)
        => identity.FindAll(ManagedSiteAuthorizationService.ManagedSiteClaimType)
            .Select(claim => claim.Value)
            .ToArray();
}
