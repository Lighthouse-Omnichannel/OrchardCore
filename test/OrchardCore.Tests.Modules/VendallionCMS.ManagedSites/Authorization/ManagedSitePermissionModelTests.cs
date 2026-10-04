using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;
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
        var provided = await new Permissions().GetPermissionsAsync();

        Assert.Equal(
            ["EditManagedSiteContent", "ManageManagedSites"],
            provided.Select(permission => permission.Name).Order());
    }

    [Fact]
    public void GoverningManagedSites_AlsoGrantsEditingTheirContent()
    {
        // Whoever grants clearance can grant it to themselves, so withholding the portal from them
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
        var stereotype = Assert.Single(new Permissions().GetDefaultStereotypes());
        var provided = await new Permissions().GetPermissionsAsync();

        Assert.Equal(
            provided.Select(permission => permission.Name).Order(),
            stereotype.Permissions.Select(permission => permission.Name).Order());
    }
}
