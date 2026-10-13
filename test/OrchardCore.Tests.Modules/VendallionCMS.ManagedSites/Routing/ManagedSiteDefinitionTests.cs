using System.Linq;
using System.Threading.Tasks;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Routing;

/// <summary>
/// Covers creating, replacing, and removing Managed Site definitions through the storage service.
/// </summary>
public class ManagedSiteDefinitionTests
{
    [Fact]
    public void ManagedSite_DefaultStatus_IsEnabled()
    {
        // With only two states, the other one reads as deliberately switched off. A Managed Site that
        // had to be turned on before anything about it worked would be a trap, not a safeguard.
        var managedSite = new ManagedSite();

        Assert.Equal(ManagedSiteStatus.Enabled, managedSite.Status);
    }

    [Fact]
    public async Task SaveAsync_NewDefinition_IsReadBack()
    {
        var service = CreateService();

        await service.SaveAsync(Definition("sitea", "Site A", "contoso.com", "shop"));

        var stored = await service.GetAsync("sitea");
        Assert.NotNull(stored);
        Assert.Equal("Site A", stored.Name);
        Assert.Equal("contoso.com", stored.Hostname);
        Assert.Equal("shop", stored.UrlPrefix);
    }

    [Fact]
    public async Task SaveAsync_NewDefinition_AppearsInList()
    {
        var service = CreateService();

        await service.SaveAsync(Definition("sitea", "Site A", "a.example"));
        await service.SaveAsync(Definition("siteb", "Site B", "b.example"));

        var all = await service.ListAsync();

        Assert.Equal(["sitea", "siteb"], all.Select(managedSite => managedSite.Code).Order());
    }

    [Fact]
    public async Task SaveAsync_ExistingDefinition_ReplacesItRatherThanDuplicating()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("sitea", "Site A", "contoso.com"));

        await service.SaveAsync(Definition("sitea", "Renamed", "contoso.com"));

        var stored = Assert.Single(await service.ListAsync());
        Assert.Equal("Renamed", stored.Name);
    }

    [Fact]
    public async Task SaveAsync_AddressChanged_ReplacesTheOldOne()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("sitea", "Site A", "contoso.com", null));

        await service.SaveAsync(Definition("sitea", "Site A", "fabrikam.com", null));

        Assert.NotNull(await service.FindByAddressAsync("fabrikam.com", "/"));
        Assert.Null(await service.FindByAddressAsync("contoso.com", "/"));
    }

    [Fact]
    public async Task SaveAsync_UnchangedAddressOnEdit_IsNotTreatedAsAConflict()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("sitea", "Site A", "contoso.com", "shop"));

        await service.SaveAsync(Definition("sitea", "Site A", "contoso.com", "shop"));

        var stored = await service.GetAsync("sitea");
        Assert.Equal("contoso.com", stored.Hostname);
    }

    [Fact]
    public async Task SaveAsync_AddressOwnedByAnotherManagedSite_IsRejected()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("sitea", "Site A", "contoso.com", "shop"));

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition("siteb", "Site B", "contoso.com", "shop")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.UrlConflict, exception.Code);
    }

    [Fact]
    public async Task SaveAsync_NameAlreadyUsedByAnotherManagedSite_IsRejected()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("sitea", "Shared name", "a.example"));

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition("siteb", "shared NAME", "b.example")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.NameConflict, exception.Code);
    }

    [Fact]
    public async Task SaveAsync_MissingName_IsRejected()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition("sitea", "  ")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.InvalidName, exception.Code);
    }

    [Fact]
    public async Task SaveAsync_RejectedDefinition_LeavesStoredStateUnchanged()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("sitea", "Site A", "contoso.com", "shop"));

        await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition("siteb", "Site B", "contoso.com", "shop")));

        var stored = Assert.Single(await service.ListAsync());
        Assert.Equal("sitea", stored.Code);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDefinitionAndItsAddress()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("sitea", "Site A", "contoso.com"));

        var removed = await service.DeleteAsync("sitea");

        Assert.True(removed);
        Assert.Null(await service.GetAsync("sitea"));
        Assert.Null(await service.FindByAddressAsync("contoso.com", "/"));
    }

    [Fact]
    public async Task DeleteAsync_UnknownManagedSite_ReturnsFalse()
    {
        var service = CreateService();

        Assert.False(await service.DeleteAsync("missing"));
    }

    [Fact]
    public async Task DeleteAsync_FreesTheAddressForAnotherManagedSite()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("sitea", "Site A", "contoso.com", "shop"));
        await service.DeleteAsync("sitea");

        await service.SaveAsync(Definition("siteb", "Site B", "contoso.com", "shop"));

        var stored = await service.GetAsync("siteb");
        Assert.Equal("contoso.com", stored.Hostname);
    }

    private static ManagedSiteService CreateService()
        => new(new FakeSiteService(), new FakeShellUrlSynchronizationService());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SaveAsync_WithoutACode_IsRejected(string code)
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition(code, "Alpha")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.InvalidCode, exception.Code);
    }

    [Theory]
    [InlineData("Alpha")]          // not lowercase
    [InlineData("alpha site")]     // a space
    [InlineData("alpha-site")]     // punctuation
    [InlineData("1alpha")]         // begins with a digit
    [InlineData("alpha_site")]
    [InlineData("alpha.site")]
    public async Task SaveAsync_ACodeThatBreaksTheRule_IsRejected(string code)
    {
        // The code is written into a URL path, a recipe, and a claim whose parts are separated by
        // colons and commas, so anything but lowercase letters and digits is refused at the door.
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition(code, "Alpha")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.InvalidCode, exception.Code);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("alpha")]
    [InlineData("alpha2")]
    [InlineData("a1b2c3")]
    public async Task SaveAsync_ACodeThatFollowsTheRule_IsAccepted(string code)
    {
        var service = CreateService();

        await service.SaveAsync(Definition(code, "Alpha"));

        Assert.Equal(code, Assert.Single(await service.ListAsync()).Code);
    }

    [Fact]
    public async Task SaveAsync_ACodeLongerThanAllowed_IsRejected()
    {
        var service = CreateService();
        var code = "a" + new string('b', ManagedSitesConstants.Code.MaximumLength);

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition(code, "Alpha")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.InvalidCode, exception.Code);
    }

    [Fact]
    public async Task SaveAsync_ACodeOfExactlyTheLengthAllowed_IsAccepted()
    {
        var service = CreateService();
        var code = "a" + new string('b', ManagedSitesConstants.Code.MaximumLength - 1);

        await service.SaveAsync(Definition(code, "Alpha"));

        Assert.Equal(code, Assert.Single(await service.ListAsync()).Code);
    }

    [Fact]
    public async Task SaveAsync_ACodeWithSpaceAroundIt_IsStoredTrimmed()
    {
        var service = CreateService();

        await service.SaveAsync(Definition("  alpha  ", "Alpha"));

        Assert.Equal("alpha", Assert.Single(await service.ListAsync()).Code);
    }

    private static ManagedSite Definition(string id, string name, string hostname = null, string urlPrefix = null)
        => new()
        {
            Code = id,
            Name = name,
            Status = ManagedSiteStatus.Enabled,
            Hostname = hostname,
            UrlPrefix = urlPrefix,
        };
}
