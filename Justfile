build:
  dotnet build

run:
  aspire run

test:
  dotnet test

clean:
  git clean -xfd

lint:
  dotnet format

push: test lint
  git push heroku main