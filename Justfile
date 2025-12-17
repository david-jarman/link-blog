build:
  dotnet build

run:
  aspire run

# Run only unit tests (excludes integration tests)
test-unit:
  dotnet test --filter "Category!=IntegrationTest"

# Run only integration tests
test-integration:
  dotnet test --filter "Category=IntegrationTest"

# Run all unit tests (default for CI)
test: test-unit

# Run all tests (unit + integration)
test-all:
  dotnet test

# Coverage for unit tests only
coverage:
  dotnet test --filter "Category!=IntegrationTest" --collect:"XPlat Code Coverage"
  reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
  open coveragereport/index.html

# Coverage for integration tests only
coverage-integration:
  dotnet test --filter "Category=IntegrationTest" --collect:"XPlat Code Coverage"
  reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
  open coveragereport/index.html

# Coverage for all tests
coverage-all:
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

push: test
  dotnet format --verify-no-changes
  git push heroku main