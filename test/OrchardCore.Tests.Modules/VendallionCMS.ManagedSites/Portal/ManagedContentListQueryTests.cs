using System.Linq;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Portal;

/// <summary>
/// Covers narrowing and paging what a Managed Site may customize.
/// </summary>
/// <remarks>
/// An item's override status lives on the Managed Site's own content and cannot be joined to the lookup
/// that found the items, so it is filtered after they are read. Paging has to follow that: paging first
/// would give short pages, and a total that counted items the caller asked not to see.
/// </remarks>
public class ManagedContentListQueryTests
{
    [Fact]
    public void WithNoNarrowing_EverythingIsListed()
    {
        var listing = ManagedContentListService.ApplyQuery(Items(6), new ManagedContentListQuery());

        Assert.Equal(6, listing.Items.Count);
        Assert.Equal(6, listing.TotalCount);
    }

    [Fact]
    public void NarrowingByStatus_CountsOnlyWhatSurvived()
    {
        // The total is what paging runs over, so it has to be the narrowed count rather than the whole.
        var listing = ManagedContentListService.ApplyQuery(
            [Item("a", ManagedContentOverrideStatus.Published), Item("b"), Item("c", ManagedContentOverrideStatus.Published)],
            new ManagedContentListQuery(OverrideStatus: ManagedContentOverrideStatus.Published));

        Assert.Equal(2, listing.TotalCount);
        Assert.Equal(["a", "c"], listing.Items.Select(item => item.SourceContentItemId));
    }

    [Fact]
    public void AnItemWithNoOverride_HasNoStatusRatherThanAnEmptyOne()
    {
        var listing = ManagedContentListService.ApplyQuery(
            [Item("a"), Item("b", ManagedContentOverrideStatus.Draft)],
            new ManagedContentListQuery(OverrideStatus: ManagedContentOverrideStatus.None));

        Assert.Equal("a", Assert.Single(listing.Items).SourceContentItemId);
    }

    [Theory]
    [InlineData(1, 2, new[] { "0", "1" })]
    [InlineData(2, 2, new[] { "2", "3" })]
    [InlineData(3, 2, new[] { "4", "5" })]
    [InlineData(4, 2, new string[0])]
    public void PagingWalksTheList(int page, int pageSize, string[] expected)
    {
        var listing = ManagedContentListService.ApplyQuery(Items(6), new ManagedContentListQuery(Page: page, PageSize: pageSize));

        Assert.Equal(expected, listing.Items.Select(item => item.SourceContentItemId));
        Assert.Equal(6, listing.TotalCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void APageBeforeTheFirst_IsTheFirst(int page)
    {
        // A caller counting from zero should see the start of the list rather than an exception.
        var listing = ManagedContentListService.ApplyQuery(Items(3), new ManagedContentListQuery(Page: page, PageSize: 2));

        Assert.Equal(["0", "1"], listing.Items.Select(item => item.SourceContentItemId));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(5000, 200)]
    public void APageSizeOutsideWhatIsAllowed_IsBroughtInside(int pageSize, int expected)
    {
        // Nothing stops a caller asking for everything at once, so the ceiling is enforced rather than
        // trusted, and a size of nothing would otherwise return nothing forever.
        var listing = ManagedContentListService.ApplyQuery(Items(400), new ManagedContentListQuery(PageSize: pageSize));

        Assert.Equal(expected, listing.Items.Count);
    }

    [Fact]
    public void NarrowingByContentType_LeavesTheRest()
    {
        var listing = ManagedContentListService.ApplyQuery(
            [Typed("a", "Page"), Typed("b", "Section"), Typed("c", "Page")],
            new ManagedContentListQuery(ContentType: "page"));

        Assert.Equal(["a", "c"], listing.Items.Select(item => item.SourceContentItemId));
        Assert.Equal(2, listing.TotalCount);
    }

    [Fact]
    public void OnlyItemsStoredInTheirOwnRight_AreListed()
    {
        // A section is read as part of its page, so it is listed where its page is rather than beside
        // it as though the two were peers.
        var own = ManagedContentListService.InTheirOwnRight(
            [Contained("page", "page"), Contained("one", "page"), Contained("two", "page")]);

        Assert.Equal(["page"], own.Select(item => item.SourceContentItemId));
    }

    [Fact]
    public void AContainer_SaysHowMuchItHoldsAndHowMuchOfThatIsCustomized()
    {
        var own = ManagedContentListService.InTheirOwnRight(
        [
            Contained("page", "page"),
            Contained("one", "page", ManagedContentOverrideStatus.Published),
            Contained("two", "page"),
            Contained("three", "page", ManagedContentOverrideStatus.Draft),
        ]);

        var page = Assert.Single(own);
        Assert.Equal(3, page.ContainedItemCount);
        Assert.Equal(2, page.ContainedOverriddenCount);
    }

    [Fact]
    public void AnItemHoldingNothing_CountsNothing()
    {
        var page = Assert.Single(ManagedContentListService.InTheirOwnRight([Contained("page", "page")]));

        Assert.Equal(0, page.ContainedItemCount);
        Assert.Equal(0, page.ContainedOverriddenCount);
    }

    [Fact]
    public void AnItemWhoseContainerIsOutOfScope_IsListedInItsOwnRight()
    {
        // The container is not listed, so anything stored inside it would otherwise be reachable from
        // nowhere: in scope, and invisible.
        var own = ManagedContentListService.InTheirOwnRight([Contained("orphan", "a-page-nobody-may-edit")]);

        Assert.Equal(["orphan"], own.Select(item => item.SourceContentItemId));
    }

    [Fact]
    public void WhatAContainerHolds_IsCountedBeforeTheListIsNarrowed()
    {
        // The count describes the container, not the search. Counting after narrowing made a page claim
        // to hold nothing as soon as the list was filtered to anything but its sections.
        var own = ManagedContentListService.InTheirOwnRight(
            [Contained("page", "page"), Contained("one", "page"), Contained("two", "page")]);

        var listing = ManagedContentListService.ApplyQuery(own, new ManagedContentListQuery(ContentType: "Page"));

        Assert.Equal(2, Assert.Single(listing.Items).ContainedItemCount);
    }

    private static ManagedContentListItem Typed(string id, string contentType)
    {
        var item = Item(id);
        item.ContentType = contentType;

        return item;
    }

    private static ManagedContentListItem Contained(
        string id,
        string containerId,
        ManagedContentOverrideStatus? status = null)
    {
        var item = Item(id, status);
        item.ContainerContentItemId = containerId;
        item.ContentType = string.Equals(id, containerId, System.StringComparison.Ordinal) ? "Page" : "Section";

        return item;
    }

    private static ManagedContentListItem[] Items(int count)
        => [.. Enumerable.Range(0, count).Select(index => Item(index.ToString()))];

    private static ManagedContentListItem Item(string id, ManagedContentOverrideStatus? status = null)
        => new()
        {
            SourceContentItemId = id,
            ContainerContentItemId = id,
            ContentType = "Section",
            DisplayText = id,
            Override = status is null ? null : new ManagedContentOverrideSummary { Status = status.ToString() },
        };
}
