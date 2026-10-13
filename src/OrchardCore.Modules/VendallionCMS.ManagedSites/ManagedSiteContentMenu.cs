using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites;

/// <summary>
/// Adds the entry an editor reaches a Managed Site's content through.
/// </summary>
/// <remarks>
/// The entry goes straight to the Managed Site when the editor holds exactly one, because a list of one
/// is not a choice, and to the chooser when they hold several. Which it is depends on who is reading the
/// menu, so the clearances are resolved while it is built.
/// </remarks>
public sealed class ManagedSiteContentMenu : AdminNavigationProvider
{
    private readonly IManagedSiteClearanceService _clearanceService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    internal readonly IStringLocalizer S;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteContentMenu" /> class.
    /// </summary>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor, which carries the reader.</param>
    /// <param name="stringLocalizer">The string localizer.</param>
    public ManagedSiteContentMenu(
        IManagedSiteClearanceService clearanceService,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<ManagedSiteContentMenu> stringLocalizer)
    {
        _clearanceService = clearanceService;
        _httpContextAccessor = httpContextAccessor;
        S = stringLocalizer;
    }

    /// <inheritdoc />
    protected override async ValueTask BuildAsync(NavigationBuilder builder)
    {
        var code = await OnlyManagedSiteCodeAsync();

        builder
            .Add(S["Managed Sites"], managedSites => managedSites
                .Add(S["Content"], S["Content"].PrefixPosition(), content => content
                    .Permission(Permissions.EditManagedSiteContent)
                    .Action(
                        code is null ? "Sites" : "Index",
                        "ManagedSiteContent",
                        code is null
                            ? new { area = ManagedSitesConstants.Features.ManagedSites }
                            : new { area = ManagedSitesConstants.Features.ManagedSites, managedSiteCode = code })
                    .LocalNav()
                )
            );
    }

    /// <summary>
    /// Gives the code of the one Managed Site the reader may work on, when there is exactly one.
    /// </summary>
    /// <returns>The code, or <see langword="null" /> when they hold none or several.</returns>
    private async ValueTask<string> OnlyManagedSiteCodeAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user is not ClaimsPrincipal principal || principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var managedSites = await _clearanceService.GetAuthorizedManagedSitesAsync(principal);

        return managedSites.Count == 1 ? managedSites[0].Code : null;
    }
}
