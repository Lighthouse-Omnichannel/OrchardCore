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



    private sealed class SequentialIdGenerator : IIdGenerator
    {
        private int _next;

        public string GenerateUniqueId() => $"generated-{++_next}";
    }
}
