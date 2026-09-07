namespace VendallionCMS.ManagedSites.Models;

/// <summary>
/// Represents the Managed Site selected for the current portal session.
/// </summary>
public sealed class ActiveManagedSiteSessionScope
{
    /// <summary>
    /// Gets or sets the user identity reference.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the selected Managed Site identifier.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets when the Managed Site was selected.
    /// </summary>
    public DateTimeOffset SelectedAt { get; set; }
}
