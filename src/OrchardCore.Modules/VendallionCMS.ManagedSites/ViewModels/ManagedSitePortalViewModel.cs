namespace VendallionCMS.ManagedSites.ViewModels;

/// <summary>
/// Bootstrap data handed to the Managed Site Admin Portal client application.
/// </summary>
public sealed class ManagedSitePortalViewModel
{
    /// <summary>
    /// Gets or sets the base URL of the Managed Sites API.
    /// </summary>
    public string ApiBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the antiforgery request token.
    /// </summary>
    /// <remarks>
    /// The portal API accepts the admin cookie, so state-changing calls made from an admin session must
    /// carry this token. Bearer-token callers are not cookie-authenticated and are not subject to it.
    /// </remarks>
    public string AntiforgeryToken { get; set; }
}
