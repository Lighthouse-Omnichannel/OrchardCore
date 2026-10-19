using Microsoft.Playwright;
using OrchardCore.Tests.Functional.Helpers;
using OrchardCore.Tests.Functional.Tests.Cms;

namespace OrchardCore.Tests.Functional.Tests.ManagedSites;

/// <summary>
/// Covers giving somebody a managed site, which is the step every other screen depends on.
/// </summary>
/// <remarks>
/// Authority over a managed site is a permission and permissions are held by roles, so a role has to
/// exist before anybody can be given anything. The feature makes one with the site for exactly that
/// reason, and these assertions are about that role being real — present in the roles screen, holding
/// what it should, and reachable from the managed site rather than only from Security.
/// </remarks>
public sealed class ManagedSiteAccessTests : CmsTestBase, IClassFixture<CmsSetupFixture>
{
    public ManagedSiteAccessTests(CmsSetupFixture fixture)
        : base(fixture)
    {
    }

    protected override string RecipeName => "ManagedSitesDevelopment";

    /// <summary>
    /// Defining a managed site makes the role that carries it.
    /// </summary>
    [Fact]
    public async Task DefiningAManagedSite_MakesTheRoleThatCarriesIt()
    {
        var page = await AdministratorAsync();

        try
        {
            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/Roles/Index");

            // The recipe's three managed sites each made one as they were defined.
            foreach (var roleName in new[] { "Alpha Editor", "Beta Editor", "Gamma Editor" })
            {
                await Assertions.Expect(page.GetByText(roleName, new PageGetByTextOptions { Exact = true }).First)
                    .ToBeVisibleAsync();
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// That role is listed against the managed site, because it is the one that carries it.
    /// </summary>
    [Fact]
    public async Task TheAccessScreen_ListsTheRolesCarryingTheManagedSite()
    {
        var page = await AdministratorAsync();

        try
        {
            await GotoAccessAsync(page, "alpha");

            await Assertions.Expect(RolesCard(page).GetByText("Alpha Editor").First).ToBeVisibleAsync();
            await Assertions.Expect(RolesCard(page).GetByText("alphaUser").First).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// A role somebody made by hand is listed too, because roles are found by what they hold.
    /// </summary>
    [Fact]
    public async Task TheAccessScreen_ListsARoleAssembledByHand()
    {
        var page = await AdministratorAsync();

        try
        {
            // The recipe declares this one itself, holding only Gamma's reading permission.
            await GotoAccessAsync(page, "gamma");

            await Assertions.Expect(RolesCard(page).GetByText("Gamma Readers").First).ToBeVisibleAsync();
            await Assertions.Expect(RolesCard(page).GetByText("betaUser").First).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Giving somebody access puts them in the role, and they can then open the managed site.
    /// </summary>
    [Fact]
    public async Task GivingAccess_PutsThemInTheRoleAndOpensTheManagedSite()
    {
        var administrator = await AdministratorAsync();

        try
        {
            // strandedUser holds the screens and no managed site, which is the state this fixes.
            await GotoAccessAsync(administrator, "alpha");

            var candidate = administrator.Locator("li.list-group-item",
                new PageLocatorOptions { HasText = "strandedUser" }).First;

            await Assertions.Expect(candidate).ToBeVisibleAsync();

            await candidate.GetByRole(AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Give strandedUser access to this managed site" })
                .ClickAsync();
            await administrator.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(administrator.GetByText("now holds Alpha Editor").First)
                .ToBeVisibleAsync();

            // And it is real: they reach Alpha, which they could not before.
            var editor = await SignedInAsync("strandedUser");

            try
            {
                await editor.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Content/alpha");
                await Assertions.Expect(editor.GetByText("Editing Alpha")).ToBeVisibleAsync();
            }
            finally
            {
                await editor.CloseAsync();
            }
        }
        finally
        {
            await administrator.CloseAsync();
        }
    }

    /// <summary>
    /// The screen is governed, so a visitor never reaches it.
    /// </summary>
    [Fact]
    public async Task TheAccessScreen_AnonymousVisitor_IsNotServedIt()
    {
        var page = await Fixture.CreatePageAsync();

        try
        {
            var response = await page.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Access/alpha",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// And an editor who merely works on the site cannot decide who else does.
    /// </summary>
    [Fact]
    public async Task TheAccessScreen_AnEditorOfTheManagedSite_IsRefused()
    {
        var page = await SignedInAsync("alphaUser");

        try
        {
            var response = await page.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Access/alpha",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// One role carrying the site is not a choice, so the screen does not ask.
    /// </summary>
    [Fact]
    public async Task TheAccessScreen_WhenOneRoleCarriesTheSite_DoesNotAskWhich()
    {
        var page = await AdministratorAsync();

        try
        {
            await GotoAccessAsync(page, "alpha");

            await Assertions.Expect(page.Locator("#roleName")).ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByText("Adds them to Alpha Editor")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Several carrying it is a decision, so the screen asks before it acts.
    /// </summary>
    [Fact]
    public async Task TheAccessScreen_WhenSeveralRolesCarryTheSite_AsksWhich()
    {
        var page = await AdministratorAsync();

        try
        {
            // Gamma is carried by the role made with it and by the reading role the recipe declares.
            await GotoAccessAsync(page, "gamma");

            var options = await page.Locator("#roleName option").AllTextContentsAsync();

            Assert.Contains("Gamma Editor", options.Select(option => option.Trim()));
            Assert.Contains("Gamma Readers", options.Select(option => option.Trim()));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Giving access puts somebody in the role that was chosen, not always the site's own.
    /// </summary>
    [Fact]
    public async Task GivingAccess_UsesTheRoleThatWasChosen()
    {
        var page = await AdministratorAsync();

        try
        {
            await GotoAccessAsync(page, "gamma");

            await page.Locator("#roleName").SelectOptionAsync("Gamma Readers");

            var candidate = page.Locator("li.list-group-item",
                new PageLocatorOptions { HasText = "alphaUser" }).First;

            await candidate.GetByRole(AriaRole.Button).First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.GetByText("now holds Gamma Readers")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// The role arrives from a form, so a role that does not carry the site is refused.
    /// </summary>
    /// <remarks>
    /// Without this the screen would add anybody to any role named in a request, which is the
    /// permission governing role assignment gone round rather than asked. The form is tampered with
    /// here because that is what an attacker would do; the page itself never offers the option.
    /// </remarks>
    [Fact]
    public async Task GivingAccess_ToARoleThatDoesNotCarryTheSite_IsRefused()
    {
        var page = await AdministratorAsync();

        try
        {
            await GotoAccessAsync(page, "gamma");

            await page.EvaluateAsync(
                @"() => {
                    const select = document.querySelector('#roleName');
                    const option = document.createElement('option');
                    option.value = 'Administrator';
                    option.textContent = 'Administrator';
                    select.appendChild(option);
                    select.value = 'Administrator';
                }");

            var candidate = page.Locator("li.list-group-item",
                new PageLocatorOptions { HasText = "alphaUser" }).First;

            var response = await page.RunAndWaitForResponseAsync(
                async () => await candidate.GetByRole(AriaRole.Button).First.ClickAsync(),
                response => response.Url.Contains("GiveAccess", StringComparison.OrdinalIgnoreCase));

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// A managed site whose role is not there can still be given to somebody.
    /// </summary>
    /// <remarks>
    /// This is the state every managed site defined before the feature made roles for them is in, and
    /// the state a deleted role puts one back into. It is worth its own test because the obvious
    /// implementation cannot work: a role is written to the roles document for update while reading
    /// the roles back reads the cached copy, which is not refreshed until the scope commits, so making
    /// the role and assigning it in one request throws "Role does not exist".
    /// </remarks>
    [Fact]
    public async Task GivingAccess_WhenTheSitesRoleWasDeleted_StillWorks()
    {
        var page = await AdministratorAsync();

        try
        {
            await DeleteRoleAsync(page, "Beta Editor");

            // Drawing the screen is what makes it again, a request before anybody presses anything.
            await GotoAccessAsync(page, "beta");

            var candidate = page.Locator("li.list-group-item",
                new PageLocatorOptions { HasText = "strandedUser" }).First;

            await candidate.GetByRole(AriaRole.Button).First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.GetByText("now holds Beta Editor")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Removes a role through the platform's own screen, as an administrator would.
    /// </summary>
    private async Task DeleteRoleAsync(IPage page, string roleName)
    {
        await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/Roles/Index");

        var row = page.Locator($"li.list-group-item[data-filter-value='{roleName}']").First;

        await row.Locator("button.dropdown-toggle").First.ClickAsync();
        await row.Locator("a:has-text('Delete')").First.ClickAsync();
        await page.ClickModalOkAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// The card listing what carries the managed site, as opposed to the picker opposite it.
    /// </summary>
    private static ILocator RolesCard(IPage page)
        => page.Locator(".card", new PageLocatorOptions { HasText = "Roles that carry this managed site" }).First;

    private Task GotoAccessAsync(IPage page, string managedSiteCode)
        => page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Access/{managedSiteCode}");

    private async Task<IPage> AdministratorAsync()
    {
        var page = await Fixture.CreatePageAsync();
        await AuthHelper.LoginAsync(page, $"/{Tenant.Prefix}");

        return page;
    }

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
