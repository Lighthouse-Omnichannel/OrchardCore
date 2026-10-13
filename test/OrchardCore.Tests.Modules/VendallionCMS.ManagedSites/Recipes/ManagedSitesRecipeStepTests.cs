using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using OrchardCore.Entities;
using OrchardCore.Recipes.Models;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Recipes;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Recipes;

/// <summary>
/// Covers declaring Managed Sites, and clearing people for them, from a recipe.
/// </summary>
/// <remarks>
/// Managed Sites were the one thing a recipe could not provision, which left the feature unreachable
/// from a setup recipe: the content could be shaped and the features enabled, but the sites that content
/// is for had to be typed in afterwards, and anyone investigating the feature spent an hour clicking
/// before they could try a single override.
///
/// Both steps match Managed Sites by name rather than by identifier, because a recipe is written by a
/// person and a name is what they know, and because a recipe that declared the sites a few steps earlier
/// does not know the identifiers they were given.
/// </remarks>
public class ManagedSitesRecipeStepTests
{
    [Fact]
    public async Task DeclaringAManagedSite_CreatesIt()
    {
        var service = new FakeManagedSiteService();

        await RunAsync(service, Step(new { Code = "alpha", Name = "Alpha", UrlPrefix = "alpha" }));

        var managedSite = Assert.Single(await service.ListAsync());
        Assert.Equal("Alpha", managedSite.Name);
        Assert.Equal("alpha", managedSite.UrlPrefix);
        Assert.Equal("alpha", managedSite.Code);
    }

    [Fact]
    public async Task ANewManagedSite_IsEnabled()
    {
        var service = new FakeManagedSiteService();

        await RunAsync(service, Step(new { Code = "alpha", Name = "Alpha", UrlPrefix = "alpha" }));

        Assert.Equal(ManagedSiteStatus.Enabled, (await service.ListAsync())[0].Status);
    }

    [Fact]
    public async Task AStatusTheRecipeNames_IsHonoured()
    {
        var service = new FakeManagedSiteService();

        await RunAsync(service, Step(new { Code = "alpha", Name = "Alpha", UrlPrefix = "alpha", Status = "Disabled" }));

        Assert.Equal(ManagedSiteStatus.Disabled, (await service.ListAsync())[0].Status);
    }

    [Fact]
    public async Task RunningTheSameRecipeAgain_UpdatesRatherThanDuplicates()
    {
        // A setup recipe is re-run while it is being written, and twice as many Managed Sites as the
        // recipe names would collide on their own addresses.
        var service = new FakeManagedSiteService();
        var step = Step(new { Code = "alpha", Name = "Alpha", UrlPrefix = "alpha" });

        await RunAsync(service, step);
        var firstCode = (await service.ListAsync())[0].Code;

        await RunAsync(service, Step(new { Code = "alpha", Name = "Alpha", UrlPrefix = "alpharenamed" }));

        var managedSite = Assert.Single(await service.ListAsync());
        Assert.Equal(firstCode, managedSite.Code);
        Assert.Equal("alpharenamed", managedSite.UrlPrefix);
    }

    [Fact]
    public async Task AManagedSiteWithNoName_IsReportedAndTheRestAreStillDeclared()
    {
        // One unusable entry must not abandon a setup recipe partway through.
        var service = new FakeManagedSiteService();

        var context = await RunAsync(
            service,
            Step(
                new { Code = "nameless", UrlPrefix = "nameless" },
                new { Code = "beta", Name = "Beta", UrlPrefix = "beta" }));

        Assert.Single(context.Errors);
        Assert.Equal("Beta", Assert.Single(await service.ListAsync()).Name);
    }

    [Fact]
    public async Task AManagedSiteWithNoCode_IsReportedAndTheRestAreStillDeclared()
    {
        // The code is what the recipe's later steps name the Managed Site by, so an entry without one
        // cannot be referred to and is not worth creating.
        var service = new FakeManagedSiteService();

        var context = await RunAsync(
            service,
            Step(new { Name = "Codeless", UrlPrefix = "codeless" }, new { Code = "beta", Name = "Beta" }));

        Assert.Single(context.Errors);
        Assert.Equal("beta", Assert.Single(await service.ListAsync()).Code);
    }

    [Fact]
    public void ClearingAnEditor_NamesTheManagedSiteByItsName()
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteEditorsStep.ApplyGrants(settings, Editor("Alpha", "edit"), Sites(), []);

        var grant = Assert.Single(settings.Grants);
        Assert.Equal("alphaid", grant.ManagedSiteCode);
        Assert.Equal(["edit"], grant.Scopes);
    }

    [Fact]
    public void AGrantNamingNoAction_IsReadOnly()
    {
        // Holding the clearance is what allows reading, so a grant naming no action names no scope.
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteEditorsStep.ApplyGrants(settings, Editor("Alpha"), Sites(), []);

        Assert.Empty(Assert.Single(settings.Grants).Scopes);
    }

    [Fact]
    public void ClearingAnEditorAgain_StatesTheClearanceRatherThanAccumulatingIt()
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteEditorsStep.ApplyGrants(settings, Editor("Alpha", "edit", "publish"), Sites(), []);
        ManagedSiteEditorsStep.ApplyGrants(settings, Editor("Alpha", "view"), Sites(), []);

        var grant = Assert.Single(settings.Grants);
        Assert.Equal(["view"], grant.Scopes);
    }

    [Fact]
    public void AGrantNamingNoKnownManagedSite_IsReportedAndNotWritten()
    {
        var settings = new ManagedSiteClearanceSettings();
        var errors = new List<string>();

        ManagedSiteEditorsStep.ApplyGrants(settings, Editor("Nowhere", "edit"), Sites(), errors);

        Assert.Empty(settings.Grants);
        Assert.Contains("Nowhere", Assert.Single(errors));
    }

    [Fact]
    public void ClearanceForOneManagedSite_LeavesAnotherAlone()
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteEditorsStep.ApplyGrants(settings, Editor("Alpha", "edit"), Sites(), []);
        ManagedSiteEditorsStep.ApplyGrants(settings, Editor("Beta", "publish"), Sites(), []);

        Assert.Equal(2, settings.Grants.Count);
        Assert.Contains(settings.Grants, grant => grant.ManagedSiteCode == "alphaid");
        Assert.Contains(settings.Grants, grant => grant.ManagedSiteCode == "beta-id");
    }

    private static async Task<RecipeExecutionContext> RunAsync(FakeManagedSiteService service, JsonObject step)
    {
        var context = new RecipeExecutionContext { Name = "ManagedSites", Step = step };

        await new ManagedSitesStep(service, new StubStringLocalizer<ManagedSitesStep>())
            .ExecuteAsync(context);

        return context;
    }

    private static JsonObject Step(params object[] managedSites)
        => new()
        {
            ["name"] = "ManagedSites",
            ["ManagedSites"] = JsonSerializer.SerializeToNode(managedSites),
        };

    private static ManagedSiteEditorEntry Editor(string managedSite, params string[] scopes)
        => new()
        {
            UserName = "alphaUser",
            Grants = [new ManagedSiteGrantEntry { ManagedSite = managedSite, Scopes = scopes }],
        };

    private static IReadOnlyList<ManagedSite> Sites()
        => [ManagedSitesTestData.ManagedSite("alphaid", name: "Alpha"),
            ManagedSitesTestData.ManagedSite("beta-id", name: "Beta")];

    private sealed class SequentialIdGenerator : IIdGenerator
    {
        private int _next;

        public string GenerateUniqueId() => $"generated-{++_next}";
    }
}
