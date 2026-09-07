using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using VendallionCMS.ManagedSites.Drivers;

namespace VendallionCMS.ManagedSites;

/// <summary>
/// Adds Managed Sites entries to the admin menu.
/// </summary>
public sealed class AdminMenu : AdminNavigationProvider
{
    private static readonly RouteValueDictionary s_routeValues = new()
    {
        { "area", "OrchardCore.Settings" },
        { "groupId", SiteBlueprintSettingsDisplayDriver.GroupId },
    };

    internal readonly IStringLocalizer S;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminMenu" /> class.
    /// </summary>
    /// <param name="stringLocalizer">The string localizer.</param>
    public AdminMenu(IStringLocalizer<AdminMenu> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override ValueTask BuildAsync(NavigationBuilder builder)
    {
        builder
            .Add(S["Settings"], settings => settings
                .Add(S["Managed Sites"], S["Managed Sites"].PrefixPosition(), managedSites => managedSites
                    .Permission(Permissions.ManageSiteBlueprint)
                    .Action("Index", "Admin", s_routeValues)
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}
