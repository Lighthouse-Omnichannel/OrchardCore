using Microsoft.Playwright;
using OrchardCore.Tests.Functional.Helpers;
using OrchardCore.Tests.Functional.Tests.Cms;
using Xunit;

namespace OrchardCore.Tests.Functional.Tests.ManagedSites;

/// <summary>
/// Covers creating, editing, and removing Managed Site definitions through the admin UI.
///
/// Managed Sites had no create path at all until this coverage was added, so these tests exercise the
/// whole loop: a definition must survive a round trip through site settings, and a URL already owned by
/// another Managed Site must be refused rather than silently reassigned.
/// </summary>
public sealed class ManagedSiteDefinitionAdminTests : CmsTestBase, IClassFixture<CmsSetupFixture>
{
    private const string ManagedSitesFeatureId = "VendallionCMS.ManagedSites";

    public ManagedSiteDefinitionAdminTests(CmsSetupFixture fixture) : base(fixture) { }

    protected override string RecipeName => "Blog";

    [Fact]
    public async Task ManagedSite_CreatedThroughTheAdmin_IsListedAndEditable()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await GotoIndexAsync(page);
            await Assertions.Expect(page.GetByText("No managed sites have been defined yet.")).ToBeVisibleAsync();

            await CreateManagedSiteAsync(page, "Contoso", "contoso");

            var row = Row(page, "Contoso");
            await Assertions.Expect(row).ToBeVisibleAsync();
            // The list names the managed site and links it to where it answers; the address itself is
            // in the editor rather than the row.
            Assert.EndsWith("/contoso", await NameLinkAsync(page, "Contoso"), StringComparison.Ordinal);

            // The definition must survive the round trip through site settings, not just the redirect.
            await GotoIndexAsync(page);
            await Assertions.Expect(Row(page, "Contoso"))
                .ToBeVisibleAsync();

            await row.Locator("a:has-text('Edit')").First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.Locator("#Name")).ToHaveValueAsync("Contoso");
            await Assertions.Expect(page.Locator("#UrlPrefix")).ToHaveValueAsync("contoso");

            await page.Locator("#Name").FillAsync("Contoso Renamed");
            await page.ClickSaveAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(Row(page, "Contoso Renamed"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ManagedSite_UrlOwnedByAnotherSite_IsRejected()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Fabrikam", "fabrikam");

            await GotoIndexAsync(page);
            await page.ClickCreateAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // A code of its own, so what is refused is the address rather than the code.
            await page.Locator("#Code").FillAsync("fabrikamclone");
            await page.Locator("#Name").FillAsync("Fabrikam Clone");
            await page.Locator("#UrlPrefix").FillAsync("fabrikam");
            await page.ClickSaveAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // The editor stays open with the conflict reported, and the clone is never stored.
            await Assertions.Expect(page.Locator("#Name")).ToHaveValueAsync("Fabrikam Clone");
            await Assertions.Expect(page.GetByText("Another managed site already answers on that address.")).ToBeVisibleAsync();

            await GotoIndexAsync(page);
            await Assertions.Expect(page.Locator("tr", new PageLocatorOptions { HasText = "Fabrikam Clone" }))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ManagedSite_Deleted_IsRemovedAndFreesItsUrl()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Northwind", "northwind");

            var row = Row(page, "Northwind");
            await row.Locator("button.dropdown-toggle").First.ClickAsync();
            await row.Locator("a:has-text('Delete')").First.ClickAsync();
            await page.ClickModalOkAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.Locator("li.list-group-item", new PageLocatorOptions { HasText = "Northwind" }))
                .ToHaveCountAsync(0);

            // Deleting must release the URL, otherwise the mapping outlives the Managed Site.
            await CreateManagedSiteAsync(page, "Northwind Reborn", "northwind");

            await Assertions.Expect(Row(page, "Northwind Reborn"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Covers the link on a managed site's name, which goes where the site answers.
    /// </summary>
    [Fact]
    public async Task ManagedSite_NameLinksToWhereTheSiteAnswers()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Prefixed", "prefixed");
            await CreateManagedSiteAsync(page, "Hosted", string.Empty, "hosted.example");
            await CreateManagedSiteAsync(page, "Rootless", string.Empty);

            await GotoIndexAsync(page);

            // A prefix is a path under this site.
            Assert.EndsWith("/prefixed", await NameLinkAsync(page, "Prefixed"), StringComparison.Ordinal);

            // A host name wins outright, so the link leaves for it rather than using any prefix.
            Assert.Contains("hosted.example", await NameLinkAsync(page, "Hosted"), StringComparison.Ordinal);

            // Naming neither means the site answers on the tenant's own address, which is not its own
            // address to link to.
            Assert.Null(await NameLinkAsync(page, "Rootless"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static ILocator Row(IPage page, string name)
        => page.Locator("li.list-group-item", new PageLocatorOptions { HasText = name }).First;

    private static async Task<string> NameLinkAsync(IPage page, string name)
    {
        var link = Row(page, name).Locator(".contentitem a");

        return await link.CountAsync() == 0 ? null : await link.GetAttributeAsync("href");
    }

    private async Task CreateManagedSiteAsync(IPage page, string name, string urlPrefix, string hostname = null)
    {
        await GotoIndexAsync(page);
        await page.ClickCreateAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The code names the managed site everywhere else and is asked for only here.
        await page.Locator("#Code").FillAsync(name.ToLowerInvariant().Replace(" ", string.Empty));
        await page.Locator("#Name").FillAsync(name);
        await page.Locator("#UrlPrefix").FillAsync(urlPrefix);

        if (!string.IsNullOrEmpty(hostname))
        {
            await page.Locator("#Hostname").FillAsync(hostname);
        }

        // Enabled is the status a new managed site opens with, so it is left alone.
        await page.ClickSaveAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private Task GotoIndexAsync(IPage page)
        => page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Index");

    private async Task<IPage> CreateAuthenticatedPageAsync()
    {
        var page = await Fixture.CreatePageAsync();
        await AuthHelper.LoginAsync(page, $"/{Tenant.Prefix}");
        await page.EnableFeatureAsync($"/{Tenant.Prefix}", ManagedSitesFeatureId);

        return page;
    }
}
