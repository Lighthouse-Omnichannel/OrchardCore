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
        await AddManagedContentOverridesAsync();

        return 4;
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

    /// <summary>
    /// Adds Managed Content override storage.
    /// </summary>
    /// <returns>The current schema version.</returns>
    public async Task<int> UpdateFrom2Async()
    {
        await AddManagedContentOverridesAsync();

        return 3;
    }

    /// <summary>
    /// Records where a Managed Content item lives, so contained items can be reached.
    /// </summary>
    /// <remarks>
    /// A page section is a content item but not a document: it is stored inside its page. Until the
    /// index carried the container and the path, only items stored in their own right could be found,
    /// so a page whose sections carried Managed Content listed nothing at all.
    /// </remarks>
    /// <returns>The current schema version.</returns>
    public async Task<int> UpdateFrom3Async()
    {
        await SchemaBuilder.AlterIndexTableAsync<ManagedContentEditScopeIndex>(table => table
            .AddColumn<string>("ContainerContentItemId", column => column.WithLength(26)));

        await SchemaBuilder.AlterIndexTableAsync<ManagedContentEditScopeIndex>(table => table
            .AddColumn<string>("JsonPath", column => column.Unlimited()));

        return 4;
    }

    private async Task AddManagedContentPartAsync()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync(nameof(ManagedContentPart), builder => builder
            .Attachable()
            .WithDescription("Lets managed sites override this content and controls which of them see it."));

        await SchemaBuilder.CreateMapIndexTableAsync<ManagedContentEditScopeIndex>(table => table
            .Column<string>("ContentItemId", column => column.WithLength(26))
            .Column<string>("ContainerContentItemId", column => column.WithLength(26))
            .Column<string>("JsonPath", column => column.Unlimited())
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

    private async Task AddManagedContentOverridesAsync()
    {
        // Not attachable. An override carries this part because the override service put it there, so
        // offering it in the content type editor would only invite an item to claim it is an override
        // of something nobody granted.
        await _contentDefinitionManager.AlterPartDefinitionAsync(nameof(ManagedContentOverridePart), builder => builder
            .WithDescription("Marks a content item as one managed site's replacement for a managed content item."));

        await SchemaBuilder.CreateMapIndexTableAsync<ManagedContentOverrideIndex>(table => table
            .Column<string>("OverrideContentItemId", column => column.WithLength(26))
            .Column<string>("ManagedSiteId", column => column.WithLength(26))
            .Column<string>("SourceContentItemId", column => column.WithLength(26))
            .Column<string>("ContentType", column => column.WithLength(255))
            .Column<bool>("Latest", column => column.WithDefault(false))
            .Column<bool>("Published", column => column.WithDefault(false)));

        await SchemaBuilder.AlterIndexTableAsync<ManagedContentOverrideIndex>(table => table
            .CreateIndex(
                "IDX_ManagedContentOverrideIndex_Source",
                "DocumentId",
                "ManagedSiteId",
                "SourceContentItemId",
                "Published",
                "Latest"));
    }
}
