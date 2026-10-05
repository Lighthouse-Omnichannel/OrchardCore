using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using OrchardCore.Entities;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using OrchardCore.Users;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites.Recipes;

/// <summary>
/// Grants users clearance to Managed Sites from a recipe, creating them when they do not exist.
/// </summary>
/// <remarks>
/// Clearance is stored as a section on the user, and the platform's <c>Users</c> step copies only the
/// fields it knows about, so it carries no clearance. Without this step a recipe can create the people
/// who edit Managed Sites but not give them anything to edit.
///
/// It creates users as well as clearing them, which is a wider job than the name clearance suggests, and
/// deliberate: the platform's step takes a password hash rather than a password, so a recipe cannot
/// produce an account anybody can sign in to. A password here is honoured only when the user is being
/// created. An existing user's password is never changed, so running a recipe again cannot be used to
/// take an account over.
///
/// Managed Sites are named rather than identified, because a recipe that declared them by name a few
/// steps earlier does not know the identifiers they were given.
/// </remarks>
public sealed class ManagedSiteEditorsStep : NamedRecipeStepHandler
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IUserService _userService;
    private readonly UserManager<IUser> _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteEditorsStep" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="userService">The user service, which creates a user from a password.</param>
    /// <param name="userManager">The user manager.</param>
    public ManagedSiteEditorsStep(
        IManagedSiteService managedSiteService,
        IUserService userService,
        UserManager<IUser> userManager)
        : base("ManagedSiteEditors")
    {
        _managedSiteService = managedSiteService;
        _userService = userService;
        _userManager = userManager;
    }

    /// <inheritdoc />
    protected override async Task HandleAsync(RecipeExecutionContext context)
    {
        var model = context.Step.ToObject<ManagedSiteEditorsStepModel>();

        if (model?.Editors is null)
        {
            return;
        }

        var managedSites = await _managedSiteService.ListAsync();

        foreach (var editor in model.Editors)
        {
            if (string.IsNullOrWhiteSpace(editor.UserName))
            {
                context.Errors.Add("An editor in this recipe has no user name.");

                continue;
            }

            var user = await FindOrCreateAsync(editor, context);

            if (user is null)
            {
                continue;
            }

            var settings = user.GetOrCreate<ManagedSiteClearanceSettings>();

            ApplyGrants(settings, editor, managedSites, context.Errors);

            user.Put(settings);

            await _userManager.UpdateAsync(user);
        }
    }

    /// <summary>
    /// Writes the Managed Sites an editor is cleared for onto their clearance.
    /// </summary>
    /// <remarks>
    /// Replaces the grant for a Managed Site rather than adding beside it, so running a recipe again
    /// states what the clearance is rather than accumulating what it has ever been.
    /// </remarks>
    /// <param name="settings">The editor's clearance, which is modified in place.</param>
    /// <param name="editor">The editor the recipe declares.</param>
    /// <param name="managedSites">The Managed Sites that exist, to resolve names against.</param>
    /// <param name="errors">Where a grant naming no known Managed Site is reported.</param>
    internal static void ApplyGrants(
        ManagedSiteClearanceSettings settings,
        ManagedSiteEditorEntry editor,
        IReadOnlyList<ManagedSite> managedSites,
        IList<string> errors)
    {
        foreach (var grant in editor.Grants ?? [])
        {
            var managedSite = managedSites.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, grant.ManagedSite?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (managedSite is null)
            {
                errors.Add(
                    $"'{editor.UserName}' was not cleared for '{grant.ManagedSite}', which no Managed Site is named.");

                continue;
            }

            settings.Grants.RemoveAll(existing =>
                string.Equals(existing.ManagedSiteId, managedSite.Id, StringComparison.Ordinal));

            settings.Grants.Add(new ManagedSiteClearanceGrant
            {
                ManagedSiteId = managedSite.Id,

                // A grant naming no action is view only, which is what the clearance editor records for
                // a Managed Site selected with nothing ticked.
                Scopes = grant.Scopes is { Length: > 0 }
                    ? [.. grant.Scopes]
                    : [ManagedSitesConstants.Scopes.View],
            });
        }
    }

    private async Task<User> FindOrCreateAsync(ManagedSiteEditorEntry editor, RecipeExecutionContext context)
    {
        if (await _userManager.FindByNameAsync(editor.UserName) is User existing)
        {
            if (editor.RoleNames is { Length: > 0 })
            {
                await _userManager.AddToRolesAsync(
                    existing,
                    editor.RoleNames.Where(role => !existing.RoleNames.Contains(role, StringComparer.OrdinalIgnoreCase)));
            }

            return existing;
        }

        if (string.IsNullOrEmpty(editor.Password))
        {
            context.Errors.Add(
                $"'{editor.UserName}' does not exist and the recipe gives no password to create them with.");

            return null;
        }

        var created = new User
        {
            UserName = editor.UserName,
            Email = editor.Email ?? $"{editor.UserName}@example.invalid",
            EmailConfirmed = true,
            IsEnabled = true,
            RoleNames = [.. editor.RoleNames ?? []],
        };

        var errors = new List<string>();
        var result = await _userService.CreateUserAsync(created, editor.Password, (_, message) => errors.Add(message));

        if (result is User user)
        {
            return user;
        }

        context.Errors.Add($"'{editor.UserName}' was not created: {string.Join(" ", errors)}");

        return null;
    }
}

/// <summary>
/// The shape of a <c>ManagedSiteEditors</c> recipe step.
/// </summary>
public sealed class ManagedSiteEditorsStepModel
{
    /// <summary>
    /// Gets or sets the editors the recipe provisions.
    /// </summary>
    public ManagedSiteEditorEntry[] Editors { get; set; }
}

/// <summary>
/// One editor in a <c>ManagedSiteEditors</c> recipe step.
/// </summary>
public sealed class ManagedSiteEditorEntry
{
    /// <summary>
    /// Gets or sets the user name, which is what an existing user is matched on.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the email address, defaulting to one on the reserved example domain.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the password to create the user with.
    /// </summary>
    /// <remarks>
    /// Used only when the user does not exist. An existing user's password is never changed, so a
    /// recipe cannot be used to take an account over.
    /// </remarks>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the roles the user should hold.
    /// </summary>
    public string[] RoleNames { get; set; }

    /// <summary>
    /// Gets or sets the Managed Sites the user is cleared for.
    /// </summary>
    public ManagedSiteGrantEntry[] Grants { get; set; }
}

/// <summary>
/// One Managed Site an editor is cleared for.
/// </summary>
public sealed class ManagedSiteGrantEntry
{
    /// <summary>
    /// Gets or sets the Managed Site's name.
    /// </summary>
    public string ManagedSite { get; set; }

    /// <summary>
    /// Gets or sets what the editor may do: <c>view</c>, <c>edit</c>, <c>publish</c>, <c>preview</c>.
    /// </summary>
    /// <remarks>
    /// Naming none grants view alone. Each of the others implies view, so listing it as well is
    /// harmless but unnecessary.
    /// </remarks>
    public string[] Scopes { get; set; }
}
