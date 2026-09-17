using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchardCore.Environment.Shell;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Routing;

/// <summary>
/// Covers reconciling the tenant hostname with the host names Managed Sites declare.
/// </summary>
/// <remarks>
/// These run against the real service and real shell settings rather than a double. A double that
/// re-implemented the rules once hid a defect, because it modelled list arithmetic and not OrchardCore's
/// own host matching.
///
/// Only host names reach the tenant. A tenant carries one URL prefix for all of its content, so a
/// Managed Site prefix is resolved inside the tenant and never becomes the tenant prefix.
/// </remarks>
public class ShellUrlSynchronizationTests
{
    [Fact]
    public void Synchronize_DeclaredHost_IsAddedEvenWhenTheTenantHadNone()
    {
        var (service, shellSettings) = CreateService(requestUrlHost: string.Empty);
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));

        var changed = service.Synchronize(document);

        Assert.True(changed);
        Assert.Equal("contoso.com", shellSettings.RequestUrlHost);
        Assert.Equal(["contoso.com"], document.AppliedShellHosts);
    }

    [Fact]
    public void Synchronize_ManagedSiteWithoutHost_ChangesNothing()
    {
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example");

        var changed = service.Synchronize(Document(ManagedSitesTestData.ManagedSite("a", urlPrefix: "shop")));

        Assert.False(changed);
        Assert.Equal("tenant.example", shellSettings.RequestUrlHost);
    }

    [Fact]
    public void Synchronize_DeclaredHost_IsAppendedToOperatorHosts()
    {
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example");

        service.Synchronize(Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com")));

        Assert.Equal("tenant.example,contoso.com", shellSettings.RequestUrlHost);
    }

    [Fact]
    public void Synchronize_HostRemoved_IsWithdrawnAndOperatorHostSurvives()
    {
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example");
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));
        service.Synchronize(document);

        document.ManagedSites = [ManagedSitesTestData.ManagedSite("a", urlPrefix: "shop")];
        var changed = service.Synchronize(document);

        Assert.True(changed);
        Assert.Equal("tenant.example", shellSettings.RequestUrlHost);
        Assert.Empty(document.AppliedShellHosts);
    }

    [Fact]
    public void Synchronize_ClearingTheLastHostWithNoPrefix_IsRefused()
    {
        // The tenant would become a catch-all and could answer for addresses meant for other tenants.
        var (service, shellSettings) = CreateService(requestUrlHost: "contoso.com");
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));
        service.Synchronize(document);

        document.ManagedSites = [];
        var changed = service.Synchronize(document);

        Assert.False(changed);
        Assert.Equal("contoso.com", shellSettings.RequestUrlHost);
    }

    [Fact]
    public void Synchronize_ClearingTheLastHostWithAPrefixSet_IsAllowed()
    {
        // The prefix still distinguishes the tenant, so it does not become a catch-all.
        var (service, shellSettings) = CreateService(requestUrlHost: "contoso.com", requestUrlPrefix: "site");
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));
        service.Synchronize(document);

        document.ManagedSites = [];
        var changed = service.Synchronize(document);

        Assert.True(changed);
        Assert.Equal(string.Empty, shellSettings.RequestUrlHost);
    }

    [Fact]
    public void Synchronize_UnchangedHosts_ReportsNoChange()
    {
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example");
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));
        service.Synchronize(document);

        var changed = service.Synchronize(document);

        Assert.False(changed);
        Assert.Equal("tenant.example,contoso.com", shellSettings.RequestUrlHost);
    }

    [Fact]
    public void Synchronize_SeveralManagedSites_ContributeTheirOwnHosts()
    {
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example");

        service.Synchronize(Document(
            ManagedSitesTestData.ManagedSite("a", name: "A", hostname: "contoso.com"),
            ManagedSitesTestData.ManagedSite("b", name: "B", hostname: "fabrikam.com")));

        Assert.Equal(
            ["contoso.com", "fabrikam.com", "tenant.example"],
            shellSettings.RequestUrlHost.Split(',').Order());
    }

    [Fact]
    public void Synchronize_OneManagedSiteWithSeveralHosts_AddsThemAll()
    {
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example");

        service.Synchronize(Document(
            ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com, www.contoso.com")));

        Assert.Equal(
            ["contoso.com", "tenant.example", "www.contoso.com"],
            shellSettings.RequestUrlHost.Split(',').Order());
    }

    [Fact]
    public void Synchronize_ArchivedManagedSite_DoesNotContributeItsHost()
    {
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example");

        service.Synchronize(Document(
            ManagedSitesTestData.ManagedSite("a", ManagedSiteStatus.Archived, hostname: "contoso.com")));

        Assert.Equal("tenant.example", shellSettings.RequestUrlHost);
    }

    [Fact]
    public async Task SaveAsync_DeclaredHost_ReachesTheTenant()
    {
        var shellSettings = new ShellSettings { Name = "Default", RequestUrlHost = "tenant.example" };
        var service = new ManagedSiteService(
            new FakeSiteService(),
            new ShellUrlSynchronizationService(
                shellSettings,
                Mock.Of<IShellHost>(),
                NullLogger<ShellUrlSynchronizationService>.Instance));

        await service.SaveAsync(ManagedSitesTestData.ManagedSite("a", name: "A", hostname: "contoso.com"));

        Assert.Equal("tenant.example,contoso.com", shellSettings.RequestUrlHost);
    }

    private static (ShellUrlSynchronizationService Service, ShellSettings ShellSettings) CreateService(
        string requestUrlHost,
        string requestUrlPrefix = "")
    {
        var shellSettings = new ShellSettings
        {
            Name = "Default",
            RequestUrlHost = requestUrlHost,
            RequestUrlPrefix = requestUrlPrefix,
        };

        var service = new ShellUrlSynchronizationService(
            shellSettings,
            Mock.Of<IShellHost>(),
            NullLogger<ShellUrlSynchronizationService>.Instance);

        return (service, shellSettings);
    }

    private static ManagedSitesDocument Document(params ManagedSite[] managedSites)
        => new() { ManagedSites = [.. managedSites] };
}
