using System.Text.Json;
using System.Text.Json.Nodes;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Handlers;
using VendallionCMS.ManagedSites.Models;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Composition;

/// <summary>
/// Covers giving a content item stored in its own right the Managed Site's version of it.
/// </summary>
/// <remarks>
/// Substituting the items stored inside a loaded item covers a page's sections, which is where most
/// Managed Content lives. It does not cover an item that is a document of its own: a menu, a widget, a
/// page. Those are loaded as themselves, so an override of one was listed, published and reported as
/// rendering while the Site Blueprint's version went on being served.
///
/// What this must not do is change who the item is. The request resolved a route, a cache key and an
/// invalidation tag from the Site Blueprint's identifiers, and everything downstream still keys on
/// them; only the content the visitor sees belongs to the Managed Site.
/// </remarks>
public class TopLevelSubstitutionTests
{
    [Fact]
    public void TheItem_CarriesTheOverridesContent()
    {
        var source = Source();

        ManagedContentCompositionHandler.ReplaceContent(source, Override());

        Assert.Equal("the managed site's words", (string)((JsonObject)source.Content)["HtmlBodyPart"]["Html"]);
    }

    [Fact]
    public void TheItem_KeepsTheIdentityItWasLoadedUnder()
    {
        var source = Source();

        ManagedContentCompositionHandler.ReplaceContent(source, Override());

        Assert.Equal(42, source.Id);
        Assert.Equal("blueprint-item", source.ContentItemId);
        Assert.Equal("LandingPage", source.ContentType);
    }

    [Fact]
    public void TheItem_TakesTheOverridesDisplayText()
    {
        // Templates show it, and it is a property of the item rather than part of its content, so
        // replacing the content alone would leave the Site Blueprint's title above the other's words.
        var source = Source();

        ManagedContentCompositionHandler.ReplaceContent(source, Override());

        Assert.Equal("The managed site's page", source.DisplayText);
    }

    [Fact]
    public void APartAlreadyReadBeforeTheSwap_IsReadAgainAfterIt()
    {
        // The platform reads every part while loading, before any handler sees the item, so a swap that
        // only rewrote the JSON would be invisible to everything that reads a part.
        var source = Source();
        Assert.Equal("the blueprint's words", source.As<HtmlBodyPart>().Html);

        ManagedContentCompositionHandler.ReplaceContent(source, Override());

        Assert.Equal("the managed site's words", source.As<HtmlBodyPart>().Html);
    }

    [Fact]
    public void WhatTheSourceCarriedAndTheOverrideDoesNot_DoesNotSurvive()
    {
        // An override stands in for the item rather than extending it. Its scopes in particular must
        // not survive, or the Managed Site's own content would be offered for overriding again.
        var source = Source();
        source.Alter<ManagedContentPart>(part => part.EditScope = ManagedContentScope.All());

        ManagedContentCompositionHandler.ReplaceContent(source, Override());

        Assert.False(source.Has(nameof(ManagedContentPart)));
        Assert.Null(((JsonObject)source.Content)["TitlePart"]);
    }

    private static ContentItem Source()
    {
        var item = new ContentItem
        {
            Id = 42,
            ContentItemId = "blueprint-item",
            ContentType = "LandingPage",
            DisplayText = "The blueprint's page",
        };

        var content = (JsonObject)item.Content;
        content["HtmlBodyPart"] = new JsonObject { ["Html"] = "the blueprint's words" };
        content["TitlePart"] = new JsonObject { ["Title"] = "The blueprint's page" };

        return item;
    }

    private static ContentItem Override()
    {
        var item = new ContentItem
        {
            ContentItemId = "override-item",
            ContentType = "LandingPage",
            DisplayText = "The managed site's page",
        };

        ((JsonObject)item.Content)["HtmlBodyPart"] = new JsonObject { ["Html"] = "the managed site's words" };

        return item;
    }

    private sealed class HtmlBodyPart : ContentPart
    {
        public string Html { get; set; }
    }
}
