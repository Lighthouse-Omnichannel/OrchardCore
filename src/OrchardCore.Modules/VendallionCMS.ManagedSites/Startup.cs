using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Recipes;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using VendallionCMS.ManagedSites.Drivers;
using VendallionCMS.ManagedSites.Handlers;
using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Migrations;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Recipes;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites;

public sealed class Startup : StartupBase
{
	public override void ConfigureServices(IServiceCollection services)
	{
		services.AddDataMigration<ManagedSitesMigrations>();
		services.AddPermissionProvider<Permissions>();
		services.AddNavigationProvider<AdminMenu>();
		services.TryAddSingleton(TimeProvider.System);

		services.AddScoped<IManagedSiteAuthorizationService, ManagedSiteAuthorizationService>();

		// Clearance is no longer only a portal concern. Content authorization asks it whether a Managed
		// Site may act on its own override content, so it belongs to the feature that defines Managed
		// Sites rather than to the one that renders the portal.
		services.AddScoped<IManagedSiteClearanceService, ManagedSiteClearanceService>();
		services.AddScoped<IManagedSiteService, ManagedSiteService>();
		services.AddScoped<IShellUrlSynchronizationService, ShellUrlSynchronizationService>();
		services.AddScoped<IManagedContentScopeService, ManagedContentScopeService>();
		services.AddScoped<IManagedContentScopeAuthorizationHandler, ManagedContentScopeAuthorizationHandler>();
		services.AddScoped<IManagedContentLocator, ManagedContentLocator>();
		services.AddScoped<IManagedContentSuppressionService, ManagedContentSuppressionService>();
		services.AddScoped<IManagedContentOverrideService, ManagedContentOverrideService>();
		services.AddScoped<IManagedContentResolutionService, ManagedContentResolutionService>();

		// What a Managed Site may customize is an answer about the feature, not about whatever renders
		// it, so the surface that shows it is not the one that works it out.
		services.AddScoped<IManagedContentListService, ManagedContentListService>();
		services.AddScoped<IManagedSiteCompositionContextAccessor, ManagedSiteCompositionContextAccessor>();

		services.AddContentPart<ManagedContentPart>()
			.UseDisplayDriver<ManagedContentPartDisplayDriver>();

		// An override is written by the override service, never attached in the content type editor, so
		// the type is registered for deserialization without a display driver.
		services.AddContentPart<ManagedContentOverridePart>();

		// An item-level driver, because the override part is welded on by the override service rather
		// than declared by any content type, and a part driver only runs for parts the type declares.
		services.AddScoped<IContentDisplayDriver, ManagedContentOverrideDisplayDriver>();

		// A recipe can declare Managed Sites and clear people for them, which is how a tenant is
		// provisioned without anyone retyping it.
		services.AddRecipeExecutionStep<ManagedSitesStep>();
		services.AddRecipeExecutionStep<ManagedSiteEditorsStep>();

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
/// Resolves incoming public requests to a Managed Site, and keeps composed output fresh.
/// </summary>
/// <remarks>
/// This is what makes every other part of the feature visible. Without the middleware nothing tells a
/// request which Managed Site it belongs to, so rendering always takes the Site Blueprint branch and
/// no override is ever served.
/// </remarks>
[Feature(ManagedSitesConstants.Features.Routing)]
public sealed class RoutingStartup : StartupBase
{
	// Ahead of the default so the Managed Site is resolved before anything renders with it.
	public override int Order => -100;

	public override void ConfigureServices(IServiceCollection services)
	{
		// Added through a startup filter rather than this feature's Configure, because OrchardCore
		// calls UseRouting before a module configures the pipeline and an endpoint is chosen there. A
		// Managed Site answering under a URL prefix needs that prefix moved onto the path base before
		// routing, exactly as a tenant's own prefix is.
		services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, ManagedSiteRequestStartupFilter>();

		services.AddScoped<IManagedSiteUrlResolver, ManagedSiteUrlResolver>();
		services.AddScoped<IManagedSiteCompositionCacheService, ManagedSiteCompositionCacheService>();
		services.AddScoped<IContentHandler, ManagedSiteCompositionInvalidationHandler>();

		// Substitutes a Managed Site's own content as content loads, which is the only point every
		// consumer sees: a Liquid template that walks the content tree never asks the display manager
		// for anything.
		services.AddScoped<IContentHandler, ManagedContentCompositionHandler>();
	}

}

/// <summary>
/// Registers the Managed Site Admin Portal and the session scope services.
/// </summary>
[Feature(ManagedSitesConstants.Features.AdminPortal)]
public sealed class AdminPortalStartup : StartupBase
{
	public override void ConfigureServices(IServiceCollection services)
	{
		services.AddScoped<IManagedSiteSessionStore, SiteSettingsManagedSiteSessionStore>();
		services.AddScoped<IManagedSiteSessionService, ManagedSiteSessionService>();
		services.AddScoped<IManagedSitePreviewService, ManagedSitePreviewService>();

		services.AddNavigationProvider<AdminPortalMenu>();

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

		// FR-011a: a Managed Site's clearance authorizes content actions on that Managed Site's own
		// override content, and on nothing else.
		services.AddScoped<IAuthorizationHandler, ManagedSiteContentAuthorizationHandler>();
	}
}
