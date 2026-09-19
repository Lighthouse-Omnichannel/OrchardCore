using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using VendallionCMS.ManagedSites.Indexes;
using VendallionCMS.ManagedSites.Models;
using YesSql.Sql;

namespace VendallionCMS.ManagedSites.Migrations;

/// <summary>
/// Creates and evolves Managed Sites persistence structures.
/// </summary>
public sealed class ManagedSitesMigrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSitesMigrations" /> class.
    /// </summary>
    /// <param name="contentDefinitionManager">The content definition manager.</param>
    public ManagedSitesMigrations(IContentDefinitionManager contentDefinitionManager)
    {
        _contentDefinitionManager = contentDefinitionManager;
    }

    /// <summary>
    /// Creates the Managed Sites schema for a fresh install.
    /// </summary>
    /// <returns>The current schema version.</returns>
    public async Task<int> CreateAsync()
    {
        await AddManagedContentPartAsync();

        return 2;
    }

    /// <summary>
    /// Adds Managed Content to installs that were created before the part existed.
    /// </summary>
    /// <remarks>
    /// Version 1 created nothing, so a tenant that already had the module recorded that version and
    /// would never run <see cref="CreateAsync" /> again. Without this step the part definition is never
    /// written and the part cannot be attached to a content type.
    /// </remarks>
    /// <returns>The current schema version.</returns>
    public async Task<int> UpdateFrom1Async()
    {
        await AddManagedContentPartAsync();

        return 2;
    }

    private async Task AddManagedContentPartAsync()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync(nameof(ManagedContentPart), builder => builder
            .Attachable()
            .WithDescription("Lets managed sites override this content and controls which of them see it."));

        await SchemaBuilder.CreateMapIndexTableAsync<ManagedContentEditScopeIndex>(table => table
            .Column<string>("ContentItemId", column => column.WithLength(26))
            .Column<string>("ContentType", column => column.WithLength(255))
            .Column<string>("ManagedSiteId", column => column.WithLength(26))
            .Column<bool>("AllManagedSites", column => column.WithDefault(false))
            .Column<bool>("Latest", column => column.WithDefault(false))
            .Column<bool>("Published", column => column.WithDefault(false)));

        await SchemaBuilder.AlterIndexTableAsync<ManagedContentEditScopeIndex>(table => table
            .CreateIndex(
                "IDX_ManagedContentEditScopeIndex_ManagedSiteId",
                "DocumentId",
                "ManagedSiteId",
                "AllManagedSites",
                "Latest",
                "Published"));
    }
}
