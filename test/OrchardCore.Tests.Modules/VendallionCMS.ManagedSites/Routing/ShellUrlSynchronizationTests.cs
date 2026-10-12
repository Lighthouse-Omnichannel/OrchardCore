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
    public void Synchronize_TenantNamingNoHost_IsLeftNamingNone()
    {
        // It answers on every host already, the Managed Site's included. Naming one would narrow the
        // tenant to that host alone, and every other address it answered on -- the one the
        // administrator is using among them -- would stop resolving and answer nothing at all.
        var (service, shellSettings) = CreateService(requestUrlHost: string.Empty);
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));

        var changed = service.Synchronize(document);

        Assert.False(changed);
        Assert.Equal(string.Empty, shellSettings.RequestUrlHost);
        Assert.Empty(document.AppliedShellHosts);
    }

    [Fact]
    public void Synchronize_HostTheTenantAlreadyNamed_IsNotClaimedAsOurs()
    {
        // Recording it would let a later withdrawal take away a host name the operator configured.
        var (service, _) = CreateService(requestUrlHost: "contoso.com");
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));

        service.Synchronize(document);

        Assert.Empty(document.AppliedShellHosts);
    }

    [Fact]
    public void Synchronize_WithdrawingEveryHostWeAdded_RestoresTheTenantToNamingNone()
    {
        // Exactly the state the tenant was found in, which is the way back for a tenant narrowed by an
        // earlier version of this.
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example");
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));
        service.Synchronize(document);

        Assert.Equal("tenant.example,contoso.com", shellSettings.RequestUrlHost);

        shellSettings.RequestUrlHost = "contoso.com";
        document.ManagedSites = [];

        Assert.True(service.Synchronize(document));
        Assert.Equal(string.Empty, shellSettings.RequestUrlHost);
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
    public void Synchronize_HostTheOperatorConfigured_IsNeverWithdrawn()
    {
        // The Managed Site names the same host the tenant already named. Withdrawing the Managed Site
        // must leave the tenant naming it, because this module did not put it there.
        var (service, shellSettings) = CreateService(requestUrlHost: "contoso.com");
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));
        service.Synchronize(document);

        document.ManagedSites = [];
        var changed = service.Synchronize(document);

        Assert.False(changed);
        Assert.Equal("contoso.com", shellSettings.RequestUrlHost);
    }

    [Fact]
    public void Synchronize_WithdrawingOurHostFromATenantWithAPrefix_LeavesItNamingNone()
    {
        // The tenant named a host of its own, a Managed Site added another, and withdrawing the Managed
        // Site takes back only what it added. Here the operator's host is gone by other means, so what
        // is left is ours alone and goes with it.
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example", requestUrlPrefix: "site");
        var document = Document(ManagedSitesTestData.ManagedSite("a", hostname: "contoso.com"));
        service.Synchronize(document);

        Assert.Equal(["contoso.com"], document.AppliedShellHosts);

        shellSettings.RequestUrlHost = "contoso.com";
        document.ManagedSites = [];

        Assert.True(service.Synchronize(document));
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
    public void Synchronize_SwitchedOffManagedSite_StillContributesItsHost()
    {
        // Withdrawing the host would stop the tenant answering on it at all. Keeping it means the
        // address still resolves and serves Site Blueprint content, which is what a visitor should get
        // from a Managed Site that is switched off.
        var (service, shellSettings) = CreateService(requestUrlHost: "tenant.example");

        service.Synchronize(Document(
            ManagedSitesTestData.ManagedSite("a", ManagedSiteStatus.Disabled, hostname: "contoso.com")));

        Assert.Equal(
            ["contoso.com", "tenant.example"],
            shellSettings.RequestUrlHost.Split(',').Order());
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
