var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin(config => config.WithVolume("data", "/var/lib/pgadmin/data"))
    .WithDataVolume(isReadOnly: false);

var postgresdb = postgres.AddDatabase("postgresdb", "postgres");

builder.AddProject<Projects.LinkBlog_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(postgresdb);

builder.Build().Run();
