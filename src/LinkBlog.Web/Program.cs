using LinkBlog.Data;
using LinkBlog.Data.Extensions;
using LinkBlog.Feed;
using LinkBlog.Web;
using LinkBlog.Web.Components;
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
builder.Services.AddControllers();
builder.Services.AddOutputCache();

builder.AddPostStore();

builder.AddAzureBlobServiceClient("blobstore");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<BlogOptions>(builder.Configuration.GetSection(nameof(BlogOptions)));
builder.Services.Configure<FeedOptions>(builder.Configuration.GetSection("Feed"));
builder.Services.Configure<PostStoreOptions>(builder.Configuration.GetSection(nameof(PostStoreOptions)));
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

// Make sure the user is not a CSRF attack.
app.UseAntiforgery();
app.UseOutputCache();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();