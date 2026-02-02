var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin(resource =>
    {
        resource
            .WithVolume("data", "/var/lib/pgadmin/data")
            .WithUrlForEndpoint("http", u => u.DisplayText = "PG Admin");
    })
    .WithDataVolume(isReadOnly: false);

var postgresdb = postgres.AddDatabase("postgresdb", "postgres");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithBlobPort(34553);
    });

var blobStore = storage.AddBlobs("blobstore");

var migrations = builder.AddProject<Projects.LinkBlog_MigrationService>("migrations")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

builder.AddProject<Projects.LinkBlog_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(postgresdb)
    .WithReference(blobStore)
    .WaitForCompletion(migrations);

builder.Build().Run();