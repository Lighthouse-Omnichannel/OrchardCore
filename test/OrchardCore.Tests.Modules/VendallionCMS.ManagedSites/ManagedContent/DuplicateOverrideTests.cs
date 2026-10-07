using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers a Managed Site holding more than one override of the same item.
/// </summary>
/// <remarks>
/// There should only ever be one, and creating a second is refused. Content imported or deployed by
/// recipe arrives without passing through that refusal, so more than one can exist anyway, and
/// something has to decide between them.
///
/// What matters is not which one wins but that the same one always does. An arbitrary winner would
/// mean the same request rendering differently on two machines, which is far worse to diagnose than
/// the wrong one of two winning consistently. The others are named rather than hidden, so an
/// administrator reading the item can see what is there and remove it.
/// </remarks>
public class DuplicateOverrideTests
{
    [Fact]
    public void OneOverride_IsServedAndSupersedesNothing()
    {
        var (served, superseded) = ManagedContentOverrideService.ChooseServed([Row("only")]);

        Assert.Equal("only", served);
        Assert.Empty(superseded);
    }

    [Fact]
    public void SeveralRowsOfTheSameOverride_AreStillOneOverride()
    {
        // A draft and a published version of the same override are two rows and one answer.
        var (served, superseded) = ManagedContentOverrideService.ChooseServed([Row("same"), Row("same")]);

        Assert.Equal("same", served);
        Assert.Empty(superseded);
    }

    [Fact]
    public void SeveralOverrides_TheLowestIdentifierIsServed()
    {
        var (served, superseded) = ManagedContentOverrideService.ChooseServed([Row("ccc"), Row("aaa"), Row("bbb")]);

        Assert.Equal("aaa", served);
        Assert.Equal(["bbb", "ccc"], superseded);
    }

    [Theory]
    [InlineData("aaa", "bbb", "ccc")]
    [InlineData("ccc", "bbb", "aaa")]
    [InlineData("bbb", "ccc", "aaa")]
    public void TheOrderTheyArrivedIn_ChangesNothing(string first, string second, string third)
    {
        // The point of the rule. Whichever order the store hands them back, the same one is served.
        var (served, superseded) = ManagedContentOverrideService.ChooseServed(
            [Row(first), Row(second), Row(third)]);

        Assert.Equal("aaa", served);
        Assert.Equal(["bbb", "ccc"], superseded);
    }

    private static ManagedContentOverrideIndex Row(string overrideContentItemId)
        => new()
        {
            ManagedSiteId = "site-a",
            SourceContentItemId = "source-item",
            OverrideContentItemId = overrideContentItemId,
        };
}
