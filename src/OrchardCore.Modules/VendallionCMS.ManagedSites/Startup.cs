using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Data.Migration;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using VendallionCMS.ManagedSites.Drivers;
using VendallionCMS.ManagedSites.Migrations;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.Settings;

namespace VendallionCMS.ManagedSites;

public sealed class Startup : StartupBase
{
	public override void ConfigureServices(IServiceCollection services)
	{
		services.AddDataMigration<ManagedSitesMigrations>();
		services.AddPermissionProvider<Permissions>();
		services.AddNavigationProvider<AdminMenu>();
		services.AddSiteDisplayDriver<SiteBlueprintSettingsDisplayDriver>();
		services.AddScoped<IManagedSiteAuthorizationService, ManagedSiteAuthorizationService>();
		services.AddScoped<IManagedSiteService, ManagedSiteService>();
		services.AddScoped<IUrlRegistrationService, UrlRegistrationService>();
		services.AddScoped<ISiteBlueprintService, SiteBlueprintService>();
	}
}
