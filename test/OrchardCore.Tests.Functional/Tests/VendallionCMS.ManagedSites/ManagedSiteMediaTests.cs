using Microsoft.Playwright;
using OrchardCore.Tests.Functional.Helpers;
using OrchardCore.Tests.Functional.Tests.Cms;

namespace OrchardCore.Tests.Functional.Tests.ManagedSites;

/// <summary>
/// Covers a Managed Site's media folder as the development recipe arranges it.
/// </summary>
/// <remarks>
/// Written against the recipe rather than against a tenant assembled here, because the arrangement is
/// the thing under test: a folder per Managed Site, one blueprint folder shared for reading, and roles
/// that hold the folders of the sites their holders work on and no others. Getting any one of those
/// wrong produces a tenant that looks right until somebody opens the media library.
///
/// What an editor may do to a folder is settled by unit tests over the handler that decides it. These
/// assertions are about what the editor is actually shown once the recipe, the permissions and the
/// handler are all in play together.
/// </remarks>
public sealed class ManagedSiteMediaTests : CmsTestBase, IClassFixture<CmsSetupFixture>
{
    public ManagedSiteMediaTests(CmsSetupFixture fixture)
        : base(fixture)
    {
    }

    protected override string RecipeName => "ManagedSitesDevelopment";

    /// <summary>
    /// The recipe stands the whole arrangement up, which is the first thing that has to be true.
    /// </summary>
    [Fact]
    public async Task DevelopmentRecipe_GivesEachManagedSiteAFolderOfItsOwn()
    {
        // Asked as the administrator, because the whole point of the rules below is that no editor is
        // shown all of these at once.
        var page = await Fixture.CreatePageAsync();

        try
        {
            await AuthHelper.LoginAsync(page, $"/{Tenant.Prefix}");

            var folders = await ReadRootFoldersAsync(page);

            Assert.Contains("alpha", folders);
            Assert.Contains("beta", folders);
            Assert.Contains("gamma", folders);
            Assert.Contains("shared", folders);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// An editor is shown their own Managed Site's folder and the blueprint folder they are granted.
    /// </summary>
    [Fact]
    public async Task AnEditor_SeesTheirOwnFolderAndTheSharedOne()
    {
        var page = await SignedInAsync("alphaUser");

        try
        {
            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/Media");

            var folders = await VisibleFoldersAsync(page);

            Assert.Contains("alpha", folders);
            Assert.Contains("shared", folders);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// And is shown neither another Managed Site's folder nor a blueprint folder nobody granted them.
    /// </summary>
    [Fact]
    public async Task AnEditor_IsNotShownAnotherManagedSitesFolder()
    {
        var page = await SignedInAsync("alphaUser");

        try
        {
            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/Media");

            var folders = await VisibleFoldersAsync(page);

            Assert.DoesNotContain("beta", folders);
            Assert.DoesNotContain("gamma", folders);
            Assert.DoesNotContain("restricted", folders);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// An editor of two Managed Sites holds both folders, because the roles granting them are held
    /// alongside the clearances rather than derived from them.
    /// </summary>
    [Fact]
    public async Task AnEditorOfTwoManagedSites_IsShownBothFolders()
    {
        var page = await SignedInAsync("betaUser");

        try
        {
            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/Media");

            var folders = await VisibleFoldersAsync(page);

            Assert.Contains("beta", folders);
            Assert.Contains("gamma", folders);
            Assert.DoesNotContain("alpha", folders);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Reads the folders the signed-in user is shown, which is the question these tests ask.
    /// </summary>
    private async Task<string[]> VisibleFoldersAsync(IPage page)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        return await ReadRootFoldersAsync(page);
    }

    /// <summary>
    /// Asks the media API what the signed-in user may see at the root.
    /// </summary>
    /// <remarks>
    /// Asked of the API rather than scraped from the tree, because the tree is rendered by a script and
    /// what it draws is exactly what this returns.
    /// </remarks>
    private async Task<string[]> ReadRootFoldersAsync(IPage page)
    {
        var response = await page.APIRequest.GetAsync(
            $"{Fixture.BaseUrl}/{Tenant.Prefix}/api/media/GetFolders?path=");

        Assert.True(response.Ok, $"The media API answered {response.Status}.");

        var body = await response.JsonAsync();

        // The endpoint answers with a page of folders rather than a bare list.
        return [.. body.Value.GetProperty("items").EnumerateArray()
            .Select(entry => entry.GetProperty("name").GetString())];
    }

    /// <summary>
    /// Signs in one of the recipe's editors, who are all given the same password by it.
    /// </summary>
    private async Task<IPage> SignedInAsync(string userName)
    {
        var page = await Fixture.CreatePageAsync();

        await page.GotoAsync($"{Fixture.BaseUrl}/{Tenant.Prefix}/Login");
        await page.Locator("#LoginForm_UserName").FillAsync(userName);
        await page.Locator("#LoginForm_Password").FillAsync("Password1!");
        await page.Locator("form").Locator("button[type='submit']").First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        return page;
    }
}
