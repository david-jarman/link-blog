[![.NET CI](https://github.com/david-jarman/link-blog/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/david-jarman/link-blog/actions/workflows/ci.yml)

# LinkBlog

LinkBlog is a multi-project solution for building a comprehensive blogging platform.

## Projects

- **LinkBlog.AppHost**  
  Hosts the application services using Aspire. 
  [LinkBlog.AppHost.csproj](src/LinkBlog.AppHost/LinkBlog.AppHost.csproj)

- **LinkBlog.ServiceDefaults**  
  Provides default implementations for various services.  
  [LinkBlog.ServiceDefaults.csproj](src/LinkBlog.ServiceDefaults/LinkBlog.ServiceDefaults.csproj)

- **LinkBlog.Web**  
  The web front-end for the blogging platform.  
  [LinkBlog.Web.csproj](src/LinkBlog.Web/LinkBlog.Web.csproj)

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build and Test

To build the entire solution, run the following command in the project root:

```sh
dotnet build LinkBlog.sln
```

Then, to run tests

```sh
dotnet test LinkBlog.sln
```

Connect to database

```sh
heroku config:get DATABASE_URL | xargs pgweb --url
```
