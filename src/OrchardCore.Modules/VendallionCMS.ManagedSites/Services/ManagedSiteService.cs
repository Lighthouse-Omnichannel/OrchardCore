using OrchardCore.Entities;
using OrchardCore.Settings;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Manages Managed Site definitions stored in site settings.
/// </summary>
public sealed class ManagedSiteService : IManagedSiteService
{
    private readonly ISiteService _siteService;
    private readonly IShellUrlSynchronizationService _shellUrlSynchronizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteService" /> class.
    /// </summary>
    /// <param name="siteService">The site service.</param>
    /// <param name="shellUrlSynchronizationService">The tenant hostname synchronization service.</param>
    public ManagedSiteService(
        ISiteService siteService,
        IShellUrlSynchronizationService shellUrlSynchronizationService)
    {
        _siteService = siteService;
        _shellUrlSynchronizationService = shellUrlSynchronizationService;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedSite> GetAsync(string managedSiteId)
    {
        if (string.IsNullOrEmpty(managedSiteId))
        {
            return null;
        }

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
    public async ValueTask<ManagedSite> FindByAddressAsync(string host, string path)
    {
        var document = await GetDocumentAsync();

        var candidates = document.ManagedSites
            .Where(managedSite => managedSite.Status == ManagedSiteStatus.Enabled)
            .Where(managedSite => ManagedSiteAddressValidator.Matches(managedSite, host, path))
            .ToArray();

        // A Managed Site naming the request host wins over one answering on every host, and a longer
        // prefix wins over a shorter one, so the most specific claim resolves the request.
        return candidates
            .OrderByDescending(managedSite => ManagedSiteAddressValidator.SplitHostnames(managedSite.Hostname).Length > 0)
            .ThenByDescending(managedSite => ManagedSiteAddressValidator.NormalizePrefix(managedSite.UrlPrefix).Length)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(ManagedSite managedSite)
    {
        ArgumentNullException.ThrowIfNull(managedSite);

        if (string.IsNullOrWhiteSpace(managedSite.Id))
        {
            throw new ManagedSiteValidationException(
                ManagedSitesConstants.ErrorCodes.ManagedSiteNotFound,
                "A Managed Site must have an identifier.");
        }

        if (string.IsNullOrWhiteSpace(managedSite.Name))
        {
            throw new ManagedSiteValidationException(
                ManagedSitesConstants.ErrorCodes.InvalidName,
                "A Managed Site must have a name.");
        }

        managedSite.Name = managedSite.Name.Trim();

        var site = await _siteService.LoadSiteSettingsAsync();
        var document = site.GetOrCreate<ManagedSitesDocument>();

        ValidateNameIsUnique(managedSite, document);

        var others = document.ManagedSites
            .Where(existing => existing.Id != managedSite.Id)
            .ToArray();

        ManagedSiteAddressValidator.ValidateNoConflict(managedSite, others);

        var replaced = document.ManagedSites.FirstOrDefault(item => item.Id == managedSite.Id);
        if (replaced != null)
        {
            document.ManagedSites.Remove(replaced);
        }

        document.ManagedSites.Add(managedSite);

        _shellUrlSynchronizationService.Synchronize(document);

        site.Put(document);

        await _siteService.UpdateSiteSettingsAsync(site);
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteAsync(string managedSiteId)
    {
        if (string.IsNullOrEmpty(managedSiteId))
        {
            return false;
        }

        var site = await _siteService.LoadSiteSettingsAsync();
        var document = site.GetOrCreate<ManagedSitesDocument>();

        var existing = document.ManagedSites.FirstOrDefault(item => item.Id == managedSiteId);
        if (existing == null)
        {
            return false;
        }

        document.ManagedSites.Remove(existing);

        _shellUrlSynchronizationService.Synchronize(document);

        site.Put(document);

        await _siteService.UpdateSiteSettingsAsync(site);

        return true;
    }

    private static void ValidateNameIsUnique(ManagedSite managedSite, ManagedSitesDocument document)
    {
        var hasConflict = document.ManagedSites.Any(existing =>
            existing.Id != managedSite.Id &&
            string.Equals(existing.Name, managedSite.Name, StringComparison.OrdinalIgnoreCase));

        if (hasConflict)
        {
            throw new ManagedSiteValidationException(
                ManagedSitesConstants.ErrorCodes.NameConflict,
                $"A Managed Site named '{managedSite.Name}' already exists.");
        }
    }

    private async ValueTask<ManagedSitesDocument> GetDocumentAsync()
    {
        var site = await _siteService.GetSiteSettingsAsync();

        return site.GetOrCreate<ManagedSitesDocument>();
    }
}
