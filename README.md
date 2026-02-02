[![.NET CI](https://github.com/david-jarman/link-blog/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/david-jarman/link-blog/actions/workflows/ci.yml)

# LinkBlog

This is the source code for my personal blog: [davidjarman.net](https://davidjarman.net)

## Projects

- **LinkBlog.Abstractions**
  Abstractions for entity definitions shared across the project.
  [LinkBlog.Abstractions.csproj](src/LinkBlog.Abstractions/LinkBlog.Abstractions.csproj)

- **LinkBlog.AppHost**
  Hosts the application services using Aspire.
  [LinkBlog.AppHost.csproj](src/LinkBlog.AppHost/LinkBlog.AppHost.csproj)

- **LinkBlog.Data**
  Data access layer with Entity Framework Core entities and contexts.
  [LinkBlog.Data.csproj](src/LinkBlog.Data/LinkBlog.Data.csproj)

- **LinkBlog.Feed**
  Atom feed for syndicating blog posts.
  [LinkBlog.Feed.csproj](src/LinkBlog.Feed/LinkBlog.Feed.csproj)

- **LinkBlog.Images**
  Image processing functionality.
  [LinkBlog.Images.csproj](src/LinkBlog.Images/LinkBlog.Images.csproj)

- **LinkBlog.MigrationService**
  Worker service for running database migrations.
  [LinkBlog.MigrationService.csproj](src/LinkBlog.MigrationService/LinkBlog.MigrationService.csproj)

- **LinkBlog.ServiceDefaults**
  Provides default implementations for various services.
  [LinkBlog.ServiceDefaults.csproj](src/LinkBlog.ServiceDefaults/LinkBlog.ServiceDefaults.csproj)

- **LinkBlog.Web**
  The web front-end for the blogging platform.
  [LinkBlog.Web.csproj](src/LinkBlog.Web/LinkBlog.Web.csproj)

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build and Run

To build the solution:

```sh
dotnet build
```

To run the application:

```sh
aspire run
```

To run tests:

```sh
dotnet test
```

Connect to database

```sh
heroku config:get DATABASE_URL | xargs pgweb --url
```
