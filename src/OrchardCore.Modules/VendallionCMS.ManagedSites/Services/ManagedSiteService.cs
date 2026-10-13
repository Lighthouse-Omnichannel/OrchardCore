using OrchardCore.Entities;
using OrchardCore.Settings;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Manages Managed Site definitions stored in site settings.
/// </summary>
public sealed partial class ManagedSiteService : IManagedSiteService
{
    private readonly ISiteService _siteService;
    private readonly IShellUrlSynchronizationService _shellUrlSynchronizationService;
    private readonly IManagedSiteCompositionCacheService _compositionCacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteService" /> class.
    /// </summary>
    /// <param name="siteService">The site service.</param>
    /// <param name="shellUrlSynchronizationService">The tenant hostname synchronization service.</param>
    /// <param name="compositionCacheService">
    /// The composition cache service, or <see langword="null" /> when composition is not being cached.
    /// </param>
    public ManagedSiteService(
        ISiteService siteService,
        IShellUrlSynchronizationService shellUrlSynchronizationService,
        IManagedSiteCompositionCacheService compositionCacheService = null)
    {
        _siteService = siteService;
        _shellUrlSynchronizationService = shellUrlSynchronizationService;
        _compositionCacheService = compositionCacheService;
    }

    /// <summary>
    /// Drops composition state that depended on which addresses resolve to which Managed Site.
    /// </summary>
    /// <remarks>
    /// Optional because the Managed Site store is usable without a cache behind it, and a store that
    /// refused to work unless one was configured would be the wrong dependency direction.
    /// </remarks>
    private ValueTask InvalidateAddressesAsync()
        => _compositionCacheService is null
            ? ValueTask.CompletedTask
            : _compositionCacheService.InvalidateAsync(managedSiteCode: null, ManagedSiteCompositionArea.Address);

    /// <inheritdoc />
    public async ValueTask<ManagedSite> GetAsync(string managedSiteCode)
    {
        if (string.IsNullOrEmpty(managedSiteCode))
        {
            return null;
        }

        var document = await GetDocumentAsync();

        return document.ManagedSites.FirstOrDefault(managedSite => managedSite.Code == managedSiteCode);
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

        return ManagedSiteAddressValidator.FindBestMatch(document.ManagedSites, host, path);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(ManagedSite managedSite)
    {
        ArgumentNullException.ThrowIfNull(managedSite);

        managedSite.Code = managedSite.Code?.Trim();

        if (string.IsNullOrEmpty(managedSite.Code))
        {
            throw new ManagedSiteValidationException(
                ManagedSitesConstants.ErrorCodes.InvalidCode,
                "A Managed Site must have a code.");
        }

        if (managedSite.Code.Length > ManagedSitesConstants.Code.MaximumLength)
        {
            throw new ManagedSiteValidationException(
                ManagedSitesConstants.ErrorCodes.InvalidCode,
                $"A Managed Site code is at most {ManagedSitesConstants.Code.MaximumLength} characters.");
        }

        if (!ManagedSiteCodeRegex().IsMatch(managedSite.Code))
        {
            throw new ManagedSiteValidationException(
                ManagedSitesConstants.ErrorCodes.InvalidCode,
                "A Managed Site code is lowercase letters and digits, beginning with a letter.");
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
            .Where(existing => existing.Code != managedSite.Code)
            .ToArray();

        ManagedSiteAddressValidator.ValidateNoConflict(managedSite, others);

        var replaced = document.ManagedSites.FirstOrDefault(item => item.Code == managedSite.Code);
        if (replaced != null)
        {
            document.ManagedSites.Remove(replaced);
        }

        document.ManagedSites.Add(managedSite);

        _shellUrlSynchronizationService.Synchronize(document);

        await InvalidateAddressesAsync();

        site.Put(document);

        await _siteService.UpdateSiteSettingsAsync(site);
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteAsync(string managedSiteCode)
    {
        if (string.IsNullOrEmpty(managedSiteCode))
        {
            return false;
        }

        var site = await _siteService.LoadSiteSettingsAsync();
        var document = site.GetOrCreate<ManagedSitesDocument>();

        var existing = document.ManagedSites.FirstOrDefault(item => item.Code == managedSiteCode);
        if (existing == null)
        {
            return false;
        }

        document.ManagedSites.Remove(existing);

        _shellUrlSynchronizationService.Synchronize(document);

        await InvalidateAddressesAsync();

        site.Put(document);

        await _siteService.UpdateSiteSettingsAsync(site);

        return true;
    }

    private static void ValidateNameIsUnique(ManagedSite managedSite, ManagedSitesDocument document)
    {
        var hasConflict = document.ManagedSites.Any(existing =>
            existing.Code != managedSite.Code &&
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


    [System.Text.RegularExpressions.GeneratedRegex(ManagedSitesConstants.Code.Pattern)]
    private static partial System.Text.RegularExpressions.Regex ManagedSiteCodeRegex();
}
