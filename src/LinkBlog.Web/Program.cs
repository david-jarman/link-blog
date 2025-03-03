using System.Security.Claims;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LinkBlog.Abstractions;
using LinkBlog.Data;
using LinkBlog.Data.Extensions;
using LinkBlog.Feed;
using LinkBlog.Web.Components;
using LinkBlog.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}
var config = builder.Configuration;

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GitHubAccountDefaults.AuthenticationScheme;
    })
    .AddGitHubAccount(options =>
    {
        options.ClientId = config["CLIENT_ID"] ?? throw new InvalidOperationException("GitHub:ClientId is required.");
        options.ClientSecret = config["CLIENT_SECRET"] ?? throw new InvalidOperationException("GitHub:ClientSecret is required.");
    })
    .AddCookie();
builder.Services.AddAuthorization(policy =>
{
    policy.AddPolicy("Admin", policy => policy.RequireClaim(ClaimTypes.NameIdentifier, AdminIdentifiers.DavidJarmanGitHubId));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddOutputCache();

var isHeroku = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DYNO"));
builder.AddPostStore("postgresdb", options =>
{
    if (isHeroku)
    {
        var match = Regex.Match(Environment.GetEnvironmentVariable("DATABASE_URL") ?? "", @"postgres://(.*):(.*)@(.*):(.*)/(.*)");
        options.ConnectionString = $"Server={match.Groups[3]};Port={match.Groups[4]};User Id={match.Groups[1]};Password={match.Groups[2]};Database={match.Groups[5]};sslmode=Prefer;Trust Server Certificate=true";
    }
});

builder.AddAzureBlobClient("blobstore");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if (isHeroku)
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
});
builder.Services.AddHttpsRedirection(options =>
{
    if (isHeroku)
    {
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        options.HttpsPort = 443;
    };
});

builder.Services.AddSingleton<ISyndicationFeed, AtomFeed>();

var app = builder.Build();

// Server sits behind a reverse proxy, so promote the forwarded headers
// so we know if the client connected via http or https
app.UseForwardedHeaders();

// Now that forwarded headers are taken care of, we can see
// if we need to redirect the user to the https endpoint.
app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    // Use the error page for all requests that are not API requests.
    app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder =>
    {
        // For any unhandled exceptions, show the user a friendly error page.
        app.UseExceptionHandler("/Error", createScopeForErrors: true);

        // Show the error page with the status code.
        // This is especially nice for 404s, so the user can see that the page doesn't exist.
        app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");
    });

    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Now we can authenticate the user, if the requested route requires it.
app.UseAuthentication();
app.UseAuthorization();

// Make sure the user is not a CSRF attack.
app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>();

app.MapDefaultEndpoints();

app.MapGet("/atom/all", async (IPostStore postStore, ISyndicationFeed feed, HttpContext httpContext, CancellationToken ct) =>
{
    List<Post> posts = new();
    var postsFromDb = postStore.GetPosts(20, ct);
    await foreach (var post in postsFromDb)
    {
        posts.Add(post);
    }

    httpContext.Response.Headers["Content-Type"] = "application/xml; charset=utf-8";

    // Prevent browsers from trying to automatically open the feed in an RSS reader
    // Useful for debugging the feed locally.
    httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";

    return feed.GetXmlForPosts(posts);
});

// Create a POST endpoint that will be used to upload an image to the blog.
// The endpoint will be authenticated so only admins can upload.
// Use BlobClientService to upload the image to the blob store.
// Return the permanent url to the blob upon completion with a 200 ok.
// Return other status codes based on potential issues, such as unsupported file types, or transient issues uploading to blob storage.
app.MapPost("/api/upload", async (BlobServiceClient blobServiceClient, IFormFile file, HttpContext httpContext, CancellationToken ct) =>
{
    if (file is null)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    // Get or create the container first. Container name is "images".
    var containerClient = blobServiceClient.GetBlobContainerClient("images");
    await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

    // Blob path should be prefixed with the current datetime to ensure uniqueness.
    // Example: "2025/08/01/12/00/00/imagename.jpg"
    string blobPath = $"{DateTimeOffset.UtcNow:yyyy/MM/dd/HH/mm/ss}/{file.FileName}";

    var blobClient = containerClient.GetBlobClient(blobPath);

    // Check if the blob already exists. If it does, return a 409 Conflict.
    if (await blobClient.ExistsAsync(ct))
    {
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return;
    }

    // Upload the image to the blob store.
    var response = await blobClient.UploadAsync(file.OpenReadStream(), true, ct);

    // Check if the response was successful. If it was, return the permanent url to the blob.
    if (response.GetRawResponse().Status == StatusCodes.Status201Created)
    {
        httpContext.Response.StatusCode = StatusCodes.Status201Created;
        httpContext.Response.Headers["Location"] = blobClient.Uri.AbsoluteUri;
    }
    else
    {
        // If the response was not successful, return a 500 Internal Server Error.
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
    }
}).DisableAntiforgery()
.RequireAuthorization("Admin");

app.Run();
