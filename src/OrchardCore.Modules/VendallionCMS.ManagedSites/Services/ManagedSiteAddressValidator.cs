using OrchardCore.Environment.Shell;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// One address a Managed Site answers on: a host name paired with a URL prefix.
/// </summary>
/// <param name="Host">The normalized host name, empty when the Managed Site answers on every host.</param>
/// <param name="Prefix">The normalized URL prefix, empty for the root.</param>
public readonly record struct ManagedSiteAddress(string Host, string Prefix);

/// <summary>
/// Normalizes and compares Managed Site addresses.
/// </summary>
/// <remarks>
/// A Managed Site is addressed like a tenant, so it expands to one address per host name, exactly as a
/// tenant expands its request hosts. An empty host name answers on every host and therefore overlaps
/// every other address sharing its prefix.
/// </remarks>
public static class ManagedSiteAddressValidator
{
    private static readonly string[] s_schemePrefixes = ["https://", "http://", "//"];

    /// <summary>
    /// Normalizes a URL prefix for comparison.
    /// </summary>
    /// <param name="prefix">The prefix to normalize.</param>
    /// <returns>The normalized prefix without leading or trailing slashes, empty for the root.</returns>
    public static string NormalizePrefix(string prefix)
        => string.IsNullOrWhiteSpace(prefix)
            ? string.Empty
            : prefix.Trim().Trim('/').ToLowerInvariant();

    /// <summary>
    /// Normalizes a host name for comparison, dropping any scheme or path that was pasted with it.
    /// </summary>
    /// <param name="host">The host name to normalize.</param>
    /// <returns>The normalized host name, or an empty string when none was supplied.</returns>
    public static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var value = host.Trim();

        foreach (var scheme in s_schemePrefixes)
        {
            if (value.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                value = value[scheme.Length..];
                break;
            }
        }

        var separatorIndex = value.IndexOf('/', StringComparison.Ordinal);
        if (separatorIndex >= 0)
        {
            value = value[..separatorIndex];
        }

        return value.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Splits a hostname value into its individual host names.
    /// </summary>
    /// <remarks>
    /// Uses the same separators a tenant uses for its request hosts, so a value can be moved between
    /// the two without reformatting.
    /// </remarks>
    /// <param name="hostname">The hostname value.</param>
    /// <returns>The normalized host names in order, without duplicates.</returns>
    public static string[] SplitHostnames(string hostname)
        => (hostname ?? string.Empty)
            .Split(ShellSettings.HostSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeHost)
            .Where(host => !string.IsNullOrEmpty(host))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// Formats host names back into a single hostname value.
    /// </summary>
    /// <param name="hosts">The host names.</param>
    /// <returns>The joined hostname value.</returns>
    public static string JoinHostnames(IEnumerable<string> hosts) => string.Join(',', hosts);

    /// <summary>
    /// Expands a Managed Site into the addresses it answers on.
    /// </summary>
    /// <param name="managedSite">The Managed Site.</param>
    /// <returns>One address per host name, or a single host-agnostic address when it names none.</returns>
    public static ManagedSiteAddress[] Expand(ManagedSite managedSite)
    {
        ArgumentNullException.ThrowIfNull(managedSite);

        var hosts = SplitHostnames(managedSite.Hostname);

        // The prefix is dropped for a host-named Managed Site, because it claims every path on its
        // hosts and two such Managed Sites collide on the host alone however their prefixes differ.
        return hosts.Length == 0
            ? [new ManagedSiteAddress(string.Empty, NormalizePrefix(managedSite.UrlPrefix))]
            : [.. hosts.Select(host => new ManagedSiteAddress(host, string.Empty))];
    }

