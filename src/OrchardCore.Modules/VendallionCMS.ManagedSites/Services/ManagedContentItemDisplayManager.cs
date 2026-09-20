using Microsoft.AspNetCore.Http;
using OrchardCore.Admin;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Serves a Managed Site its own version of a content item, or nothing at all.
/// </summary>
/// <remarks>
/// Replacing what a content item renders cannot be done from a part display driver: a part driver
/// contributes its own shapes and cannot withdraw the shapes its siblings produce. The single place
/// where a content item becomes a shape is <see cref="IContentItemDisplayManager" />, so the swap
/// happens here, wrapping the platform implementation rather than reimplementing it.
///
/// Three things are deliberately left alone. Editors are never intercepted, because an override is
/// authored as itself and the source item is authored in the Site Blueprint. Admin requests are never
/// intercepted, because a blueprint administrator has to keep seeing every item, including the ones
/// their display scope hides from visitors. An item that carries no Managed Content is handed straight
/// through, which is what keeps the feature invisible to content that never opted in.
/// </remarks>
public sealed class ManagedContentItemDisplayManager : IContentItemDisplayManager
{
    /// <summary>
    /// Shape rendered in place of a content item the request context must not see.
    /// </summary>
    public const string HiddenShapeType = "ManagedContentHidden";

    private readonly IContentItemDisplayManager _inner;
    private readonly IManagedContentResolutionService _resolutionService;
    private readonly IManagedSiteCompositionContextAccessor _contextAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IShapeFactory _shapeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentItemDisplayManager" /> class.
    /// </summary>
    /// <param name="inner">The platform content item display manager this one wraps.</param>
    /// <param name="resolutionService">The Managed Content resolution service.</param>
    /// <param name="contextAccessor">The Managed Site composition context accessor.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="shapeFactory">The shape factory.</param>
    public ManagedContentItemDisplayManager(
        IContentItemDisplayManager inner,
        IManagedContentResolutionService resolutionService,
        IManagedSiteCompositionContextAccessor contextAccessor,
        IHttpContextAccessor httpContextAccessor,
        IShapeFactory shapeFactory)
    {
        _inner = inner;
        _resolutionService = resolutionService;
        _contextAccessor = contextAccessor;
        _httpContextAccessor = httpContextAccessor;
        _shapeFactory = shapeFactory;
    }

    /// <inheritdoc />
    public async Task<IShape> BuildDisplayAsync(
        ContentItem content,
        IUpdateModel updater,
        string displayType = "",
        string groupId = "")
    {
        if (!ShouldCompose(content))
        {
            return await _inner.BuildDisplayAsync(content, updater, displayType, groupId);
        }

        var resolution = await _resolutionService.ResolveAsync(content, _contextAccessor.Current?.ManagedSiteId);

        if (!resolution.ShouldRender)
        {
            return await _shapeFactory.CreateAsync(HiddenShapeType);
        }

        return await _inner.BuildDisplayAsync(resolution.Content, updater, displayType, groupId);
    }

    /// <inheritdoc />
    public Task<IShape> BuildEditorAsync(
        ContentItem content,
        IUpdateModel updater,
        bool isNew,
        string groupId = "",
        string htmlFieldPrefix = "")
        => _inner.BuildEditorAsync(content, updater, isNew, groupId, htmlFieldPrefix);

    /// <inheritdoc />
    public Task<IShape> UpdateEditorAsync(
        ContentItem content,
        IUpdateModel updater,
        bool isNew,
        string groupId = "",
        string htmlFieldPrefix = "")
        => _inner.UpdateEditorAsync(content, updater, isNew, groupId, htmlFieldPrefix);

    private bool ShouldCompose(ContentItem content)
    {
        if (content is null || !content.Has(nameof(ManagedContentPart)))
        {
            return false;
        }

        // An override renders as itself. Composing it again would ask whether the override has an
        // override, which is both meaningless and a loop.
        if (content.Has(nameof(ManagedContentOverridePart)))
        {
            return false;
        }

        var httpContext = _httpContextAccessor.HttpContext;

        return httpContext is not null && !AdminAttribute.IsApplied(httpContext);
    }
}
