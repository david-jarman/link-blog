using System.Security.Claims;
using System.Text.RegularExpressions;
using LinkBlog.Contracts;
using LinkBlog.Feed;
using LinkBlog.Web.Components;
using LinkBlog.Web.Security;
using LinkBlog.Web.Services;
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
builder.AddNpgsqlDbContext<PostDbContext>(connectionName: "postgresdb", options =>
{
    if (isHeroku)
    {
        var match = Regex.Match(Environment.GetEnvironmentVariable("DATABASE_URL") ?? "", @"postgres://(.*):(.*)@(.*):(.*)/(.*)");
        options.ConnectionString = $"Server={match.Groups[3]};Port={match.Groups[4]};User Id={match.Groups[1]};Password={match.Groups[2]};Database={match.Groups[5]};sslmode=Prefer;Trust Server Certificate=true";
    }
});

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

builder.Services.AddScoped<IPostStore, PostStoreDb>();
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
    // For any unhandled exceptions, show the user a friendly error page.
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Show the error page with the status code.
// This is especially nice for 404s, so the user can see that the page doesn't exist.
app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");

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

app.Run();
