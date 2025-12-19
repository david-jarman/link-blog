using System.Net;
using LinkBlog.IntegrationTests.Infrastructure;

namespace LinkBlog.IntegrationTests;

/// <summary>
/// Integration tests for the homepage and main navigation.
/// </summary>
public class HomepageTests : LinkBlogIntegrationTestBase
{
    [Fact]
    public async Task Homepage_ReturnsSuccess()
    {
        // Act
        var response = await WebClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Homepage_ReturnsHtmlContent()
    {
        // Act
        var response = await WebClient.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Homepage_ContainsExpectedContent()
    {
        // Act
        var response = await WebClient.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify it's a valid HTML page
        Assert.Contains("<!DOCTYPE html>", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<html", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Homepage_HasOutputCaching()
    {
        // Act
        var response = await WebClient.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();

        // The homepage should have cache-control headers (60s cache as per Home.razor)
        Assert.True(response.Headers.Contains("Cache-Control") ||
                   response.Headers.Contains("cache-control"));
    }

    [Fact]
    public async Task SearchPage_ReturnsSuccess()
    {
        // Act
        var response = await WebClient.GetAsync("/search");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SearchPage_WithQuery_ReturnsSuccess()
    {
        // Act
        var response = await WebClient.GetAsync("/search?q=test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminPage_WithoutAuth_RequiresAuthentication()
    {
        // Act
        var response = await WebClient.GetAsync("/admin");

        // Assert
        // Should redirect to authentication or return unauthorized/forbidden
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden,
            $"Expected redirect or auth error, got {response.StatusCode}");
    }
}
