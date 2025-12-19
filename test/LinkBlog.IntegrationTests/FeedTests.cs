using System.Net;
using System.Xml.Linq;
using LinkBlog.IntegrationTests.Infrastructure;

namespace LinkBlog.IntegrationTests;

/// <summary>
/// Integration tests for the Atom feed endpoint.
/// </summary>
public class FeedTests : LinkBlogIntegrationTestBase
{
    [Fact]
    public async Task AtomFeed_ReturnsXmlContent()
    {
        // Act
        var response = await WebClient.GetAsync("atom/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AtomFeed_ReturnsValidAtomXml()
    {
        // Act
        var response = await WebClient.GetAsync("/atom/all");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();

        // Parse XML to verify it's well-formed
        var xml = XDocument.Parse(content);
        Assert.NotNull(xml.Root);

        // Verify it's an Atom feed
        XNamespace atomNs = "http://www.w3.org/2005/Atom";
        Assert.Equal(atomNs + "feed", xml.Root.Name);
    }

    [Fact]
    public async Task AtomFeed_ContainsFeedMetadata()
    {
        // Act
        var response = await WebClient.GetAsync("/atom/all");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        var xml = XDocument.Parse(content);
        XNamespace atomNs = "http://www.w3.org/2005/Atom";

        // Check for required Atom elements
        var title = xml.Root?.Element(atomNs + "title");
        var id = xml.Root?.Element(atomNs + "id");
        var updated = xml.Root?.Element(atomNs + "updated");

        Assert.NotNull(title);
        Assert.NotNull(id);
        Assert.NotNull(updated);
    }

    [Fact]
    public async Task AtomFeed_EntriesHaveRequiredElements()
    {
        // Act
        var response = await WebClient.GetAsync("/atom/all");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        var xml = XDocument.Parse(content);
        XNamespace atomNs = "http://www.w3.org/2005/Atom";

        var entries = xml.Root?.Elements(atomNs + "entry").ToList();

        // If there are entries, verify they have required elements
        if (entries?.Count > 0)
        {
            foreach (var entry in entries)
            {
                Assert.NotNull(entry.Element(atomNs + "id"));
                Assert.NotNull(entry.Element(atomNs + "title"));
                Assert.NotNull(entry.Element(atomNs + "updated"));
                Assert.NotNull(entry.Element(atomNs + "content"));
            }
        }
    }
}
