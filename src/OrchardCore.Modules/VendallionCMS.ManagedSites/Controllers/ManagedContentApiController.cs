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
    private readonly IManagedContentLocator _locator;
    private readonly IManagedContentScopeService _scopeService;
    private readonly IManagedContentOverrideService _overrideService;
    private readonly IManagedContentListService _listService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentApiController" /> class.
    /// </summary>
    /// <param name="locator">The Managed Content locator.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    /// <param name="overrideService">The Managed Content override service.</param>
    /// <param name="listService">The Managed Content listing service.</param>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="sessionService">The Managed Site session service.</param>
    public ManagedContentApiController(
        IManagedContentLocator locator,
        IManagedContentScopeService scopeService,
        IManagedContentOverrideService overrideService,
        IManagedContentListService listService,
        IManagedSiteClearanceService clearanceService,
        IManagedSiteSessionService sessionService)
        : base(clearanceService, sessionService)
    {
        _locator = locator;
        _scopeService = scopeService;
        _overrideService = overrideService;
        _listService = listService;
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

        ManagedContentOverrideStatus? status = null;

        if (!string.IsNullOrWhiteSpace(overrideStatus))
        {
            if (!Enum.TryParse<ManagedContentOverrideStatus>(overrideStatus, ignoreCase: true, out var parsed))
            {
                return ManagedSitesProblem(
                    StatusCodes.Status400BadRequest,
                    "Invalid override status",
                    $"'{overrideStatus}' is not a valid override status.",
                    ManagedSitesConstants.ErrorCodes.InvalidOverrideStatus);
            }

            status = parsed;
        }

        var listing = await _listService.ListAsync(
            managedSiteId,
            new ManagedContentListQuery(contentType, status, page, pageSize));

        return Ok(new ManagedContentListResponse
        {
            Items = [.. listing.Items],
            TotalCount = listing.TotalCount,
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

        var location = await _locator.FindAsync(sourceContentItemId, options: VersionOptions.Published);
        var source = location?.ContentItem;

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
            Override = ManagedContentOverrideSummary.Of(managedContentOverride),
        });
    }

    /// <summary>
    /// Creates the content item the Managed Site will use to override a Managed Content item.
    /// </summary>
    /// <remarks>
    /// Creating the item here rather than through the platform content API is what lets a Managed Site
    /// editor author an override with their clearance alone. Asking them to create it themselves first
    /// would require tenant-wide permission for its content type, which FR-011 forbids.
    /// </remarks>
    /// <param name="managedSiteId">The Managed Site identifier from the route.</param>
    /// <param name="sourceContentItemId">The source content item identifier.</param>
    /// <returns>The new override, or a problem describing why it was rejected.</returns>
    [HttpPost("{sourceContentItemId}/override")]
    [ProducesResponseType(typeof(ManagedContentOverrideSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ManagedSitesApiProblem), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOverride(string managedSiteId, string sourceContentItemId)
    {
        var validation = await ValidateManagedSiteScopeAsync(managedSiteId, ManagedSitesConstants.Scopes.Edit);

        if (!validation.Succeeded)
        {
            return validation.Failure;
        }

        var result = await _overrideService.CreateAsync(managedSiteId, sourceContentItemId);

        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, ManagedContentOverrideSummary.Of(result.Override))
            : Problem(result.Error);
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

        return result.Succeeded ? Ok(ManagedContentOverrideSummary.Of(result.Override)) : Problem(result.Error);
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
}
