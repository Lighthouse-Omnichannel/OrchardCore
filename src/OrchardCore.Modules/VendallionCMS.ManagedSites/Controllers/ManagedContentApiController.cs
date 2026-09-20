using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;
using YesSql;
using ISession = YesSql.ISession;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// Discovers the Managed Content a Managed Site may override, and owns its overrides.
/// </summary>
/// <remarks>
/// Every action is scoped to the Managed Site in the route, validated against signed clearance and the
/// active portal session before anything is read or written. The edit scope is checked again on every
/// call rather than trusted from the listing, because a blueprint administrator can narrow it between
/// the moment an editor opens an item and the moment they save.
/// </remarks>
[Route("api/managed-sites/{managedSiteId}/managed-content")]
public sealed class ManagedContentApiController : ManagedSitesApiControllerBase
{
    /// <summary>
    /// Parts that make a content item hold children of its own.
    /// </summary>
    /// <remarks>
    /// Named rather than referenced, so discovering that an item is a container does not make this
    /// module depend on the modules that supply those parts.
    /// </remarks>
    private static readonly string[] _containerParts = ["ListPart", "BagPart", "FlowPart"];

    private readonly ISession _session;
    private readonly IContentManager _contentManager;
    private readonly IContentDefinitionManager _contentDefinitionManager;
    private readonly IManagedContentScopeService _scopeService;
    private readonly IManagedContentOverrideService _overrideService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentApiController" /> class.
    /// </summary>
    /// <param name="session">The document session.</param>
    /// <param name="contentManager">The content manager.</param>
    /// <param name="contentDefinitionManager">The content definition manager.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    /// <param name="overrideService">The Managed Content override service.</param>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="sessionService">The Managed Site session service.</param>
    public ManagedContentApiController(
        ISession session,
        IContentManager contentManager,
        IContentDefinitionManager contentDefinitionManager,
        IManagedContentScopeService scopeService,
        IManagedContentOverrideService overrideService,
        IManagedSiteClearanceService clearanceService,
        IManagedSiteSessionService sessionService)
        : base(clearanceService, sessionService)
    {
        _session = session;
        _contentManager = contentManager;
        _contentDefinitionManager = contentDefinitionManager;
        _scopeService = scopeService;
        _overrideService = overrideService;
    }

