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

            await Assertions.Expect(page.GetByText("Alpha Editor").First).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("alphaUser").First).ToBeVisibleAsync();
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

            await Assertions.Expect(page.GetByText("Gamma Readers").First).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("betaUser").First).ToBeVisibleAsync();
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

            // The admin writes a btn anchor with role="button", so that is what it is exposed as.
            await candidate.GetByRole(AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Give strandedUser access to this managed site" })
                .ClickAsync();
            await administrator.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(administrator.GetByText("can now work on this managed site").First)
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
