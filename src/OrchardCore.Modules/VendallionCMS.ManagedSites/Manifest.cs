using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Vendallion Managed Sites",
    Author = "VendallionCMS Team",
    Website = "https://vendallioncms.example",
    Version = "1.0.0",
    Description = "Serves several sites from one tenant, sharing most of their content: each Managed Site is an address that may replace named pieces of the tenant's content with its own.",
    Category = "Content Management",
    Dependencies =
    [
        "OrchardCore.Contents",
        "OrchardCore.Settings",
        "OrchardCore.Autoroute",
        "OrchardCore.Admin",
        "OrchardCore.Navigation",
        "OrchardCore.Roles",
        "OrchardCore.Users",
    ]
)]
