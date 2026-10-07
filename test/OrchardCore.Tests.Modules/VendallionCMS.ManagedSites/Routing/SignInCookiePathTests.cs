using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using OrchardCore.Environment.Shell;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Routing;

/// <summary>
/// Covers where the sign-in cookie is written when Managed Sites are addressed by URL prefix.
/// </summary>
/// <remarks>
/// The platform leaves the cookie's path unset so that it follows the request's path base, which for an
/// ordinary tenant is that tenant's prefix. This feature puts a Managed Site's prefix on the same path
/// base, which is what makes the content underneath it resolve, and the cookie followed it there.
///
/// That made a sign-in impossible to undo. Signing in at the tenant root wrote the cookie at "/";
/// signing out from a Managed Site under "/beta" wrote the expiry at "/beta"; the browser kept the
/// cookie it already had and the user stayed signed in with no way to stop. A Managed Site is not a
/// tenant, the person is signed in to the tenant, and the cookie belongs at the tenant's base.
/// </remarks>
public class SignInCookiePathTests
{
    [Fact]
    public void OnATenantAtTheRoot_TheCookieIsWrittenAtTheRoot()
    {
        var options = Configure(new ShellSettings());

        Assert.Equal("/", options.Cookie.Path);
    }

    [Fact]
    public void OnATenantUnderItsOwnPrefix_TheCookieStaysWithTheTenant()
    {
        // The path the cookie would have had anyway. Pinning it changes nothing for a tenant that has
        // no Managed Sites; it only stops the path moving once one answers.
        var options = Configure(new ShellSettings { RequestUrlPrefix = "tenant" });

        Assert.Equal("/tenant", options.Cookie.Path);
    }

    [Theory]
    [InlineData("/tenant")]
    [InlineData("tenant/")]
    public void APrefixWrittenWithSlashes_IsReadTheSameWay(string prefix)
    {
        var options = Configure(new ShellSettings { RequestUrlPrefix = prefix });

        Assert.Equal("/tenant", options.Cookie.Path);
    }

    [Fact]
    public void AnotherSchemesCookie_IsLeftAlone()
    {
        // Only the sign-in cookie follows the path base in a way this feature disturbs.
        var options = new CookieAuthenticationOptions();

        new ManagedSiteCookiePathConfiguration(new ShellSettings())
            .Configure("SomeOtherScheme", options);

        Assert.Null(options.Cookie.Path);
    }

    private static CookieAuthenticationOptions Configure(ShellSettings shellSettings)
    {
        var options = new CookieAuthenticationOptions();

        new ManagedSiteCookiePathConfiguration(shellSettings)
            .Configure(IdentityConstants.ApplicationScheme, options);

        return options;
    }
}
