using System.Text.RegularExpressions;
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
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();

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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
