using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Resolves the Managed Site that answers a public request.
/// </summary>
/// <remarks>
/// The incoming URL is the only authority. A request carries no say in which Managed Site it belongs
/// to, so whatever a client sends in headers or query values is ignored here by construction: this
/// service is given a host and a path and nothing else.
///
/// Matching, and which Managed Site wins when several could answer, already belong to the Managed Site
/// service, which is also what validates that two Managed Sites never claim the same address. Resolving
/// a request asks that same question rather than a second implementation of it that could disagree.
/// </remarks>
public sealed class ManagedSiteUrlResolver : IManagedSiteUrlResolver
{
    private readonly IManagedSiteService _managedSiteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteUrlResolver" /> class.
    /// </summary>
    /// <param name="managedSiteService">The Managed Site service.</param>
    public ManagedSiteUrlResolver(IManagedSiteService managedSiteService)
    {
        _managedSiteService = managedSiteService;
    }

    /// <inheritdoc />
    public async ValueTask<ManagedSiteRequestContext> ResolveAsync(string host, string path)
    {
        var managedSite = await _managedSiteService.FindByAddressAsync(host, path);

        return new ManagedSiteRequestContext
        {
            // Null when nothing answers, which every consumer reads as the Site Blueprint context. A
            // request that matches no Managed Site is not an error; it is the tenant's own content.
            ManagedSiteId = managedSite?.Id,

            // Empty for a Managed Site that matched on its host name, whose prefix decides nothing and
            // so must not be stripped from the path either.
            UrlPrefix = ManagedSiteAddressValidator.AppliedPrefix(managedSite),
            Host = host,
            Path = path,
        };
    }
}
