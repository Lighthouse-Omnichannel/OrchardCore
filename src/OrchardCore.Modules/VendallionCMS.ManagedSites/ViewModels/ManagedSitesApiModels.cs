using System.Text.Json.Serialization;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// Consistent problem response returned by every Managed Sites API failure.
/// </summary>
public sealed class ManagedSitesApiProblem
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the short, human-readable summary.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the explanation specific to this occurrence.
    /// </summary>
    public string Detail { get; set; }

    /// <summary>
    /// Gets or sets the stable machine-readable error code.
    /// </summary>
    public string Code { get; set; }
}

/// <summary>
/// Describes a Managed Site exposed to the portal.
/// </summary>
public sealed class ManagedSiteSummary
{
    /// <summary>
    /// Gets or sets the Managed Site identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the lifecycle status.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets the host names the Managed Site answers on.
    /// </summary>
    public string Hostname { get; set; }

    /// <summary>
    /// Gets or sets the URL prefix the Managed Site answers under.
    /// </summary>
    public string UrlPrefix { get; set; }

    /// <summary>
    /// Creates a summary from a Managed Site definition.
    /// </summary>
    /// <param name="managedSite">The Managed Site.</param>
    /// <returns>The portal-facing summary.</returns>
    public static ManagedSiteSummary From(ManagedSite managedSite) => new()
    {
        Id = managedSite.Id,
        Name = managedSite.Name,
        Status = managedSite.Status.ToString(),
        Hostname = managedSite.Hostname,
        UrlPrefix = managedSite.UrlPrefix,
    };
}

/// <summary>
/// Response for the authorized Managed Sites listing.
/// </summary>
public sealed class AuthorizedManagedSitesResponse
{
    /// <summary>
    /// Gets the Managed Sites the caller may manage.
    /// </summary>
    public IList<ManagedSiteSummary> Items { get; init; } = [];
}

/// <summary>
/// Request body for selecting the active Managed Site.
/// </summary>
public sealed class SelectManagedSiteRequest
{
    /// <summary>
    /// Gets or sets the Managed Site to activate for the session.
    /// </summary>
    public string ManagedSiteId { get; set; }
}

/// <summary>
/// Response describing the active Managed Site session scope.
/// </summary>
public sealed class ManagedSiteSessionResponse
{
    /// <summary>
    /// Gets or sets the active Managed Site identifier, when one is selected.
    /// </summary>
    public string ManagedSiteId { get; set; }

    /// <summary>
    /// Gets or sets when the active Managed Site was selected.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? SelectedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the caller must choose a Managed Site before editing.
    /// </summary>
    public bool RequiresSelection { get; set; }

    /// <summary>
    /// Gets the Managed Sites the caller may choose from.
    /// </summary>
    public IList<ManagedSiteSummary> AuthorizedManagedSites { get; init; } = [];
}

/// <summary>
/// Request body for creating or replacing a Managed Site definition.
/// </summary>
public sealed class ManagedSiteDefinitionRequest
{
    /// <summary>
    /// Gets or sets the Managed Site display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the Managed Site lifecycle status.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets the host names the Managed Site answers on, separated the same way a tenant
    /// separates its request hosts. Empty answers on every host the tenant serves.
    /// </summary>
    public string Hostname { get; set; }

    /// <summary>
    /// Gets or sets the URL prefix the Managed Site answers under, empty for the root.
    /// </summary>
    public string UrlPrefix { get; set; }
}
