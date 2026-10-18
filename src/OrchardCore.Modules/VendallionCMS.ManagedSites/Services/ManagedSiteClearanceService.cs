using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Answers which Managed Sites a user may work on, and what they may do in each.
/// </summary>
public interface IManagedSiteClearanceService
{
    /// <summary>
    /// Gets the Managed Sites the user is authorized to work on.
    /// </summary>
    /// <remarks>
    /// Only Managed Sites that exist under the active Site Blueprint and are enabled are returned, so
    /// a permission for a removed or disabled Managed Site never reaches the content screens.
    /// </remarks>
    /// <param name="user">The user principal to evaluate.</param>
    /// <returns>The authorized Managed Sites, ordered by name.</returns>
    ValueTask<IReadOnlyList<ManagedSite>> GetAuthorizedManagedSitesAsync(ClaimsPrincipal user);

    /// <summary>
    /// Determines whether the user holds the authority a scope needs over one Managed Site.
    /// </summary>
    /// <param name="user">The user principal to evaluate.</param>
    /// <param name="managedSiteCode">The Managed Site code.</param>
    /// <param name="scope">The requested action scope, or <see langword="null" /> for reading.</param>
    /// <returns><see langword="true" /> when the user holds it; otherwise, <see langword="false" />.</returns>
    ValueTask<bool> HasClearanceAsync(ClaimsPrincipal user, string managedSiteCode, string scope);
}

/// <summary>
/// Reads authority over a Managed Site from the permissions its own generated permissions carry.
/// </summary>
/// <remarks>
/// Everything here is a question for the platform's authorization service, asked about a permission
/// naming one Managed Site. Nothing is stored against the user by this feature, so a role change takes
/// effect the way every other role change does.
///
/// The authorization service is resolved late. Handlers this feature registers consult this service,
/// and this service consults the authorization service that owns them, so taking it as a constructor
/// dependency would be a cycle. Asking it about a Managed Site permission does not re-enter those
/// handlers, because each of them answers a different permission and returns at once.
/// </remarks>
public sealed class ManagedSiteClearanceService : IManagedSiteClearanceService
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteClearanceService" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site definition service.</param>
    /// <param name="serviceProvider">The service provider the authorization service is read from.</param>
    public ManagedSiteClearanceService(
        IManagedSiteService managedSiteService,
        IServiceProvider serviceProvider)
    {
        _managedSiteService = managedSiteService;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ManagedSite>> GetAuthorizedManagedSitesAsync(ClaimsPrincipal user)
    {
        var managedSites = await _managedSiteService.ListAsync();

        if (managedSites.Count == 0)
        {
            return [];
        }

        var authorizationService = _serviceProvider.GetService<IAuthorizationService>();

        if (authorizationService is null)
        {
            return [];
        }

        var authorized = new List<ManagedSite>();

        foreach (var managedSite in managedSites)
        {
            if (managedSite.Status != ManagedSiteStatus.Enabled)
            {
                continue;
            }

            var permission = ManagedSiteContentPermissions.For(managedSite.Code, scope: null);

            if (permission is not null && await authorizationService.AuthorizeAsync(user, permission))
            {
                authorized.Add(managedSite);
            }
        }

        return [.. authorized.OrderBy(managedSite => managedSite.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasClearanceAsync(ClaimsPrincipal user, string managedSiteCode, string scope)
    {
        var permission = ManagedSiteContentPermissions.For(managedSiteCode, scope);

        if (permission is null)
        {
            return false;
        }

        var managedSite = await _managedSiteService.GetAsync(managedSiteCode);

        if (managedSite is null || managedSite.Status != ManagedSiteStatus.Enabled)
        {
            return false;
        }

        var authorizationService = _serviceProvider.GetService<IAuthorizationService>();

        return authorizationService is not null
            && await authorizationService.AuthorizeAsync(user, permission);
    }
}
