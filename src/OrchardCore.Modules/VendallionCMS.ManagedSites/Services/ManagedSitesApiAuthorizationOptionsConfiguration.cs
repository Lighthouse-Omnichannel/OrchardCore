using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OrchardCore;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Builds the authorization policy used by the Managed Sites API.
/// </summary>
/// <remarks>
/// The policy accepts the bearer API scheme and the admin cookie scheme so the portal works both as a
/// token client and as an admin page. Authentication only establishes who the caller is; which Managed
/// Sites they may touch is decided separately from their signed clearance claims on every request.
/// </remarks>
public sealed class ManagedSitesApiAuthorizationOptionsConfiguration : IConfigureOptions<AuthorizationOptions>
{
    /// <inheritdoc />
    public void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ManagedSitesConstants.AuthorizationPolicies.ManagedSitesApi, policy =>
        {
            policy.AddAuthenticationSchemes(
                OrchardCoreConstants.AuthenticationSchemes.Api,
                IdentityConstants.ApplicationScheme);
            policy.RequireAuthenticatedUser();
        });
    }
}
