using System.Text.Json.Nodes;
using OrchardCore.Entities;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites.Recipes;

/// <summary>
/// Declares Managed Sites from a recipe.
/// </summary>
/// <remarks>
/// Managed Sites are the one thing a recipe could not previously provision, which left the feature
/// unreachable from a setup recipe: the content could be shaped and the features enabled, but the sites
/// that content is for had to be typed in by hand afterwards.
///
/// Sites are matched by name rather than by identifier, because a recipe is written by a person and a
/// name is what they know. Running the same recipe twice updates the sites it names rather than adding
/// second copies of them.
///
/// Saving goes through <see cref="IManagedSiteService" /> rather than the store, so a recipe is held to
/// the same address rules as the admin screen: two Managed Sites cannot claim the same address, and the
/// host names declared here reach the tenant's own Hostname setting the same way.
/// </remarks>
public sealed class ManagedSitesStep : NamedRecipeStepHandler
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IIdGenerator _idGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSitesStep" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="idGenerator">The identifier generator.</param>
    public ManagedSitesStep(IManagedSiteService managedSiteService, IIdGenerator idGenerator)
        : base("ManagedSites")
    {
        _managedSiteService = managedSiteService;
        _idGenerator = idGenerator;
    }

    /// <inheritdoc />
    protected override async Task HandleAsync(RecipeExecutionContext context)
    {
        var model = context.Step.ToObject<ManagedSitesStepModel>();

        if (model?.ManagedSites is null)
        {
            return;
        }

        var existing = await _managedSiteService.ListAsync();

        foreach (var declared in model.ManagedSites)
        {
            if (string.IsNullOrWhiteSpace(declared.Name))
            {
                context.Errors.Add("A Managed Site in this recipe has no name.");

                continue;
            }

            var managedSite = existing.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, declared.Id, StringComparison.Ordinal)
                || string.Equals(candidate.Name, declared.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            // A recipe that fixes the identifier can refer to the Managed Site from the scopes it sets
            // later in the same recipe, which a generated one makes impossible.
            managedSite ??= new ManagedSite
            {
                Id = string.IsNullOrWhiteSpace(declared.Id) ? _idGenerator.GenerateUniqueId() : declared.Id.Trim(),
            };

            managedSite.Name = declared.Name;
            managedSite.Hostname = declared.Hostname;
            managedSite.UrlPrefix = declared.UrlPrefix;
            managedSite.Status = Enum.TryParse<ManagedSiteStatus>(declared.Status, ignoreCase: true, out var status)
                ? status
                : ManagedSiteStatus.Enabled;

            try
            {
                await _managedSiteService.SaveAsync(managedSite);
            }
            catch (ManagedSiteValidationException exception)
            {
                // Reported rather than thrown, so one unusable Managed Site does not abandon the rest of
                // a setup recipe partway through.
                context.Errors.Add($"Managed Site '{declared.Name}' was not saved: {exception.Message}");
            }
        }
    }
}

/// <summary>
/// The shape of a <c>ManagedSites</c> recipe step.
/// </summary>
public sealed class ManagedSitesStepModel
{
    /// <summary>
    /// Gets or sets the Managed Sites the recipe declares.
    /// </summary>
    public ManagedSiteStepEntry[] ManagedSites { get; set; }
}

/// <summary>
/// One Managed Site in a <c>ManagedSites</c> recipe step.
/// </summary>
public sealed class ManagedSiteStepEntry
{
    /// <summary>
    /// Gets or sets the identifier, which a recipe fixes when it needs to name this Managed Site again.
    /// </summary>
    /// <remarks>
    /// Setting a scope to <c>Selected</c> names Managed Sites by identifier, so a recipe that both
    /// declares a Managed Site and puts content in its scope has to know what that identifier will be.
    /// Left empty, one is generated.
    /// </remarks>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the name, which is what the recipe matches an existing Managed Site on when no
    /// identifier is given.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the host names this Managed Site answers on, separated by a comma or a space.
    /// </summary>
    /// <remarks>
    /// A host name claims every path on that host and the URL prefix is then not consulted. Leaving it
    /// empty is usually the better choice: a prefix needs no host file entry and no certificate.
    /// </remarks>
    public string Hostname { get; set; }

    /// <summary>
    /// Gets or sets the single path prefix this Managed Site answers under, without slashes.
    /// </summary>
    public string UrlPrefix { get; set; }

    /// <summary>
    /// Gets or sets the status, <c>Enabled</c> or <c>Disabled</c>, defaulting to <c>Enabled</c>.
    /// </summary>
    public string Status { get; set; }
}
