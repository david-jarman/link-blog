build:
  dotnet build

run:
  aspire run

test:
  dotnet test

coverage:
  dotnet test --collect:"XPlat Code Coverage"
  reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
  open coveragereport/index.html

clean-coverage:
  git clean -Xfd coveragereport
  git clean -Xfd test/LinkBlog.Web.Tests/TestResults/*/coverage.cobertura.xml

clean:
  git clean -xfd

lint:
  dotnet format

push: test lint
  git push heroku main