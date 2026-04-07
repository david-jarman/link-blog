var builder = DistributedApplication.CreateBuilder(args);

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
    .WithReference(blobStore)
    .WaitFor(blobStore);

builder.Build().Run();