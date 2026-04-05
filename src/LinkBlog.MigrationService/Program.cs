using LinkBlog.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// TODO: MigrationService is being removed as part of the Blob Storage migration (Task 7/8)
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