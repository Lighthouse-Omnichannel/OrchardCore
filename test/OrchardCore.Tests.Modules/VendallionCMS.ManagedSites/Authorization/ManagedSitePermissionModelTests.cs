using System;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;
using VendallionCMS.ManagedSites.Models;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers the shape of the permission model itself.
/// </summary>
/// <remarks>
/// There are two permissions, and the line between them is the whole point: governing Managed Sites on
/// one side, editing the content of one on the other. Everything that would let someone widen their own
/// reach, defining a Managed Site, deciding what content it may override, and granting clearance, sits
/// on the governing side. A third permission, or an implication running the wrong way, would quietly
/// move one of those across the line.
/// </remarks>
public class ManagedSitePermissionModelTests
{
    [Fact]
    public async Task TheFeature_OffersGoverningAndEditingAndNothingElse()
    {
        // With no Managed Sites defined there is nothing to generate, so only the two that always
        // exist are offered.
        var provided = await Provider().GetPermissionsAsync();

        Assert.Equal(
            ["EditManagedSiteContent", "ManageManagedSites"],
            provided.Select(permission => permission.Name).Order());
    }

    [Fact]
    public void GoverningManagedSites_AlsoGrantsEditingTheirContent()
    {
        // Whoever grants clearance can grant it to themselves, so withholding the screens from them
        // would be a formality rather than a restriction.
        Assert.Contains(
            Permissions.EditManagedSiteContent.ImpliedBy ?? [],
            permission => permission.Name == Permissions.ManageManagedSites.Name);
    }

    [Fact]
    public void EditingManagedSiteContent_DoesNotGrantGoverningThem()
    {
        // The implication must run one way only. The other way round, every Managed Site editor could
        // define new Managed Sites, put more content in scope, and grant themselves clearance.
        Assert.DoesNotContain(
            Permissions.ManageManagedSites.ImpliedBy ?? [],
            permission => permission.Name == Permissions.EditManagedSiteContent.Name);
    }

    [Fact]
    public void GoverningManagedSites_IsMarkedSecurityCritical()
    {
        // It carries the authority to grant clearance, which decides who may change Managed Site
        // content, so the admin UI should say so when it is handed out.
        Assert.True(Permissions.ManageManagedSites.IsSecurityCritical);
    }

    [Fact]
    public async Task TheAdministratorRole_HoldsBoth()
    {
        var stereotype = Assert.Single(Provider().GetDefaultStereotypes());
        var provided = await Provider().GetPermissionsAsync();

        Assert.Equal(
            provided.Select(permission => permission.Name).Order(),
            stereotype.Permissions.Select(permission => permission.Name).Order());
    }
    [Fact]
    public async Task EachManagedSite_ContributesItsOwnPermissions()
    {
        // This is what puts authority over one Managed Site in the roles screen, the way a media folder
        // and a content type put theirs there.
        var provided = await Provider(ManagedSitesTestData.ManagedSite("alpha", name: "Alpha"))
            .GetPermissionsAsync();

        Assert.Equal(
            [
                "EditManagedSiteContent_alpha",
                "PreviewManagedSiteContent_alpha",
                "PublishManagedSiteContent_alpha",
                "ViewManagedSiteContent_alpha",
            ],
            provided.Where(permission => permission.Name.EndsWith("_alpha", StringComparison.Ordinal))
                .Select(permission => permission.Name)
                .Order());
    }

    [Fact]
    public async Task AManagedSitesPermissions_AreGroupedUnderItsName()
    {
        var provided = await Provider(ManagedSitesTestData.ManagedSite("alpha", name: "Alpha"))
            .GetPermissionsAsync();

        Assert.All(
            provided.Where(permission => permission.Name.EndsWith("_alpha", StringComparison.Ordinal)),
            permission => Assert.Equal("Managed Site - Alpha", permission.Category));
    }

    [Fact]
    public async Task ADisabledManagedSite_KeepsItsPermissions()
    {
        // Switching a Managed Site off stops it serving. Dropping its permissions would quietly revoke
        // authority somebody was granted, and give nobody a way to grant it before switching back on.
        var provided = await Provider(
                ManagedSitesTestData.ManagedSite("alpha", status: ManagedSiteStatus.Disabled))
            .GetPermissionsAsync();

        Assert.Contains(
            provided,
            permission => permission.Name == "EditManagedSiteContent_alpha");
    }

    private static Permissions Provider(params ManagedSite[] managedSites)
        => new(new FakeManagedSiteService(managedSites));
}
