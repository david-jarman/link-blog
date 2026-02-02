using LinkBlog.Data.Extensions;
using LinkBlog.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Register only the database context for migrations (not the full post store)
builder.AddPostDbContext("postgresdb");

builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySource.Name));

var host = builder.Build();
host.Run();