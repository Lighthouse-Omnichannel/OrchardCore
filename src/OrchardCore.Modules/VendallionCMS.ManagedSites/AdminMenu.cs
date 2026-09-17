using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace VendallionCMS.ManagedSites;

/// <summary>
/// Adds Managed Sites entries to the admin menu.
/// </summary>
public sealed class AdminMenu : AdminNavigationProvider
{
    private static readonly RouteValueDictionary s_definitionsRouteValues = new()
    {
        { "area", ManagedSitesConstants.Features.ManagedSites },
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
            .Add(S["Managed Sites"], managedSites => managedSites
                .Add(S["Sites"], S["Sites"].PrefixPosition(), sites => sites
                    .Permission(Permissions.ManageManagedSites)
                    .Action("Index", "Admin", s_definitionsRouteValues)
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}
