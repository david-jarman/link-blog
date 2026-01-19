#!/bin/bash

# TODO: Check for running in remote environment
# TODO: A set -e to fail for child processes

# Install .NET SDK
echo "Installing .NET SDK..."

DOTNET_INSTALL_DIR="${HOME}/.dotnet"
GLOBAL_JSON="${CLAUDE_PROJECT_DIR}/global.json"

# Validate global.json exists
if [ ! -f $GLOBAL_JSON ]; then
    echo "ERROR: global.json not found" >&2
    exit 2
fi

if [ ! -d "$DOTNET_INSTALL_DIR" ]; then
    echo "Downloading and installing .NET SDK using global.json..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
    chmod +x dotnet-install.sh
    if ! ./dotnet-install.sh --jsonfile $GLOBAL_JSON --install-dir $DOTNET_INSTALL_DIR; then
        echo "ERROR: .NET SDK install failed"
        rm -f dotnet-install.sh
        exit 2
    fi
    rm -f dotnet-install.sh
else
    echo ".NET SDK already installed at $DOTNET_INSTALL_DIR"
fi

# Add dotnet to PATH
export PATH="$DOTNET_INSTALL_DIR:$PATH"
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# Persist environment variables for Claude Code
if [ -n "${CLAUDE_ENV_FILE}" ]; then
    echo "export DOTNET_ROOT=$DOTNET_ROOT" >> "$CLAUDE_ENV_FILE"
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1" >> "$CLAUDE_ENV_FILE"
    echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
fi

# Verify installation
echo "Verifying .NET SDK installation..."
dotnet --version

exit 0