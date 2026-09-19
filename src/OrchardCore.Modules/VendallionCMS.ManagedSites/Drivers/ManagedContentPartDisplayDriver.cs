using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using VendallionCMS.ManagedSites.ViewModels;

namespace VendallionCMS.ManagedSites.Drivers;

/// <summary>
/// Edits the two scopes a Managed Content item carries.
/// </summary>
/// <remarks>
/// The editor is shown only to users holding Site Blueprint management access, and an update from
/// anyone else is discarded rather than partially applied. Scopes decide who may change common content,
/// so a Managed Site administrator must not be able to widen their own reach.
/// </remarks>
public sealed class ManagedContentPartDisplayDriver : ContentPartDisplayDriver<ManagedContentPart>
{
    private readonly IManagedSiteService _managedSiteService;
    private readonly IManagedContentScopeAuthorizationHandler _scopeAuthorization;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentPartDisplayDriver" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    /// <param name="scopeAuthorization">The scope configuration authorization handler.</param>
    public ManagedContentPartDisplayDriver(
        IManagedSiteService managedSiteService,
        IManagedContentScopeAuthorizationHandler scopeAuthorization)
    {
        _managedSiteService = managedSiteService;
        _scopeAuthorization = scopeAuthorization;
    }

    /// <inheritdoc />
    public override IDisplayResult Edit(ManagedContentPart part, BuildPartEditorContext context)
        => Initialize<ManagedContentPartViewModel>(GetEditorShapeType(context), async model =>
        {
            model.CanConfigure = await _scopeAuthorization.CanConfigureScopesAsync();
            model.EditScopeMode = part.EditScope?.Mode ?? ManagedContentScopeMode.None;
            model.DisplayScopeMode = part.DisplayScope?.Mode ?? ManagedContentScopeMode.All;
            model.DisplayInBlueprintContext = part.DisplayInBlueprintContext;

            var managedSites = await _managedSiteService.ListAsync();

            // Only a Selected scope names Managed Sites, so the rows stay empty for the other modes
            // rather than showing identifiers the scope no longer consults.
            var editIds = NamedManagedSites(part.EditScope);
            var displayIds = NamedManagedSites(part.DisplayScope);

            model.ManagedSites = managedSites
                .OrderBy(managedSite => managedSite.Name, StringComparer.OrdinalIgnoreCase)
                .Select(managedSite =>
                {
                    var canEdit = editIds.Contains(managedSite.Id, StringComparer.Ordinal);

                    return new ManagedContentScopeEntryViewModel
                    {
                        ManagedSiteId = managedSite.Id,
                        Name = managedSite.Name,
                        CanEdit = canEdit,

                        // A Managed Site that may override the item always renders it, so the row shows
                        // the state the form will save rather than one the update would overrule.
                        CanDisplay = canEdit || displayIds.Contains(managedSite.Id, StringComparer.Ordinal),
                    };
                })
                .ToArray();
        })
        .RenderWhen(static driver => driver._scopeAuthorization.CanConfigureScopesAsync(), this);

    /// <inheritdoc />
    public override async Task<IDisplayResult> UpdateAsync(ManagedContentPart part, UpdatePartEditorContext context)
    {
        if (!await _scopeAuthorization.CanConfigureScopesAsync())
        {
            return Edit(part, context);
        }

        var model = new ManagedContentPartViewModel();
        await context.Updater.TryUpdateModelAsync(model, Prefix);

        var known = (await _managedSiteService.ListAsync())
            .Select(managedSite => managedSite.Id)
            .ToHashSet(StringComparer.Ordinal);

        part.EditScope = BuildScope(
            model.EditScopeMode,
            model.ManagedSites?.Where(entry => entry.CanEdit),
            known);

        part.DisplayScope = BuildScope(
            model.DisplayScopeMode,
            model.ManagedSites?.Where(entry => entry.CanDisplay),
            known);

        part.DisplayInBlueprintContext = model.DisplayInBlueprintContext;

        // The editor disables a display control the edit scope already decides, so that control posts
        // nothing. Restoring the rule here is what makes the disabled state safe.
        part.EnsureDisplayCoversEdit();

        return Edit(part, context);
    }

    private static List<string> NamedManagedSites(ManagedContentScope scope)
        => scope?.Mode == ManagedContentScopeMode.Selected ? scope.ManagedSiteIds : [];

    private static ManagedContentScope BuildScope(
        ManagedContentScopeMode mode,
        IEnumerable<ManagedContentScopeEntryViewModel> selected,
        HashSet<string> knownManagedSiteIds)
    {
        if (mode != ManagedContentScopeMode.Selected)
        {
            return new ManagedContentScope { Mode = mode };
        }

        // A Managed Site that no longer exists is dropped rather than stored, so a stale identifier
        // cannot silently come back into scope if that identifier is reused later.
        var managedSiteIds = (selected ?? [])
            .Select(entry => entry.ManagedSiteId)
            .Where(managedSiteId => !string.IsNullOrEmpty(managedSiteId) && knownManagedSiteIds.Contains(managedSiteId))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new ManagedContentScope
        {
            Mode = ManagedContentScopeMode.Selected,
            ManagedSiteIds = managedSiteIds,
        };
    }
}
