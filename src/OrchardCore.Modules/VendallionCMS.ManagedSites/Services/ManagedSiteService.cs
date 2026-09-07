using OrchardCore.Settings;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Manages Managed Site definitions stored in site settings.
/// </summary>
public sealed class ManagedSiteService : IManagedSiteService
{
    private readonly ISiteService _siteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteService" /> class.
    /// </summary>
    /// <param name="siteService">The site service.</param>
    public ManagedSiteService(ISiteService siteService)
    {
        _siteService = siteService;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedSite> GetAsync(string managedSiteId)
    {
        var document = await GetDocumentAsync();

        return document.ManagedSites.FirstOrDefault(managedSite => managedSite.Id == managedSiteId);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ManagedSite>> ListAsync()
    {
        var document = await GetDocumentAsync();

        return document.ManagedSites.ToArray();
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(ManagedSite managedSite)
    {
        ArgumentNullException.ThrowIfNull(managedSite);

        var site = await _siteService.LoadSiteSettingsAsync();
        var document = site.GetOrCreate<ManagedSitesDocument>();

        foreach (var registration in managedSite.UrlRegistrations)
        {
            UrlRegistrationValidator.ValidateNoConflict(registration, document.UrlRegistrations.Where(existing => existing.OwnerId != managedSite.Id));
            registration.OwnerType = UrlRegistrationOwnerType.ManagedSite;
            registration.OwnerId = managedSite.Id;
        }

        var existing = document.ManagedSites.FirstOrDefault(item => item.Id == managedSite.Id);
        if (existing != null)
        {
            document.ManagedSites.Remove(existing);
        }

        document.ManagedSites.Add(managedSite);

        foreach (var registration in managedSite.UrlRegistrations)
        {
            var existingRegistration = document.UrlRegistrations.FirstOrDefault(item => item.Id == registration.Id);
            if (existingRegistration != null)
            {
                document.UrlRegistrations.Remove(existingRegistration);
            }

            document.UrlRegistrations.Add(registration);
        }

        await _siteService.UpdateSiteSettingsAsync(site);
    }

    private async ValueTask<ManagedSitesDocument> GetDocumentAsync()
    {
        var site = await _siteService.GetSiteSettingsAsync();

        return site.GetOrCreate<ManagedSitesDocument>();
    }
}
