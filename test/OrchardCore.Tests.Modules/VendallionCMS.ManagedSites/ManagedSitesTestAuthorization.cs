using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Security;
using OrchardCore.Security.AuthorizationHandlers;

namespace VendallionCMS.ManagedSites.Tests;

/// <summary>
/// Builds the platform's own authorization over a principal's permission claims.
/// </summary>
/// <remarks>
/// Authority over a Managed Site is now a permission, and what a permission grants is decided by the
/// platform's implication graph: publishing a Managed Site's content grants editing it, and either
/// grants reading it. A double that answered yes to whatever it was told about would never exercise
/// that, and the implications are the part most easily got wrong, so the tests run the real granting
/// service instead.
/// </remarks>
public static class ManagedSitesTestAuthorization
{
    /// <summary>
    /// Builds a service provider whose authorization service answers from permission claims.
    /// </summary>
    /// <param name="handlers">Extra handlers under test, registered alongside the platform's own.</param>
    /// <returns>The service provider.</returns>
    public static IServiceProvider Build(params IAuthorizationHandler[] handlers)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorization();
        services.AddSingleton<IPermissionGrantingService, DefaultPermissionGrantingService>();
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

        foreach (var handler in handlers ?? [])
        {
            services.AddSingleton(handler);
        }

        return services.BuildServiceProvider();
    }
}
