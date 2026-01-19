set -euo pipefail

# Log all output to a file for debugging
INSTALL_LOG="$HOME/.install_pkgs.log"
exec > >(tee -a "$INSTALL_LOG") 2>&1
echo "=== Install started at $(date) ==="

# Only run in remote environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  echo "Local run, exiting setup script early"
  exit 0
fi

# Install .NET SDK
echo "Installing .NET SDK..."

DOTNET_INSTALL_DIR="$HOME/.dotnet"

GLOBAL_JSON="$CLAUDE_PROJECT_DIR/global.json"

# Validate global.json exists
if [ ! -f $GLOBAL_JSON ]; then
    echo "ERROR: global.json not found"
    exit 1
fi

if [ ! -d "$DOTNET_INSTALL_DIR" ]; then
    echo "Downloading and installing .NET SDK using global.json..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --jsonfile $GLOBAL_JSON --install-dir $DOTNET_INSTALL_DIR
    rm dotnet-install.sh
else
    echo ".NET SDK already installed at $DOTNET_INSTALL_DIR"
fi

# Add dotnet to PATH
export PATH="$DOTNET_INSTALL_DIR:$PATH"
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"

# Persist environment variables for Claude Code
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    echo "export DOTNET_INSTALL_DIR=$DOTNET_INSTALL_DIR" >> "$CLAUDE_ENV_FILE"
    echo "export DOTNET_ROOT=$DOTNET_INSTALL_DIR" >> "$CLAUDE_ENV_FILE"
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1" >> "$CLAUDE_ENV_FILE"
    echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
fi

# Verify installation
echo "Verifying .NET SDK installation..."
dotnet --version

# Function to start local auth proxy for NuGet
start_auth_proxy() {
    if [ -z "${HTTP_PROXY:-}" ]; then
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
dotnet restore "$CLAUDE_PROJECT_DIR"

echo "Restoring tools"
dotnet tool restore

echo "Setup complete!"