# LinkBlog Development Guide

## Build/Run Commands
```bash
# Build solution
dotnet build LinkBlog.slnxx

# Run the application
dotnet run --project src/LinkBlog.AppHost/LinkBlog.AppHost.csproj

# Run tests
dotnet test LinkBlog.slnx

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

# Best practices

CSS style for forms should NEVER be inline or defined in the actual razor component file (.razor). Use CSS isolation by adding CSS style to the associated .razor.css file instead. The web app should adhere to the principal that HTML should define content and not style.