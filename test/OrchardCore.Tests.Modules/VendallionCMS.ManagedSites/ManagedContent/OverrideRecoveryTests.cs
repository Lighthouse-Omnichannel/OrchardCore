using Moq;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers what remains of an override once it stops rendering.
/// </summary>
/// <remarks>
/// Suppression withdraws an override from rendering, never from existence. The content it holds stays
/// readable, still points at the item it was written for, and starts rendering again by itself once
/// the cause is reversed, so nothing has to be rebuilt to recover it.
/// </remarks>
public class OverrideRecoveryTests
{
    [Fact]
    public void SuppressedOverride_KeepsItsContentAndTarget()
    {
        var suppressed = new ManagedContentOverride
        {
            ManagedSiteId = "site-a",
            SourceContentItemId = "source-item",
            OverrideContentItemId = "override-item",
            ContentType = ManagedContentTestContent.ContentType,
            Status = ManagedContentOverrideStatus.Suppressed,
            SuppressionReason = ManagedContentOverrideSuppressionReason.EditScopeRemoved,
        };

        Assert.False(suppressed.Renders);
        Assert.Equal("override-item", suppressed.OverrideContentItemId);
        Assert.Equal("source-item", suppressed.SourceContentItemId);
    }

    [Fact]
    public void SuppressedOverride_CarriesTheReasonToActOn()
    {
        var suppressed = new ManagedContentOverride
        {
            Status = ManagedContentOverrideStatus.Suppressed,
            SuppressionReason = ManagedContentOverrideSuppressionReason.SourceUnpublished,
        };

        Assert.NotEqual(ManagedContentOverrideSuppressionReason.None, suppressed.SuppressionReason);
    }

    [Fact]
    public void RestoringTheEditScope_MakesTheOverrideEligibleAgain()
    {
        // Nothing is rewritten. The same override is evaluated against the restored scope and stops
        // being suppressed on the spot.
        var withdrawn = ManagedContentTestContent.Source("source-item", ManagedContentScope.None());
        var restored = ManagedContentTestContent.Source("source-item", ManagedContentScope.Selected("site-a"));

        Assert.Equal(
            ManagedContentOverrideSuppressionReason.EditScopeRemoved,
            Evaluate(Enabled(), withdrawn));

        Assert.Equal(
            ManagedContentOverrideSuppressionReason.None,
            Evaluate(Enabled(), restored));
    }

    [Fact]
    public void RepublishingTheSource_MakesTheOverrideEligibleAgain()
    {
        var published = ManagedContentTestContent.Source("source-item", ManagedContentScope.Selected("site-a"));

        Assert.Equal(
            ManagedContentOverrideSuppressionReason.SourceUnpublished,
            Evaluate(Enabled(), publishedSource: null));

        Assert.Equal(
            ManagedContentOverrideSuppressionReason.None,
            Evaluate(Enabled(), published));
    }

    [Fact]
    public void ReenablingTheManagedSite_MakesItsOverridesEligibleAgain()
    {
        var source = ManagedContentTestContent.Source("source-item", ManagedContentScope.Selected("site-a"));
        var disabled = ManagedSitesTestData.ManagedSite("site-a", ManagedSiteStatus.Disabled);

        Assert.Equal(
            ManagedContentOverrideSuppressionReason.ManagedSiteDisabled,
            Evaluate(disabled, source));

        Assert.Equal(
            ManagedContentOverrideSuppressionReason.None,
            Evaluate(Enabled(), source));
    }

    [Fact]
    public void DeletedSource_StaysSuppressedBecauseThereIsNothingToRecoverItAgainst()
    {
        // The only irreversible cause. The override content survives for review and cleanup, but no
        // change to the Managed Site can bring back the item it stood in for.
        Assert.Equal(
            ManagedContentOverrideSuppressionReason.SourceDeleted,
            Evaluate(Enabled(), publishedSource: null, sourceExists: false));
    }

    private static ManagedContentOverrideSuppressionReason Evaluate(
        ManagedSite managedSite,
        ContentItem publishedSource,
        bool sourceExists = true)
    {
        var service = new ManagedContentSuppressionService(
            new Mock<IContentManager>(MockBehavior.Strict).Object,
            new Mock<IManagedSiteService>(MockBehavior.Strict).Object,
            new ManagedContentScopeService());

        return service.Evaluate("site-a", managedSite, publishedSource, sourceExists);
    }

    private static ManagedSite Enabled() => ManagedSitesTestData.ManagedSite("site-a");
}
