using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Users.Models;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Drivers;

/// <summary>
/// Adds the Managed Site clearance editor to the user admin screen.
/// </summary>
/// <remarks>
/// Grants recorded here are what the claims provider turns into clearance claims, so this editor is
/// the single place a user gains or loses access to a Managed Site.
/// </remarks>
public sealed class ManagedSiteClearanceDisplayDriver : SectionDisplayDriver<User, ManagedSiteClearanceSettings>
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteClearanceDisplayDriver" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site definition service.</param>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public ManagedSiteClearanceDisplayDriver(
        IManagedSiteService managedSiteService,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _managedSiteService = managedSiteService;
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public override IDisplayResult Edit(User user, ManagedSiteClearanceSettings settings, BuildEditorContext context)
    {
        return Initialize<ManagedSiteClearanceViewModel>("ManagedSiteClearance_Edit", async model =>
        {
            model.CanManage = await CanManageAsync();

            var managedSites = await _managedSiteService.ListAsync();
            var grants = settings.Grants.ToDictionary(
                grant => grant.ManagedSiteId ?? string.Empty,
                StringComparer.Ordinal);

            model.ManagedSites = managedSites
                .OrderBy(managedSite => managedSite.Name, StringComparer.OrdinalIgnoreCase)
                .Select(managedSite =>
                {
                    var hasGrant = grants.TryGetValue(managedSite.Id, out var grant);
                    var scopes = hasGrant
                        ? grant.Scopes.ToHashSet(StringComparer.OrdinalIgnoreCase)
                        : [];

                    // An empty scope set on an existing grant is the wildcard form, so every box is ticked.
                    var allScopes = hasGrant && scopes.Count == 0;

                    return new ManagedSiteClearanceEntryViewModel
                    {
                        ManagedSiteId = managedSite.Id,
                        Name = managedSite.Name,
                        Status = managedSite.Status.ToString(),
                        IsGranted = hasGrant,
                        View = allScopes || scopes.Contains(ManagedSitesConstants.Scopes.View),
                        Edit = allScopes || scopes.Contains(ManagedSitesConstants.Scopes.Edit),
                        Publish = allScopes || scopes.Contains(ManagedSitesConstants.Scopes.Publish),
                        Preview = allScopes || scopes.Contains(ManagedSitesConstants.Scopes.Preview),
                    };
                })
                .ToArray();

            var knownIds = managedSites.Select(managedSite => managedSite.Id).ToHashSet(StringComparer.Ordinal);
            model.OrphanedManagedSiteIds = settings.Grants
                .Select(grant => grant.ManagedSiteId)
                .Where(managedSiteId => !string.IsNullOrEmpty(managedSiteId) && !knownIds.Contains(managedSiteId))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        })
        .Location("Content:10")
        .RenderWhen(static driver => driver.CanManageAsync(), this);
    }

    /// <inheritdoc />
    public override async Task<IDisplayResult> UpdateAsync(
        User user,
        ManagedSiteClearanceSettings settings,
        UpdateEditorContext context)
    {
        if (!await CanManageAsync())
        {
            return Edit(user, settings, context);
        }

        var model = new ManagedSiteClearanceViewModel();
        await context.Updater.TryUpdateModelAsync(model, Prefix);

        var managedSites = await _managedSiteService.ListAsync();
        var knownIds = managedSites.Select(managedSite => managedSite.Id).ToHashSet(StringComparer.Ordinal);

        // Grants for Managed Sites that no longer exist are kept, so removing and restoring a Managed
        // Site does not silently drop everyone's access.
        var preserved = settings.Grants
            .Where(grant => !string.IsNullOrEmpty(grant.ManagedSiteId) && !knownIds.Contains(grant.ManagedSiteId))
            .ToArray();

        settings.Grants.Clear();

        foreach (var entry in model.ManagedSites ?? [])
        {
            if (!entry.IsGranted || string.IsNullOrEmpty(entry.ManagedSiteId) || !knownIds.Contains(entry.ManagedSiteId))
            {
                continue;
            }

            var grant = new ManagedSiteClearanceGrant { ManagedSiteId = entry.ManagedSiteId };

            AddScope(grant, ManagedSitesConstants.Scopes.View, entry.View);
            AddScope(grant, ManagedSitesConstants.Scopes.Edit, entry.Edit);
            AddScope(grant, ManagedSitesConstants.Scopes.Publish, entry.Publish);
            AddScope(grant, ManagedSitesConstants.Scopes.Preview, entry.Preview);

            // A granted site with no scope ticked would otherwise become the wildcard form and grant
            // more than the administrator selected, so it falls back to read-only access.
            if (grant.Scopes.Count == 0)
            {
                grant.Scopes.Add(ManagedSitesConstants.Scopes.View);
            }

            settings.Grants.Add(grant);
        }

        foreach (var grant in preserved)
        {
            settings.Grants.Add(grant);
        }

        return Edit(user, settings, context);
    }

    private static void AddScope(ManagedSiteClearanceGrant grant, string scope, bool isSelected)
    {
        if (isSelected)
        {
            grant.Scopes.Add(scope);
        }
    }

    private async Task<bool> CanManageAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        return httpContext is not null
            && await _authorizationService.AuthorizeAsync(httpContext.User, Permissions.ManageManagedSiteClearances);
    }
}
