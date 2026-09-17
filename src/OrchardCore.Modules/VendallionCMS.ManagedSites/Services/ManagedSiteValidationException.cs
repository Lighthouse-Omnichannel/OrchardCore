namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Raised when a Managed Site definition or its URL registrations fail validation.
/// </summary>
/// <remarks>
/// Derives from <see cref="InvalidOperationException" /> so existing callers that guard against invalid
/// definitions keep working, while carrying a stable code the API maps onto a status and problem body.
/// </remarks>
public sealed class ManagedSiteValidationException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteValidationException" /> class.
    /// </summary>
    /// <param name="code">The stable machine-readable error code.</param>
    /// <param name="message">The human-readable explanation.</param>
    public ManagedSiteValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Gets the stable machine-readable error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets a value indicating whether the failure is a conflict with existing state rather than
    /// malformed input.
    /// </summary>
    public bool IsConflict
        => Code == ManagedSitesConstants.ErrorCodes.UrlConflict
        || Code == ManagedSitesConstants.ErrorCodes.NameConflict;
}
