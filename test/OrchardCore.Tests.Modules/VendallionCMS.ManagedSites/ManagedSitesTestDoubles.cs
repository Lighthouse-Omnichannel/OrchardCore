using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using OrchardCore.Entities;
using System.Text.Json.Nodes;
using OrchardCore.Settings;
using System.Threading.Tasks;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;

namespace VendallionCMS.ManagedSites.Tests;

/// <summary>
/// In-memory Managed Site definitions for tests.
/// </summary>
public sealed class FakeManagedSiteService : IManagedSiteService
{
    private readonly List<ManagedSite> _managedSites;

    public FakeManagedSiteService(params ManagedSite[] managedSites)
    {
        _managedSites = [.. managedSites];
    }

    public ValueTask<ManagedSite> GetAsync(string managedSiteId)
        => ValueTask.FromResult(_managedSites.FirstOrDefault(managedSite => managedSite.Id == managedSiteId));

    public ValueTask<IReadOnlyList<ManagedSite>> ListAsync()
        => ValueTask.FromResult<IReadOnlyList<ManagedSite>>(_managedSites.ToArray());

    // Defers to the real rule rather than re-stating it. A double that picked the first match would
    // let precedence tests pass against behaviour production does not have.
    public ValueTask<ManagedSite> FindByAddressAsync(string host, string path)
        => ValueTask.FromResult(ManagedSiteAddressValidator.FindBestMatch(_managedSites, host, path));

    public ValueTask SaveAsync(ManagedSite managedSite)
    {
        _managedSites.RemoveAll(item => item.Id == managedSite.Id);
        _managedSites.Add(managedSite);

        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> DeleteAsync(string managedSiteId)
        => ValueTask.FromResult(_managedSites.RemoveAll(item => item.Id == managedSiteId) > 0);
}

/// <summary>
/// In-memory active scope store for tests.
/// </summary>
public sealed class InMemoryManagedSiteSessionStore : IManagedSiteSessionStore
{
    private readonly Dictionary<string, ActiveManagedSiteSessionScope> _scopes = new(StringComparer.Ordinal);

    public int SetCount { get; private set; }

    public int ClearCount { get; private set; }

    public ValueTask<ActiveManagedSiteSessionScope> GetAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return ValueTask.FromResult<ActiveManagedSiteSessionScope>(null);
        }

        return ValueTask.FromResult(_scopes.GetValueOrDefault(userId));
    }

    public ValueTask SetAsync(ActiveManagedSiteSessionScope scope)
    {
        SetCount++;
        _scopes[scope.UserId] = scope;

        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(string userId)
    {
        ClearCount++;
        _scopes.Remove(userId);

        return ValueTask.CompletedTask;
    }

    public void Seed(string userId, string managedSiteId)
        => _scopes[userId] = new ActiveManagedSiteSessionScope
        {
            UserId = userId,
            ManagedSiteId = managedSiteId,
            SelectedAt = DateTimeOffset.UnixEpoch,
        };
}

/// <summary>
/// Builds the service graph used by portal session and clearance tests.
/// </summary>
public sealed class ManagedSitePortalTestContext
{
    public ManagedSitePortalTestContext(params ManagedSite[] managedSites)
    {
        ManagedSiteService = new FakeManagedSiteService(managedSites);
        SessionStore = new InMemoryManagedSiteSessionStore();
        ClearanceService = new ManagedSiteClearanceService(
            new ManagedSiteAuthorizationService(),
            ManagedSiteService,
            TimeProvider.System);
        SessionService = new ManagedSiteSessionService(
            ClearanceService,
            ManagedSiteService,
            SessionStore,
            TimeProvider.System);
    }

    public FakeManagedSiteService ManagedSiteService { get; }

    public InMemoryManagedSiteSessionStore SessionStore { get; }

    public IManagedSiteClearanceService ClearanceService { get; }

    public IManagedSiteSessionService SessionService { get; }
}

/// <summary>
/// In-memory site settings for tests.
/// </summary>
/// <remarks>
/// Models the real store rather than sharing one object: each call returns a fresh instance over a
/// serialized copy of the persisted state, and only <see cref="UpdateSiteSettingsAsync" /> commits.
/// A service that mutates the detached document from <c>GetOrCreate</c> without writing it back
/// therefore loses the change here exactly as it would in production.
/// </remarks>
public sealed class FakeSiteService : ISiteService
{
    private JsonObject _persisted = [];

    public int UpdateCount { get; private set; }

    public Task<ISite> LoadSiteSettingsAsync() => Task.FromResult<ISite>(Snapshot());

    public Task<ISite> GetSiteSettingsAsync() => Task.FromResult<ISite>(Snapshot());

    public Task UpdateSiteSettingsAsync(ISite site)
    {
        UpdateCount++;
        _persisted = (JsonObject)site.Properties.DeepClone();

        return Task.CompletedTask;
    }

    private SiteSettings Snapshot() => new() { Properties = (JsonObject)_persisted.DeepClone() };
}

/// <summary>
/// Authorization service stub that allows or denies every requirement.
/// </summary>
public sealed class StubAuthorizationService : IAuthorizationService
{
    private readonly bool _isAuthorized;

    public StubAuthorizationService(bool isAuthorized)
    {
        _isAuthorized = isAuthorized;
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object resource,
        IEnumerable<IAuthorizationRequirement> requirements)
        => Task.FromResult(_isAuthorized ? AuthorizationResult.Success() : AuthorizationResult.Failed());

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
        => Task.FromResult(_isAuthorized ? AuthorizationResult.Success() : AuthorizationResult.Failed());
}

/// <summary>
/// Deterministic identifier generator for tests.
/// </summary>
public sealed class SequentialIdGenerator : IIdGenerator
{
    private int _next;

    public string GenerateUniqueId() => $"id-{++_next}";
}

/// <summary>
/// Shell synchronization double for tests that do not exercise shell behavior.
/// </summary>
/// <remarks>
/// Deliberately does nothing. Shell reconciliation is covered against the real service instead, because
/// a double that re-implemented the rules once hid a defect: it modelled list arithmetic but not
/// OrchardCore's rule that an empty request host means the tenant answers on every host.
/// </remarks>
public sealed class FakeShellUrlSynchronizationService : IShellUrlSynchronizationService
{
    public int SynchronizeCount { get; private set; }

    public bool Synchronize(ManagedSitesDocument document)
    {
        SynchronizeCount++;

        return false;
    }
}
