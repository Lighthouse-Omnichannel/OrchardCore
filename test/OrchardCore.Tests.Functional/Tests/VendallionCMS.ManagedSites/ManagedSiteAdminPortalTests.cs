using Microsoft.Playwright;
using OrchardCore.Tests.Functional.Helpers;
using OrchardCore.Tests.Functional.Tests.Cms;
using Xunit;

namespace OrchardCore.Tests.Functional.Tests.ManagedSites;

/// <summary>
/// Covers the Managed Site Admin Portal: which managed site an editor ends up working in, and what they
/// are shown when the answer is none or not yet decided.
///
/// The portal is rendered by the server, so these assertions are about the screens an editor actually
/// gets. Which managed site a request acts on is never taken from the URL; it comes from the portal
/// session, and these tests pin that by arriving at the same address with different clearance and
/// expecting different screens.
/// </summary>
public sealed class ManagedSiteAdminPortalTests : CmsTestBase, IClassFixture<CmsSetupFixture>
{
    private const string ManagedSitesFeatureId = "VendallionCMS.ManagedSites";

    public ManagedSiteAdminPortalTests(CmsSetupFixture fixture) : base(fixture) { }

    protected override string RecipeName => "Blog";

    /// <summary>
    /// Holding the permission is not holding clearance. An administrator who has never been granted a
    /// managed site must be told so, rather than shown an empty list they would read as "nothing to do".
    /// </summary>
    [Fact]
    public async Task Portal_AdministratorHoldingNoClearance_IsToldSoRatherThanShownAnEmptyList()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");

            await GotoPortalAsync(page);

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
    public async Task Portal_AnonymousVisitor_IsNotServedTheList()
    {
        var page = await CreateAuthenticatedPageAsync();
        await page.CloseAsync();

        var anonymousPage = await Fixture.CreatePageAsync();

        try
        {
            var response = await anonymousPage.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Portal/Index",
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
    public async Task Portal_ClearedForOneManagedSite_OpensInThatManagedSite()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");
            await GrantClearanceAsync(page, "Contoso");

            await GotoPortalAsync(page);

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
    public async Task Portal_ClearedForSeveralManagedSites_AsksWhichBeforeShowingAnything()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");
            await CreateManagedSiteAsync(page, "Fabrikam", "fabrikam");
            await GrantClearanceAsync(page, "Contoso", "Fabrikam");

            await GotoPortalAsync(page);

            // Asking for the list lands on the chooser instead.
            Assert.Contains("ManagedSites/Portal/Select", page.Url, StringComparison.OrdinalIgnoreCase);
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Contoso" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Fabrikam" }))
                .ToBeVisibleAsync();

            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Fabrikam" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.GetByText("Editing Fabrikam")).ToBeVisibleAsync();

            // And the choice is the session's, so it survives leaving the portal and coming back.
            await GotoPortalAsync(page);
            await Assertions.Expect(page.GetByText("Editing Fabrikam")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// The chooser only offers managed sites the editor is cleared for, so asking for another one means
    /// asking directly. Clearance is checked when the choice is made, not only when it is offered.
    /// </summary>
    [Fact]
    public async Task Select_AManagedSiteTheEditorHoldsNoClearanceFor_IsRefused()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");
            await CreateManagedSiteAsync(page, "Fabrikam", "fabrikam");
            await GrantClearanceAsync(page, "Contoso");

            var unclearedId = await ReadManagedSiteIdAsync(page, "Fabrikam");

            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Portal/Select");

            var response = await PostSelectAsync(page, unclearedId, await ReadAntiforgeryTokenAsync(page));

            // Refused back to the chooser rather than acted on, and the session is left where it was.
            Assert.Contains("ManagedSites/Portal/Select", response.Url, StringComparison.OrdinalIgnoreCase);

            await GotoPortalAsync(page);
            await Assertions.Expect(page.GetByText("Editing Fabrikam")).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Choosing a managed site changes what every later request in the session acts on, so it is a state
    /// change and must carry the token, whatever the admin cookie says.
    /// </summary>
    [Fact]
    public async Task Select_WithoutAnAntiforgeryToken_IsRejected()
    {
        var page = await CreateAuthenticatedPageAsync();

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");
            await GrantClearanceAsync(page, "Contoso");

            var contosoId = await ReadManagedSiteIdAsync(page, "Contoso");

            var response = await PostSelectAsync(page, contosoId, antiforgeryToken: null);

            Assert.Equal(400, response.Status);
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
    public async Task Security_AuthorizedAdministrator_MovesAUserBetweenTheTwoLists()
    {
        var page = await CreateAuthenticatedPageAsync();
        var administrator = TestUtils.DefaultConfig.Username;

        try
        {
            await CreateManagedSiteAsync(page, "Contoso", "contoso");
            await GotoSecurityAsync(page, "Contoso");

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

            // And the clearance is real: the portal opens in the site it was granted for.
            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Portal/Index");
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
    public async Task Security_AnonymousVisitor_IsNotServedTheScreen()
    {
        var page = await CreateAuthenticatedPageAsync();
        await CreateManagedSiteAsync(page, "Contoso", "contoso");
        var id = await ReadManagedSiteIdAsync(page, "Contoso");
        await page.CloseAsync();

        var anonymousPage = await Fixture.CreatePageAsync();

        try
        {
            var response = await anonymousPage.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Security/{id}",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await anonymousPage.CloseAsync();
        }
    }

    private Task GotoPortalAsync(IPage page)
        => page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Portal/Index");

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
            await GotoSecurityAsync(page, name);

            await page.GetByRole(AriaRole.Button,
                new PageGetByRoleOptions { Name = $"Give {TestUtils.DefaultConfig.Username} access" }).ClickAsync();

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    private async Task GotoSecurityAsync(IPage page, string managedSiteName)
        => await page.GotoAndAssertOkAsync(
            $"/{Tenant.Prefix}/Admin/ManagedSites/Security/{await ReadManagedSiteIdAsync(page, managedSiteName)}");

    private async Task CreateManagedSiteAsync(IPage page, string name, string urlPrefix)
    {
        await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Index");
        await page.ClickCreateAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
    private async Task<string> ReadManagedSiteIdAsync(IPage page, string name)
    {
        await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Index");

        var href = await page.Locator("tr", new PageLocatorOptions { HasText = name })
            .First
            .Locator("a[href*='ManagedSites/Edit/']")
            .First
            .GetAttributeAsync("href");

        return href[(href.LastIndexOf('/') + 1)..];
    }

    private static async Task<string> ReadAntiforgeryTokenAsync(IPage page)
        => await page.Locator("input[name='__RequestVerificationToken']").First.GetAttributeAsync("value");

    private Task<IAPIResponse> PostSelectAsync(IPage page, string managedSiteId, string antiforgeryToken)
    {
        var form = $"managedSiteId={Uri.EscapeDataString(managedSiteId)}";

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            form += $"&__RequestVerificationToken={Uri.EscapeDataString(antiforgeryToken)}";
        }

        return page.APIRequest.PostAsync(
            $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Portal/Select",
            new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/x-www-form-urlencoded",
                },
                Data = form,
            });
    }

    private async Task<IPage> CreateAuthenticatedPageAsync()
    {
        var page = await Fixture.CreatePageAsync();
        await AuthHelper.LoginAsync(page, $"/{Tenant.Prefix}");
        await page.EnableFeatureAsync($"/{Tenant.Prefix}", ManagedSitesFeatureId);

        return page;
    }
}
