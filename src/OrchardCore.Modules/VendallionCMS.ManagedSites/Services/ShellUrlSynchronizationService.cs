using Microsoft.Extensions.Logging;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Scope;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Keeps the tenant hostname in step with the host names Managed Sites declare.
/// </summary>
public interface IShellUrlSynchronizationService
{
    /// <summary>
    /// Reconciles the tenant hostname with the host names the supplied document declares.
    /// </summary>
    /// <remarks>
    /// Mutates the applied-host record on the document, so the caller must persist it afterwards.
    /// </remarks>
    /// <param name="document">The Managed Sites document being saved.</param>
    /// <returns><see langword="true" /> when the tenant hostname changed.</returns>
    bool Synchronize(ManagedSitesDocument document);
}

/// <summary>
/// Reconciles the tenant hostname from Managed Site host names.
/// </summary>
/// <remarks>
/// Only host names are synchronized. A tenant carries one URL prefix for all of its content, so a
/// Managed Site prefix is resolved inside the tenant and never becomes the tenant prefix.
///
/// Host names this module previously added are recorded, so synchronization withdraws exactly those and
/// leaves any host name an operator configured on the tenant in place.
///
/// Declaring a host name narrows a tenant that previously answered on every host, which is the intended
/// model: a tenant serving Managed Sites answers on the host names it declares. Any other address the
/// tenant must serve has to be declared alongside them.
///
/// The one case refused is clearing the hostname while the tenant URL prefix is also empty, because the
/// tenant would become a catch-all and could answer for addresses meant for other tenants. With a
/// prefix set the tenant stays addressable, so clearing the hostname is allowed.
/// </remarks>
public sealed class ShellUrlSynchronizationService : IShellUrlSynchronizationService
{
    private readonly ShellSettings _shellSettings;
    private readonly IShellHost _shellHost;
    private readonly ILogger<ShellUrlSynchronizationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellUrlSynchronizationService" /> class.
    /// </summary>
    /// <param name="shellSettings">The current shell settings.</param>
    /// <param name="shellHost">The shell host.</param>
    /// <param name="logger">The logger.</param>
    public ShellUrlSynchronizationService(
        ShellSettings shellSettings,
        IShellHost shellHost,
        ILogger<ShellUrlSynchronizationService> logger)
    {
        _shellSettings = shellSettings;
        _shellHost = shellHost;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool Synchronize(ManagedSitesDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Parsed from the authoritative string rather than from ShellSettings.RequestUrlHosts: that
        // array is cached on first read and the RequestUrlHost setter does not invalidate it, so a
        // second pass in the same scope would reconcile against a stale host list.
        var currentHosts = ReadCurrentHosts();

        // Every Managed Site declares its host names, switched off or not. Withdrawing the host of a
        // disabled Managed Site would stop the tenant answering on it at all; keeping it means the
        // address still resolves and serves Site Blueprint content, which is what a visitor should get.
        var declaredHosts = document.ManagedSites
            .SelectMany(managedSite => ManagedSiteAddressValidator.SplitHostnames(managedSite.Hostname))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var previouslyApplied = document.AppliedShellHosts
            .Select(ManagedSiteAddressValidator.NormalizeHost)
            .Where(host => !string.IsNullOrEmpty(host))
            .ToHashSet(StringComparer.Ordinal);

        // Start from the host names the tenant already serves, drop the ones this module added last
        // time, then append the current set. Operator-configured host names survive because they were
        // never applied here.
        var retained = currentHosts
            .Where(host => !previouslyApplied.Contains(host))
            .ToList();

        foreach (var host in declaredHosts)
        {
            if (!retained.Contains(host, StringComparer.Ordinal))
            {
                retained.Add(host);
            }
        }

        if (retained.Count == 0
            && currentHosts.Length > 0
            && string.IsNullOrEmpty(_shellSettings.RequestUrlPrefix))
        {
            // Clearing the hostname with no prefix set would make this tenant a catch-all, so the host
            // names are kept and reported instead. With a prefix the tenant stays addressable.
            _logger.LogWarning(
                "Managed Sites left the hostname for tenant '{TenantName}' unchanged because clearing it with no URL prefix set would make the tenant answer on every host.",
                _shellSettings.Name);

            return false;
        }

        document.AppliedShellHosts = [.. declaredHosts];

        var updated = string.Join(',', retained);
        var current = string.Join(',', currentHosts);

        if (string.Equals(updated, current, StringComparison.Ordinal))
        {
            return false;
        }

        _shellSettings.RequestUrlHost = updated;

        _logger.LogInformation(
            "Managed Sites updated the hostname for tenant '{TenantName}' to '{RequestUrlHost}'.",
            _shellSettings.Name,
            updated);

        // Persisting shell settings reloads the tenant, so it is deferred until the current request has
        // finished rather than pulled out from under it.
        ShellScope.AddDeferredTask(async scope =>
        {
            var shellHost = scope.ServiceProvider.GetService(typeof(IShellHost)) as IShellHost ?? _shellHost;

            await shellHost.UpdateShellSettingsAsync(_shellSettings);
        });

        return true;
    }

    private string[] ReadCurrentHosts()
        => (_shellSettings.RequestUrlHost ?? string.Empty)
            .Split(ShellSettings.HostSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(ManagedSiteAddressValidator.NormalizeHost)
            .Where(host => !string.IsNullOrEmpty(host))
            .ToArray();
}
