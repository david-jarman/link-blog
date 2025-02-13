var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.LinkBlog_Web>("webfrontend")
    .WithExternalHttpEndpoints();

builder.Build().Run();
