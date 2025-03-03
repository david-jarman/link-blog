var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin(config => config.WithVolume("data", "/var/lib/pgadmin/data"))
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
