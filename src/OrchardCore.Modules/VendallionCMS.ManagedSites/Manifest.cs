using OrchardCore.Modules.Manifest;
using VendallionCMS.ManagedSites;

[assembly: Module(
    Name = "Vendallion Managed Sites",
    Author = "VendallionCMS Team",
    Website = "https://vendallioncms.example",
    Version = "1.0.0",
    Description = "Serves several sites from one tenant, sharing most of their content.",
    Category = "Content Management"
)]

[assembly: Feature(
    Id = ManagedSitesConstants.Features.ManagedSites,
    Name = "Managed Sites",
    Description = "Adds the Managed Content part, which decides who may override a content item and who renders it, and composes each request from the result.",
    Category = "Content Management",
    Dependencies = ["OrchardCore.Contents", "OrchardCore.Settings"]
)]

[assembly: Feature(
    Id = ManagedSitesConstants.Features.AdminPortal,
    Name = "Managed Sites Admin Portal",
    Description = "Adds the portal a managed site editor works in, scoped to the managed sites their clearance names, and the screens for defining managed sites.",
    Category = "Content Management",
    Dependencies = [ManagedSitesConstants.Features.ManagedSites, ManagedSitesConstants.Features.Routing, ManagedSitesConstants.Features.Permissions, "OrchardCore.Admin", "OrchardCore.Navigation"]
)]

[assembly: Feature(
    Id = ManagedSitesConstants.Features.Routing,
    Name = "Managed Sites Routing",
    Description = "Resolves an incoming URL to the managed site that answers it, and keeps the tenant's host names in step with the ones managed sites declare.",
    Category = "Infrastructure",
    Dependencies = [ManagedSitesConstants.Features.ManagedSites, "OrchardCore.Autoroute"]
)]

[assembly: Feature(
    Id = ManagedSitesConstants.Features.Permissions,
    Name = "Managed Sites Permissions",
    Description = "Adds the two permissions: governing managed sites, and editing the content of one.",
    Category = "Security",
    Dependencies = [ManagedSitesConstants.Features.ManagedSites, "OrchardCore.Roles", "OrchardCore.Users"]
)]
