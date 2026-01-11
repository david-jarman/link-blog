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

# Verify installation
echo "Verifying .NET SDK installation..."
dotnet --version

# Restore dotnet tools (includes dotnet-ef)
echo "Restoring dotnet tools..."
dotnet tool restore