# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy source code and publish app
COPY . .
RUN dotnet publish LinkBlog.Web/LinkBlog.Web.csproj -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["./LinkBlog.Web"]