using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkBlog.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for LinkBlog integration tests using Aspire testing infrastructure.
/// Launches the entire AppHost including PostgreSQL, Azure Storage, and the web service.
/// </summary>
public class LinkBlogIntegrationTestBase : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    protected DistributedApplication? App { get; private set; }
    protected HttpClient WebClient { get; private set; } = null!;

    /// <summary>
    /// Initializes the Aspire AppHost and waits for all services to be healthy.
    /// </summary>
    public async Task InitializeAsync()
    {
        var cancellationToken = new CancellationTokenSource(DefaultTimeout).Token;

        // Create the AppHost builder
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.LinkBlog_AppHost>(["--environment=Testing"], cancellationToken);

        // Configure logging to reduce noise
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Aspire.Hosting", LogLevel.Warning);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Warning);
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        });

        // Configure HTTP client with standard resilience
        // appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        // {
        //     clientBuilder.AddStandardResilienceHandler();
        // });

        // Build and start the application
        App = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await App.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Wait for web service to be healthy
        // (webfrontend waits for migrations to complete, so this implicitly ensures migrations are done)
        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("webfrontend", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Create HTTP client for the web frontend
        WebClient = App.CreateHttpClient("webfrontend");

        WebClient.Timeout = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Disposes the AppHost and HTTP client.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
        WebClient?.Dispose();
    }

    /// <summary>
    /// Helper to create a non-redirecting HTTP client for testing redirects.
    /// </summary>
    protected HttpClient CreateNonRedirectingClient()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };
        var client = new HttpClient(handler)
        {
            BaseAddress = WebClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(30)
        };
        return client;
    }
}
