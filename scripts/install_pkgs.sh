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
    echo "export DOTNET_ROOT=$DOTNET_INSTALL_DIR" >> "$CLAUDE_ENV_FILE"
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1" >> "$CLAUDE_ENV_FILE"
    echo "PATH=$DOTNET_INSTALL_DIR:\$PATH" >> "$CLAUDE_ENV_FILE"
fi

# Verify installation
echo "Verifying .NET SDK installation..."
dotnet --version

# Configure NuGet proxy if HTTP_PROXY is set
if [ -n "$HTTP_PROXY" ]; then
    echo "Configuring NuGet proxy from HTTP_PROXY..."

    PROXY_SCRIPT="$CLAUDE_PROJECT_DIR/scripts/get-userinfo.cs"

    # Use C# to parse the proxy URL components
    proxy_user=$(dotnet run "$PROXY_SCRIPT" -- user 2>/dev/null) || proxy_user=""
    proxy_pass=$(dotnet run "$PROXY_SCRIPT" -- password 2>/dev/null) || proxy_pass=""
    proxy_url=$(dotnet run "$PROXY_SCRIPT" -- url 2>/dev/null) || proxy_url=""

    echo "DEBUG user: $proxy_user"
    echo "DEBUG pass: $proxy_pass"
    echo "DEBUG url: $proxy_url"

    if [ -n "$proxy_user" ]; then
        # Set proxy with credentials
        dotnet nuget config set http_proxy "$proxy_url"
        dotnet nuget config set http_proxy.user "$proxy_user"
        dotnet nuget config set http_proxy.password "$proxy_pass"

        echo "NuGet proxy configured with authentication"

        # Verify config was set
        echo "DEBUG: Verifying config..."
        dotnet nuget config get http_proxy
        dotnet nuget config get http_proxy.user

        # Unset HTTP_PROXY to force NuGet to use config instead of env var
        unset HTTP_PROXY
        unset HTTPS_PROXY
        unset http_proxy
        unset https_proxy
        echo "DEBUG: Unset HTTP_PROXY env vars to force config usage"
    else
        # No credentials, just set the proxy URL
        dotnet nuget config set http_proxy "$HTTP_PROXY"
        echo "NuGet proxy configured without authentication"
    fi
fi

# try to restore using the above settings
dotnet restore
