using OrchardCore.Entities;
using OrchardCore.Settings;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Stores active Managed Site session scopes in the Site Blueprint tenant settings.
/// </summary>
public sealed class SiteSettingsManagedSiteSessionStore : IManagedSiteSessionStore
{
    private readonly ISiteService _siteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SiteSettingsManagedSiteSessionStore" /> class.
    /// </summary>
    /// <param name="siteService">The site service.</param>
    public SiteSettingsManagedSiteSessionStore(ISiteService siteService)
    {
        _siteService = siteService;
    }

    /// <inheritdoc />
    public async ValueTask<ActiveManagedSiteSessionScope> GetAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var site = await _siteService.GetSiteSettingsAsync();
        var document = site.GetOrCreate<ManagedSiteSessionsDocument>();

        return document.ActiveScopes.TryGetValue(userId, out var scope) ? scope : null;
    }

    /// <inheritdoc />
    public async ValueTask SetAsync(ActiveManagedSiteSessionScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (string.IsNullOrEmpty(scope.UserId))
        {
            throw new ArgumentException("The active scope must identify a user.", nameof(scope));
        }

        var site = await _siteService.LoadSiteSettingsAsync();
        var document = site.GetOrCreate<ManagedSiteSessionsDocument>();

        document.ActiveScopes[scope.UserId] = scope;

        site.Put(document);

        await _siteService.UpdateSiteSettingsAsync(site);
    }

    /// <inheritdoc />
    public async ValueTask ClearAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var site = await _siteService.LoadSiteSettingsAsync();
        var document = site.GetOrCreate<ManagedSiteSessionsDocument>();

        if (document.ActiveScopes.Remove(userId))
        {
            site.Put(document);

            await _siteService.UpdateSiteSettingsAsync(site);
        }
    }
}
