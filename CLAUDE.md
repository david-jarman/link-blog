# LinkBlog Development Guide

## Build/Run Commands
```bash
# Build solution
dotnet build LinkBlog.sln

# Run the application
dotnet run --project src/LinkBlog.AppHost/LinkBlog.AppHost.csproj

# Database migrations
dotnet ef migrations add [MigrationName] --project src/LinkBlog.Web/LinkBlog.Web.csproj
dotnet ef database update --project src/LinkBlog.Web/LinkBlog.Web.csproj
```

## Code Style Guidelines
- **Naming**: PascalCase for classes, methods, public properties; camelCase for parameters and private fields
- **Types**: Use interfaces prefixed with 'I'; entity classes suffixed with 'Entity'
- **Files**: Organize by feature; one class per file
- **Async**: Use async/await consistently with methods suffixed with 'Async'
- **Error Handling**: Use exceptions for exceptional cases; return values for expected failures
- **Formatting**: 4-space indentation; opening braces on same line

## Database Scripts
```bash
# Database management
psql -f src/LinkBlog.Web/Scripts/create.sql
psql -f src/LinkBlog.Web/Scripts/seed.sql
psql -f src/LinkBlog.Web/Scripts/dev-deleteall.sql
```