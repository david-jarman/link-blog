Look at the recent changes to the Entity Framework entities and come up with a meaningful MigrationName. Then, run this command using the name you came up with to create the migration: 

```bash
dotnet ef migrations add [MigrationName] --project src/LinkBlog.Data/LinkBlog.Data.csproj --startup-project src/LinkBlog.Web/LinkBlog.Web.csproj
```

Then, update the idempotent migration script with:

```bash
dotnet ef migrations script --idempotent --project src/LinkBlog.Data/LinkBlog.Data.csproj --startup-project src/LinkBlog.Web/LinkBlog.Web.csproj --output src/LinkBlog.Data/Scripts/migrate-idempotent.sql
```