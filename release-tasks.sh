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
dotnet test

if [ $? -ne 0 ]; then
    echo "Tests failed! Aborting release."
    exit 1
fi

echo "Tests passed!"

# Apply database migrations
echo "Applying database migrations..."

cd src/LinkBlog.Web
dotnet ef database update

if [ $? -ne 0 ]; then
    echo "Database migration failed! Aborting release."
    exit 1
fi

echo "Database migrations applied successfully!"

echo "Release tasks completed successfully!"
