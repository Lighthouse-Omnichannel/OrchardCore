using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Decides who may configure Managed Content edit and display scopes.
/// </summary>
public interface IManagedContentScopeAuthorizationHandler
{
    /// <summary>
    /// Determines whether the current user may configure Managed Content scopes.
    /// </summary>
    /// <returns><see langword="true" /> when the user holds Site Blueprint management access.</returns>
    Task<bool> CanConfigureScopesAsync();

    /// <summary>
    /// Determines whether a user may configure Managed Content scopes.
    /// </summary>
    /// <param name="user">The user principal to evaluate.</param>
    /// <returns><see langword="true" /> when the user holds Site Blueprint management access.</returns>
    Task<bool> CanConfigureScopesAsync(ClaimsPrincipal user);
}

/// <summary>
/// Gates Managed Content scope configuration behind Site Blueprint management access.
/// </summary>
/// <remarks>
/// Scopes decide who may change the tenant's common content, so changing them is a blueprint-level act.
/// A Managed Site administrator must never be able to widen their own reach, which is why this check
/// lives in one place and is shared by every surface that exposes the scopes.
/// </remarks>
public sealed class ManagedContentScopeAuthorizationHandler : IManagedContentScopeAuthorizationHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedContentScopeAuthorizationHandler" /> class.
    /// </summary>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public ManagedContentScopeAuthorizationHandler(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Task<bool> CanConfigureScopesAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        return user is null ? Task.FromResult(false) : CanConfigureScopesAsync(user);
    }

    /// <inheritdoc />
    public async Task<bool> CanConfigureScopesAsync(ClaimsPrincipal user)
    {
        if (user is null)
        {
            return false;
        }

        return await _authorizationService.AuthorizeAsync(user, Permissions.ManageSiteBlueprint);
    }
}
