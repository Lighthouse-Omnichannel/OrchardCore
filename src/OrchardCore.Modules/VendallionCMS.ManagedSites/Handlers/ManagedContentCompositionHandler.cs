using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
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
/// Collaborators are resolved when the handler runs rather than when it is built. A content handler
/// that asks in its constructor for anything reaching <see cref="IContentManager" /> cannot be
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
    private IManagedSiteClearanceService _clearanceService;
    private IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentCompositionHandler" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider, used to resolve collaborators lazily.</param>
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
        var requestContext = _contextAccessor.Current;
        var managedSiteId = requestContext?.ManagedSiteId;

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
        _overrideService ??= _serviceProvider.GetRequiredService<IManagedContentOverrideService>();

        var includeDrafts = await IncludeDraftsAsync(requestContext, managedSiteId);

        // The item itself first. If it is replaced, nothing inside it is visited: what it now holds is
        // the Managed Site's own content, not a set of further items for that same Managed Site to
        // override, and asking would be the same loop an override of an override is.
        if (await TrySubstituteSelfAsync(contentItem, managedSiteId, includeDrafts))
        {
            return;
        }

        await SubstituteAsync(contentItem, managedSiteId, includeDrafts);
    }

    /// <summary>
    /// Decides whether this request may see work the Managed Site has not published.
    /// </summary>
    /// <remarks>
    /// The request asks through a query value, and anyone can type one, so the answer comes from the
    /// caller's preview clearance for the Managed Site the URL resolved to. Authentication has run by
    /// the time content loads, which it had not when the request was resolved.
    /// </remarks>
    private async Task<bool> IncludeDraftsAsync(ManagedSiteRequestContext requestContext, string managedSiteId)
    {
        if (!requestContext.PreviewRequested)
        {
            return false;
        }

        _clearanceService ??= _serviceProvider.GetRequiredService<IManagedSiteClearanceService>();
        _httpContextAccessor ??= _serviceProvider.GetRequiredService<IHttpContextAccessor>();

        var user = _httpContextAccessor.HttpContext?.User;

        return user is not null
            && await _clearanceService.HasClearanceAsync(user, managedSiteId, ManagedSitesConstants.Scopes.Preview);
    }

    /// <summary>
    /// Replaces a content item that is stored in its own right with the Managed Site's version of it.
    /// </summary>
    /// <remarks>
    /// Substituting items stored inside the loaded item covers a page's sections, which is where most
    /// Managed Content lives, but not an item that is a document of its own: a menu, a widget, a page.
    /// Those are loaded as themselves and nothing replaced them, so an override of one was listed,
    /// published, and reported as rendering while the Site Blueprint's version went on being served.
    ///
    /// Identity stays the Site Blueprint's. The database key, the content item identifier and the type
    /// are the ones the request resolved and the ones everything downstream keys on; what changes is
    /// the content, plus the display text, which templates show. Removing each property rather than
    /// overwriting it also drops the copy of the part the platform kept while loading, so what is read
    /// afterwards is the substituted content.
    /// </remarks>
    /// <param name="contentItem">The loaded content item.</param>
    /// <param name="managedSiteId">The Managed Site the request resolved to.</param>
    /// <param name="includeDrafts">Whether unpublished work may be shown.</param>
    /// <returns><see langword="true" /> when the item was replaced.</returns>
    private async Task<bool> TrySubstituteSelfAsync(
        ContentItem contentItem,
        string managedSiteId,
        bool includeDrafts)
    {
        if (!contentItem.TryGet<ManagedContentPart>(out var part) || !_scopeService.CanEdit(part, managedSiteId))
        {
            return false;
        }

        var overrideContent = await _overrideService.FindOverrideContentAsync(
            managedSiteId,
            contentItem.ContentItemId,
            includeDrafts);

        if (overrideContent is null)
        {
            return false;
        }

        return ReplaceContent(contentItem, overrideContent);
    }

    /// <summary>
    /// Gives a content item another's content, keeping the identity it was loaded under.
    /// </summary>
    /// <remarks>
    /// Identity stays the Site Blueprint's: the database key, the content item identifier and the type
    /// are the ones the request resolved and the ones everything downstream keys on, caching and
    /// invalidation included. What changes is the content, and the display text, because templates
    /// show it and it is not part of the content.
    ///
    /// Each property is removed rather than overwritten, which also drops the copy of that part the
    /// platform kept while loading. Overwriting alone would leave the item handing out the content it
    /// had when it was loaded, however the JSON beneath had been rewritten.
    /// </remarks>
    /// <param name="target">The loaded content item to replace the content of.</param>
    /// <param name="replacement">The content item whose content it should carry.</param>
    /// <returns><see langword="true" /> when the content was replaced.</returns>
    internal static bool ReplaceContent(ContentItem target, ContentItem replacement)
    {
        var replacementContent = JObject.FromObject(replacement, JOptions.Default);

        if (replacementContent is null)
        {
            return false;
        }

        var content = (JsonObject)target.Content;

        foreach (var name in content.Select(property => property.Key).ToArray())
        {
            target.Remove(name);
        }

        foreach (var property in replacementContent)
        {
            content[property.Key] = property.Value?.DeepClone();
        }

        target.DisplayText = replacement.DisplayText;

        return true;
    }

    private async Task SubstituteAsync(ContentItem owner, string managedSiteId, bool includeDrafts)
    {
        // Substituting an item replaces everything inside it, so the list is taken once and a replaced
        // item's former children are simply never visited.
        var substituted = new List<string>();
        var touched = new HashSet<string>(StringComparer.Ordinal);

        foreach (var contained in ManagedContentContainment.ListContained(owner))
        {
            if (substituted.Any(path =>
                contained.JsonPath.StartsWith(path, StringComparison.Ordinal)
                && contained.JsonPath.Length > path.Length))
            {
                continue;
            }

            if (await TrySubstituteAsync(contained.Content, contained.ContentItem, managedSiteId, includeDrafts))
            {
                substituted.Add(contained.JsonPath);

                var partName = PartNameOf(contained.JsonPath);

                if (partName is not null)
                {
                    touched.Add(partName);
                }
            }
        }

        Refresh(owner, touched);
    }

    /// <summary>
    /// Drops the parts a substitution changed underneath, so they are read again.
    /// </summary>
    /// <remarks>
    /// A content item keeps each part it has been asked for, and by the time a handler sees a loaded
    /// item the platform has already asked for all of them, to hand them to the part handlers. So this
    /// swap, which edits the stored JSON, is invisible to anything reading a part: the menu kept
    /// handing out the Site Blueprint's entries however the JSON beneath them had been rewritten.
    ///
    /// Removing a part takes it out of both the JSON and the kept copy, so putting the JSON straight
    /// back leaves the item reading the substituted content the next time the part is asked for. The
    /// one visible effect is that the part moves to the end of the item's JSON, which matters only to
    /// something comparing serialized output; nothing saves a content item on a rendering request.
    /// </remarks>
    /// <param name="owner">The content item whose parts were substituted underneath.</param>
    /// <param name="partNames">The parts to read again.</param>
    internal static void Refresh(ContentItem owner, IEnumerable<string> partNames)
    {
        var content = (JsonObject)owner.Content;

        foreach (var partName in partNames)
        {
            if (!content.TryGetPropertyValue(partName, out var part) || part is null)
            {
                continue;
            }

            var substituted = part.DeepClone();

            owner.Remove(partName);

            content[partName] = substituted;
        }
    }

    /// <summary>
    /// Reads the part a contained item sits under from its path.
    /// </summary>
    /// <param name="jsonPath">The path, as <c>$.PartName.Collection[0]</c>.</param>
    /// <returns>The part name, or <see langword="null" /> when the path names none.</returns>
    internal static string PartNameOf(string jsonPath)
    {
        if (string.IsNullOrEmpty(jsonPath))
        {
            return null;
        }

        var path = jsonPath.StartsWith('$') ? jsonPath[1..] : jsonPath;

        if (path.StartsWith('.'))
        {
            path = path[1..];
        }

        var end = path.IndexOfAny(['.', '[']);

        if (end < 0)
        {
            end = path.Length;
        }

        return end > 0 ? path[..end] : null;
    }

    private async Task<bool> TrySubstituteAsync(
        JsonObject jItem,
        ContentItem contained,
        string managedSiteId,
        bool includeDrafts)
    {
        if (!contained.TryGet<ManagedContentPart>(out var part) || !_scopeService.CanEdit(part, managedSiteId))
        {
            return false;
        }

        var overrideContent = await _overrideService.FindOverrideContentAsync(
            managedSiteId,
            contained.ContentItemId,
            includeDrafts);

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
