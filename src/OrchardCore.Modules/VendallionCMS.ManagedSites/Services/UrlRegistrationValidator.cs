using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Validates Managed Site URL registrations.
/// </summary>
public sealed class UrlRegistrationValidator
{
    /// <summary>
    /// Normalizes a URL registration value for comparison.
    /// </summary>
    /// <param name="url">The URL to normalize.</param>
    /// <returns>The normalized URL.</returns>
    public static string Normalize(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var normalized = url.Trim().ToLowerInvariant();

        if (!normalized.StartsWith('/'))
        {
            normalized = '/' + normalized;
        }

        return normalized.TrimEnd('/');
    }

    /// <summary>
    /// Validates that the registration has no active URL conflict.
    /// </summary>
    /// <param name="registration">The registration to validate.</param>
    /// <param name="existingRegistrations">Existing registrations to compare against.</param>
    /// <exception cref="InvalidOperationException">Thrown when a conflict is detected.</exception>
    public static void ValidateNoConflict(UrlRegistration registration, IEnumerable<UrlRegistration> existingRegistrations)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var normalizedUrl = Normalize(registration.Url);

        if (string.IsNullOrEmpty(normalizedUrl))
        {
            throw new InvalidOperationException("URL registration must include a URL.");
        }

        var hasConflict = existingRegistrations.Any(existing =>
            existing.Status == UrlRegistrationStatus.Active &&
            existing.Id != registration.Id &&
            Normalize(existing.Url) == normalizedUrl);

        if (hasConflict)
        {
            throw new InvalidOperationException($"The URL '{registration.Url}' is already assigned to another active registration.");
        }

        registration.Url = normalizedUrl;
    }
}
