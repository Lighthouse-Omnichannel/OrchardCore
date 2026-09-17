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
    private const string AdminPortalFeatureId = "VendallionCMS.ManagedSites.AdminPortal";

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

            await CreateManagedSiteAsync(page, "Contoso", "/contoso");

            var row = page.Locator("tr", new PageLocatorOptions { HasText = "Contoso" }).First;
            await Assertions.Expect(row).ToBeVisibleAsync();
            await Assertions.Expect(row).ToContainTextAsync("/contoso");

            // The definition must survive the round trip through site settings, not just the redirect.
            await GotoIndexAsync(page);
            await Assertions.Expect(page.Locator("tr", new PageLocatorOptions { HasText = "Contoso" }).First)
                .ToBeVisibleAsync();

            await row.Locator("a:has-text('Edit')").First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.Locator("#Name")).ToHaveValueAsync("Contoso");
            await Assertions.Expect(page.Locator("#Urls")).ToHaveValueAsync("/contoso");

            await page.Locator("#Name").FillAsync("Contoso Renamed");
            await page.ClickSaveAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.Locator("tr", new PageLocatorOptions { HasText = "Contoso Renamed" }).First)
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
            await CreateManagedSiteAsync(page, "Fabrikam", "/fabrikam");

            await GotoIndexAsync(page);
            await page.ClickCreateAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#Name").FillAsync("Fabrikam Clone");
            await page.Locator("#Urls").FillAsync("/fabrikam");
            await page.ClickSaveAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // The editor stays open with the conflict reported, and the clone is never stored.
            await Assertions.Expect(page.Locator("#Name")).ToHaveValueAsync("Fabrikam Clone");
            await Assertions.Expect(page.GetByText("already assigned to another active registration")).ToBeVisibleAsync();

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
            await CreateManagedSiteAsync(page, "Northwind", "/northwind");

            var row = page.Locator("tr", new PageLocatorOptions { HasText = "Northwind" }).First;
            await row.Locator("a:has-text('Delete')").First.ClickAsync();
            await page.ClickModalOkAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.Locator("tr", new PageLocatorOptions { HasText = "Northwind" }))
                .ToHaveCountAsync(0);

            // Deleting must release the URL, otherwise the mapping outlives the Managed Site.
            await CreateManagedSiteAsync(page, "Northwind Reborn", "/northwind");

            await Assertions.Expect(page.Locator("tr", new PageLocatorOptions { HasText = "Northwind Reborn" }).First)
                .ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task CreateManagedSiteAsync(IPage page, string name, string url)
    {
        await GotoIndexAsync(page);
        await page.ClickCreateAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.Locator("#Name").FillAsync(name);
        await page.Locator("#Urls").FillAsync(url);
        await page.SelectOptionAsync("#Status", "Enabled");
        await page.ClickSaveAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private Task GotoIndexAsync(IPage page)
        => page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Index");

    private async Task<IPage> CreateAuthenticatedPageAsync()
    {
        var page = await Fixture.CreatePageAsync();
        await AuthHelper.LoginAsync(page, $"/{Tenant.Prefix}");
        await page.EnableFeatureAsync($"/{Tenant.Prefix}", AdminPortalFeatureId);

        return page;
    }
}
