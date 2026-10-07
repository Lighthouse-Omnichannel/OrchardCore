using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OrchardCore.Environment.Shell;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Keeps the sign-in cookie attached to the tenant rather than to whichever Managed Site wrote it.
/// </summary>
/// <remarks>
/// The platform deliberately leaves the cookie's path unset so that it follows the request's path
/// base, which for an ordinary tenant is the tenant's own prefix and so scopes the cookie to that
/// tenant. This feature puts a Managed Site's prefix on that same path base, which is what makes the
/// content underneath it resolve, and the cookie followed it there.
///
/// The result was a sign-in that could not be undone. Signing in at the tenant root wrote the cookie at
/// "/", signing out from a Managed Site under "/beta" wrote the expiry at "/beta", the browser kept the
/// cookie it already had, and the user stayed signed in with no way to stop being so. The same mismatch
/// the other way round would have left somebody signed in on one Managed Site and anonymous on the
/// next.
///
/// A Managed Site is not a tenant. It is one of several addresses the same tenant answers on, and the
/// person signed in is signed in to the tenant, so the cookie belongs at the tenant's base and nowhere
/// narrower. Pinning it there restores exactly the path the cookie would have had if this feature were
/// not enabled.
///
/// One case is not covered: an application hosted under a virtual directory has that directory in its
/// path base too, and the tenant knows only its own prefix. The cookie is then written one segment
/// wider than it was before, which costs it being sent on paths it need not be, and nothing else: the
/// cookie carries the tenant's name, so no other tenant can read it as its own.
/// </remarks>
public sealed class ManagedSiteCookiePathConfiguration : IConfigureNamedOptions<CookieAuthenticationOptions>
{
    private readonly ShellSettings _shellSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteCookiePathConfiguration" /> class.
    /// </summary>
    /// <param name="shellSettings">The current shell settings.</param>
    public ManagedSiteCookiePathConfiguration(ShellSettings shellSettings)
    {
        _shellSettings = shellSettings;
    }

    /// <inheritdoc />
    public void Configure(string name, CookieAuthenticationOptions options)
    {
        if (!string.Equals(name, IdentityConstants.ApplicationScheme, StringComparison.Ordinal))
        {
            return;
        }

        Configure(options);
    }

    /// <inheritdoc />
    public void Configure(CookieAuthenticationOptions options)
    {
        var prefix = _shellSettings.RequestUrlPrefix?.Trim('/');

        options.Cookie.Path = string.IsNullOrEmpty(prefix) ? "/" : "/" + prefix;
    }
}
