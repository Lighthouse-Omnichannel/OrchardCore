using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.DisplayManagement;
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
		services.AddScoped<IManagedContentSuppressionService, ManagedContentSuppressionService>();
		services.AddScoped<IManagedContentOverrideService, ManagedContentOverrideService>();
		services.AddScoped<IManagedContentResolutionService, ManagedContentResolutionService>();
		services.AddScoped<IManagedSiteCompositionContextAccessor, ManagedSiteCompositionContextAccessor>();

		services.AddContentPart<ManagedContentPart>()
			.UseDisplayDriver<ManagedContentPartDisplayDriver>();

		// An override is written by the override service, never attached in the content type editor, so
		// the type is registered for deserialization without a display driver.
		services.AddContentPart<ManagedContentOverridePart>();

		services.AddIndexProvider<ManagedContentEditScopeIndexProvider>();
		services.AddIndexProvider<ManagedContentOverrideIndexProvider>();

		// Serving a Managed Site its own version of an item means replacing what the item renders, which
		// no part driver can do. The platform display manager is wrapped rather than replaced, and hands
		// every item that carries no Managed Content straight through.
		services.AddTransient<ContentItemDisplayManager>();
		services.Replace(ServiceDescriptor.Transient<IContentItemDisplayManager>(serviceProvider =>
			new ManagedContentItemDisplayManager(
				serviceProvider.GetRequiredService<ContentItemDisplayManager>(),
				serviceProvider.GetRequiredService<IManagedContentResolutionService>(),
				serviceProvider.GetRequiredService<IManagedSiteCompositionContextAccessor>(),
				serviceProvider.GetRequiredService<IHttpContextAccessor>(),
				serviceProvider.GetRequiredService<IShapeFactory>())));
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
