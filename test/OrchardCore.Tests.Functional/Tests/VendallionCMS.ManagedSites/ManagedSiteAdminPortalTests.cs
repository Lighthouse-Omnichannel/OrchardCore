using System.Text.Json;
using Microsoft.Playwright;
using OrchardCore.Tests.Functional.Helpers;
using OrchardCore.Tests.Functional.Tests.Cms;
using Xunit;

namespace OrchardCore.Tests.Functional.Tests.ManagedSites;

/// <summary>
/// Covers the Managed Site Admin Portal host page and the scope endpoints the client calls on mount.
///
/// The assertions stay on server-observable behavior so they hold whether or not the portal asset
/// bundle has been built. Clearance is carried by claims on the authenticated principal, so an
/// administrator without Managed Site clearance must be able to open the portal shell while being
/// denied every scoped action.
/// </summary>
public sealed class ManagedSiteAdminPortalTests : CmsTestBase, IClassFixture<CmsSetupFixture>
{
    private const string AdminPortalFeatureId = "VendallionCMS.ManagedSites.AdminPortal";

    public ManagedSiteAdminPortalTests(CmsSetupFixture fixture) : base(fixture) { }

    protected override string RecipeName => "Blog";

    [Fact]
    public async Task Portal_AuthenticatedAdministrator_RendersHostPageWithBootstrap()
    {
        var page = await CreateAuthenticatedPortalPageAsync();

        try
        {
            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Portal");

            await Assertions.Expect(page.Locator("#managed-site-admin-root")).ToHaveCountAsync(1);

            var bootstrap = await ReadBootstrapAsync(page);
            Assert.Contains("/api/managed-sites", bootstrap.ApiBaseUrl);
            Assert.False(string.IsNullOrWhiteSpace(bootstrap.AntiforgeryToken));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Portal_AnonymousVisitor_IsNotServedTheHostPage()
    {
        var page = await CreateAuthenticatedPortalPageAsync();
        await page.CloseAsync();

        var anonymousPage = await Fixture.CreatePageAsync();

        try
        {
            var response = await anonymousPage.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/Admin/ManagedSites/Portal",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.NotEqual(200, response.Status);
        }
        finally
        {
            await anonymousPage.CloseAsync();
        }
    }

    [Fact]
    public async Task AuthorizedEndpoint_AdministratorWithoutClearance_ReturnsEmptyList()
    {
        var page = await CreateAuthenticatedPortalPageAsync();

        try
        {
            var response = await page.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/api/managed-sites/authorized",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.Equal(200, response.Status);

            var body = await response.TextAsync();
            Assert.Contains("\"items\":[]", body.Replace(" ", string.Empty));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionEndpoint_AdministratorWithoutClearance_DeniesScopedAccess()
    {
        var page = await CreateAuthenticatedPortalPageAsync();

        try
        {
            var response = await page.APIRequest.GetAsync(
                $"{Fixture.BaseUrl}/{Tenant.Prefix}/api/managed-sites/session",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.Equal(403, response.Status);
            Assert.Contains("managed-sites.no-clearance", await response.TextAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionEndpoint_SelectingAnUnclearedManagedSite_IsRejected()
    {
        var page = await CreateAuthenticatedPortalPageAsync();

        try
        {
            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/ManagedSites/Portal");
            var bootstrap = await ReadBootstrapAsync(page);

            var response = await PostSessionAsync(page, bootstrap.AntiforgeryToken, "not-cleared");

            var body = await response.TextAsync();
            Assert.True(response.Status == 403, $"Expected 403 but got {response.Status}: {body}");
            Assert.Contains("managed-sites.no-clearance", body);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// The portal API accepts the admin cookie, so a state-changing call from an admin session without
    /// the antiforgery token must be rejected rather than acted on.
    /// </summary>
    [Fact]
    public async Task SessionEndpoint_CookieSessionWithoutAntiforgeryToken_IsRejected()
    {
        var page = await CreateAuthenticatedPortalPageAsync();

        try
        {
            var response = await PostSessionAsync(page, antiforgeryToken: null, managedSiteId: "not-cleared");

            Assert.Equal(400, response.Status);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Clearance is granted on the user entity and carried into the signed principal as claims, so the
    /// grant editor must be reachable from the standard user admin screen.
    /// </summary>
    [Fact]
    public async Task UserEditor_AuthorizedAdministrator_ShowsManagedSiteClearanceEditor()
    {
        var page = await CreateAuthenticatedPortalPageAsync();

        try
        {
            await UserHelper.CreateUserAsync(
                page,
                $"/{Tenant.Prefix}",
                "managed-site-editor",
                "managed-site-editor@example.com",
                "Orchard1!");

            await page.GotoAndAssertOkAsync($"/{Tenant.Prefix}/Admin/Users/Index");

            // The list markup is owned by the Users module, so the edit target is taken from the first
            // edit link rather than from an assumed row structure.
            var editLink = page.Locator("a[href*='Users/Edit']").First;
            await editLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

            var editHref = await editLink.GetAttributeAsync("href");
            await page.GotoAndAssertOkAsync(editHref);

            await Assertions.Expect(page.Locator("legend", new PageLocatorOptions { HasText = "Managed Sites" }).First)
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("No managed sites have been defined yet."))
                .ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private Task<IAPIResponse> PostSessionAsync(IPage page, string antiforgeryToken, string managedSiteId)
    {
        var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            headers["RequestVerificationToken"] = antiforgeryToken;
        }

        return page.APIRequest.PostAsync(
            $"{Fixture.BaseUrl}/{Tenant.Prefix}/api/managed-sites/session",
            new APIRequestContextOptions
            {
                MaxRedirects = 0,
                Headers = headers,
                DataObject = new { managedSiteId },
            });
    }

    private static async Task<PortalBootstrap> ReadBootstrapAsync(IPage page)
    {
        var json = await page.Locator("#managed-site-portal-bootstrap").InnerTextAsync();

        return JsonSerializer.Deserialize<PortalBootstrap>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task<IPage> CreateAuthenticatedPortalPageAsync()
    {
        var page = await Fixture.CreatePageAsync();
        await AuthHelper.LoginAsync(page, $"/{Tenant.Prefix}");
        await page.EnableFeatureAsync($"/{Tenant.Prefix}", AdminPortalFeatureId);

        return page;
    }

    private sealed class PortalBootstrap
    {
        public string ApiBaseUrl { get; set; }

        public string AntiforgeryToken { get; set; }
    }
}
