using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
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

/// <summary>
/// Registers everything the module is made of.
/// </summary>
/// <remarks>
/// One feature, registered in one place. The parts below are sections of a single thing rather than
/// pieces anybody would run without the others: resolving a URL to a Managed Site is pointless without
/// the content rules that say what it may override, those rules decide nothing without the clearance
/// that authorizes them, and clearance is granted from the portal. Separating them only offered a way to
/// configure the module into a state where it could not work.
/// </remarks>
public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // ---- Storage ----

        services.AddDataMigration<ManagedSitesMigrations>();
        services.AddIndexProvider<ManagedContentEditScopeIndexProvider>();
        services.AddIndexProvider<ManagedContentOverrideIndexProvider>();

        // ---- Managed sites ----

        services.AddScoped<IManagedSiteService, ManagedSiteService>();
        services.AddScoped<IManagedSiteAuthorizationService, ManagedSiteAuthorizationService>();
        services.AddScoped<IManagedSiteClearanceService, ManagedSiteClearanceService>();
        services.AddScoped<IShellUrlSynchronizationService, ShellUrlSynchronizationService>();

        // ---- Managed content ----

        services.AddContentPart<ManagedContentPart>()
            .UseDisplayDriver<ManagedContentPartDisplayDriver>();

        // An override is written by the override service, never attached in the content type editor, so
        // the type is registered for deserialization without a display driver.
        services.AddContentPart<ManagedContentOverridePart>();

        // An item-level driver, because the override part is welded on by the override service rather
        // than declared by any content type, and a part driver only runs for parts the type declares.
        services.AddScoped<IContentDisplayDriver, ManagedContentOverrideDisplayDriver>();

        services.AddScoped<IManagedContentScopeService, ManagedContentScopeService>();
        services.AddScoped<IManagedContentScopeAuthorizationHandler, ManagedContentScopeAuthorizationHandler>();
        services.AddScoped<IManagedContentLocator, ManagedContentLocator>();
        services.AddScoped<IManagedContentSuppressionService, ManagedContentSuppressionService>();
        services.AddScoped<IManagedContentOverrideService, ManagedContentOverrideService>();
        services.AddScoped<IManagedContentResolutionService, ManagedContentResolutionService>();

        // What a Managed Site may customize is an answer about the module, not about whatever renders
        // it, so the surface that shows it is not the one that works it out.
        services.AddScoped<IManagedContentListService, ManagedContentListService>();

        // ---- Routing ----

        // Added through a startup filter rather than this module's Configure, because OrchardCore calls
        // UseRouting before a module configures the pipeline and an endpoint is chosen there. A Managed
        // Site answering under a URL prefix needs that prefix moved onto the path base before routing,
        // exactly as a tenant's own prefix is.
        services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, ManagedSiteRequestStartupFilter>();

        // Moving a Managed Site's prefix onto the path base is what makes the content underneath it
        // resolve, and the sign-in cookie's path follows that same path base. Left alone, a sign-in
        // becomes undoable: signing out from under a prefix writes the expiry somewhere the cookie is
        // not. The cookie belongs to the tenant, so it is pinned there.
        services.AddTransient<IConfigureOptions<CookieAuthenticationOptions>, ManagedSiteCookiePathConfiguration>();

        services.AddScoped<IManagedSiteUrlResolver, ManagedSiteUrlResolver>();

        // ---- Composition ----

        services.AddScoped<IManagedSiteCompositionContextAccessor, ManagedSiteCompositionContextAccessor>();
        services.AddScoped<IManagedSiteCompositionCacheService, ManagedSiteCompositionCacheService>();
        services.AddScoped<IContentHandler, ManagedSiteCompositionInvalidationHandler>();

        // Substitutes a Managed Site's own content as content loads, which is the only point every
        // consumer sees: a Liquid template that walks the content tree never asks the display manager
        // for anything.
        services.AddScoped<IContentHandler, ManagedContentCompositionHandler>();

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

        // ---- Permissions ----

        services.AddPermissionProvider<Permissions>();
        services.AddDisplayDriver<User, ManagedSiteClearanceDisplayDriver>();
        services.AddScoped<IUserClaimsProvider, ManagedSiteClaimsProvider>();

        // FR-011a: a Managed Site's clearance authorizes content actions on that Managed Site's own
        // override content, and on nothing else.
        services.AddScoped<IAuthorizationHandler, ManagedSiteContentAuthorizationHandler>();

        // ---- Admin portal ----

        services.AddScoped<IManagedSiteSessionStore, SiteSettingsManagedSiteSessionStore>();
        services.AddScoped<IManagedSiteSessionService, ManagedSiteSessionService>();
        services.AddScoped<IManagedSitePreviewService, ManagedSitePreviewService>();

        services.AddNavigationProvider<AdminMenu>();
        services.AddNavigationProvider<AdminPortalMenu>();

        // ---- Recipes ----

        // A recipe can declare Managed Sites and clear people for them, which is how a tenant is
        // provisioned without anyone retyping it.
        services.AddRecipeExecutionStep<ManagedSitesStep>();
        services.AddRecipeExecutionStep<ManagedSiteEditorsStep>();
    }
}
