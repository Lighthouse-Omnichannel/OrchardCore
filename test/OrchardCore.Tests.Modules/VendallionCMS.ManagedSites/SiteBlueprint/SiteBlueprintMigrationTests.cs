using System.Threading.Tasks;
using VendallionCMS.ManagedSites.Migrations;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.SiteBlueprint;

public class SiteBlueprintMigrationTests
{
    [Fact]
    public async Task CreateAsync_ReturnsInitialSchemaVersion()
    {
        var version = await ManagedSitesMigrations.CreateAsync();

        Assert.Equal(1, version);
    }
}
