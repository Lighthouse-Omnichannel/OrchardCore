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
    public void Granting_WithNoScopesNamed_AllowsEverything()
    {
        // An empty set is the wildcard the clearance claim already uses, so a grant made without saying
        // otherwise allows every scope rather than none.
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", null);

        Assert.Empty(Assert.Single(settings.Grants).Scopes);
    }

    [Fact]
    public void Granting_NamesTheManagedSiteItCovers()
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", [ManagedSitesConstants.Scopes.View]);

        Assert.Equal("site-a", Assert.Single(settings.Grants).ManagedSiteId);
    }

    [Fact]
    public void GrantingTwice_SaysWhatItAllowsNowRatherThanAddingASecond()
    {
        // Two grants for one Managed Site would be two answers to one question.
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", [ManagedSitesConstants.Scopes.View]);
        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", [ManagedSitesConstants.Scopes.Edit]);

        Assert.Equal([ManagedSitesConstants.Scopes.Edit], Assert.Single(settings.Grants).Scopes);
    }

    [Fact]
    public void Granting_LeavesOtherManagedSitesAlone()
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", [ManagedSitesConstants.Scopes.View]);
        ManagedSiteSecurityService.ApplyGrant(settings, "site-b", [ManagedSitesConstants.Scopes.Publish]);

        Assert.Equal(["site-a", "site-b"], settings.Grants.Select(grant => grant.ManagedSiteId));
    }

    [Theory]
    [InlineData("VIEW", "view")]
    [InlineData(" edit ", "edit")]
    public void Granting_TakesAScopeHoweverItWasWritten(string given, string expected)
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", [given]);

        Assert.Equal([expected], Assert.Single(settings.Grants).Scopes);
    }

    [Fact]
    public void Granting_DropsAScopeThatMeansNothing()
    {
        // The set arrives from a form, where anything can be put, and a scope nothing checks would read
        // as a grant that allows something.
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", ["view", "wander-in-freely"]);

        Assert.Equal([ManagedSitesConstants.Scopes.View], Assert.Single(settings.Grants).Scopes);
    }

    [Fact]
    public void Granting_KeepsScopesInOneOrderHoweverTheyArrive()
    {
        var settings = new ManagedSiteClearanceSettings();

        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", ["publish", "view", "edit", "view"]);

        Assert.Equal(
            [ManagedSitesConstants.Scopes.View, ManagedSitesConstants.Scopes.Edit, ManagedSitesConstants.Scopes.Publish],
            Assert.Single(settings.Grants).Scopes);
    }

    [Theory]
    // An empty set is the wildcard, so replacing it with anything named takes the rest away.
    [InlineData(new string[0], new[] { "view" }, true)]
    [InlineData(new[] { "view", "edit" }, new[] { "view" }, true)]
    [InlineData(new[] { "view" }, new[] { "view", "edit" }, false)]
    [InlineData(new[] { "view" }, new[] { "view" }, false)]
    // Nothing named at all is refused before it reaches here, so it is read as taking nothing.
    [InlineData(new[] { "view" }, new string[0], false)]
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
        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", null);
        ManagedSiteSecurityService.ApplyGrant(settings, "site-b", null);

        Assert.True(ManagedSiteSecurityService.RemoveGrant(settings, "site-a"));
        Assert.Equal(["site-b"], settings.Grants.Select(grant => grant.ManagedSiteId));
    }

    [Fact]
    public void Withdrawing_WhatWasNeverGranted_ChangesNothing()
    {
        var settings = new ManagedSiteClearanceSettings();
        ManagedSiteSecurityService.ApplyGrant(settings, "site-a", null);

        Assert.False(ManagedSiteSecurityService.RemoveGrant(settings, "site-b"));
        Assert.Single(settings.Grants);
    }
}
