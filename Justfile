build:
  dotnet build

run:
  aspire run

# Collect code coverage
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

deploy: test-all
  dotnet format --verify-no-changes
  fly deploy