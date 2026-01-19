set -e

# Only run in remote environments
if [ "$CLAUDE_CODE_REMOTE" != "true" ]; then
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
if [ -n "$CLAUDE_ENV_FILE" ]; then
    echo "DOTNET_INSTALL_DIR=$DOTNET_INSTALL_DIR" >> "$CLAUDE_ENV_FILE"
    echo "DOTNET_ROOT=$DOTNET_INSTALL_DIR" >> "$CLAUDE_ENV_FILE"
    echo "DOTNET_CLI_TELEMETRY_OPTOUT=1" >> "$CLAUDE_ENV_FILE"
    echo "PATH=$DOTNET_INSTALL_DIR:\$PATH" >> "$CLAUDE_ENV_FILE"
fi

# Verify installation
echo "Verifying .NET SDK installation..."
dotnet --version

# Auth proxy configuration
AUTH_PROXY_PORT=3128
AUTH_PROXY_PID_FILE="$HOME/.auth_proxy.pid"

# Function to check if auth proxy is already running
is_proxy_running() {
    if [ -f "$AUTH_PROXY_PID_FILE" ]; then
        local pid=$(cat "$AUTH_PROXY_PID_FILE")
        if kill -0 "$pid" 2>/dev/null; then
            return 0  # Running
        fi
        # Stale PID file, remove it
        rm -f "$AUTH_PROXY_PID_FILE"
    fi
    return 1  # Not running
}

# Function to start local auth proxy for NuGet
start_auth_proxy() {
    if [ -z "$HTTP_PROXY" ]; then
        return 0  # No proxy configured, nothing to do
    fi

    # Check if proxy is already running
    if is_proxy_running; then
        echo "Auth proxy already running (PID: $(cat "$AUTH_PROXY_PID_FILE"))"
        return 0
    fi

    echo "Starting local authentication proxy for NuGet..."

    # Save original proxy as upstream (needed by the proxy script)
    export UPSTREAM_HTTP_PROXY="$HTTP_PROXY"

    # Start proxy in background with nohup so it survives script exit
    nohup python3 "$CLAUDE_PROJECT_DIR/scripts/auth_proxy.py" > "$HOME/.auth_proxy.log" 2>&1 &
    local proxy_pid=$!

    # Save PID for later reference
    echo "$proxy_pid" > "$AUTH_PROXY_PID_FILE"

    # Wait for proxy to start
    sleep 1

    # Check if proxy started successfully
    if ! kill -0 "$proxy_pid" 2>/dev/null; then
        echo "ERROR: Failed to start auth proxy. Check $HOME/.auth_proxy.log for details."
        rm -f "$AUTH_PROXY_PID_FILE"
        return 1
    fi

    echo "Auth proxy started (PID: $proxy_pid, log: $HOME/.auth_proxy.log)"
}

# Start auth proxy if HTTP_PROXY is set (for NuGet compatibility)
# The proxy will keep running for the entire Claude Code session
if [ -n "$HTTP_PROXY" ]; then
    # Store original proxy URL for the proxy script
    export UPSTREAM_HTTP_PROXY="$HTTP_PROXY"

    start_auth_proxy

    # Persist proxy environment variables for Claude Code session
    if [ -n "$CLAUDE_ENV_FILE" ]; then
        echo "UPSTREAM_HTTP_PROXY=$HTTP_PROXY" >> "$CLAUDE_ENV_FILE"
        echo "HTTP_PROXY=http://127.0.0.1:$AUTH_PROXY_PORT" >> "$CLAUDE_ENV_FILE"
        echo "HTTPS_PROXY=http://127.0.0.1:$AUTH_PROXY_PORT" >> "$CLAUDE_ENV_FILE"
        echo "http_proxy=http://127.0.0.1:$AUTH_PROXY_PORT" >> "$CLAUDE_ENV_FILE"
        echo "https_proxy=http://127.0.0.1:$AUTH_PROXY_PORT" >> "$CLAUDE_ENV_FILE"
    fi

    # Set for current script execution
    export HTTP_PROXY="http://127.0.0.1:$AUTH_PROXY_PORT"
    export HTTPS_PROXY="http://127.0.0.1:$AUTH_PROXY_PORT"
    export http_proxy="$HTTP_PROXY"
    export https_proxy="$HTTPS_PROXY"
fi

# Restore NuGet packages
echo "Restoring NuGet packages..."
dotnet restore "$CLAUDE_PROJECT_DIR"

echo "Setup complete!"