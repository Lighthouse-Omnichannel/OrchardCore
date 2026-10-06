using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Admin;
using OrchardCore.ContentManagement;
using OrchardCore.DisplayManagement.Notify;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// The Managed Site Admin Portal: what a Managed Site may customize, and what it has done with it.
/// </summary>
/// <remarks>
/// Every action answers for one Managed Site, and which one is never taken from the request. It comes
/// from the session the editor selected, checked against their clearance on each action rather than
/// trusted from the one that listed the items, because a blueprint administrator can narrow an item's
/// scope between an editor opening it and saving it.
///
/// An editor cleared for exactly one Managed Site is working in it as soon as they arrive. One cleared
/// for several chooses, and until they do, nothing scoped is shown.
/// </remarks>
[Admin("ManagedSites/Portal/{action}/{sourceContentItemId?}", "ManagedSitesPortal{action}")]
public sealed class ManagedSitePortalController : Controller
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IManagedSiteSessionService _sessionService;
    private readonly IManagedSiteClearanceService _clearanceService;
    private readonly IManagedContentListService _listService;
    private readonly IManagedContentOverrideService _overrideService;
    private readonly IManagedContentLocator _locator;
    private readonly IManagedContentScopeService _scopeService;
    private readonly IManagedSitePreviewService _previewService;
    private readonly INotifier _notifier;

    private readonly IStringLocalizer S;
    private readonly IHtmlLocalizer H;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSitePortalController" /> class.
    /// </summary>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="sessionService">The Managed Site session service.</param>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="listService">The Managed Content listing service.</param>
    /// <param name="overrideService">The Managed Content override service.</param>
    /// <param name="locator">The Managed Content locator.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    /// <param name="previewService">The Managed Site preview service.</param>
    /// <param name="notifier">The notifier.</param>
    /// <param name="stringLocalizer">The string localizer.</param>
    /// <param name="htmlLocalizer">The HTML localizer.</param>
    public ManagedSitePortalController(
        IAuthorizationService authorizationService,
        IManagedSiteSessionService sessionService,
        IManagedSiteClearanceService clearanceService,
        IManagedContentListService listService,
        IManagedContentOverrideService overrideService,
        IManagedContentLocator locator,
        IManagedContentScopeService scopeService,
        IManagedSitePreviewService previewService,
        INotifier notifier,
        IStringLocalizer<ManagedSitePortalController> stringLocalizer,
        IHtmlLocalizer<ManagedSitePortalController> htmlLocalizer)
    {
        _authorizationService = authorizationService;
        _sessionService = sessionService;
        _clearanceService = clearanceService;
        _listService = listService;
        _overrideService = overrideService;
        _locator = locator;
        _scopeService = scopeService;
        _previewService = previewService;
        _notifier = notifier;
        S = stringLocalizer;
        H = htmlLocalizer;
    }

    /// <summary>
    /// Lists what the active Managed Site may customize.
    /// </summary>
    /// <param name="contentType">One content type, or nothing for all of them.</param>
    /// <param name="overrideStatus">One override status, or nothing for all of them.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="pageSize">How many items a page holds.</param>
    /// <returns>The list, the Managed Site chooser, or a refusal.</returns>
    public async Task<IActionResult> Index(
        string contentType = null,
        string overrideStatus = null,
        int page = 1,
        int pageSize = 50)
    {
        var scope = await ResolveScopeAsync();

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        ManagedContentOverrideStatus? status = null;

        if (!string.IsNullOrWhiteSpace(overrideStatus)
            && Enum.TryParse<ManagedContentOverrideStatus>(overrideStatus, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var listing = await _listService.ListAsync(
            scope.ManagedSite.Id,
            new ManagedContentListQuery(contentType, status, page, pageSize));

        return View(new ManagedSitePortalListViewModel
        {
            ManagedSite = scope.ManagedSite,
            Items = listing.Items,
            TotalCount = listing.TotalCount,
            Page = page,
            PageSize = pageSize,
            ContentType = contentType,
            OverrideStatus = overrideStatus,
        });
    }

    /// <summary>
    /// Offers the Managed Sites the editor may work in.
    /// </summary>
    /// <returns>The chooser, or a refusal when they are cleared for none.</returns>
    public async Task<IActionResult> Select()
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditManagedSiteContent))
        {
            return Forbid();
        }

        var session = await _sessionService.ResolveAsync(User);

        if (session.Status == ManagedSiteSessionStatus.NoClearance)
        {
            return View("NoClearance");
        }

        return View(new ManagedSitePortalSelectViewModel
        {
            ManagedSites = session.AuthorizedManagedSites,
            SelectedManagedSiteId = session.Scope?.ManagedSiteId,
        });
    }

    /// <summary>
    /// Takes the Managed Site the editor chose as the one this session works in.
    /// </summary>
    /// <param name="managedSiteId">The chosen Managed Site.</param>
    /// <returns>The content list, or the chooser again when the choice was refused.</returns>
    [HttpPost]
    [ActionName(nameof(Select))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPost(string managedSiteId)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditManagedSiteContent))
        {
            return Forbid();
        }

        var session = await _sessionService.SelectAsync(User, managedSiteId);

        if (session.Status != ManagedSiteSessionStatus.Selected)
        {
            await _notifier.ErrorAsync(H["You do not hold clearance for that managed site."]);

            return RedirectToAction(nameof(Select));
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows one item, and what the active Managed Site has done with it.
    /// </summary>
    /// <param name="sourceContentItemId">The item being customized.</param>
    /// <returns>The item, or a refusal.</returns>
    public async Task<IActionResult> Detail(string sourceContentItemId)
    {
        var scope = await ResolveScopeAsync();

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var location = await _locator.FindAsync(sourceContentItemId, options: VersionOptions.Published);
        var source = location?.ContentItem;

        // Checked here and not trusted from the listing, because the scope can be narrowed between an
        // editor seeing an item and opening it.
        if (source is null
            || !source.TryGet<ManagedContentPart>(out var part)
            || !_scopeService.CanEdit(part, scope.ManagedSite.Id))
        {
            return NotFound();
        }

        return View(new ManagedSitePortalDetailViewModel
        {
            ManagedSite = scope.ManagedSite,
            SourceContentItemId = sourceContentItemId,
            ContentType = source.ContentType,
            DisplayText = source.DisplayText ?? source.ContentType,
            DisplayScopeIncludesManagedSite = _scopeService.CanDisplay(part, scope.ManagedSite.Id),
            Override = ManagedContentOverrideSummary.Of(
                await _overrideService.GetAsync(scope.ManagedSite.Id, sourceContentItemId)),
        });
    }

    /// <summary>
    /// Creates the Managed Site's own version of an item, starting from the Site Blueprint's content.
    /// </summary>
    /// <remarks>
    /// Creating it here rather than through the platform's own content screens is what lets an editor
    /// author an override with their Managed Site clearance alone. Asking them to create the item
    /// themselves first would require tenant-wide permission over its content type, which FR-011
    /// forbids.
    /// </remarks>
    /// <param name="sourceContentItemId">The item being customized.</param>
    /// <returns>The item screen, carrying what happened.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string sourceContentItemId)
    {
        var scope = await ResolveScopeAsync(ManagedSitesConstants.Scopes.Edit);

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var result = await _overrideService.CreateAsync(scope.ManagedSite.Id, sourceContentItemId);

        await ReportAsync(result.Error, S["Your version was created as a draft, starting from the blueprint content."]);

        return RedirectToAction(nameof(Detail), new { sourceContentItemId });
    }

    /// <summary>
    /// Publishes the Managed Site's own version of an item, so that visitors receive it.
    /// </summary>
    /// <param name="sourceContentItemId">The item being customized.</param>
    /// <param name="overrideContentItemId">The content item holding the Managed Site's version.</param>
    /// <returns>The item screen, carrying what happened.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(string sourceContentItemId, string overrideContentItemId)
    {
        var scope = await ResolveScopeAsync(ManagedSitesConstants.Scopes.Publish);

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var result = await _overrideService.SaveAsync(
            scope.ManagedSite.Id,
            sourceContentItemId,
            overrideContentItemId,
            publish: true);

        await ReportAsync(result.Error, S["Your version is now being served on this managed site."]);

        return RedirectToAction(nameof(Detail), new { sourceContentItemId });
    }

    /// <summary>
    /// Withdraws the Managed Site's own version, restoring the Site Blueprint's content for it.
    /// </summary>
    /// <param name="sourceContentItemId">The item being customized.</param>
    /// <returns>The item screen, carrying what happened.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string sourceContentItemId)
    {
        var scope = await ResolveScopeAsync(ManagedSitesConstants.Scopes.Edit);

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        if (await _overrideService.DeleteAsync(scope.ManagedSite.Id, sourceContentItemId))
        {
            await _notifier.SuccessAsync(H["Your version was removed. This managed site is served the blueprint content again."]);
        }
        else
        {
            await _notifier.WarningAsync(H["There was no version of your own to remove."]);
        }

        return RedirectToAction(nameof(Detail), new { sourceContentItemId });
    }

    /// <summary>
    /// Lists the Managed Site's versions that exist but are not being served.
    /// </summary>
    /// <returns>The list, each entry carrying why it stopped rendering.</returns>
    public async Task<IActionResult> Suppressed()
    {
        var scope = await ResolveScopeAsync();

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var suppressed = await _overrideService.ListSuppressedAsync(scope.ManagedSite.Id);

        return View(new ManagedSitePortalSuppressedViewModel
        {
            ManagedSite = scope.ManagedSite,
            Items = [.. suppressed],
        });
    }

    /// <summary>
    /// Builds a link that opens a path at the Managed Site's own address.
    /// </summary>
    /// <remarks>
    /// The link goes to the Managed Site's address rather than to a renderer of its own, so what it
    /// shows is composed by the pipeline that serves the site and cannot drift from what visitors get.
    /// Asking for unpublished work is not being granted it: that is decided when the link is opened,
    /// from the preview clearance of whoever opens it.
    /// </remarks>
    /// <param name="path">The path to open.</param>
    /// <param name="includeDrafts">Whether to ask for unpublished work.</param>
    /// <returns>The preview screen.</returns>
    public async Task<IActionResult> Preview(string path = null, bool includeDrafts = false)
    {
        var scope = await ResolveScopeAsync();

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var preview = path is null
            ? null
            : await _previewService.CreateAsync(scope.ManagedSite.Id, path, includeDrafts);

        return View(new ManagedSitePortalPreviewViewModel
        {
            ManagedSite = scope.ManagedSite,
            Path = path,
            IncludeDrafts = includeDrafts,
            PreviewUrl = preview?.PreviewUrl,
        });
    }

    /// <summary>
    /// Works out which Managed Site this request acts on, and whether the editor may act on it.
    /// </summary>
    /// <remarks>
    /// Resolved once, here, rather than in each action, so that the refusals are the same wherever an
    /// editor arrives: no permission, no clearance at all, a choice not yet made, or clearance that
    /// does not reach what this action does.
    /// </remarks>
    /// <param name="requiredScope">What the action needs clearance to do, beyond seeing the site.</param>
    /// <returns>The Managed Site, or the result to return instead.</returns>
    private async Task<(ManagedSite ManagedSite, IActionResult Failure)> ResolveScopeAsync(
        string requiredScope = ManagedSitesConstants.Scopes.View)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditManagedSiteContent))
        {
            return (null, Forbid());
        }

        var session = await _sessionService.ResolveAsync(User);

        if (session.Status == ManagedSiteSessionStatus.NoClearance)
        {
            return (null, View("NoClearance"));
        }

        if (session.Status != ManagedSiteSessionStatus.Selected)
        {
            // Cleared for several and not yet chosen. Nothing scoped is shown until they do.
            return (null, RedirectToAction(nameof(Select)));
        }

        var managedSiteId = session.Scope.ManagedSiteId;

        if (!await _clearanceService.HasClearanceAsync(User, managedSiteId, requiredScope))
        {
            return (null, Forbid());
        }

        var managedSite = session.AuthorizedManagedSites
            .FirstOrDefault(candidate => string.Equals(candidate.Id, managedSiteId, StringComparison.Ordinal));

        return managedSite is null ? (null, RedirectToAction(nameof(Select))) : (managedSite, null);
    }

    private async Task ReportAsync(ManagedContentOverrideError error, LocalizedString success)
    {
        if (error == ManagedContentOverrideError.None)
        {
            await _notifier.SuccessAsync(H[success.Value]);

            return;
        }

        await _notifier.ErrorAsync(H[Describe(error).Value]);
    }

    private LocalizedString Describe(ManagedContentOverrideError error) => error switch
    {
        ManagedContentOverrideError.ManagedSiteUnavailable =>
            S["That managed site is not available."],
        ManagedContentOverrideError.SourceNotFound =>
            S["The item you are customizing no longer exists."],
        ManagedContentOverrideError.SourceNotManagedContent =>
            S["That item is no longer open to managed sites."],
        ManagedContentOverrideError.EditScopeExcluded =>
            S["This managed site is no longer allowed to customize that item."],
        ManagedContentOverrideError.OverrideNotFound =>
            S["There is no version of your own to publish."],
        ManagedContentOverrideError.ContentTypeMismatch =>
            S["Your version is not the same kind of content as the item it replaces."],
        ManagedContentOverrideError.OverrideAlreadyExists =>
            S["You already have a version of this item."],
        _ => S["That could not be done."],
    };
}
