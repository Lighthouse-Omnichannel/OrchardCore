using OrchardCore.Settings;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Manages URL registrations for the Site Blueprint and Managed Sites.
/// </summary>
public sealed class UrlRegistrationService : IUrlRegistrationService
{
    private readonly ISiteService _siteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlRegistrationService" /> class.
    /// </summary>
    /// <param name="siteService">The site service.</param>
    public UrlRegistrationService(ISiteService siteService)
    {
        _siteService = siteService;
    }

    /// <inheritdoc />
    public async ValueTask<UrlRegistration> FindByUrlAsync(string url)
    {
        var site = await _siteService.GetSiteSettingsAsync();
        var document = site.GetOrCreate<ManagedSitesDocument>();
        var normalizedUrl = UrlRegistrationValidator.Normalize(url);

        return document.UrlRegistrations.FirstOrDefault(registration =>
            registration.Status == UrlRegistrationStatus.Active &&
            UrlRegistrationValidator.Normalize(registration.Url) == normalizedUrl);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(UrlRegistration registration)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        var document = site.GetOrCreate<ManagedSitesDocument>();

        UrlRegistrationValidator.ValidateNoConflict(registration, document.UrlRegistrations);

        var existing = document.UrlRegistrations.FirstOrDefault(item => item.Id == registration.Id);
        if (existing != null)
        {
            document.UrlRegistrations.Remove(existing);
        }

        document.UrlRegistrations.Add(registration);

        await _siteService.UpdateSiteSettingsAsync(site);
    }
}
