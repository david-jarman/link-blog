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

# Configure NuGet proxy if HTTP_PROXY is set
if [ -n "$HTTP_PROXY" ]; then
    echo "Configuring NuGet proxy from HTTP_PROXY..."

    # Parse HTTP_PROXY - format: http://[user:pass@]host:port
    # Remove protocol prefix
    proxy_without_protocol="${HTTP_PROXY#http://}"
    proxy_without_protocol="${proxy_without_protocol#https://}"

    # Check if credentials are present (contains @)
    if [[ "$proxy_without_protocol" == *"@"* ]]; then
        # Extract credentials (everything before @)
        credentials="${proxy_without_protocol%%@*}"
        # Extract proxy URL (everything after @)
        proxy_host="${proxy_without_protocol#*@}"

        # Split credentials into username and password
        proxy_user="${credentials%%:*}"
        proxy_pass="${credentials#*:}"

        # Set proxy with credentials
        dotnet nuget config set http_proxy "http://$proxy_host"
        dotnet nuget config set http_proxy.user "$proxy_user"
        dotnet nuget config set http_proxy.password "$proxy_pass"

        echo "NuGet proxy configured with authentication"
    else
        # No credentials, just set the proxy URL
        dotnet nuget config set http_proxy "$HTTP_PROXY"
        echo "NuGet proxy configured without authentication"
    fi
fi