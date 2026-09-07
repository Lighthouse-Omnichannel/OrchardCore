using System.Text.Json.Nodes;
using OrchardCore.Settings;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Settings;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Manages the configured Site Blueprint for the current site.
/// </summary>
public sealed class SiteBlueprintService : ISiteBlueprintService
{
    private readonly ISiteService _siteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SiteBlueprintService" /> class.
    /// </summary>
    /// <param name="siteService">The site service.</param>
    public SiteBlueprintService(ISiteService siteService)
    {
        _siteService = siteService;
    }

    /// <inheritdoc />
    public async ValueTask<SiteBlueprint> GetAsync()
    {
        var settings = await _siteService.GetSettingsAsync<SiteBlueprintSettings>();

        if (!settings.IsSiteBlueprint)
        {
            return null;
        }

        return new SiteBlueprint
        {
            Id = "default",
            Name = settings.Name,
            IsEnabled = settings.IsSiteBlueprint,
        };
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(SiteBlueprint blueprint)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Properties[nameof(SiteBlueprintSettings)] = new JsonObject
        {
            [nameof(SiteBlueprintSettings.IsSiteBlueprint)] = blueprint.IsEnabled,
            [nameof(SiteBlueprintSettings.Name)] = blueprint.Name,
        };

        await _siteService.UpdateSiteSettingsAsync(site);
    }
}
