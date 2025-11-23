#!/bin/bash
set -e

echo "Starting Heroku release tasks..."

# Run unit tests
echo "Running unit tests..."
dotnet test test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj --configuration Release --no-build --verbosity normal

if [ $? -ne 0 ]; then
    echo "Tests failed! Aborting release."
    exit 1
fi

echo "Tests passed!"

# Apply database migrations
echo "Applying database migrations..."

if [ -z "$DATABASE_URL" ]; then
    echo "ERROR: DATABASE_URL environment variable is not set"
    exit 1
fi

# Parse DATABASE_URL (format: postgres://user:password@host:port/database)
if [[ $DATABASE_URL =~ postgres://([^:]+):([^@]+)@([^:]+):([^/]+)/(.+) ]]; then
    DB_USER="${BASH_REMATCH[1]}"
    DB_PASSWORD="${BASH_REMATCH[2]}"
    DB_HOST="${BASH_REMATCH[3]}"
    DB_PORT="${BASH_REMATCH[4]}"
    DB_NAME="${BASH_REMATCH[5]}"

    CONNECTION_STRING="Server=$DB_HOST;Port=$DB_PORT;User Id=$DB_USER;Password=$DB_PASSWORD;Database=$DB_NAME;sslmode=Prefer;Trust Server Certificate=true"

    echo "Applying migrations to database: $DB_NAME on $DB_HOST:$DB_PORT"

    cd src/LinkBlog.Web
    dotnet ef database update --connection "$CONNECTION_STRING" --no-build

    if [ $? -ne 0 ]; then
        echo "Database migration failed! Aborting release."
        exit 1
    fi

    echo "Database migrations applied successfully!"
else
    echo "ERROR: Invalid DATABASE_URL format"
    exit 1
fi

echo "Release tasks completed successfully!"
