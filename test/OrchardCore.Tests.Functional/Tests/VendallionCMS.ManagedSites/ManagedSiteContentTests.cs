using Microsoft.Playwright;
using OrchardCore.Tests.Functional.Helpers;
using OrchardCore.Tests.Functional.Tests.Cms;
using Xunit;

namespace OrchardCore.Tests.Functional.Tests.ManagedSites;

/// <summary>
/// Covers reaching a managed site's content: which one an editor ends up working on, and what they are
/// shown when they hold none.
///
/// The screens are rendered by the server, so these assertions are about what an editor actually gets.
/// Which managed site a request acts on is in the address, and naming one decides nothing: these tests
/// pin that by arriving at the same address with different clearance and expecting different answers.
/// </summary>
public sealed class ManagedSiteContentTests : CmsTestBase, IClassFixture<CmsSetupFixture>
{
    private const string ManagedSitesFeatureId = "VendallionCMS.ManagedSites";

    public ManagedSiteContentTests(CmsSetupFixture fixture) : base(fixture) { }

    protected override string RecipeName => "Blog";

    /// <summary>
    /// Holding the permission is not holding clearance. An administrator who has never been granted a
    /// managed site must be told so, rather than shown an empty list they would read as "nothing to do".
    /// </summary>
    [Fact]
    public async Task Content_AdministratorHoldingNoClearance_IsToldSoRatherThanShownAnEmptyList()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");

            await GotoContentAsync(page);

