using System.Threading.Tasks;
using VendallionCMS.ManagedSites;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

public class SiteBlueprintAuthorizationTests
{
    [Fact]
    public async Task GetPermissionsAsync_ReturnsBlueprintManagementPermission()
    {
        var provider = new Permissions();

        var permissions = await provider.GetPermissionsAsync();

        Assert.Contains(permissions, permission => permission.Name == Permissions.ManageSiteBlueprint.Name);
    }
}
