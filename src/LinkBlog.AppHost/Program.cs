var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.LinkBlog_ApiService>("apiservice");

builder.AddProject<Projects.LinkBlog_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
