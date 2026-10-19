using System.Security.Claims;
using OrchardCore.Security;
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

    [Theory]
    [InlineData("edit")]
    [InlineData("publish")]
    [InlineData("preview")]
    public void AnyAuthorityOverAManagedSite_GrantsReadingIt(string scope)
    {
        // Reading is the floor rather than a scope of its own: whatever somebody may do to a Managed
        // Site, they may see it. A role granting only editing that could not open the site would be
        // unusable, and one that had to be granted reading alongside would be a trap.
        Assert.True(Grants(scope, reaches: null));
    }

    [Fact]
    public void PublishingAManagedSite_GrantsEditingIt()
    {
        // Publishing somebody else's draft without being able to change it is not a job anyone has.
        Assert.True(Grants("publish", reaches: "edit"));
    }

    [Fact]
    public void EditingAManagedSite_DoesNotGrantPublishingIt()
    {
        // The line that makes publishing worth granting separately.
        Assert.False(Grants("edit", reaches: "publish"));
    }

    [Fact]
    public void EditingAManagedSite_GrantsPreviewingIt()
    {
        // An editor writes the drafts and already reads them in the content editor, so withholding the
        // preview of their own unpublished work would withhold nothing.
        Assert.True(Grants("edit", reaches: "preview"));
    }

    [Fact]
    public void PreviewingAManagedSite_DoesNotGrantEditingIt()
    {
        // It runs one way only: somebody can be shown unpublished work without being handed the
        // ability to change it.
        Assert.False(Grants("preview", reaches: "edit"));
    }

    [Fact]
    public void PreviewingAManagedSite_DoesNotGrantPublishingIt()
        => Assert.False(Grants("preview", reaches: "publish"));

    [Theory]
    [InlineData(null)]
    [InlineData("edit")]
    [InlineData("publish")]
    [InlineData("preview")]
    public void GoverningManagedSites_GrantsEveryAuthorityOverEachOne(string scope)
    {
        // Whoever decides what a Managed Site is could grant themselves authority over it, so
        // withholding it would be a formality.
        Assert.True(Granted(
            ManagedSiteContentPermissions.For("alpha", scope),
            holding: Permissions.ManageManagedSites.Name));
    }

    [Fact]
    public void AuthorityOverOneManagedSite_ReachesNoOther()
    {
        Assert.False(Granted(
            ManagedSiteContentPermissions.For("beta", scope: null),
            holding: ManagedSiteContentPermissions.For("alpha", "publish").Name));
    }

    /// <summary>
    /// Answers whether holding one scope over a Managed Site reaches another.
    /// </summary>
    /// <param name="held">The scope actually granted.</param>
    /// <param name="reaches">The scope asked about, or <see langword="null" /> for reading.</param>
    /// <returns>Whether the platform grants it.</returns>
    private static bool Grants(string held, string reaches)
        => Granted(
            ManagedSiteContentPermissions.For("alpha", reaches),
            holding: ManagedSiteContentPermissions.For("alpha", held).Name);

    /// <summary>
    /// Asks the platform's own granting service, so the implication graph is the one that will decide
    /// this in production rather than one restated here.
    /// </summary>
    private static bool Granted(Permission permission, string holding)
        => new DefaultPermissionGrantingService().IsGranted(
            new PermissionRequirement(permission),
            [new Claim(Permission.ClaimType, holding)]);

    private static Permissions Provider(params ManagedSite[] managedSites)
        => new(new FakeManagedSiteService(managedSites));
}
