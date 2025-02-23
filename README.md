# LinkBlog

LinkBlog is a multi-project solution for building a comprehensive blogging platform.

## Projects

- **LinkBlog.ApiService**  
  Provides the API services for the platform.  
  [LinkBlog.ApiService.csproj](src/LinkBlog.ApiService/LinkBlog.ApiService.csproj)

- **LinkBlog.AppHost**  
  Hosts the application services.  
  [LinkBlog.AppHost.csproj](src/LinkBlog.AppHost/LinkBlog.AppHost.csproj)

- **LinkBlog.Data**  
  Contains entities and classes for saving and retrieving data.
  [LinkBlog.Data.csproj](src/LinkBlog.Data/LinkBlog.Data.csproj)

- **LinkBlog.ServiceDefaults**  
  Provides default implementations for various services.  
  [LinkBlog.ServiceDefaults.csproj](src/LinkBlog.ServiceDefaults/LinkBlog.ServiceDefaults.csproj)

- **LinkBlog.Web**  
  The web front-end for the blogging platform.  
  [LinkBlog.Web.csproj](src/LinkBlog.Web/LinkBlog.Web.csproj)

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Building the Solution

To build the entire solution, run the following command in the project root:

```sh
dotnet build LinkBlog.sln
```