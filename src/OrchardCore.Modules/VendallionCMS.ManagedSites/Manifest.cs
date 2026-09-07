using OrchardCore.Modules.Manifest;
using VendallionCMS.ManagedSites;

[assembly: Module(
    Name = "Vendallion Managed Sites",
    Author = "VendallionCMS Team",
    Website = "https://vendallioncms.example",
    Version = "1.0.0",
    Description = "Provides Site Blueprint and Managed Site content composition capabilities.",
    Category = "Content Management"
)]

[assembly: Feature(
    Id = ManagedSitesConstants.Features.ManagedSites,
    Name = "Managed Sites",
    Description = "Defines Site Blueprint and Managed Site scopes, URL registrations, and composition rules.",
    Category = "Content Management",
    Dependencies = ["OrchardCore.Contents", "OrchardCore.Settings"]
)]

[assembly: Feature(
    Id = ManagedSitesConstants.Features.Admin,
    Name = "Managed Sites Admin",
    Description = "Provides the Managed Site Admin Portal and administrative screens.",
    Category = "Content Management",
    Dependencies = [ManagedSitesConstants.Features.ManagedSites, "OrchardCore.Admin", "OrchardCore.Navigation"]
)]

[assembly: Feature(
    Id = ManagedSitesConstants.Features.Routing,
    Name = "Managed Sites Routing",
    Description = "Resolves incoming URLs to Managed Site request context and synchronizes shell URL mappings.",
    Category = "Infrastructure",
    Dependencies = [ManagedSitesConstants.Features.ManagedSites, "OrchardCore.Autoroute"]
)]

[assembly: Feature(
    Id = ManagedSitesConstants.Features.Permissions,
    Name = "Managed Sites Permissions",
    Description = "Adds scoped permissions for Site Blueprint and Managed Site management.",
    Category = "Security",
    Dependencies = [ManagedSitesConstants.Features.ManagedSites, "OrchardCore.Roles", "OrchardCore.Users"]
)]

[assembly: Feature(
    Id = ManagedSitesConstants.Features.Composition,
    Name = "Managed Sites Composition",
    Description = "Composes request content from Site Blueprint and Managed Site layers.",
    Category = "Content Management",
    Dependencies = [ManagedSitesConstants.Features.ManagedSites, ManagedSitesConstants.Features.Routing]
)]
