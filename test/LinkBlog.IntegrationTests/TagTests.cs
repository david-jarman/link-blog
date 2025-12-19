using System.Net;
using LinkBlog.IntegrationTests.Infrastructure;

namespace LinkBlog.IntegrationTests;

/// <summary>
/// Integration tests for tag-based post retrieval.
/// </summary>
public class TagTests : LinkBlogIntegrationTestBase
{
    [Fact]
    public async Task TagPage_ReturnsSuccess()
    {
        // Act - use a common tag that likely exists
        var response = await WebClient.GetAsync("/tags/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TagPage_ReturnsHtmlContent()
    {
        // Act
        var response = await WebClient.GetAsync("/tags/dotnet");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TagPage_HasCacheHeaders()
    {
        // Act
        var response = await WebClient.GetAsync("/tags/csharp");

        // Assert
        response.EnsureSuccessStatusCode();

        // Tag pages have 5s cache as per PostsForTag.razor
        Assert.True(response.Headers.Contains("Cache-Control") ||
                   response.Headers.Contains("cache-control"));
    }

    [Fact]
    public async Task TagPage_WithSpecialCharacters_IsHandled()
    {
        // Act - test URL encoding
        var response = await WebClient.GetAsync("/tags/c%23"); // c# encoded

        // Assert
        // Should either return success or not found, but not error
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound, got {response.StatusCode}");
    }

    [Fact]
    public async Task TagPage_ContainsExpectedStructure()
    {
        // Act
        var response = await WebClient.GetAsync("/tags/dotnet");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify it's a valid HTML page
        Assert.Contains("<!DOCTYPE html>", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<html", content, StringComparison.OrdinalIgnoreCase);
    }
}
