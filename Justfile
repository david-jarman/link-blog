build:
  dotnet build

run:
  dotnet run --project src/LinkBlog.AppHost/LinkBlog.AppHost.csproj

test:
  dotnet test

clean:
  git clean -xfd

lint:
  dotnet format

push: test lint
  git push heroku main