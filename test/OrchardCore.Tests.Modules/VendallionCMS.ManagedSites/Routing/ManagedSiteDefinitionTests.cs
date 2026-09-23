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

        await service.SaveAsync(Definition("site-a", "Site A", "contoso.com", "shop"));

        var stored = await service.GetAsync("site-a");
        Assert.NotNull(stored);
        Assert.Equal("Site A", stored.Name);
        Assert.Equal("contoso.com", stored.Hostname);
        Assert.Equal("shop", stored.UrlPrefix);
    }

    [Fact]
    public async Task SaveAsync_NewDefinition_AppearsInList()
    {
        var service = CreateService();

        await service.SaveAsync(Definition("site-a", "Site A", "a.example"));
        await service.SaveAsync(Definition("site-b", "Site B", "b.example"));

        var all = await service.ListAsync();

        Assert.Equal(["site-a", "site-b"], all.Select(managedSite => managedSite.Id).Order());
    }

    [Fact]
    public async Task SaveAsync_ExistingDefinition_ReplacesItRatherThanDuplicating()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("site-a", "Site A", "contoso.com"));

        await service.SaveAsync(Definition("site-a", "Renamed", "contoso.com"));

        var stored = Assert.Single(await service.ListAsync());
        Assert.Equal("Renamed", stored.Name);
    }

    [Fact]
    public async Task SaveAsync_AddressChanged_ReplacesTheOldOne()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("site-a", "Site A", "contoso.com", null));

        await service.SaveAsync(Definition("site-a", "Site A", "fabrikam.com", null));

        Assert.NotNull(await service.FindByAddressAsync("fabrikam.com", "/"));
        Assert.Null(await service.FindByAddressAsync("contoso.com", "/"));
    }

    [Fact]
    public async Task SaveAsync_UnchangedAddressOnEdit_IsNotTreatedAsAConflict()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("site-a", "Site A", "contoso.com", "shop"));

        await service.SaveAsync(Definition("site-a", "Site A", "contoso.com", "shop"));

        var stored = await service.GetAsync("site-a");
        Assert.Equal("contoso.com", stored.Hostname);
    }

    [Fact]
    public async Task SaveAsync_AddressOwnedByAnotherManagedSite_IsRejected()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("site-a", "Site A", "contoso.com", "shop"));

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition("site-b", "Site B", "contoso.com", "shop")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.UrlConflict, exception.Code);
    }

    [Fact]
    public async Task SaveAsync_NameAlreadyUsedByAnotherManagedSite_IsRejected()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("site-a", "Shared name", "a.example"));

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition("site-b", "shared NAME", "b.example")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.NameConflict, exception.Code);
    }

    [Fact]
    public async Task SaveAsync_MissingName_IsRejected()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition("site-a", "  ")));

        Assert.Equal(ManagedSitesConstants.ErrorCodes.InvalidName, exception.Code);
    }

    [Fact]
    public async Task SaveAsync_RejectedDefinition_LeavesStoredStateUnchanged()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("site-a", "Site A", "contoso.com", "shop"));

        await Assert.ThrowsAsync<ManagedSiteValidationException>(
            async () => await service.SaveAsync(Definition("site-b", "Site B", "contoso.com", "shop")));

        var stored = Assert.Single(await service.ListAsync());
        Assert.Equal("site-a", stored.Id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDefinitionAndItsAddress()
    {
        var service = CreateService();
        await service.SaveAsync(Definition("site-a", "Site A", "contoso.com"));

        var removed = await service.DeleteAsync("site-a");

        Assert.True(removed);
        Assert.Null(await service.GetAsync("site-a"));
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
        await service.SaveAsync(Definition("site-a", "Site A", "contoso.com", "shop"));
        await service.DeleteAsync("site-a");

        await service.SaveAsync(Definition("site-b", "Site B", "contoso.com", "shop"));

        var stored = await service.GetAsync("site-b");
        Assert.Equal("contoso.com", stored.Hostname);
    }

    private static ManagedSiteService CreateService()
        => new(new FakeSiteService(), new FakeShellUrlSynchronizationService());

    private static ManagedSite Definition(string id, string name, string hostname = null, string urlPrefix = null)
        => new()
        {
            Id = id,
            Name = name,
            Status = ManagedSiteStatus.Enabled,
            Hostname = hostname,
            UrlPrefix = urlPrefix,
        };
}