    /// <summary>
    /// Determines whether two addresses would answer the same request.
    /// </summary>
    /// <remarks>
    /// Prefixes must match exactly. Host names overlap when they are equal or when either side is empty,
    /// because an empty host name answers on every host and would otherwise make resolution ambiguous.
    /// </remarks>
    /// <param name="first">The first address.</param>
    /// <param name="second">The second address.</param>
    /// <returns><see langword="true" /> when the two addresses overlap.</returns>
    public static bool Overlaps(ManagedSiteAddress first, ManagedSiteAddress second)
    {
        var firstNamesAHost = !string.IsNullOrEmpty(first.Host);
        var secondNamesAHost = !string.IsNullOrEmpty(second.Host);

        // One claims a host outright, the other a prefix on every host. They do not collide: a request
        // to the named host goes to the Managed Site that named it, and every other host is left to the
        // prefix. Precedence settles this rather than validation forbidding it.
        if (firstNamesAHost != secondNamesAHost)
        {
            return false;
        }

        // Two host-named Managed Sites collide on a shared host whatever their prefixes say, because
        // neither prefix is consulted once a host matches.
        if (firstNamesAHost)
        {
            return string.Equals(first.Host, second.Host, StringComparison.Ordinal);
        }

        // Two host-agnostic Managed Sites collide only on the same prefix.
        return string.Equals(first.Prefix, second.Prefix, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether a Managed Site answers a request host and path.
    /// </summary>
    /// <param name="managedSite">The Managed Site.</param>
    /// <param name="host">The request host.</param>
    /// <param name="path">The request path.</param>
    /// <returns><see langword="true" /> when the Managed Site answers the request.</returns>
    public static bool Matches(ManagedSite managedSite, string host, string path)
    {
        ArgumentNullException.ThrowIfNull(managedSite);

        var hosts = SplitHostnames(managedSite.Hostname);

        // A Managed Site that names host names claims those hosts entirely, and its URL prefix decides
        // nothing. The prefix is how Managed Sites share one host, so it only has work to do for a
        // Managed Site that named no host of its own.
        if (hosts.Length > 0)
        {
            return hosts.Contains(NormalizeHost(host), StringComparer.Ordinal);
        }

        return PathStartsWithPrefix(path, NormalizePrefix(managedSite.UrlPrefix));
    }

    /// <summary>
    /// Finds the enabled Managed Site that answers a request, when more than one could.
    /// </summary>
    /// <remarks>
    /// Precedence lives here rather than in the store so that matching and choosing between matches are
    /// one implementation. A Managed Site naming the request host beats one answering on every host, and
    /// a longer prefix beats a shorter one, so the most specific claim wins.
    /// </remarks>
    /// <param name="managedSites">The Managed Sites to consider.</param>
    /// <param name="host">The request host.</param>
    /// <param name="path">The request path.</param>
    /// <returns>The matching Managed Site, or <see langword="null" /> when none answers.</returns>
    public static ManagedSite FindBestMatch(IEnumerable<ManagedSite> managedSites, string host, string path)
        => managedSites
            .Where(managedSite => managedSite.Status == ManagedSiteStatus.Enabled)
            .Where(managedSite => Matches(managedSite, host, path))
            .OrderByDescending(managedSite => SplitHostnames(managedSite.Hostname).Length > 0)
            .ThenByDescending(managedSite => AppliedPrefix(managedSite).Length)
            .FirstOrDefault();

    /// <summary>
    /// Gets the URL prefix a Managed Site actually answers under.
    /// </summary>
    /// <remarks>
    /// Empty for a Managed Site that names host names, because those claim every path on the host and
    /// the stored prefix is not consulted. Callers that strip the prefix from a request path ask this
    /// rather than reading the stored value, so nothing is stripped that was never matched.
    /// </remarks>
    /// <param name="managedSite">The Managed Site, or <see langword="null" />.</param>
    /// <returns>The prefix in force, or an empty string.</returns>
    public static string AppliedPrefix(ManagedSite managedSite)
        => managedSite is null || SplitHostnames(managedSite.Hostname).Length > 0
            ? string.Empty
            : NormalizePrefix(managedSite.UrlPrefix);

    /// <summary>
    /// Determines whether a request path falls under a prefix.
    /// </summary>
    /// <remarks>
    /// An empty prefix matches every path. A prefix matches its own root and any deeper segment, but
    /// never a path that merely starts with the same characters.
    /// </remarks>
    /// <param name="path">The request path.</param>
    /// <param name="prefix">The normalized prefix.</param>
    /// <returns><see langword="true" /> when the path falls under the prefix.</returns>
    public static bool PathStartsWithPrefix(string path, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return true;
        }

        var normalizedPath = (path ?? string.Empty).Trim().Trim('/').ToLowerInvariant();

        return normalizedPath.Equals(prefix, StringComparison.Ordinal)
            || normalizedPath.StartsWith(prefix + '/', StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates a Managed Site address against the addresses other Managed Sites already claim.
    /// </summary>
    /// <param name="managedSite">The Managed Site being saved, normalized in place.</param>
    /// <param name="others">The other Managed Sites to compare against.</param>
    /// <exception cref="ManagedSiteValidationException">Thrown when the address collides.</exception>
    public static void ValidateNoConflict(ManagedSite managedSite, IEnumerable<ManagedSite> others)
    {
        ArgumentNullException.ThrowIfNull(managedSite);

        managedSite.UrlPrefix = NormalizePrefix(managedSite.UrlPrefix);
        managedSite.Hostname = JoinHostnames(SplitHostnames(managedSite.Hostname));

        var addresses = Expand(managedSite);

        foreach (var other in others)
        {
            // A switched-off Managed Site releases its address, so another can take it over rather than
            // be blocked by one that answers nothing.
            if (other.Status != ManagedSiteStatus.Enabled)
            {
                continue;
            }

            foreach (var otherAddress in Expand(other))
            {
                if (addresses.Any(address => Overlaps(address, otherAddress)))
                {
                    throw new ManagedSiteValidationException(
                        ManagedSitesConstants.ErrorCodes.UrlConflict,
                        $"The address is already claimed by the managed site '{other.Name}'.");
                }
            }
        }
    }
}
