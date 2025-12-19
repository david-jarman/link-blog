using System.Net;
using LinkBlog.IntegrationTests.Infrastructure;

namespace LinkBlog.IntegrationTests;

/// <summary>
/// Integration tests for date-based archive functionality.
/// </summary>
public class ArchiveTests : LinkBlogIntegrationTestBase
{
    [Fact(Skip = "yep")]
    public async Task Archive_ByYear_ReturnsSuccess()
    {
        // Act - test current year
        var currentYear = DateTime.Now.Year;
        var response = await WebClient.GetAsync($"/archive/{currentYear}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "yep")]
    public async Task Archive_ByYearMonth_ReturnsSuccess()
    {
        // Act
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;
        var response = await WebClient.GetAsync($"/archive/{currentYear}/{currentMonth:D2}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "yep")]
    public async Task Archive_ByYearMonthDay_ReturnsSuccess()
    {
        // Act
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;
        var currentDay = DateTime.Now.Day;
        var response = await WebClient.GetAsync($"/archive/{currentYear}/{currentMonth:D2}/{currentDay:D2}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "yep")]
    public async Task Archive_ReturnsHtmlContent()
    {
        // Act
        var currentYear = DateTime.Now.Year;
        var response = await WebClient.GetAsync($"/archive/{currentYear}");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact(Skip = "yep")]
    public async Task Archive_WithInvalidYear_IsHandled()
    {
        // Act - test with invalid year
        var response = await WebClient.GetAsync("/archive/999");

        // Assert - should handle gracefully
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK/NotFound/BadRequest for invalid year, got {response.StatusCode}");
    }

    [Fact(Skip = "yep")]
    public async Task Archive_WithInvalidMonth_IsHandled()
    {
        // Act - test with invalid month
        var currentYear = DateTime.Now.Year;
        var response = await WebClient.GetAsync($"/archive/{currentYear}/13");

        // Assert - should handle gracefully
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK/NotFound/BadRequest for invalid month, got {response.StatusCode}");
    }

    [Fact(Skip = "yep")]
    public async Task Archive_WithInvalidDay_IsHandled()
    {
        // Act - test with invalid day
        var currentYear = DateTime.Now.Year;
        var response = await WebClient.GetAsync($"/archive/{currentYear}/01/32");

        // Assert - should handle gracefully
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK/NotFound/BadRequest for invalid day, got {response.StatusCode}");
    }

    [Fact(Skip = "yep")]
    public async Task Archive_PageStructure_IsValid()
    {
        // Act
        var currentYear = DateTime.Now.Year;
        var response = await WebClient.GetAsync($"/archive/{currentYear}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify it's a valid HTML page
        Assert.Contains("<!DOCTYPE html>", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<html", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "yep")]
    public async Task Archive_FutureDate_ReturnsSuccess()
    {
        // Act - test future date (should be empty but valid)
        var futureYear = DateTime.Now.Year + 10;
        var response = await WebClient.GetAsync($"/archive/{futureYear}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
