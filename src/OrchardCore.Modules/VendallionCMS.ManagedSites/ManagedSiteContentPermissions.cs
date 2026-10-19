using OrchardCore.Security.Permissions;

namespace VendallionCMS.ManagedSites;

/// <summary>
/// Generates one set of permissions per Managed Site, the way the platform generates one per media
/// folder and one per content type.
/// </summary>
/// <remarks>
/// Which Managed Site a person may work on is a question about that Managed Site, so it is answered by
/// a permission naming it rather than by a record kept against the person. Following the platform here
/// is what lets a Managed Site's content and its media folder be granted together, in the screen
/// administrators already use, by a recipe step that needs nothing special, and with the implications
/// carried by the platform's own permission graph.
///
/// The implications run from the larger authority to the smaller, as they do for content types:
/// publishing a Managed Site's content implies editing it, editing implies previewing it, and all of
/// them imply reading it. Reading is the floor rather than a scope of its own, which is the rule the
/// earlier clearance model expressed by granting no scope at all.
///
/// Only previewing runs one way on its own. An editor writes the drafts, so withholding the preview of
/// their own unpublished work would withhold nothing; but somebody can be shown unpublished work
/// without being handed the ability to change it.
///
/// Every one of them is also implied by governing Managed Sites, for the same reason the content
/// screens are: whoever may grant authority over a Managed Site can grant it to themselves, so
/// withholding it from them would be a formality. It is also what the platform does with media, where
/// the permission to view every folder implies the permission to view each one.
/// </remarks>
public static class ManagedSiteContentPermissions
{
    /// <summary>
    /// Allows publishing one Managed Site's own content.
    /// </summary>
    public static readonly Permission PublishManagedSiteContent = new(
        "PublishManagedSiteContent_{0}",
        "Publish the content of managed site '{0}'",
        [Permissions.ManageManagedSites]);

    /// <summary>
    /// Allows changing one Managed Site's own content.
    /// </summary>
    public static readonly Permission EditManagedSiteContent = new(
        "EditManagedSiteContent_{0}",
        "Edit the content of managed site '{0}'",
        [PublishManagedSiteContent]);

    /// <summary>
    /// Allows seeing one Managed Site's unpublished work.
    /// </summary>
    /// <remarks>
    /// Implied by editing, because an editor writes the drafts: they already read them in the content
    /// editor, and showing the same content composed on the Managed Site's own address discloses
    /// nothing they could not already see. It does not run the other way, so somebody can be shown
    /// unpublished work without being able to change it.
    /// </remarks>
    public static readonly Permission PreviewManagedSiteContent = new(
        "PreviewManagedSiteContent_{0}",
        "Preview the unpublished content of managed site '{0}'",
        [EditManagedSiteContent]);

    /// <summary>
    /// Allows reading one Managed Site's content, which is the least any authority over it carries.
    /// </summary>
    public static readonly Permission ViewManagedSiteContent = new(
        "ViewManagedSiteContent_{0}",
        "View the content of managed site '{0}'",
        [EditManagedSiteContent, PreviewManagedSiteContent]);

    /// <summary>
    /// The template answering each clearance scope, and the floor when a scope is not named.
    /// </summary>
    private static readonly Dictionary<string, Permission> _templatesByScope = new(StringComparer.OrdinalIgnoreCase)
    {
        [ManagedSitesConstants.Scopes.Edit] = EditManagedSiteContent,
        [ManagedSitesConstants.Scopes.Publish] = PublishManagedSiteContent,
        [ManagedSitesConstants.Scopes.Preview] = PreviewManagedSiteContent,
    };

    private static Dictionary<(string Template, string ManagedSiteCode), Permission> s_permissionsByManagedSite = [];

    /// <summary>
    /// Gets every template, in the order an administrator reads them.
    /// </summary>
    public static IReadOnlyList<Permission> Templates { get; } =
    [
        ViewManagedSiteContent,
        EditManagedSiteContent,
        PublishManagedSiteContent,
        PreviewManagedSiteContent,
    ];

    /// <summary>
    /// Gets the permission that answers a scope for one Managed Site.
    /// </summary>
    /// <param name="managedSiteCode">The Managed Site code.</param>
    /// <param name="scope">The scope, or <see langword="null" /> for reading, which is the floor.</param>
    /// <returns>The permission, or <see langword="null" /> when the scope is not one this feature has.</returns>
    public static Permission For(string managedSiteCode, string scope)
    {
        if (string.IsNullOrEmpty(managedSiteCode))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            return Create(ViewManagedSiteContent, managedSiteCode);
        }

        return _templatesByScope.TryGetValue(scope, out var template)
            ? Create(template, managedSiteCode)
            : null;
    }

    /// <summary>
    /// Builds every permission one Managed Site has, for the roles screen.
    /// </summary>
    /// <param name="managedSiteCode">The Managed Site code.</param>
    /// <param name="managedSiteName">What to call it in the screen.</param>
    /// <returns>The permissions, grouped under the Managed Site by category.</returns>
    public static IEnumerable<Permission> ForManagedSite(string managedSiteCode, string managedSiteName)
    {
        foreach (var template in Templates)
        {
            var permission = Create(template, managedSiteCode);

            yield return new Permission(
                permission.Name,
                string.Format(template.Description, managedSiteName ?? managedSiteCode),
                permission.ImpliedBy)
            {
                Category = $"Managed Site - {managedSiteName ?? managedSiteCode}",
            };
        }
    }

    /// <summary>
    /// Stamps a template with one Managed Site's code, keeping the implications stamped with it too.
    /// </summary>
    /// <remarks>
    /// Cached because authorization asks for the same handful over and over, and because two instances
    /// naming the same permission would still compare as different objects.
    /// </remarks>
    /// <param name="template">The template.</param>
    /// <param name="managedSiteCode">The Managed Site code.</param>
    /// <returns>The permission.</returns>
    public static Permission Create(Permission template, string managedSiteCode)
    {
        ArgumentNullException.ThrowIfNull(template);

        var key = (template.Name, managedSiteCode);

        if (s_permissionsByManagedSite.TryGetValue(key, out var permission))
        {
            return permission;
        }

        permission = new Permission(
            string.Format(template.Name, managedSiteCode),
            string.Format(template.Description, managedSiteCode),
            (template.ImpliedBy ?? [])
                .Where(implied => implied is not null)
                .Select(implied => Create(implied, managedSiteCode)));

        // Replaced wholesale rather than written into, so a reader never sees a half-built dictionary.
        s_permissionsByManagedSite = new Dictionary<(string, string), Permission>(s_permissionsByManagedSite)
        {
            [key] = permission,
        };

        return permission;
    }
}
