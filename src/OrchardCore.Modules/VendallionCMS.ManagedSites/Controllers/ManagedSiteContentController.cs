using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.ContentManagement;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Navigation;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Controllers;

/// <summary>
/// One Managed Site's content: what it may customize, and what it has done with it.
/// </summary>
/// <remarks>
/// Every action answers for one Managed Site, and the Managed Site is in the address. An editor can see
/// which site they are working on, link to it, bookmark it, and keep two open at once, none of which is
/// possible when the answer is remembered somewhere instead.
///
/// Naming it in the address decides nothing: the code is checked against the editor's clearance on every
/// action, and on the action itself rather than once at the start, because a blueprint administrator can
/// narrow an item's scope between an editor opening it and saving it.
/// </remarks>
[Admin("ManagedSites/Content/{managedSiteCode}/{action}/{sourceContentItemId?}", "ManagedSiteContent{action}")]
public sealed class ManagedSiteContentController : Controller
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IManagedSiteClearanceService _clearanceService;
    private readonly IManagedContentListService _listService;
    private readonly IManagedContentOverrideService _overrideService;
    private readonly IManagedContentLocator _locator;
    private readonly IManagedContentScopeService _scopeService;
    private readonly IManagedSitePreviewService _previewService;
    private readonly INotifier _notifier;
    private readonly IShapeFactory _shapeFactory;
    private readonly PagerOptions _pagerOptions;

    private readonly IStringLocalizer S;
    private readonly IHtmlLocalizer H;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteContentController" /> class.
    /// </summary>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="clearanceService">The Managed Site clearance service.</param>
    /// <param name="listService">The Managed Content listing service.</param>
    /// <param name="overrideService">The Managed Content override service.</param>
    /// <param name="locator">The Managed Content locator.</param>
    /// <param name="scopeService">The Managed Content scope service.</param>
    /// <param name="previewService">The Managed Site preview service.</param>
    /// <param name="notifier">The notifier.</param>
    /// <param name="shapeFactory">The shape factory, used to build the pager.</param>
    /// <param name="pagerOptions">The site's pager options.</param>
    /// <param name="stringLocalizer">The string localizer.</param>
    /// <param name="htmlLocalizer">The HTML localizer.</param>
    public ManagedSiteContentController(
        IAuthorizationService authorizationService,
        IManagedSiteClearanceService clearanceService,
        IManagedContentListService listService,
        IManagedContentOverrideService overrideService,
        IManagedContentLocator locator,
        IManagedContentScopeService scopeService,
        IManagedSitePreviewService previewService,
        INotifier notifier,
        IShapeFactory shapeFactory,
        IOptions<PagerOptions> pagerOptions,
        IStringLocalizer<ManagedSiteContentController> stringLocalizer,
        IHtmlLocalizer<ManagedSiteContentController> htmlLocalizer)
    {
        _authorizationService = authorizationService;
        _clearanceService = clearanceService;
        _listService = listService;
        _overrideService = overrideService;
        _locator = locator;
        _scopeService = scopeService;
        _previewService = previewService;
        _notifier = notifier;
        _shapeFactory = shapeFactory;
        _pagerOptions = pagerOptions.Value;
        S = stringLocalizer;
        H = htmlLocalizer;
    }

    /// <summary>
    /// Lists what the active Managed Site may customize.
    /// </summary>
    /// <param name="contentType">One content type, or nothing for all of them.</param>
    /// <param name="overrideStatus">One override status, or nothing for all of them.</param>
    /// <param name="pagerParameters">Which page to show, and how large.</param>
    /// <returns>The list, the Managed Site chooser, or a refusal.</returns>
    [Admin("ManagedSites/Content/{managedSiteCode}", "ManagedSiteContentIndex")]
    public async Task<IActionResult> Index(
        string managedSiteCode,
        string contentType,
        string overrideStatus,
        PagerParameters pagerParameters)
    {
        var scope = await ResolveScopeAsync(managedSiteCode);

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

        var pager = new Pager(pagerParameters, _pagerOptions.GetPageSize());

        var listing = await _listService.ListAsync(
            scope.ManagedSite.Code,
            new ManagedContentListQuery(contentType, status, pager.Page, pager.PageSize));

        // Carried onto the page links, so paging past the first page does not quietly drop the filter
        // the editor is looking through.
        var routeData = new RouteData();

        if (!string.IsNullOrEmpty(contentType))
        {
            routeData.Values.TryAdd(nameof(contentType), contentType);
        }

        if (!string.IsNullOrEmpty(overrideStatus))
        {
            routeData.Values.TryAdd(nameof(overrideStatus), overrideStatus);
        }

        return View(new ManagedSiteContentListViewModel
        {
            ManagedSite = scope.ManagedSite,
            CanEdit = await CanAsync(scope.ManagedSite.Code, ManagedSitesConstants.Scopes.Edit),
            CanPublish = await CanAsync(scope.ManagedSite.Code, ManagedSitesConstants.Scopes.Publish),
            Items = listing.Items,
            TotalCount = listing.TotalCount,
            StartIndex = listing.TotalCount == 0 ? 0 : ((pager.Page - 1) * pager.PageSize) + 1,
            Pager = await _shapeFactory.PagerAsync(pager, listing.TotalCount, routeData),
            ContentType = contentType,
            OverrideStatus = overrideStatus,
        });
    }

    /// <summary>
    /// Offers the Managed Sites the editor may work in.
    /// </summary>
    /// <remarks>
    /// Reached at the root of this area, where no Managed Site is named. An editor cleared for exactly
    /// one is sent straight to it: asking somebody to choose from a list of one is asking them to
    /// confirm something they did not decide.
    /// </remarks>
    /// <returns>The chooser, the one site they hold, or a refusal.</returns>
    [Admin("ManagedSites/Content", "ManagedSiteContentSites")]
    public async Task<IActionResult> Sites()
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditManagedSiteContent))
        {
            return Forbid();
        }

        var managedSites = await _clearanceService.GetAuthorizedManagedSitesAsync(User);

        if (managedSites.Count == 0)
        {
            return View("NoClearance");
        }

        if (managedSites.Count == 1)
        {
            return RedirectToAction(nameof(Index), new { managedSiteCode = managedSites[0].Code });
        }

        return View(new ManagedSiteContentSitesViewModel { ManagedSites = managedSites });
    }

    /// <summary>
    /// Lists what the active Managed Site may customize inside one item.
    /// </summary>
    /// <remarks>
    /// Reached from the container's row rather than from the main list, which holds only items stored
    /// in their own right. A section is read as part of its page, so it is listed where its page is
    /// rather than beside it.
    /// </remarks>
    /// <param name="sourceContentItemId">The item whose contents to list.</param>
    /// <param name="contentType">One content type, or nothing for all of them.</param>
    /// <param name="overrideStatus">One override status, or nothing for all of them.</param>
    /// <param name="pagerParameters">Which page to show, and how large.</param>
    /// <returns>The list, or a refusal.</returns>
    public async Task<IActionResult> Contained(
        string managedSiteCode,
        string sourceContentItemId,
        string contentType,
        string overrideStatus,
        PagerParameters pagerParameters)
    {
        var scope = await ResolveScopeAsync(managedSiteCode);

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var location = await _locator.FindAsync(sourceContentItemId, options: VersionOptions.Published);
        var container = location?.ContentItem;

        // Checked here rather than trusted from the row that linked here, because the scope can be
        // narrowed between an editor seeing a container and opening it.
        if (container is null
            || !container.TryGet<ManagedContentPart>(out var part)
            || !_scopeService.CanEdit(part, scope.ManagedSite.Code))
        {
            return NotFound();
        }

        ManagedContentOverrideStatus? status = null;

        if (!string.IsNullOrWhiteSpace(overrideStatus)
            && Enum.TryParse<ManagedContentOverrideStatus>(overrideStatus, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var pager = new Pager(pagerParameters, _pagerOptions.GetPageSize());

        var listing = await _listService.ListContainedAsync(
            scope.ManagedSite.Code,
            sourceContentItemId,
            new ManagedContentListQuery(contentType, status, pager.Page, pager.PageSize));

        var routeData = new RouteData();
        routeData.Values.TryAdd(nameof(sourceContentItemId), sourceContentItemId);

        if (!string.IsNullOrEmpty(contentType))
        {
            routeData.Values.TryAdd(nameof(contentType), contentType);
        }

        if (!string.IsNullOrEmpty(overrideStatus))
        {
            routeData.Values.TryAdd(nameof(overrideStatus), overrideStatus);
        }

        var containerOverride = await _overrideService.GetAsync(scope.ManagedSite.Code, sourceContentItemId);

        return View(new ManagedSiteContentContainedViewModel
        {
            ManagedSite = scope.ManagedSite,
            CanEdit = await CanAsync(scope.ManagedSite.Code, ManagedSitesConstants.Scopes.Edit),
            CanPublish = await CanAsync(scope.ManagedSite.Code, ManagedSitesConstants.Scopes.Publish),
            SourceContentItemId = sourceContentItemId,
            ContainerDisplayText = container.DisplayText ?? container.ContentType,
            ContainerIsOverridden = containerOverride is not null,
            Items = listing.Items,
            TotalCount = listing.TotalCount,
            StartIndex = listing.TotalCount == 0 ? 0 : ((pager.Page - 1) * pager.PageSize) + 1,
            Pager = await _shapeFactory.PagerAsync(pager, listing.TotalCount, routeData),
            ContentType = contentType,
            OverrideStatus = overrideStatus,
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
    public async Task<IActionResult> Create(string managedSiteCode, string sourceContentItemId, string returnUrl = null)
    {
        var scope = await ResolveScopeAsync(managedSiteCode, ManagedSitesConstants.Scopes.Edit);

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var result = await _overrideService.CreateAsync(scope.ManagedSite.Code, sourceContentItemId);

        await ReportAsync(result.Error, S["Your version was created as a draft, starting from the blueprint content."]);

        var back = Back(managedSiteCode, returnUrl);

        if (!result.Succeeded)
        {
            return Redirect(back);
        }

        // Straight into the editor. Creating a version is the beginning of changing it, and the copy an
        // editor is handed is identical to the blueprint content until they do, so a screen between the
        // two only asks them to say they meant it twice. Nothing is served until they publish it.
        return RedirectToAction("Edit", "Admin", new
        {
            area = "OrchardCore.Contents",
            contentItemId = result.Override.OverrideContentItemId,
            returnUrl = back,
        });
    }

    /// <summary>
    /// Answers whether the editor holds a clearance for the active Managed Site.
    /// </summary>
    /// <param name="managedSiteCode">The active Managed Site.</param>
    /// <param name="scope">What the clearance would allow.</param>
    /// <returns><see langword="true" /> when they hold it.</returns>
    private ValueTask<bool> CanAsync(string managedSiteCode, string scope)
        => _clearanceService.HasClearanceAsync(User, managedSiteCode, scope);

    /// <summary>
    /// Works out where an action should hand the editor back to.
    /// </summary>
    /// <remarks>
    /// A caller says where it came from, so creating a version from the list returns to the list and
    /// creating one from an item returns to the item. Only local URLs are honoured: the value arrives
    /// in the query string, where anyone can put anything, and an unchecked one would bounce the editor
    /// to another site.
    /// </remarks>
    /// <param name="managedSiteCode">The Managed Site being worked on.</param>
    /// <param name="returnUrl">Where the caller came from.</param>
    /// <returns>A local URL.</returns>
    private string Back(string managedSiteCode, string returnUrl)
        => Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Index), new { managedSiteCode });

    /// <summary>
    /// Publishes the Managed Site's own version of an item, so that visitors receive it.
    /// </summary>
    /// <param name="sourceContentItemId">The item being customized.</param>
    /// <param name="overrideContentItemId">The content item holding the Managed Site's version.</param>
    /// <returns>The item screen, carrying what happened.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(
        string managedSiteCode,
        string sourceContentItemId,
        string overrideContentItemId,
        string returnUrl = null)
    {
        var scope = await ResolveScopeAsync(managedSiteCode, ManagedSitesConstants.Scopes.Publish);

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var result = await _overrideService.SaveAsync(
            scope.ManagedSite.Code,
            sourceContentItemId,
            overrideContentItemId,
            publish: true);

        await ReportAsync(result.Error, S["Your version is now being served on this managed site."]);

        return Redirect(Back(managedSiteCode, returnUrl));
    }

    /// <summary>
    /// Withdraws the Managed Site's own version, restoring the Site Blueprint's content for it.
    /// </summary>
    /// <param name="sourceContentItemId">The item being customized.</param>
    /// <returns>The item screen, carrying what happened.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string managedSiteCode, string sourceContentItemId, string returnUrl = null)
    {
        var scope = await ResolveScopeAsync(managedSiteCode, ManagedSitesConstants.Scopes.Edit);

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        if (await _overrideService.DeleteAsync(scope.ManagedSite.Code, sourceContentItemId))
        {
            await _notifier.SuccessAsync(H["Your version was removed. This managed site is served the blueprint content again."]);
        }
        else
        {
            await _notifier.WarningAsync(H["There was no version of your own to remove."]);
        }

        return Redirect(Back(managedSiteCode, returnUrl));
    }

    /// <summary>
    /// Lists the Managed Site's versions that exist but are not being served.
    /// </summary>
    /// <returns>The list, each entry carrying why it stopped rendering.</returns>
    public async Task<IActionResult> Suppressed(string managedSiteCode)
    {
        var scope = await ResolveScopeAsync(managedSiteCode);

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var suppressed = await _overrideService.ListSuppressedAsync(scope.ManagedSite.Code);

        return View(new ManagedSiteContentSuppressedViewModel
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
    public async Task<IActionResult> Preview(string managedSiteCode, string path = null, bool includeDrafts = false)
    {
        var scope = await ResolveScopeAsync(managedSiteCode);

        if (scope.Failure is not null)
        {
            return scope.Failure;
        }

        var preview = path is null
            ? null
            : await _previewService.CreateAsync(
                scope.ManagedSite.Code,
                path,
                includeDrafts,
                Request.Host.Port);

        return View(new ManagedSiteContentPreviewViewModel
        {
            ManagedSite = scope.ManagedSite,
            Path = path,
            IncludeDrafts = includeDrafts,
            PreviewUrl = preview?.PreviewUrl,
            OpensAnotherHost = OpensAnotherHost(scope.ManagedSite),
        });
    }

    /// <summary>
    /// Determines whether a preview link would open a host the editor is not signed in on.
    /// </summary>
    /// <remarks>
    /// A sign-in reaches only the host that issued it. A Managed Site that names a different host is
    /// therefore previewed as nobody, and served published content however the link asked for drafts.
    /// </remarks>
    /// <param name="managedSite">The Managed Site being previewed.</param>
    /// <returns><see langword="true" /> when the link leaves this host.</returns>
    private bool OpensAnotherHost(ManagedSite managedSite)
    {
        var host = ManagedSiteAddressValidator.SplitHostnames(managedSite.Hostname).FirstOrDefault();

        return !string.IsNullOrEmpty(host)
            && !string.Equals(host, Request.Host.Host, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Works out whether the editor may act on the Managed Site the address names.
    /// </summary>
    /// <remarks>
    /// Resolved once, here, so the refusals are the same wherever an editor arrives: no permission, a
    /// Managed Site that does not exist, one they hold no clearance for, or clearance that does not
    /// reach what this action does.
    ///
    /// A Managed Site they hold no clearance for is refused rather than hidden, because they named it:
    /// there is nothing to hide, and a refusal says what happened.
    /// </remarks>
    /// <param name="managedSiteCode">The Managed Site the address names.</param>
    /// <param name="requiredScope">What the action needs beyond reading, or nothing when reading is all it does.</param>
    /// <returns>The Managed Site, or the result to return instead.</returns>
    private async Task<(ManagedSite ManagedSite, IActionResult Failure)> ResolveScopeAsync(
        string managedSiteCode,
        string requiredScope = null)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditManagedSiteContent))
        {
            return (null, Forbid());
        }

        if (string.IsNullOrEmpty(managedSiteCode))
        {
            return (null, RedirectToAction(nameof(Sites)));
        }

        if (!await _clearanceService.HasClearanceAsync(User, managedSiteCode, requiredScope))
        {
            return (null, Forbid());
        }

        // Read from the Managed Sites the clearance service vouches for rather than from the store, so
        // that a site switched off, or removed since the clearance was granted, is not worked in.
        var managedSite = (await _clearanceService.GetAuthorizedManagedSitesAsync(User))
            .FirstOrDefault(candidate => string.Equals(candidate.Code, managedSiteCode, StringComparison.Ordinal));

        return managedSite is null ? (null, NotFound()) : (managedSite, null);
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
