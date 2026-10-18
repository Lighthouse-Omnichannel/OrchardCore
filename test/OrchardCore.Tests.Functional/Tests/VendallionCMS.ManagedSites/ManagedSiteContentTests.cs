using Microsoft.Playwright;
using OrchardCore.Tests.Functional.Helpers;
using OrchardCore.Tests.Functional.Tests.Cms;

namespace OrchardCore.Tests.Functional.Tests.ManagedSites;

/// <summary>
/// Covers reaching a managed site's content: which one an editor ends up working on, and what they are
/// shown when they hold authority over none.
/// </summary>
/// <remarks>
/// Written against the development recipe, because what is under test is now decided by roles and the
/// recipe is where the roles are. Each editor it provisions holds one role per managed site they work
/// on, which is the arrangement every tenant will have.
///
/// Which managed site a request acts on is in the address, and naming one decides nothing: these tests
/// pin that by arriving at the same address as different people and expecting different answers.
/// </remarks>
public sealed class ManagedSiteContentTests : CmsTestBase, IClassFixture<CmsSetupFixture>
{
    public ManagedSiteContentTests(CmsSetupFixture fixture)
        : base(fixture)
    {
    }

    protected override string RecipeName => "ManagedSitesDevelopment";

    /// <summary>
    /// Somebody who may open the screens but holds no managed site is told so.
    /// </summary>
    [Fact]
    public async Task Content_HoldingNoManagedSite_IsToldSoRatherThanShownAnEmptyList()
    {
        // An empty list reads as "nothing to do here", which sends somebody looking for the wrong fault.
        var page = await SignedInAsync("strandedUser");

        try
        {
            await GotoContentAsync(page);

            await Assertions.Expect(page.GetByText("You hold no clearance for any managed site."))
                .ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// The screens are in the admin, so a visitor never reaches them at all.
    /// </summary>
    [Fact]
    public async Task Content_AnonymousVisitor_IsNotServedTheList()
    {
        var page = await Fixture.CreatePageAsync();

        try
        {
            var response = await page.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Content",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// An editor holding one managed site is taken to it: a list of one is not a choice.
    /// </summary>
    [Fact]
    public async Task Content_HoldingOneManagedSite_OpensInThatManagedSite()
    {
        var page = await SignedInAsync("alphaUser");

        try
        {
            await GotoContentAsync(page);

            Assert.EndsWith("/Admin/ManagedSites/Content/alpha", page.Url, StringComparison.OrdinalIgnoreCase);
            await Assertions.Expect(page.GetByText("Editing Alpha")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// An editor holding two is offered both, and opening one takes them to its own address.
    /// </summary>
    [Fact]
    public async Task Content_HoldingSeveralManagedSites_AsksWhichBeforeShowingAnything()
    {
        var page = await SignedInAsync("betaUser");

        try
        {
            await GotoContentAsync(page);

            await Assertions.Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Work on Beta" }))
                .ToBeVisibleAsync();

            await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Work on Gamma" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            Assert.EndsWith("/Admin/ManagedSites/Content/gamma", page.Url, StringComparison.OrdinalIgnoreCase);
            await Assertions.Expect(page.GetByText("Editing Gamma")).ToBeVisibleAsync();

            // The managed site is in the address, so it can be returned to directly.
            await GotoContentAsync(page, "gamma");
            await Assertions.Expect(page.GetByText("Editing Gamma")).ToBeVisibleAsync();
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
    public async Task Content_AManagedSiteTheEditorDoesNotHold_IsRefused()
    {
        var page = await SignedInAsync("alphaUser");

        try
        {
            var response = await page.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Content/beta",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Reading a managed site without being able to change it is a role holding only the floor.
    /// </summary>
    [Fact]
    public async Task Content_HoldingOnlyReadingOnAManagedSite_IsNotOfferedTheActionsThatChangeIt()
    {
        var page = await SignedInAsync("betaUser");

        try
        {
            // betaUser edits Beta and only reads Gamma, so the same screen differs between the two.
            await GotoContentAsync(page, "beta");
            await Assertions.Expect(page.Locator("a.create").First).ToBeVisibleAsync();

            await GotoContentAsync(page, "gamma");
            await Assertions.Expect(page.Locator("a.create")).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private Task GotoContentAsync(IPage page)
        => page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Content");

    private Task GotoContentAsync(IPage page, string managedSiteCode)
        => page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Content/{managedSiteCode}");

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
