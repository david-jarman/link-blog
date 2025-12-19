using System.Net;
using System.Web;
using LinkBlog.IntegrationTests.Infrastructure;

namespace LinkBlog.IntegrationTests;

/// <summary>
/// Integration tests for full-text search functionality.
/// </summary>
public class SearchTests : LinkBlogIntegrationTestBase
{
    [Fact]
    public async Task Search_WithoutQuery_ReturnsSuccess()
    {
        // Act
        var response = await WebClient.GetAsync("/search");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithQuery_ReturnsSuccess()
    {
        // Act
        var response = await WebClient.GetAsync("/search?q=test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsHtmlContent()
    {
        // Act
        var response = await WebClient.GetAsync("/search?q=programming");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Search_WithSpecialCharacters_IsHandled()
    {
        // Act - test various special characters that might break search
        var queries = new[] { "c#", "test & demo", "hello \"world\"", "a+b" };

        foreach (var query in queries)
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var response = await WebClient.GetAsync($"/search?q={encodedQuery}");

            // Assert - should handle gracefully without server error
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsSuccess()
    {
        // Act
        var response = await WebClient.GetAsync("/search?q=");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithLongQuery_IsHandled()
    {
        // Act - test with a very long search query
        var longQuery = new string('a', 1000);
        var response = await WebClient.GetAsync($"/search?q={longQuery}");

        // Assert - should handle gracefully
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK or BadRequest for long query, got {response.StatusCode}");
    }

    [Fact]
    public async Task Search_PageStructure_IsValid()
    {
        // Act
        var response = await WebClient.GetAsync("/search?q=test");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify it's a valid HTML page
        Assert.Contains("<!DOCTYPE html>", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<html", content, StringComparison.OrdinalIgnoreCase);
    }
}
