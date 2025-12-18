using LinkBlog.Data.Extensions;
using LinkBlog.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Register the database context with the same connection name used in AppHost
builder.AddPostStore("postgresdb", null);

builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySource.Name));

var host = builder.Build();
host.Run();