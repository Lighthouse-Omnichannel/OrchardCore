using System.Linq;
using VendallionCMS.ManagedSites;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Authorization;

/// <summary>
/// Covers granting and withdrawing the clearance to work on a Managed Site.
/// </summary>
/// <remarks>
/// Clearance is stored on the user and administered from the Managed Site, so these rules are about
/// what one user's record holds afterwards rather than about what either screen shows.
/// </remarks>
public class ManagedSiteSecurityTests
{
    [Fact]
    public void Granting_WithNoScopesNamed_AllowsReadingAndNothingElse()
    {
        // Holding the clearance is what allows reading, so a grant naming no scope is read-only rather
        // than a grant of everything.
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", null);

        Assert.Empty(Assert.Single(settings.Grants).Scopes);
    }

    [Fact]
    public void Granting_NamesTheManagedSiteItCovers()
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", [ManagedSitesConstants.Scopes.Edit]);

        Assert.Equal("sitea", Assert.Single(settings.Grants).ManagedSiteCode);
    }

    [Fact]
    public void GrantingTwice_SaysWhatItAllowsNowRatherThanAddingASecond()
    {
        // Two grants for one Managed Site would be two answers to one question.
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", [ManagedSitesConstants.Scopes.Edit]);
        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", [ManagedSitesConstants.Scopes.Publish]);

        Assert.Equal([ManagedSitesConstants.Scopes.Publish], Assert.Single(settings.Grants).Scopes);
    }

    [Fact]
    public void Granting_LeavesOtherManagedSitesAlone()
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", [ManagedSitesConstants.Scopes.Edit]);
        ManagedSiteSecurityService.ApplyGrant(settings, "siteb", [ManagedSitesConstants.Scopes.Publish]);

        Assert.Equal(["sitea", "siteb"], settings.Grants.Select(grant => grant.ManagedSiteCode));
    }

    [Theory]
    [InlineData("EDIT", "edit")]
    [InlineData(" publish ", "publish")]
    public void Granting_TakesAScopeHoweverItWasWritten(string given, string expected)
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", [given]);

        Assert.Equal([expected], Assert.Single(settings.Grants).Scopes);
    }

    [Fact]
    public void Granting_DropsAScopeThatMeansNothing()
    {
        // The set arrives from a form, where anything can be put, and a scope nothing checks would read
        // as a grant that allows something.
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", ["edit", "wander-in-freely"]);

        Assert.Equal([ManagedSitesConstants.Scopes.Edit], Assert.Single(settings.Grants).Scopes);
    }

    [Fact]
    public void Granting_KeepsScopesInOneOrderHoweverTheyArrive()
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", ["preview", "edit", "publish", "edit"]);

        Assert.Equal(
            [ManagedSitesConstants.Scopes.Edit, ManagedSitesConstants.Scopes.Publish, ManagedSitesConstants.Scopes.Preview],
            Assert.Single(settings.Grants).Scopes);
    }

    [Theory]
    // Nothing named is read-only, so adding to it only ever widens.
    [InlineData(new string[0], new[] { "edit" }, false)]
    [InlineData(new[] { "edit", "publish" }, new[] { "edit" }, true)]
    [InlineData(new[] { "edit" }, new[] { "edit", "publish" }, false)]
    [InlineData(new[] { "edit" }, new[] { "edit" }, false)]
    // Back to reading alone takes away whatever was there.
    [InlineData(new[] { "edit" }, new string[0], true)]
    public void WhetherAChangeTakesSomethingAway_DecidesIfSessionsHaveToBeTold(
        string[] before,
        string[] after,
        bool narrows)
    {
        // A session carries what its clearance allowed when it signed in, so a loss has to reach it.
        Assert.Equal(narrows, ManagedSiteSecurityService.Narrows(before, after));
    }

    [Fact]
    public void Withdrawing_TakesOnlyTheManagedSiteNamed()
    {
        var settings = new ManagedSiteClearanceSettings();
        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", null);
        ManagedSiteSecurityService.ApplyGrant(settings, "siteb", null);

        Assert.True(ManagedSiteSecurityService.RemoveGrant(settings, "sitea"));
        Assert.Equal(["siteb"], settings.Grants.Select(grant => grant.ManagedSiteCode));
    }

    [Fact]
    public void Withdrawing_WhatWasNeverGranted_ChangesNothing()
    {
        var settings = new ManagedSiteClearanceSettings();
        ManagedSiteSecurityService.ApplyGrant(settings, "sitea", null);

        Assert.False(ManagedSiteSecurityService.RemoveGrant(settings, "siteb"));
        Assert.Single(settings.Grants);
    }
}
