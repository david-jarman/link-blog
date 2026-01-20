#!/bin/bash

# Only run in remote environments
if [ "$CLAUDE_CODE_REMOTE" != "true" ]; then
  exit 0
fi

# TODO: A set -e to fail for child processes
# TODO: add a timeout of 2 minutes to this script and kill it and print out info to stdout

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
    ./dotnet-install.sh --jsonfile $GLOBAL_JSON --install-dir $DOTNET_INSTALL_DIR >&1
    rm -f dotnet-install.sh
else
    echo ".NET SDK already installed at $DOTNET_INSTALL_DIR"
fi

# Add dotnet to PATH
export PATH="$DOTNET_INSTALL_DIR:$PATH"
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# Persist environment variables for Claude Code
if [ -n "${CLAUDE_ENV_FILE}" ]; then
    echo "export DOTNET_ROOT=$DOTNET_ROOT" >> "$CLAUDE_ENV_FILE"
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1" >> "$CLAUDE_ENV_FILE"
    echo "export DOTNET_NOLOGO=1" >> "$CLAUDE_ENV_FILE"
    echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
fi

# Verify installation
echo "Verifying .NET SDK installation..."
dotnet --version

# Function to start local auth proxy for NuGet
start_auth_proxy() {
    if [ -z "${HTTP_PROXY:-}" ]; then
        echo "No proxy configured"
        return 0  # No proxy configured, nothing to do
    fi

    echo "Starting local authentication proxy for NuGet..."

    # Save original proxy as upstream
    export UPSTREAM_HTTP_PROXY="$HTTP_PROXY"

    # Start proxy in background
    python3 "$CLAUDE_PROJECT_DIR/scripts/auth_proxy.py" &
    AUTH_PROXY_PID=$!

    # Wait for proxy to start
    sleep 1

    # Check if proxy started successfully
    if ! kill -0 $AUTH_PROXY_PID 2>/dev/null; then
        echo "ERROR: Failed to start auth proxy"
        return 1
    fi

    # Point HTTP_PROXY to local proxy
    export HTTP_PROXY="http://127.0.0.1:3128"
    export HTTPS_PROXY="http://127.0.0.1:3128"
    export http_proxy="$HTTP_PROXY"
    export https_proxy="$HTTPS_PROXY"

    echo "Auth proxy started (PID: $AUTH_PROXY_PID)"
}

# Function to stop auth proxy
stop_auth_proxy() {
    if [ -n "${AUTH_PROXY_PID:-}" ]; then
        echo "Stopping auth proxy..."
        kill $AUTH_PROXY_PID 2>/dev/null || true
        wait $AUTH_PROXY_PID 2>/dev/null || true
        unset AUTH_PROXY_PID
    fi
}

# Trap to ensure proxy is stopped on exit
trap stop_auth_proxy EXIT

# Start auth proxy if HTTP_PROXY is set (for NuGet compatibility)
start_auth_proxy

# Restore NuGet packages
echo "Restoring NuGet packages..."
if ! dotnet restore "$CLAUDE_PROJECT_DIR"; then
    echo "ERROR: dotnet restore failed" >&2
    exit 2
fi

echo "Install completed in ${SECONDS}s"
exit 0