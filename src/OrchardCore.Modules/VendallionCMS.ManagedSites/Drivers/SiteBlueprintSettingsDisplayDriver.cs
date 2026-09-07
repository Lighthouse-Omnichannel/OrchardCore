using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings;
using VendallionCMS.ManagedSites.Settings;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Drivers;

/// <summary>
/// Displays and updates Site Blueprint settings.
/// </summary>
public sealed class SiteBlueprintSettingsDisplayDriver : SiteDisplayDriver<SiteBlueprintSettings>
{
    /// <summary>
    /// The settings group identifier.
    /// </summary>
    public const string GroupId = "managed-sites";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SiteBlueprintSettingsDisplayDriver" /> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="authorizationService">The authorization service.</param>
    public SiteBlueprintSettingsDisplayDriver(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
    }

    protected override string SettingsGroupId => GroupId;

    /// <inheritdoc />
    public override async Task<IDisplayResult> EditAsync(ISite site, SiteBlueprintSettings settings, BuildEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageSiteBlueprint))
        {
            return null;
        }

        return Initialize<SiteBlueprintSettingsViewModel>("SiteBlueprintSettings_Edit", model =>
        {
            model.IsSiteBlueprint = settings.IsSiteBlueprint;
            model.Name = settings.Name;
        }).Location("Content")
        .OnGroup(SettingsGroupId);
    }

    /// <inheritdoc />
    public override async Task<IDisplayResult> UpdateAsync(ISite site, SiteBlueprintSettings settings, UpdateEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageSiteBlueprint))
        {
            return null;
        }

        var model = new SiteBlueprintSettingsViewModel();

        await context.Updater.TryUpdateModelAsync(model, Prefix);

        settings.IsSiteBlueprint = model.IsSiteBlueprint;
        settings.Name = model.Name;

        return await EditAsync(site, settings, context);
    }
}
