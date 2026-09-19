namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Describes how widely a Managed Content scope reaches.
/// </summary>
public enum ManagedContentScopeMode
{
    /// <summary>
    /// The scope covers no Managed Site.
    /// </summary>
    None,

    /// <summary>
    /// The scope covers only the Managed Sites it names.
    /// </summary>
    Selected,

    /// <summary>
    /// The scope covers every Managed Site.
    /// </summary>
    All,
}

/// <summary>
/// A set of Managed Sites, expressed either as a mode or as an explicit list.
/// </summary>
/// <remarks>
/// Used for both scopes a Managed Content item carries. They stay separate types of question: the edit
/// scope answers who may override the item, the display scope answers who renders it.
/// </remarks>
public sealed class ManagedContentScope
{
    /// <summary>
    /// Gets or sets how widely the scope reaches.
    /// </summary>
    public ManagedContentScopeMode Mode { get; set; }

    /// <summary>
    /// Gets or sets the Managed Sites the scope names when its mode is
    /// <see cref="ManagedContentScopeMode.Selected" />.
    /// </summary>
    public List<string> ManagedSiteIds { get; set; } = [];

    /// <summary>
    /// Determines whether the scope covers a Managed Site.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <returns><see langword="true" /> when the scope covers the Managed Site.</returns>
    public bool Includes(string managedSiteId)
    {
        if (string.IsNullOrEmpty(managedSiteId))
        {
            return false;
        }

        return Mode switch
        {
            ManagedContentScopeMode.All => true,
            ManagedContentScopeMode.Selected => ManagedSiteIds.Contains(managedSiteId, StringComparer.Ordinal),
            _ => false,
        };
    }

    /// <summary>
    /// Creates a scope covering every Managed Site.
    /// </summary>
    /// <returns>The scope.</returns>
    public static ManagedContentScope All() => new() { Mode = ManagedContentScopeMode.All };

    /// <summary>
    /// Creates a scope covering no Managed Site.
    /// </summary>
    /// <returns>The scope.</returns>
    public static ManagedContentScope None() => new() { Mode = ManagedContentScopeMode.None };

    /// <summary>
    /// Creates a scope covering the named Managed Sites.
    /// </summary>
    /// <param name="managedSiteIds">The Managed Sites the scope names.</param>
    /// <returns>The scope.</returns>
    public static ManagedContentScope Selected(params string[] managedSiteIds) => new()
    {
        Mode = ManagedContentScopeMode.Selected,
        ManagedSiteIds = [.. managedSiteIds],
    };
}
