# LinkBlog Development Guide

## Build/Run Commands
```bash
# Build solution
dotnet build

# Run the application
aspire run

# Run unit tests only (excludes integration tests that require Docker)
dotnet test --filter "Category!=IntegrationTest"
# Or using Just:
just test-unit

# Run integration tests only (requires Docker)
dotnet test --filter "Category=IntegrationTest"
# Or using Just:
just test-integration

# Run all tests (unit + integration, requires Docker for integration tests)
dotnet test
# Or using Just:
just test-all

# Check for outdated packages
dotnet outdated

# Update outdated packages
dotnet outdated -u
```

## Code Style Guidelines
- **Naming**: PascalCase for classes, methods, public properties; camelCase for parameters and private fields
- **Types**: Use interfaces prefixed with 'I'; entity classes suffixed with 'Entity'
- **Files**: Organize by feature; one class per file
- **Async**: Use async/await consistently with methods suffixed with 'Async'
- **Error Handling**: Use exceptions for exceptional cases; return values for expected failures
- **Formatting**: 4-space indentation; opening braces on same line

# Best practices

- CSS style for forms should NEVER be inline or defined in the actual razor component file (.razor). Use CSS isolation by adding CSS style to the associated .razor.css file instead. The web app should adhere to the principal that HTML should define content and not style.
- Blog uses Blazor Static Server Side (SSR) Rendering. Do not use any features of Blazor that require a constant connection to the backend server.
- Do not use Javascript except on admin pages. Any page that is public will not use Javascript or files hosted on external sites.
- Prefer to use semantic HTML elements.