    /// <summary>
    /// Lists the content items the Managed Site may override.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier from the route.</param>
    /// <param name="contentType">Optional content type filter.</param>
    /// <param name="overrideStatus">Optional override status filter.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>The editable items, or a problem describing why the request was rejected.</returns>
    [HttpGet("")]
    [ProducesResponseType(typeof(ManagedContentListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        string managedSiteId,
        [FromQuery] string contentType = null,
        [FromQuery] string overrideStatus = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var validation = await ValidateManagedSiteScopeAsync(managedSiteId, ManagedSitesConstants.Scopes.View);

        if (!validation.Succeeded)
        {
            return validation.Failure;
        }

        var items = await ListEditableAsync(managedSiteId, contentType);

        if (!string.IsNullOrWhiteSpace(overrideStatus))
        {
            if (!Enum.TryParse<ManagedContentOverrideStatus>(overrideStatus, ignoreCase: true, out var status))
            {
                return ManagedSitesProblem(
                    StatusCodes.Status400BadRequest,
                    "Invalid override status",
                    $"'{overrideStatus}' is not a valid override status.",
                    ManagedSitesConstants.ErrorCodes.InvalidOverrideStatus);
            }

            items = [.. items.Where(item => OverrideStatusOf(item) == status)];
        }

        // Paging is applied after filtering rather than in the query, because override status lives on
        // the Managed Site's own content and cannot be joined into the edit scope lookup.
        var take = Math.Clamp(pageSize, 1, 200);
        var skip = Math.Max(page - 1, 0) * take;

        return Ok(new ManagedContentListResponse
        {
            Items = [.. items.Skip(skip).Take(take)],
            TotalCount = items.Count,
        });
    }

    /// <summary>
    /// Lists the Managed Site's overrides that exist but do not render.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier from the route.</param>
    /// <returns>The suppressed overrides, each carrying its reason.</returns>
    [HttpGet("suppressed")]
    [ProducesResponseType(typeof(SuppressedOverridesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Suppressed(string managedSiteId)
    {
        var validation = await ValidateManagedSiteScopeAsync(managedSiteId, ManagedSitesConstants.Scopes.View);

        if (!validation.Succeeded)
        {
            return validation.Failure;
        }

        var suppressed = await _overrideService.ListSuppressedAsync(managedSiteId);

        return Ok(new SuppressedOverridesResponse
        {
            Items =
            [
                .. suppressed.Select(item => new SuppressedOverrideItem
                {
                    SourceContentItemId = item.SourceContentItemId,
                    OverrideContentItemId = item.OverrideContentItemId,
                    Status = item.Status.ToString(),
                    SuppressionReason = item.SuppressionReason.ToString(),
                }),
            ],
        });
    }

    /// <summary>
    /// Gets one Managed Content item as the Managed Site sees it.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier from the route.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <returns>The item detail, or a problem describing why the request was rejected.</returns>
    [HttpGet("{sourceContentItemId}")]
    [ProducesResponseType(typeof(ManagedContentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail(string managedSiteId, string sourceContentItemId)
    {
        var validation = await ValidateManagedSiteScopeAsync(managedSiteId, ManagedSitesConstants.Scopes.View);

        if (!validation.Succeeded)
        {
            return validation.Failure;
        }

        var source = await _contentManager.GetAsync(sourceContentItemId, VersionOptions.Published);

        if (source is null || !source.TryGet<ManagedContentPart>(out var part))
        {
            return ManagedSitesProblem(
                StatusCodes.Status404NotFound,
                "Managed Content item not found",
                "No published content item with that identifier carries Managed Content.",
                ManagedSitesConstants.ErrorCodes.SourceNotFound);
        }

        if (!_scopeService.CanEdit(part, managedSiteId))
        {
            return EditScopeExcluded();
        }

        var managedContentOverride = await _overrideService.GetAsync(managedSiteId, sourceContentItemId);

        return Ok(new ManagedContentDetailResponse
        {
            SourceContentItemId = sourceContentItemId,
            ContentType = source.ContentType,
            DisplayText = source.DisplayText,
            EditScopeIncludesManagedSite = true,
            DisplayScopeIncludesManagedSite = _scopeService.CanDisplay(part, managedSiteId),
            Override = Describe(managedContentOverride),
        });
    }

    /// <summary>
    /// Registers a content item as the Managed Site's override of a Managed Content item.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier from the route.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <param name="request">The override to register.</param>
    /// <returns>The stored override, or a problem describing why it was rejected.</returns>
    [HttpPut("{sourceContentItemId}/override")]
    [ProducesResponseType(typeof(ManagedContentOverrideSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PutOverride(
        string managedSiteId,
        string sourceContentItemId,
        [FromBody] ManagedContentOverrideRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.OverrideContentItemId))
        {
            return ManagedSitesProblem(
                StatusCodes.Status400BadRequest,
                "Override content item required",
                "Name the content item that holds the override content.",
                ManagedSitesConstants.ErrorCodes.OverrideNotFound);
        }

        var status = ManagedContentOverrideStatus.Draft;

        if (!string.IsNullOrWhiteSpace(request.Status)
            && (!Enum.TryParse(request.Status, ignoreCase: true, out status)
                || (status != ManagedContentOverrideStatus.Draft && status != ManagedContentOverrideStatus.Published)))
        {
            return ManagedSitesProblem(
                StatusCodes.Status400BadRequest,
                "Invalid override status",
                $"'{request.Status}' is not a status an override can be saved with.",
                ManagedSitesConstants.ErrorCodes.InvalidOverrideStatus);
        }

        var publish = status == ManagedContentOverrideStatus.Published;

        // Publishing an override changes what visitors see, so it needs the publish clearance rather
        // than the clearance that lets an editor keep working on a draft.
        var validation = await ValidateManagedSiteScopeAsync(
            managedSiteId,
            publish ? ManagedSitesConstants.Scopes.Publish : ManagedSitesConstants.Scopes.Edit);

        if (!validation.Succeeded)
        {
            return validation.Failure;
        }

        var result = await _overrideService.SaveAsync(
            managedSiteId,
            sourceContentItemId,
            request.OverrideContentItemId,
            publish);

        return result.Succeeded ? Ok(Describe(result.Override)) : Problem(result.Error);
    }

    /// <summary>
    /// Removes the Managed Site's override, restoring the original content for that Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier from the route.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <returns>No content when removed, or a problem describing why the request was rejected.</returns>
    [HttpDelete("{sourceContentItemId}/override")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOverride(string managedSiteId, string sourceContentItemId)
    {
        var validation = await ValidateManagedSiteScopeAsync(managedSiteId, ManagedSitesConstants.Scopes.Edit);

        if (!validation.Succeeded)
        {
            return validation.Failure;
        }

        var removed = await _overrideService.DeleteAsync(managedSiteId, sourceContentItemId);

        if (!removed)
        {
            return ManagedSitesProblem(
                StatusCodes.Status404NotFound,
                "Override not found",
                "This Managed Site holds no override for that content item.",
                ManagedSitesConstants.ErrorCodes.OverrideNotFound);
        }

        return NoContent();
    }

    private async Task<List<ManagedContentListItem>> ListEditableAsync(string managedSiteId, string contentType)
    {
        var rows = await _session
            .QueryIndex<ManagedContentEditScopeIndex>(index =>
                (index.ManagedSiteId == managedSiteId || index.AllManagedSites) && index.Published)
            .ListAsync();

        var sourceIds = rows
            .Where(row => string.IsNullOrEmpty(contentType)
                || string.Equals(row.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
            .Select(row => row.ContentItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (sourceIds.Length == 0)
        {
            return [];
        }

        var sources = await _contentManager.GetAsync(sourceIds, VersionOptions.Published);

        var overrides = (await _overrideService.ListAsync(managedSiteId))
            .ToDictionary(item => item.SourceContentItemId, StringComparer.Ordinal);

        var items = new List<ManagedContentListItem>();

        foreach (var source in sources)
        {
            if (!source.TryGet<ManagedContentPart>(out var part) || !_scopeService.CanEdit(part, managedSiteId))
            {
                continue;
            }

            overrides.TryGetValue(source.ContentItemId, out var managedContentOverride);

            items.Add(new ManagedContentListItem
            {
                SourceContentItemId = source.ContentItemId,
                ContentType = source.ContentType,
                DisplayText = source.DisplayText,
                IsContainer = await IsContainerAsync(source.ContentType),
                DisplayScopeIncludesManagedSite = _scopeService.CanDisplay(part, managedSiteId),
                Override = Describe(managedContentOverride),
            });
        }

        return [.. items.OrderBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase)];
    }

    private async Task<bool> IsContainerAsync(string contentType)
    {
        var definition = await _contentDefinitionManager.GetTypeDefinitionAsync(contentType);

        return definition is not null
            && definition.Parts.Any(part => _containerParts.Contains(part.PartDefinition?.Name, StringComparer.Ordinal));
    }

    private IActionResult Problem(ManagedContentOverrideError error) => error switch
    {
        ManagedContentOverrideError.ManagedSiteUnavailable => ManagedSitesProblem(
            StatusCodes.Status409Conflict,
            "Managed Site unavailable",
            "The Managed Site no longer accepts content changes.",
            ManagedSitesConstants.ErrorCodes.ManagedSiteUnavailable),

        ManagedContentOverrideError.SourceNotFound => ManagedSitesProblem(
            StatusCodes.Status404NotFound,
            "Managed Content item not found",
            "No published content item with that identifier exists.",
            ManagedSitesConstants.ErrorCodes.SourceNotFound),

        ManagedContentOverrideError.SourceNotManagedContent => ManagedSitesProblem(
            StatusCodes.Status409Conflict,
            "Item no longer carries Managed Content",
            "The source content item no longer carries Managed Content, so it cannot be overridden.",
            ManagedSitesConstants.ErrorCodes.SourceNotManagedContent),

        ManagedContentOverrideError.EditScopeExcluded => EditScopeExcluded(),

        ManagedContentOverrideError.OverrideNotFound => ManagedSitesProblem(
            StatusCodes.Status404NotFound,
            "Override content item not found",
            "No content item with that identifier exists to register as the override.",
            ManagedSitesConstants.ErrorCodes.OverrideNotFound),

        ManagedContentOverrideError.ContentTypeMismatch => ManagedSitesProblem(
            StatusCodes.Status400BadRequest,
            "Override content type mismatch",
            "An override must use the same content type as the item it replaces.",
            ManagedSitesConstants.ErrorCodes.ContentTypeMismatch),

        ManagedContentOverrideError.OverrideAlreadyExists => ManagedSitesProblem(
            StatusCodes.Status409Conflict,
            "Override already exists",
            "A different content item already overrides this item for this Managed Site. Remove it first.",
            ManagedSitesConstants.ErrorCodes.OverrideAlreadyExists),

        _ => ManagedSitesProblem(
            StatusCodes.Status400BadRequest,
            "Override rejected",
            "The override could not be saved.",
            ManagedSitesConstants.ErrorCodes.OverrideNotFound),
    };

    private IActionResult EditScopeExcluded() => ManagedSitesProblem(
        StatusCodes.Status403Forbidden,
        "Edit scope excludes this Managed Site",
        "The item's edit scope does not allow this Managed Site to override it.",
        ManagedSitesConstants.ErrorCodes.EditScopeExcluded);

    private static ManagedContentOverrideStatus OverrideStatusOf(ManagedContentListItem item)
        => item.Override is null
            ? ManagedContentOverrideStatus.None
            : Enum.Parse<ManagedContentOverrideStatus>(item.Override.Status);

    private static ManagedContentOverrideSummary Describe(ManagedContentOverride managedContentOverride)
        => managedContentOverride is null
            ? null
            : new ManagedContentOverrideSummary
            {
                OverrideContentItemId = managedContentOverride.OverrideContentItemId,
                Status = managedContentOverride.Status.ToString(),
                SuppressionReason =
                    managedContentOverride.SuppressionReason == ManagedContentOverrideSuppressionReason.None
                        ? null
                        : managedContentOverride.SuppressionReason.ToString(),
            };
}
