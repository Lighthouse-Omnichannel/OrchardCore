using OrchardCore.ContentManagement;

namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Marks a content item as one Managed Site's replacement for a Managed Content item.
/// </summary>
/// <remarks>
/// An override is a content item of the same type as the item it replaces, carrying this part to say
/// who owns it and what it stands in for. Storing it as a content item rather than as a record inside
/// the source item is what gives each Managed Site its own draft and publish lifecycle for free.
/// </remarks>
public class ManagedContentOverridePart : ContentPart
{
    /// <summary>
    /// Gets or sets the Managed Site that owns this override.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the Managed Content item this override replaces.
    /// </summary>
    public string SourceContentItemId { get; set; }
}

/// <summary>
/// Describes where an override sits in its lifecycle.
/// </summary>
public enum ManagedContentOverrideStatus
{
    /// <summary>
    /// The Managed Site has not created an override for the item.
    /// </summary>
    None,

    /// <summary>
    /// An override exists but has never been published, so rendered output is unchanged.
    /// </summary>
    Draft,

    /// <summary>
    /// A published override that renders in place of the original content.
    /// </summary>
    Published,

    /// <summary>
    /// An override that exists but does not render, and stays available for review and recovery.
    /// </summary>
    Suppressed,
}

/// <summary>
/// Explains why an override that exists does not render.
/// </summary>
/// <remarks>
/// Recorded so an administrator can tell apart a scope they can ask to have restored, a source item
/// that went away, and a capability that was detached from the content type.
/// </remarks>
public enum ManagedContentOverrideSuppressionReason
{
    /// <summary>
    /// The override is not suppressed.
    /// </summary>
    None,

    /// <summary>
    /// The owning Managed Site is no longer in the source item's edit scope.
    /// </summary>
    EditScopeRemoved,

    /// <summary>
    /// The source content item is no longer published.
    /// </summary>
    SourceUnpublished,

    /// <summary>
    /// The source content item no longer exists.
    /// </summary>
    SourceDeleted,

    /// <summary>
    /// Managed Content was detached from the source content type.
    /// </summary>
    CapabilityDetached,

    /// <summary>
    /// The owning Managed Site is disabled or archived.
    /// </summary>
    ManagedSiteDisabled,
}

/// <summary>
/// One Managed Site's override of one Managed Content item, as administrators and the portal see it.
/// </summary>
/// <remarks>
/// A read model rather than a stored record. <see cref="Status" /> and <see cref="SuppressionReason" />
/// are derived when the override is read, because the conditions that suppress an override change
/// without the override itself being touched: removing a Managed Site from an edit scope must take
/// effect immediately, not the next time somebody saves the override.
/// </remarks>
public sealed class ManagedContentOverride
{
    /// <summary>
    /// Gets or sets the Managed Site that owns the override.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets the Managed Content item the override replaces.
    /// </summary>
    public string SourceContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the content item holding the override content.
    /// </summary>
    public string OverrideContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the content type shared by the override and its source.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the override lifecycle status.
    /// </summary>
    public ManagedContentOverrideStatus Status { get; set; }

    /// <summary>
    /// Gets or sets why the override does not render, when it is suppressed.
    /// </summary>
    public ManagedContentOverrideSuppressionReason SuppressionReason { get; set; }

    /// <summary>
    /// Gets a value indicating whether this override is what renders for its Managed Site.
    /// </summary>
    public bool Renders => Status == ManagedContentOverrideStatus.Published;
}