            await Assertions.Expect(page.GetByText("You hold no clearance for any managed site."))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("ul.list-group li.list-group-item"))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Content_AnonymousVisitor_IsNotServedTheList()
    {
        var page = await CreateAuthenticatedPageAsync();
        await page.CloseAsync();

        var anonymousPage = await Fixture.CreatePageAsync();

        try
        {
            var response = await anonymousPage.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Content",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await anonymousPage.CloseAsync();
        }
    }

    /// <summary>
    /// Cleared for exactly one, there is nothing to choose, so the editor is working in it on arrival.
    /// </summary>
    [Fact]
    public async Task Content_ClearedForOneManagedSite_OpensInThatManagedSite()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");
            await GrantClearanceAsync(page, "Contoso");

            await GotoContentAsync(page);

            // A list of one is not a choice, so the root opens it.
            Assert.EndsWith("/Admin/ManagedSites/Content/contoso", page.Url, StringComparison.OrdinalIgnoreCase);
            await Assertions.Expect(page.GetByText("Editing Contoso")).ToBeVisibleAsync();

            // The Blog recipe's content carries no Managed Content, so there is nothing in scope yet.
            // The list says so in its own words rather than rendering an unexplained blank.
            await Assertions.Expect(page.GetByText("Nothing here!")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Cleared for several, nothing scoped is shown until the editor says which one they mean.
    /// </summary>
    [Fact]
    public async Task Content_ClearedForSeveralManagedSites_AsksWhichBeforeShowingAnything()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");
            await CreateManagedSiteAsync(page, "Fabrikam", "fabrikam");
            await GrantClearanceAsync(page, "Contoso", "Fabrikam");

            await GotoContentAsync(page);

            // Cleared for two, so the root of the area offers both rather than picking one.
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Work on Contoso" }))
                .ToBeVisibleAsync();

            await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Work on Fabrikam" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // The managed site is in the address, so it can be returned to directly.
            Assert.EndsWith("/Admin/ManagedSites/Content/fabrikam", page.Url, StringComparison.OrdinalIgnoreCase);
            await Assertions.Expect(page.GetByText("Editing Fabrikam")).ToBeVisibleAsync();

            await GotoContentAsync(page, "Fabrikam");
            await Assertions.Expect(page.GetByText("Editing Fabrikam")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// The address names the managed site, and anyone can type one, so naming it decides nothing.
    /// </summary>
    [Fact]
    public async Task Content_AManagedSiteTheEditorHoldsNoClearanceFor_IsRefused()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");
            await CreateManagedSiteAsync(page, "Fabrikam", "fabrikam");
            await GrantClearanceAsync(page, "Contoso");

            var response = await page.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Content/fabrikam",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Clearance is granted from the managed site it covers, so the screen has to say who can work on
    /// the site and who cannot, and move a user between the two.
    /// </summary>
    [Fact]
    public async Task Access_AuthorizedAdministrator_MovesAUserBetweenTheTwoLists()
    {
        var page = await CreateAuthenticatedPageAsync();
        var administrator = TestUtils.DefaultConfig.Username;

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");
            await GotoAccessAsync(page, "Contoso");

            await Assertions.Expect(page.GetByText("Nobody can work on this managed site yet."))
                .ToBeVisibleAsync();

            await page.GetByRole(AriaRole.Button,
                new PageGetByRoleOptions { Name = $"Give {administrator} access" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Now on the other side, with what they may do offered a scope at a time.
            var cleared = page.Locator(".card", new PageLocatorOptions { HasText = "Users with access" });
            await Assertions.Expect(cleared.GetByText(administrator).First).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Checkbox,
                new PageGetByRoleOptions { Name = "publish" }).First).ToBeVisibleAsync();

            // And the clearance is real: the content opens in the site it was granted for.
            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Content");
            await Assertions.Expect(page.GetByText("Editing Contoso")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Deciding who may work on a managed site is the same kind of decision as deciding what the site
    /// is, so it takes the permission that governs managed sites rather than the one that edits their
    /// content.
    /// </summary>
    [Fact]
    public async Task Access_AnonymousVisitor_IsNotServedTheScreen()
    {
        var page = await CreateAuthenticatedPageAsync();
        await CreateManagedSiteAsync(page, "Contoso", "contoso");
        var id = await ReadManagedSiteCodeAsync(page, "Contoso");
        await page.CloseAsync();

        var anonymousPage = await Fixture.CreatePageAsync();

        try
        {
            var response = await anonymousPage.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Access/{id}",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await anonymousPage.CloseAsync();
        }
    }

    private Task GotoContentAsync(IPage page)
        => page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Content");

    private Task GotoContentAsync(IPage page, string managedSiteName)
        => page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Content/{managedSiteName.ToLowerInvariant()}");

    /// <summary>
    /// Grants the signed-in administrator view and edit clearance for each named managed site, through
    /// the same editor an administrator would use.
    /// </summary>
    /// <summary>
    /// Gives the signed-in administrator the run of each named managed site, from the screen an
    /// administrator would use: the managed site's own.
    /// </summary>
    private async Task GrantClearanceAsync(IPage page, params string[] managedSiteNames)
    {
        foreach (var name in managedSiteNames)
        {
            await GotoAccessAsync(page, name);

            await page.GetByRole(AriaRole.Button,
                new PageGetByRoleOptions { Name = $"Give {TestUtils.DefaultConfig.Username} access" }).ClickAsync();

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    private async Task GotoAccessAsync(IPage page, string managedSiteName)
        => await page.GotoAndAssertOkAsync(
            $"/{Tenant.Prefix}/Admin/ManagedSites/Access/{await ReadManagedSiteCodeAsync(page, managedSiteName)}");

    private async Task CreateManagedSiteAsync(IPage page, string name, string urlPrefix)
    {
        await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Index");
        await page.ClickCreateAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The code names the managed site everywhere else and is asked for only here.
        await page.Locator("#Code").FillAsync(name.ToLowerInvariant().Replace(" ", string.Empty));
        await page.Locator("#Name").FillAsync(name);
        await page.Locator("#UrlPrefix").FillAsync(urlPrefix);

        // Enabled is the status a new managed site opens with, so it is left alone.
        await page.ClickSaveAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Reads a managed site's generated identifier out of its row, since the admin assigns one rather
    /// than deriving it from the name.
    /// </summary>
    private async Task<string> ReadManagedSiteCodeAsync(IPage page, string name)
    {
        await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Index");

        var href = await page.Locator("li.list-group-item", new PageLocatorOptions { HasText = name })
            .First
            .Locator("a[href*='ManagedSites/Edit/']")
            .First
            .GetAttributeAsync("href");

        return href[(href.LastIndexOf('/') + 1)..];
    }

    private async Task<IPage> CreateAuthenticatedPageAsync()
    {
        var page = await Fixture.CreatePageAsync();
        await AuthHelper.LoginAsync(page, $"/{Tenant.Prefix}");
        await page.EnableFeatureAsync($"/{Tenant.Prefix}", ManagedSitesFeatureId);

        return page;
    }
}
