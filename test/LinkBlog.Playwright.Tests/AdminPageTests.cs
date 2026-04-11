using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;

namespace LinkBlog.Playwright.Tests;

public class AdminPageTests : IAsyncLifetime
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
        Environment.SetEnvironmentVariable("DisableAdminAuth", null);
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
    }

    [Fact]
    public async Task AdminPage_CreatePost_AppearsOnHomePage()
    {
        // Use a unique short title per test run to avoid conflicts with existing data
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var postTitle = $"Playwright Test Post {timestamp}";
        var shortTitle = $"playwright-test-{timestamp}";
        const string postContent = "This is a test post created by Playwright.";
        const string postTags = "test";

        BrowserNewPageOptions options = new()
        {
            IgnoreHTTPSErrors = true
        };
        var page = await _browser!.NewPageAsync(options);

        // Navigate to the admin page
        await page.GotoAsync($"{_baseUrl}/admin");
        await Assertions.Expect(page).ToHaveTitleAsync("Admin");

        // Fill in the post form
        await page.Locator("#PostTitle").FillAsync(postTitle);
        await page.Locator("#ShortTitle").FillAsync(shortTitle);

        // Set the EasyMDE/CodeMirror editor content via the underlying CodeMirror API,
        // then dispatch a change event so EasyMDE syncs the value to the hidden textarea
        await page.Locator(".CodeMirror").WaitForAsync();
        await page.EvaluateAsync(@"(content) => {
            var cmElement = document.querySelector('.CodeMirror');
            var cm = cmElement.CodeMirror;
            cm.setValue(content);
            cm.getDoc().markClean();
        }", postContent);

        await page.Locator("#PostTags").FillAsync(postTags);

        // Submit the form
        await page.Locator("button.btn-primary[type=submit]").ClickAsync();

        // Wait for the success confirmation
        await Assertions.Expect(page.Locator(".alert-success")).ToBeVisibleAsync();

        // Navigate to the home page and verify the new post appears
        await page.GotoAsync(_baseUrl);
        await Assertions.Expect(
            page.GetByRole(AriaRole.Heading, new() { Name = postTitle })
        ).ToBeVisibleAsync();
    }
}