using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using VendallionCMS.ManagedSites;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Registration;

/// <summary>
/// Covers the shape of the module's service graph, rather than what any one service does.
/// </summary>
/// <remarks>
/// Two dependency cycles reached a running tenant unnoticed while every other test passed, because a
/// cycle is a property of how services are registered and nothing else here builds a container. Both
/// are structural mistakes that can be read off the registrations without resolving anything.
///
/// The graph is taken from the module's own Startup classes, so it is whatever the module actually
/// registers rather than a list kept in step by hand.
/// </remarks>
public class TenantServiceRegistrationTests
{
    [Fact]
    public void ModuleServices_HaveNoDependencyCycle()
    {
        // The first cycle shipped: the Managed Site store told the composition cache when addresses
        // changed, and the cache asked the store which Managed Sites existed.
        var graph = new RegistrationGraph();

        var cycle = graph.FindCycle();

        Assert.True(cycle is null, $"Dependency cycle: {cycle}");
    }

    [Fact]
    public void ContentHandlers_DoNotReachTheContentManagerThroughTheirConstructors()
    {
        // The second cycle shipped: the content manager depends on every content handler, so a handler
        // asking for it cannot be constructed at all. A handler that needs it resolves it when it runs,
        // by which point the content manager that called it is built and cached.
        var graph = new RegistrationGraph();

        foreach (var handler in graph.ImplementationsOf(typeof(IContentHandler)))
        {
            var path = graph.FindPathTo(handler, typeof(IContentManager));

            Assert.True(path is null, $"{handler.Name} reaches IContentManager through its constructor: {path}");
        }
    }

    [Fact]
    public void EveryServiceInterface_IsRegisteredOnceForOneImplementation()
    {
        // Two implementations of the same interface would mean whichever registration ran last decides
        // behaviour, which is not something to discover from a running site.
        var duplicates = new RegistrationGraph().DuplicateRegistrations().ToArray();

        Assert.True(duplicates.Length == 0, $"Registered more than once: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void TheGraphIsNotEmpty()
    {
        // Guards the other three: if the Startup classes stopped being read, they would all pass by
        // finding nothing to check.
        Assert.NotEmpty(new RegistrationGraph().ImplementationsOf(typeof(IContentHandler)));
    }

    /// <summary>
    /// The module's registrations, read from its Startup classes.
    /// </summary>
    private sealed class RegistrationGraph
    {
        private readonly List<ServiceDescriptor> _descriptors = [];

        public RegistrationGraph()
        {
            var services = new ServiceCollection();

            foreach (var startup in StartupTypes())
            {
                var instance = (OrchardCore.Modules.StartupBase)Activator.CreateInstance(startup);

                instance.ConfigureServices(services);
            }

            _descriptors.AddRange(services);
        }

        public IEnumerable<Type> ImplementationsOf(Type serviceType)
            => _descriptors
                .Where(descriptor => descriptor.ServiceType == serviceType && descriptor.ImplementationType is not null)
                .Select(descriptor => descriptor.ImplementationType)
                .Where(IsOurs)
                .Distinct();

        public IEnumerable<string> DuplicateRegistrations()
            => _descriptors
                .Where(descriptor => descriptor.ImplementationType is not null && IsOurs(descriptor.ServiceType))
                .GroupBy(descriptor => descriptor.ServiceType)
                .Where(group => group.Select(descriptor => descriptor.ImplementationType).Distinct().Count() > 1)
                .Select(group => group.Key.Name);

        /// <summary>
        /// Finds a constructor dependency path from an implementation to a service type, or null.
        /// </summary>
        public string FindPathTo(Type implementation, Type target)
            => Walk(implementation, target, []);

        /// <summary>
        /// Finds a cycle among the module's own services, or null when there is none.
        /// </summary>
        public string FindCycle()
        {
            foreach (var implementation in _descriptors
                .Select(descriptor => descriptor.ImplementationType)
                .Where(type => type is not null && IsOurs(type))
                .Distinct())
            {
                var cycle = Walk(implementation, implementation, [], startIsTarget: true);

                if (cycle is not null)
                {
                    return cycle;
                }
            }

            return null;
        }

        private string Walk(Type implementation, Type target, HashSet<Type> seen, bool startIsTarget = false)
        {
            if (implementation is null || !seen.Add(implementation))
            {
                return null;
            }

            foreach (var parameter in Dependencies(implementation))
            {
                if (parameter == target && (!startIsTarget || seen.Count > 1))
                {
                    return $"{implementation.Name} -> {parameter.Name}";
                }

                foreach (var next in Implementations(parameter))
                {
                    if (startIsTarget && next == target)
                    {
                        return $"{implementation.Name} -> {parameter.Name} -> {next.Name}";
                    }

                    var rest = Walk(next, target, seen, startIsTarget);

                    if (rest is not null)
                    {
                        return $"{implementation.Name} -> {parameter.Name} -> {rest}";
                    }
                }
            }

            return null;
        }

        private static IEnumerable<Type> Dependencies(Type implementation)
        {
            var constructor = implementation
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(candidate => candidate.GetParameters().Length)
                .FirstOrDefault();

            return constructor is null
                ? []
                : constructor.GetParameters().Select(parameter => parameter.ParameterType);
        }

        private IEnumerable<Type> Implementations(Type serviceType)
            => _descriptors
                .Where(descriptor => descriptor.ServiceType == serviceType && descriptor.ImplementationType is not null)
                .Select(descriptor => descriptor.ImplementationType)
                .Where(IsOurs)
                .Distinct();

        // Only the module's own types are walked. Anything else is a boundary this test cannot see
        // into, and IContentManager is checked as a destination rather than walked through.
        private static bool IsOurs(Type type)
            => type.Assembly == typeof(Startup).Assembly;

        private static IEnumerable<Type> StartupTypes()
            => typeof(Startup).Assembly
                .GetTypes()
                .Where(type => !type.IsAbstract && typeof(OrchardCore.Modules.StartupBase).IsAssignableFrom(type))
                .OrderBy(type => type.Name);
    }
}
