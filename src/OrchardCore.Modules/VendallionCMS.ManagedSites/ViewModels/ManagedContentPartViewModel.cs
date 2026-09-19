using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// Editor model for the two scopes a Managed Content item carries.
/// </summary>
public class ManagedContentPartViewModel
{
    /// <summary>
    /// Gets or sets how widely the edit scope reaches.
    /// </summary>
    public ManagedContentScopeMode EditScopeMode { get; set; }

    /// <summary>
    /// Gets or sets how widely the display scope reaches.
    /// </summary>
    public ManagedContentScopeMode DisplayScopeMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item renders when no Managed Site is resolved.
    /// </summary>
    public bool DisplayInBlueprintContext { get; set; }

    /// <summary>
    /// Gets or sets the Managed Sites offered for both scopes.
    /// </summary>
    public ManagedContentScopeEntryViewModel[] ManagedSites { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the current user may change the scopes.
    /// </summary>
    public bool CanConfigure { get; set; }

    /// <summary>
    /// Gets a value indicating whether any Managed Site has been defined yet.
    /// </summary>
    public bool HasManagedSites => ManagedSites.Length > 0;
}

/// <summary>
/// One Managed Site row in the scope editor.
/// </summary>
public sealed class ManagedContentScopeEntryViewModel
{
    /// <summary>
    /// Gets or sets the Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this Managed Site may override the item.
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this Managed Site renders the item.
    /// </summary>
    public bool CanDisplay { get; set; }
}
