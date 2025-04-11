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

builder.AddProject<Projects.LinkBlog_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(postgresdb)
    .WithReference(blobStore);

builder.Build().Run();