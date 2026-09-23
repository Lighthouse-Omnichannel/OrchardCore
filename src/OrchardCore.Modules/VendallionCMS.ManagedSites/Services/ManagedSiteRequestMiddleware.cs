using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Puts the Managed Site a public request belongs to into request-scoped context.
/// </summary>
/// <remarks>
/// This is the step that turns a URL into a Managed Site, and it is the only one. Everything
/// downstream, rendering included, reads the answer from
/// <see cref="IManagedSiteCompositionContextAccessor" /> rather than working it out again, so there is
/// one place where a request acquires its Managed Site and one place to look when it has the wrong one.
///
/// Client-supplied scope metadata is deliberately not consulted. A visitor could otherwise ask for
/// another Managed Site's content by sending a header, which FR-022 forbids: for public rendering the
/// URL decides and nothing else does. The header the API accepts as optional consistency metadata is
/// checked there, against clearance, and never here.
///
/// A Managed Site answering under a URL prefix has that prefix moved out of the path and onto the
/// request's PathBase, exactly as a tenant's own prefix is. Without that, a prefixed Managed Site would
/// resolve only for URLs under the prefix while no content is routed there, so it would match and then
/// serve nothing. Moving it also means generated links carry the prefix back, because link generation
/// already prepends PathBase.
///
/// Admin and API requests are left alone. Both are how content is edited, and a resolved Managed Site
/// is what licenses the load-time swap of content for that Managed Site's own, so resolving one here
/// would let an editor save a Managed Site's content over the Site Blueprint's. Admin requests are
/// recognised by path rather than by <see cref="AdminAttribute" />, which is applied by a filter during
/// action execution and so has decided nothing yet this early.
/// </remarks>
public sealed class ManagedSiteRequestMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteRequestMiddleware" /> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public ManagedSiteRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Resolves the Managed Site for the request and stores it for the rest of the pipeline.
    /// </summary>
    /// <param name="httpContext">The request context.</param>
    /// <param name="resolver">The Managed Site URL resolver.</param>
    /// <param name="contextAccessor">The Managed Site composition context accessor.</param>
    /// <param name="adminOptions">The admin options, used to recognise admin requests by path.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(
        HttpContext httpContext,
        IManagedSiteUrlResolver resolver,
        IManagedSiteCompositionContextAccessor contextAccessor,
        IOptions<AdminOptions> adminOptions)
    {
        var request = httpContext.Request;

        if (IsPublicRenderingRequest(request, adminOptions.Value))
        {
            var context = await resolver.ResolveAsync(request.Host.Host, request.Path.Value);

            contextAccessor.Current = context;

            Rebase(request, context);
        }

        await _next(httpContext);
    }

    /// <summary>
    /// Moves a resolved Managed Site's URL prefix from the path onto the path base.
    /// </summary>
    /// <remarks>
    /// Appended rather than assigned, because something ahead of this, a tenant prefix or a hosting
    /// path, may already have set one.
    /// </remarks>
    private static void Rebase(HttpRequest request, ManagedSiteRequestContext context)
    {
        if (string.IsNullOrEmpty(context.ManagedSiteId) || string.IsNullOrEmpty(context.UrlPrefix))
        {
            return;
        }

        var prefix = new PathString('/' + context.UrlPrefix);

        if (request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out var remaining))
        {
            request.PathBase += prefix;
            request.Path = remaining;
        }
    }

    /// <summary>
    /// Determines whether the request is one that renders a public page.
    /// </summary>
    /// <remarks>
    /// A resolved Managed Site is what licenses content to be swapped for that Managed Site's own as it
    /// loads, so resolving one for a request that edits content would let an editor save a Managed
    /// Site's content over the Site Blueprint's. Admin and API requests are how content is edited, so
    /// neither resolves a Managed Site and both always see the original.
    /// </remarks>
    private static bool IsPublicRenderingRequest(HttpRequest request, AdminOptions adminOptions)
    {
        var adminPrefix = adminOptions.AdminUrlPrefix;

        if (!string.IsNullOrEmpty(adminPrefix)
            && request.Path.StartsWithSegments('/' + adminPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }
}
