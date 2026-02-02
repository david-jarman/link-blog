using LinkBlog.Data.Extensions;
using LinkBlog.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Register only the database context for migrations (not the full post store)
builder.AddPostDbContext("postgresdb");

builder.Services.AddHostedService<Worker>();

// Register seeder for development environment only
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<DatabaseSeeder>();
}

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource(Worker.ActivitySource.Name);
        tracing.AddSource(DatabaseSeeder.ActivitySource.Name);
    });

var host = builder.Build();
host.Run();