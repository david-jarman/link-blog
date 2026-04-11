using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;

namespace LinkBlog.Playwright.Tests;

public class HomePageTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl = "";

    public async Task InitializeAsync()
    {
        // Bypass GitHub OAuth for admin pages in test environment
        Environment.SetEnvironmentVariable("DisableAdminAuth", "true");

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LinkBlog_AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        var webResource = appHost.Resources.First(r=>r.Name == "webfrontend");
        var endpoint = webResource.Annotations.OfType<EndpointAnnotation>().First(x => x.Name == "http");

        _baseUrl = endpoint.AllocatedEndpoint!.UriString;

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
    }

    [Fact]
    public async Task HomePage_LoadsCorrectly()
    {
        BrowserNewPageOptions options = new()
        {
            IgnoreHTTPSErrors = true
        };
        var page = await _browser!.NewPageAsync(options);
        var response = await page.GotoAsync(_baseUrl);

        Assert.NotNull(response);
        Assert.True(response.Ok, $"Expected OK response but got {response.Status}");

        await Assertions.Expect(page).ToHaveTitleAsync("David Jarman's Blog");

        var nav = page.Locator("nav.page-links");
        await Assertions.Expect(nav).ToBeVisibleAsync();
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Blogroll" })).ToBeVisibleAsync();
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "RSS" })).ToBeVisibleAsync();
    }
}