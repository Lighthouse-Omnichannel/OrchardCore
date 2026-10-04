using System.Text;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Where to go to see a Managed Site's composed output for one address.
/// </summary>
public sealed class ManagedSitePreview
{
    /// <summary>
    /// Gets the address to open, which resolves to the Managed Site and composes as a visitor's request does.
    /// </summary>
    public required string PreviewUrl { get; init; }

    /// <summary>
    /// Gets the Managed Site the address resolves to, or <see langword="null" /> when it resolves to none.
    /// </summary>
    public string ManagedSiteId { get; init; }

    /// <summary>
    /// Gets how the address composes, either as a Managed Site or as the Site Blueprint.
    /// </summary>
    public required string CompositionMode { get; init; }
}

/// <summary>
/// Builds the address that shows a Managed Site's composed output.
/// </summary>
public interface IManagedSitePreviewService
{
    /// <summary>
    /// Builds the preview address for a Managed Site and a path.
    /// </summary>
    /// <param name="managedSiteId">The Managed Site to preview.</param>
    /// <param name="url">The path to preview, relative to the site root.</param>
    /// <param name="includeDrafts">Whether the address should ask to show unpublished work.</param>
    /// <returns>The preview, or <see langword="null" /> when the Managed Site cannot be previewed.</returns>
    ValueTask<ManagedSitePreview> CreateAsync(string managedSiteId, string url, bool includeDrafts);
}

/// <summary>
/// Points preview at the Managed Site's own address rather than at a separate rendering path.
/// </summary>
/// <remarks>
/// FR-048 asks preview to simulate the request composition pipeline for the Managed Site's URL context.
/// The closest thing to a simulation of that pipeline is the pipeline: an address that resolves to the
/// Managed Site composes exactly as a visitor's request does, by the same middleware and the same
/// substitution, so a preview cannot drift from what is served.
///
/// Drafts are asked for in the address and granted by clearance when the request arrives. This service
/// only writes the question down; nothing here decides who may see unpublished work.
/// </remarks>
public sealed class ManagedSitePreviewService : IManagedSitePreviewService
{
    private readonly IManagedSiteService _managedSiteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSitePreviewService" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    public ManagedSitePreviewService(IManagedSiteService managedSiteService)
    {
        _managedSiteService = managedSiteService;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedSitePreview> CreateAsync(string managedSiteId, string url, bool includeDrafts)
    {
        var managedSite = await _managedSiteService.GetAsync(managedSiteId);

        if (managedSite is null || managedSite.Status != ManagedSiteStatus.Enabled)
        {
            // A Managed Site that answers no request has nothing to preview: the address would resolve
            // to the Site Blueprint and show content that is not the Managed Site's.
            return null;
        }

        var path = NormalizePath(url);
        var host = ManagedSiteAddressValidator.SplitHostnames(managedSite.Hostname).FirstOrDefault();
        var prefix = ManagedSiteAddressValidator.AppliedPrefix(managedSite);

        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(host))
        {
            builder.Append("//").Append(host);
        }

        if (!string.IsNullOrEmpty(prefix))
        {
            builder.Append('/').Append(prefix);
        }

        builder.Append(path);

        if (includeDrafts)
        {
            builder
                .Append(path.Contains('?', StringComparison.Ordinal) ? '&' : '?')
                .Append(ManagedSitesConstants.Preview.DraftsQueryKey)
                .Append("=1");
        }

        return new ManagedSitePreview
        {
            PreviewUrl = builder.ToString(),
            ManagedSiteId = managedSite.Id,
            CompositionMode = ManagedSitesConstants.Preview.ManagedSiteMode,
        };
    }

    private static string NormalizePath(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "/";
        }

        var path = url.Trim();

        // Only a path is accepted. An absolute address would let a caller point preview at another host
        // and have the answer look like this Managed Site's.
        var scheme = path.IndexOf("://", StringComparison.Ordinal);

        if (scheme >= 0)
        {
            var afterHost = path.IndexOf('/', scheme + 3);
            path = afterHost < 0 ? "/" : path[afterHost..];
        }

        return path.StartsWith('/') ? path : '/' + path;
    }
}
