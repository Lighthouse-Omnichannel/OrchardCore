using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using VendallionCMS.ManagedSites.Drivers;
using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Migrations;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites;

public sealed class Startup : StartupBase
{
	public override void ConfigureServices(IServiceCollection services)
	{
		services.AddDataMigration<ManagedSitesMigrations>();
		services.AddPermissionProvider<Permissions>();
		services.AddNavigationProvider<AdminMenu>();
		services.AddScoped<IManagedSiteAuthorizationService, ManagedSiteAuthorizationService>();
		services.AddScoped<IManagedSiteService, ManagedSiteService>();
		services.AddScoped<IShellUrlSynchronizationService, ShellUrlSynchronizationService>();
		services.AddScoped<IManagedContentScopeService, ManagedContentScopeService>();
		services.AddScoped<IManagedContentScopeAuthorizationHandler, ManagedContentScopeAuthorizationHandler>();

		services.AddContentPart<ManagedContentPart>()
			.UseDisplayDriver<ManagedContentPartDisplayDriver>();

		services.AddIndexProvider<ManagedContentEditScopeIndexProvider>();
	}
}

/// <summary>
/// Registers the Managed Site Admin Portal, its API surface, and the session scope services.
/// </summary>
[Feature(ManagedSitesConstants.Features.AdminPortal)]
public sealed class AdminPortalStartup : StartupBase
{
	public override void ConfigureServices(IServiceCollection services)
	{
		services.TryAddSingleton(TimeProvider.System);

		services.AddScoped<IManagedSiteClearanceService, ManagedSiteClearanceService>();
		services.AddScoped<IManagedSiteSessionStore, SiteSettingsManagedSiteSessionStore>();
		services.AddScoped<IManagedSiteSessionService, ManagedSiteSessionService>();

		services.AddNavigationProvider<AdminPortalMenu>();

		services.AddTransient<IConfigureOptions<AuthorizationOptions>, ManagedSitesApiAuthorizationOptionsConfiguration>();
	}
}

/// <summary>
/// Registers Managed Site clearance: the user editor that grants it and the claims provider that
/// carries it into the signed principal.
/// </summary>
[Feature(ManagedSitesConstants.Features.Permissions)]
public sealed class PermissionsStartup : StartupBase
{
	public override void ConfigureServices(IServiceCollection services)
	{
		services.AddDisplayDriver<User, ManagedSiteClearanceDisplayDriver>();
		services.AddScoped<IUserClaimsProvider, ManagedSiteClaimsProvider>();
	}
}
