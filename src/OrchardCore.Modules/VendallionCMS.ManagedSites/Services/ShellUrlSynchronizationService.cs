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
/// This module confines itself to the host names it added. Those are recorded, so synchronization
/// withdraws exactly those and leaves any host name an operator configured on the tenant in place.
///
/// A tenant that declares no host of its own already answers on every host, the ones Managed Sites
/// name included, so nothing is added to it. Adding one would narrow the tenant to that host alone and
/// every other address it answered on would stop resolving -- including the one the administrator was
/// using, which is how filling in a host name blanked the site they were looking at. A tenant that does
/// declare host names is extended with the ones Managed Sites name, because there it is the only way
/// the Managed Site's address reaches the tenant at all.
///
/// Withdrawing leaves the tenant with nothing only when every host name present was one this module
/// added, which restores the tenant to how it was found. That is not the tenant becoming a catch-all at
/// anyone's expense: a tenant naming a host is matched before the fallback, so the fallback answers
/// only for addresses nothing else claims.
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

        // Start from the host names the tenant already serves, dropping the ones this module added last
        // time. Operator-configured host names survive because they were never added here.
        var retained = currentHosts
            .Where(host => !previouslyApplied.Contains(host))
            .ToList();

        // A tenant naming nothing answers on every host already, so there is nothing to add and adding
        // anything would take away every address but the one added.
        var tenantNamesItsOwnHosts = retained.Count > 0;

        var added = new List<string>();

        if (tenantNamesItsOwnHosts)
        {
            foreach (var host in declaredHosts.Where(host => !retained.Contains(host, StringComparer.Ordinal)))
            {
                retained.Add(host);
                added.Add(host);
            }
        }

        // Only what this module put there, so that withdrawing it later takes back exactly that and the
        // record never claims a host name the operator had configured all along.
        document.AppliedShellHosts = [.. added];

        var updated = string.Join(',', retained);
        var current = string.Join(',', currentHosts);

        if (string.Equals(updated, current, StringComparison.Ordinal))
        {
            return false;
        }

        _shellSettings.RequestUrlHost = updated;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Managed Sites updated the hostname for tenant '{TenantName}' to '{RequestUrlHost}'.",
                _shellSettings.Name,
                updated);
        }

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
