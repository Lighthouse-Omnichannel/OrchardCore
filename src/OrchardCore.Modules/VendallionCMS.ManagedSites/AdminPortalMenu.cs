using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace VendallionCMS.ManagedSites;

/// <summary>
/// Adds the Managed Site Admin Portal entry to the admin menu.
/// </summary>
public sealed class AdminPortalMenu : AdminNavigationProvider
{
    internal readonly IStringLocalizer S;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminPortalMenu" /> class.
    /// </summary>
    /// <param name="stringLocalizer">The string localizer.</param>
    public AdminPortalMenu(IStringLocalizer<AdminPortalMenu> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override ValueTask BuildAsync(NavigationBuilder builder)
    {
        builder
            .Add(S["Managed Sites"], managedSites => managedSites
                .Add(S["Admin Portal"], S["Admin Portal"].PrefixPosition(), portal => portal
                    .Permission(Permissions.ManageManagedSites)
                    .Action("Index", "Portal", new { area = ManagedSitesConstants.Features.ManagedSites })
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}
