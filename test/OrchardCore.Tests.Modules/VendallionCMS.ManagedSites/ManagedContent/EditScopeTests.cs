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
        => Assert.False(Service.CanEdit(Part(ManagedContentScope.None()), "sitea"));

    [Fact]
    public void ScopeAll_AllowsAnyManagedSite()
    {
        Assert.True(Service.CanEdit(Part(ManagedContentScope.All()), "sitea"));
        Assert.True(Service.CanEdit(Part(ManagedContentScope.All()), "siteb"));
    }

    [Fact]
    public void ScopeSelected_AllowsOnlyTheNamedManagedSites()
    {
        var part = Part(ManagedContentScope.Selected("sitea", "siteb"));

        Assert.True(Service.CanEdit(part, "sitea"));
        Assert.True(Service.CanEdit(part, "siteb"));
        Assert.False(Service.CanEdit(part, "sitec"));
    }

    [Fact]
    public void ScopeSelected_IsCaseSensitiveOnIdentifiers()
    {
        // Identifiers are generated, so a case-insensitive match would only ever widen the scope.
        var part = Part(ManagedContentScope.Selected("sitea"));

        Assert.False(Service.CanEdit(part, "SITE-A"));
    }

    [Fact]
    public void BlueprintContext_NeverEdits()
    {
        // Only a Managed Site holds an override; blueprint administrators change the original item.
        Assert.False(Service.CanEdit(Part(ManagedContentScope.All()), managedSiteCode: null));
        Assert.False(Service.CanEdit(Part(ManagedContentScope.All()), string.Empty));
    }

    [Fact]
    public void ItemWithoutThePart_IsNotEditableByAnyManagedSite()
        => Assert.False(Service.CanEdit(part: null, "sitea"));

    [Fact]
    public void EditScope_IsIndependentOfDisplayScope()
    {
        // The service answers each question from its own scope and infers nothing from the other, so a
        // part written before the rule existed, or imported by a recipe, evaluates exactly as stored.
        // Keeping the two answers coupled is the editor's job, covered by ManagedContentScopeCoherenceTests.
        var part = new ManagedContentPart
        {
            EditScope = ManagedContentScope.Selected("sitea"),
            DisplayScope = ManagedContentScope.None(),
        };

        Assert.True(Service.CanEdit(part, "sitea"));
        Assert.False(Service.CanDisplay(part, "sitea"));
    }

    private static ManagedContentScopeService Service => new();

    private static ManagedContentPart Part(ManagedContentScope editScope)
        => new() { EditScope = editScope };
}
