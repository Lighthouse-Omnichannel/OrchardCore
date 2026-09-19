using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers who may override a Managed Content item.
/// </summary>
public class EditScopeTests
{
    [Fact]
    public void ScopeNone_AllowsNobody()
        => Assert.False(Service.CanEdit(Part(ManagedContentScope.None()), "site-a"));

    [Fact]
    public void ScopeAll_AllowsAnyManagedSite()
    {
        Assert.True(Service.CanEdit(Part(ManagedContentScope.All()), "site-a"));
        Assert.True(Service.CanEdit(Part(ManagedContentScope.All()), "site-b"));
    }

    [Fact]
    public void ScopeSelected_AllowsOnlyTheNamedManagedSites()
    {
        var part = Part(ManagedContentScope.Selected("site-a", "site-b"));

        Assert.True(Service.CanEdit(part, "site-a"));
        Assert.True(Service.CanEdit(part, "site-b"));
        Assert.False(Service.CanEdit(part, "site-c"));
    }

    [Fact]
    public void ScopeSelected_IsCaseSensitiveOnIdentifiers()
    {
        // Identifiers are generated, so a case-insensitive match would only ever widen the scope.
        var part = Part(ManagedContentScope.Selected("site-a"));

        Assert.False(Service.CanEdit(part, "SITE-A"));
    }

    [Fact]
    public void BlueprintContext_NeverEdits()
    {
        // Only a Managed Site holds an override; blueprint administrators change the original item.
        Assert.False(Service.CanEdit(Part(ManagedContentScope.All()), managedSiteId: null));
        Assert.False(Service.CanEdit(Part(ManagedContentScope.All()), string.Empty));
    }

    [Fact]
    public void ItemWithoutThePart_IsNotEditableByAnyManagedSite()
        => Assert.False(Service.CanEdit(part: null, "site-a"));

    [Fact]
    public void EditScope_IsIndependentOfDisplayScope()
    {
        // The service answers each question from its own scope and infers nothing from the other, so a
        // part written before the rule existed, or imported by a recipe, evaluates exactly as stored.
        // Keeping the two answers coupled is the editor's job, covered by ManagedContentScopeCoherenceTests.
        var part = new ManagedContentPart
        {
            EditScope = ManagedContentScope.Selected("site-a"),
            DisplayScope = ManagedContentScope.None(),
        };

        Assert.True(Service.CanEdit(part, "site-a"));
        Assert.False(Service.CanDisplay(part, "site-a"));
    }

    private static ManagedContentScopeService Service => new();

    private static ManagedContentPart Part(ManagedContentScope editScope)
        => new() { EditScope = editScope };
}
