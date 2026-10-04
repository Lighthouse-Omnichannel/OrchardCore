using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Moq;
using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;
using YesSql;

namespace VendallionCMS.ManagedSites.Tests.ManagedContent;

/// <summary>
/// Covers what an override must not inherit from the item it replaces.
/// </summary>
/// <remarks>
/// An override starts as a copy, so that an editor changes what differs rather than retyping the item.
/// What it must not copy is anything that would give the copy a place of its own, because an override
/// is only ever reached by standing in for the item it replaces.
///
/// Two ways that went wrong. An alias or a route names one owner for one address and the platform
/// rejects a second claimant outright, so an override of an aliased item could not be published at all,
/// which took most content types worth overriding out of the feature. A layer membership is worse than
/// that: the copy was published, the Layers module knew nothing of Managed Sites, and so one Managed
/// Site's widget was drawn on every other Managed Site and on the Site Blueprint too.
/// </remarks>
public class OverrideIdentityTests
{
    [Theory]
    [InlineData("AliasPart")]
    [InlineData("AutoroutePart")]
    [InlineData("LayerMetadata")]
    public async Task AnOverride_DoesNotInheritAPartThatClaimsAPlaceOfItsOwn(string partName)
    {
        var context = new OverrideCreationContext(partName);

        await context.CreateAsync();

        Assert.False(context.Created.Has(partName));
    }

    [Fact]
    public async Task AnOverride_StillInheritsTheContentAnEditorCameToChange()
    {
        // Guards the test above: dropping too much would leave an editor with an empty item.
        var context = new OverrideCreationContext("AliasPart");

        await context.CreateAsync();

        Assert.True(context.Created.Has("HtmlBodyPart"));
    }

    [Fact]
    public async Task AnOverride_StillDoesNotInheritTheScopesOfTheItemItReplaces()
    {
        var context = new OverrideCreationContext("AliasPart");

        await context.CreateAsync();

        Assert.False(context.Created.Has(nameof(ManagedContentPart)));
    }

    /// <summary>
    /// Assembles the override service around a source carrying one address-claiming part.
    /// </summary>
    private sealed class OverrideCreationContext
    {
        private readonly ManagedContentOverrideService _service;

        public OverrideCreationContext(string partName)
        {
            var source = ManagedContentTestContent.Source("source-item", ManagedContentScope.All());
            var content = (JsonObject)source.Content;
            content[partName] = new JsonObject { ["Alias"] = "footer", ["Path"] = "footer" };
            content["HtmlBodyPart"] = new JsonObject { ["Html"] = "<p>blueprint</p>" };

            var contentManager = new Mock<IContentManager>(MockBehavior.Strict);
            contentManager
                .Setup(manager => manager.NewAsync(source.ContentType))
                .ReturnsAsync(() => new ContentItem { ContentType = source.ContentType });
            contentManager
                .Setup(manager => manager.CreateAsync(It.IsAny<ContentItem>(), VersionOptions.Draft))
                .Callback<ContentItem, VersionOptions>((item, _) => Created = item)
                .ReturnsAsync(true);

            _service = new ManagedContentOverrideService(
                // Auto-mocked: the creation path asks whether an override already exists, and the
                // answer here is simply "none".
                new Mock<ISession> { DefaultValue = DefaultValue.Mock }.Object,
                contentManager.Object,
                new FakeManagedContentLocator().WithPublished(source),
                new FakeManagedSiteService(ManagedSitesTestData.ManagedSite("site-a")),
                new ManagedContentScopeService(),
                new Mock<IManagedContentSuppressionService>(MockBehavior.Strict).Object);
        }

        public ContentItem Created { get; private set; }

        public async Task CreateAsync()
        {
            var result = await _service.CreateAsync("site-a", "source-item");

            Assert.Equal(ManagedContentOverrideError.None, result.Error);
            Assert.NotNull(Created);
        }
    }
}
