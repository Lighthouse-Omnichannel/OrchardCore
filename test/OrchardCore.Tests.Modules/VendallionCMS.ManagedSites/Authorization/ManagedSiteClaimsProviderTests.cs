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
        var identity = await GenerateAsync(Grant("sitea", "edit", "publish"));

        var claim = Assert.Single(ClearanceClaims(identity));
        Assert.Equal("sitea:edit,publish", claim);
    }

    [Fact]
    public async Task GenerateAsync_GrantWithoutScopes_EmitsWildcardClaim()
    {
        var identity = await GenerateAsync(Grant("sitea"));

        var claim = Assert.Single(ClearanceClaims(identity));
        Assert.Equal("sitea", claim);
    }

    [Fact]
    public async Task GenerateAsync_SeveralGrants_EmitsOneClaimEach()
    {
        var identity = await GenerateAsync(Grant("sitea", "edit"), Grant("siteb", "preview"));

        Assert.Equal(["sitea:edit", "siteb:preview"], ClearanceClaims(identity));
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
        var identity = await GenerateAsync(Grant("sitea", "edit"), Grant("sitea", "edit"));

        Assert.Single(ClearanceClaims(identity));
    }

    [Theory]
    [InlineData("site:a")]
    [InlineData("site,a")]
    public async Task GenerateAsync_IdentifierContainsSeparator_SkipsGrant(string managedSiteCode)
    {
        // A separator inside the identifier would reparse as a different site, silently misdirecting access.
        var identity = await GenerateAsync(Grant(managedSiteCode, "edit"));

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
        var identity = await GenerateAsync(Grant("sitea", "edit", "pub,lish"));

        var claim = Assert.Single(ClearanceClaims(identity));
        Assert.Equal("sitea:edit", claim);
    }

    [Fact]
    public async Task GenerateAsync_EmittedClaims_AreAcceptedByTheClearanceParser()
    {
        // The provider and the parser must agree on the claim format, or a granted user is still denied.
        var identity = await GenerateAsync(Grant("sitea", "edit"), Grant("siteb"));
        var principal = new ClaimsPrincipal(identity);
        var authorizationService = new ManagedSiteAuthorizationService();

        Assert.True(await authorizationService.AuthorizeAsync(principal, "sitea", "edit"));
        Assert.False(await authorizationService.AuthorizeAsync(principal, "sitea", "publish"));

        // siteb was granted without naming a scope, which allows reading it and nothing else.
        Assert.True(await authorizationService.AuthorizeAsync(principal, "siteb", null));
        Assert.False(await authorizationService.AuthorizeAsync(principal, "siteb", "publish"));

        Assert.False(await authorizationService.AuthorizeAsync(principal, "sitec", "edit"));
    }

    [Fact]
    public async Task GenerateAsync_EmittedClaims_ResolveThroughTheSessionService()
    {
        var identity = await GenerateAsync(Grant("sitea", "edit"));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, ManagedSitesTestData.UserId));

        var context = new ManagedSitePortalTestContext(ManagedSitesTestData.ManagedSite("sitea"));

        var result = await context.SessionService.ResolveAsync(new ClaimsPrincipal(identity));

        Assert.Equal(ManagedSiteSessionStatus.Selected, result.Status);
        Assert.Equal("sitea", result.Scope.ManagedSiteCode);
    }

    private static ManagedSiteClearanceGrant Grant(string managedSiteCode, params string[] scopes)
    {
        var grant = new ManagedSiteClearanceGrant { ManagedSiteCode = managedSiteCode };

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
