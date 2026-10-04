using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Places <see cref="ManagedSiteRequestMiddleware" /> ahead of routing in the tenant pipeline.
/// </summary>
/// <remarks>
/// A module's <c>Configure</c> runs after OrchardCore has already called <c>UseRouting</c>, which is
/// where an endpoint is chosen. Middleware added there can still read the request, but moving a Managed
/// Site's URL prefix onto the path base at that point changes nothing: the endpoint was selected from
/// the original path, so a Managed Site answering under a prefix matched and then served a 404, because
/// no content is routed under the prefix.
///
/// A startup filter is added to the pipeline before <c>UseRouting</c>, which is the same position from
/// which OrchardCore rebases a tenant's own URL prefix, in <c>ModularTenantRouterMiddleware</c>. Running
/// here means the request reaches routing already rebased, so the content underneath resolves unchanged.
/// </remarks>
public sealed class ManagedSiteRequestStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            app.UseMiddleware<ManagedSiteRequestMiddleware>();

            next(app);
        };
}
