using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Get configuration values
string environment = builder.Configuration.GetValue("environment", "Production");

Console.WriteLine($"Environment: {environment}");

// Set up resources
var postgres = builder.AddPostgres("postgres");

if (environment is not "Testing")
{
    postgres.WithPgAdmin(resource =>
    {
        resource
            .WithVolume("data", "/var/lib/pgadmin/data")
            .WithUrlForEndpoint("http", u => u.DisplayText = "PG Admin");
    })
    .WithDataVolume(isReadOnly: false);
}

var postgresdb = postgres.AddDatabase("postgresdb", "postgres");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        if (environment is not "Testing")
        {
            azurite.WithDataVolume()
                .WithLifetime(ContainerLifetime.Persistent)
                .WithBlobPort(34553);
        }
    });

var blobStore = storage.AddBlobs("blobstore");

var migrations = builder.AddProject<Projects.LinkBlog_MigrationService>("migrations")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

builder.AddProject<Projects.LinkBlog_Web>("webfrontend")
    .WithEnvironment(ctx => ctx.EnvironmentVariables["DOTNET_ENVIRONMENT"] = environment)
    .WithExternalHttpEndpoints()
    .WithReference(postgresdb)
    .WithReference(blobStore)
    .WithReference(migrations)
    .WaitForCompletion(migrations);

builder.Build().Run();