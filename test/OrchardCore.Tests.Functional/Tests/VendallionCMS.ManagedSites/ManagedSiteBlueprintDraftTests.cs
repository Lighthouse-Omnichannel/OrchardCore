using Microsoft.Playwright;
using OrchardCore.Tests.Functional.Helpers;
using OrchardCore.Tests.Functional.Tests.Cms;

namespace OrchardCore.Tests.Functional.Tests.ManagedSites;

/// <summary>
/// Covers what a Managed Site is offered while the Site Blueprint is part way through changing it.
/// </summary>
/// <remarks>
/// Drafting a published item leaves the published version where it was and starts a second version
/// beside it. What a Managed Site may override is what is being served, so the published version is
/// what the content list is built from and beginning to change an item must not take it away from the
/// Managed Sites that were already answering it.
/// </remarks>
public sealed class ManagedSiteBlueprintDraftTests : CmsTestBase, IClassFixture<CmsSetupFixture>
{
    public ManagedSiteBlueprintDraftTests(CmsSetupFixture fixture)
        : base(fixture)
    {
    }

    protected override string RecipeName => "ManagedSitesDevelopment";

    [Fact]
    public async Task APublishedBlueprintItem_StaysListedWhileADraftOfItIsOpen()
    {
        var administrator = await Fixture.CreatePageAsync();

        try
        {
            await AuthHelper.LoginAsync(administrator, $"/{Tenant.Prefix}");

            // It is there before anybody touches it.
            Assert.Contains("Home", await ListedForAlphaAsync());

            await SaveADraftOfAsync(administrator, "Home");

            // And still there once the Site Blueprint has begun changing it.
            Assert.Contains("Home", await ListedForAlphaAsync());
        }
        finally
        {
            await administrator.CloseAsync();
        }
    }

    /// <summary>
    /// Opens a published blueprint item and saves a draft of it, changing nothing else.
    /// </summary>
    private async Task SaveADraftOfAsync(IPage page, string displayText)
    {
        await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/Contents/ContentItems");

        await page.Locator("li.list-group-item", new PageLocatorOptions { HasText = displayText })
            .First
            .Locator("a:has-text('Edit')")
            .First
            .ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The content editor's own Save Draft, which is what leaves a published version in place and
        // starts a second one beside it.
        await page.Locator("button.draft[name='submit.Save']").First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Reads what Alpha's editor is offered, which is the list under test.
    /// </summary>
    private async Task<string[]> ListedForAlphaAsync()
    {
        var page = await Fixture.CreatePageAsync();

        try
        {
            await page.GotoAsync($"{Fixture.BaseUrl}/{Tenant.Prefix}/Login");
            await page.Locator("#LoginForm_UserName").FillAsync("alphaUser");
            await page.Locator("#LoginForm_Password").FillAsync("Password1!");
            await page.Locator("form").Locator("button[type='submit']").First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Content/alpha");

            return await page.Locator("ul.list-group .contentitem").AllTextContentsAsync()
                is var texts
                ? [.. texts.Select(text => text.Trim())]
                : [];
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
