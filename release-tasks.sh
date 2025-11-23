#!/bin/bash
set -e

echo "Starting Heroku release tasks..."

# Install .NET SDK
echo "Installing .NET SDK..."
DOTNET_INSTALL_DIR="$HOME/.dotnet"

# Validate global.json exists
if [ ! -f "global.json" ]; then
    echo "ERROR: global.json not found"
    exit 1
fi

if [ ! -d "$DOTNET_INSTALL_DIR" ]; then
    echo "Downloading and installing .NET SDK using global.json..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --jsonfile global.json --install-dir $DOTNET_INSTALL_DIR
    rm dotnet-install.sh
else
    echo ".NET SDK already installed at $DOTNET_INSTALL_DIR"
fi

# Add dotnet to PATH
export PATH="$DOTNET_INSTALL_DIR:$PATH"
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"

# Verify installation
echo "Verifying .NET SDK installation..."
dotnet --version

# Restore dotnet tools (includes dotnet-ef)
echo "Restoring dotnet tools..."
dotnet tool restore

# Build and Run unit tests
echo "Running unit tests..."
dotnet test test/LinkBlog.Web.Tests/LinkBlog.Web.Tests.csproj --configuration Release

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
