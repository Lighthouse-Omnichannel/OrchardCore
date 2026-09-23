using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentManagement.Routing;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites.Handlers;

/// <summary>
/// Puts a Managed Site's own content in place of the original, as the content is loaded.
/// </summary>
/// <remarks>
/// Substituting at display time reaches only content rendered through the display pipeline. A Liquid
/// template that walks <c>Model.ContentItem.Content.Services.ContentItems</c> and reads fields off each
/// child never asks the display manager for anything, and that is how most themes render a page. So the
/// swap happens as the content is loaded, before anything decides how to render it, and Liquid, shapes,
/// and anything else reading the item all see the Managed Site's version.
///
/// Everything that needs the content manager is resolved when the handler runs rather than when it is
/// built. A content handler that asks for <see cref="IContentManager" /> in its constructor cannot be
/// constructed at all, because the content manager depends on the handlers.
///
/// The danger this carries is writing a Managed Site's content back over the Site Blueprint's. The
/// guard is that nothing is substituted unless a Managed Site was resolved for the current request, and
/// that only ever happens for public page rendering: the middleware resolves nothing for admin requests
/// or for API requests, which are the paths content is edited through. An editor therefore always loads
/// the original, whichever Managed Site they are working on.
/// </remarks>
public sealed class ManagedContentCompositionHandler : ContentHandlerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IManagedSiteCompositionContextAccessor _contextAccessor;
    private readonly IManagedContentScopeService _scopeService;
    private IManagedContentOverrideService _overrideService;
    private IContentManager _contentManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentCompositionHandler" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider, used to resolve the content manager lazily.</param>
    /// <param name="contextAccessor">The Managed Site composition context accessor.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    public ManagedContentCompositionHandler(
        IServiceProvider serviceProvider,
        IManagedSiteCompositionContextAccessor contextAccessor,
        IManagedContentScopeService scopeService)
    {
        _serviceProvider = serviceProvider;
        _contextAccessor = contextAccessor;
        _scopeService = scopeService;
    }

    /// <inheritdoc />
    public override async Task LoadedAsync(LoadContentContext context)
    {
        var managedSiteId = _contextAccessor.Current?.ManagedSiteId;

        if (string.IsNullOrEmpty(managedSiteId))
        {
            return;
        }

        var contentItem = context.ContentItem;

        // An override renders as itself. Composing it again would ask whether the override has an
        // override, which is both meaningless and a loop.
        if (contentItem is null || contentItem.Has(nameof(ManagedContentOverridePart)))
        {
            return;
        }

        // Resolved here rather than in the constructor: by the time a handler runs, the content manager
        // that called it is built and cached, so asking for it now is a lookup rather than a cycle.
        _contentManager ??= _serviceProvider.GetRequiredService<IContentManager>();
        _overrideService ??= _serviceProvider.GetRequiredService<IManagedContentOverrideService>();

        await SubstituteAsync(contentItem, (JsonObject)contentItem.Content, managedSiteId);
    }

    private async Task SubstituteAsync(ContentItem owner, JsonObject content, string managedSiteId)
    {
        var aspect = await _contentManager.PopulateAspectAsync<ContainedContentItemsAspect>(owner);

        foreach (var accessor in aspect.Accessors)
        {
            foreach (var jItem in accessor.Invoke(content).Cast<JsonObject>())
            {
                var contained = jItem.ToObject<ContentItem>();

                if (await TrySubstituteAsync(jItem, contained, managedSiteId))
                {
                    // The override replaced the whole item, so anything inside it belongs to the Managed
                    // Site already and is not composed again.
                    continue;
                }

                await SubstituteAsync(contained, jItem, managedSiteId);
            }
        }
    }

    private async Task<bool> TrySubstituteAsync(JsonObject jItem, ContentItem contained, string managedSiteId)
    {
        if (!contained.TryGet<ManagedContentPart>(out var part) || !_scopeService.CanEdit(part, managedSiteId))
        {
            return false;
        }

        var overrideContent = await _overrideService.FindPublishedOverrideAsync(
            managedSiteId,
            contained.ContentItemId);

        if (overrideContent is null)
        {
            return false;
        }

        var replacement = JObject.FromObject(overrideContent, JOptions.Default);

        if (replacement is null)
        {
            return false;
        }

        // Replace rather than merge. An override stands in for the item rather than extending it, so
        // anything the original carried that the override does not must not survive.
        foreach (var name in jItem.Select(property => property.Key).ToArray())
        {
            jItem.Remove(name);
        }

        foreach (var property in replacement)
        {
            jItem[property.Key] = property.Value?.DeepClone();
        }

        return true;
    }
}
