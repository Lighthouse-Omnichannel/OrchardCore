using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using OrchardCore.Security.Permissions;
using VendallionCMS.ManagedSites;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites.Tests;

/// <summary>
/// Provides test data builders for Managed Sites tests.
/// </summary>
public static class ManagedSitesTestData
{
    /// <summary>
    /// The user identifier used by <see cref="User" />.
    /// </summary>
    public const string UserId = "user-id";

    /// <summary>
    /// Creates a test Managed Site addressed by host name and URL prefix.
    /// </summary>
    /// <param name="managedSiteCode">The Managed Site identifier.</param>
    /// <param name="status">The Managed Site lifecycle status.</param>
    /// <param name="name">The display name, defaulting to the identifier.</param>
    /// <param name="hostname">The host names it answers on, empty for every host.</param>
    /// <param name="urlPrefix">The URL prefix it answers under, empty for the root.</param>
    /// <returns>A test Managed Site.</returns>
    public static ManagedSite ManagedSite(
        string managedSiteCode = "managed-site",
        ManagedSiteStatus status = ManagedSiteStatus.Enabled,
        string name = null,
        string hostname = null,
        string urlPrefix = null)
        => new()
        {
            Code = managedSiteCode,
            Name = name ?? managedSiteCode,
            Status = status,
            Hostname = hostname,
            UrlPrefix = urlPrefix,
        };

    /// <summary>
    /// Creates an authenticated principal holding the permissions an authority is written as.
    /// </summary>
    /// <remarks>
    /// Authority over a Managed Site is a permission held through a role, and a role puts it on the
    /// principal as a permission claim. The shorthand here is the one the tests were written in --
    /// <c>sitea</c> for reading, <c>sitea:edit,publish</c> for more, <c>sitea:*</c> for everything --
    /// translated into the claims a real sign-in would carry.
    /// </remarks>
    /// <param name="authorities">Authority shorthand such as <c>sitea:edit,publish</c>.</param>
    /// <returns>An authenticated principal.</returns>
    public static ClaimsPrincipal User(params string[] authorities)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, UserId) };

        foreach (var authority in authorities ?? [])
        {
            foreach (var permission in PermissionsFor(authority))
            {
                claims.Add(new Claim(Permission.ClaimType, permission));
            }
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>
    /// Creates an authenticated principal holding permissions by name.
    /// </summary>
    /// <param name="permissions">The permission names.</param>
    /// <returns>An authenticated principal.</returns>
    public static ClaimsPrincipal UserHolding(params string[] permissions)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, UserId) };

        foreach (var permission in permissions ?? [])
        {
            claims.Add(new Claim(Permission.ClaimType, permission));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static IEnumerable<string> PermissionsFor(string authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
        {
            yield break;
        }

        var parts = authority.Split(':', 2, StringSplitOptions.TrimEntries);
        var code = parts[0];

        if (string.IsNullOrEmpty(code))
        {
            yield break;
        }

        var scopes = parts.Length == 1
            ? []
            : parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (scopes.Length == 0)
        {
            // Naming no scope is reading, which is the floor.
            yield return ManagedSiteContentPermissions.For(code, scope: null).Name;

            yield break;
        }

        if (scopes.Contains("*"))
        {
            foreach (var scope in ManagedSitesConstants.Scopes.All)
            {
                yield return ManagedSiteContentPermissions.For(code, scope).Name;
            }

            yield break;
        }

        foreach (var scope in scopes)
        {
            if (ManagedSiteContentPermissions.For(code, scope) is { } permission)
            {
                yield return permission.Name;
            }
        }
    }

    /// <summary>
    /// Creates an anonymous principal.
    /// </summary>
    /// <returns>An unauthenticated principal.</returns>
    public static ClaimsPrincipal AnonymousUser() => new(new ClaimsIdentity());
}
