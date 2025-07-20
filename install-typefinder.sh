#!/bin/bash

# TypeFinder Installation Script
# This script publishes the TypeFinder tool and creates a symlink for easy access

set -e

# Default installation directory
INSTALL_DIR="${HOME}/.local/bin"
TOOL_NAME="find-type"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --install-dir)
            INSTALL_DIR="$2"
            shift 2
            ;;
        --help|-h)
            echo "Usage: $0 [options]"
            echo ""
            echo "Options:"
            echo "  --install-dir <path>  Specify installation directory (default: ~/.local/bin)"
            echo "  --help, -h           Show this help message"
            echo ""
            echo "This script publishes the TypeFinder tool and creates a symlink for easy access."
            echo "The tool will be available as 'find-type' in the specified installation directory."
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Ensure .NET is available
if ! command -v dotnet &> /dev/null; then
    if [ -d "$HOME/.dotnet" ]; then
        export PATH="$HOME/.dotnet:$PATH"
    else
        echo "Error: .NET is not installed. Please install .NET 8.0 or later."
        echo "You can install it using: curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel LTS"
        exit 1
    fi
fi

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="${SCRIPT_DIR}/src/TypeFinder"

# Check if the project directory exists
if [ ! -d "$PROJECT_DIR" ]; then
    echo "Error: TypeFinder project not found at $PROJECT_DIR"
    exit 1
fi

# Create installation directory if it doesn't exist
mkdir -p "$INSTALL_DIR"

# Publish the application
echo "Publishing TypeFinder..."

cd "$PROJECT_DIR"
dotnet publish -c Release -r linux-x64 --self-contained true
PUBLISH_DIR="../../artifacts/publish/TypeFinder/release_linux-x64"

# Find the executable
EXECUTABLE=""
if [ -f "$PUBLISH_DIR/TypeFinder" ]; then
    EXECUTABLE="$PUBLISH_DIR/TypeFinder"
elif [ -f "$PUBLISH_DIR/TypeFinder.exe" ]; then
    EXECUTABLE="$PUBLISH_DIR/TypeFinder.exe"
else
    echo "Error: Could not find TypeFinder executable in $PUBLISH_DIR"
    exit 1
fi

# Make the executable executable
chmod +x "$EXECUTABLE"

# Create symlink
SYMLINK_PATH="$INSTALL_DIR/$TOOL_NAME"
if [ -L "$SYMLINK_PATH" ]; then
    echo "Removing existing symlink..."
    rm "$SYMLINK_PATH"
elif [ -f "$SYMLINK_PATH" ]; then
    echo "Removing existing file..."
    rm "$SYMLINK_PATH"
fi

echo "Creating symlink: $SYMLINK_PATH -> $EXECUTABLE"
ln -sf "$(realpath "$EXECUTABLE")" "$SYMLINK_PATH"

# Add to PATH if not already there
if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
    echo ""
    echo "Note: $INSTALL_DIR is not in your PATH."
    echo "Add the following line to your shell profile (.bashrc, .zshrc, etc.):"
    echo "export PATH=\"$INSTALL_DIR:\$PATH\""
    echo ""
fi

echo ""
echo "TypeFinder has been successfully installed!"
echo "You can now use it with: $TOOL_NAME <type-name> [options]"
echo ""
echo "Example usage:"
echo "  $TOOL_NAME Program"
echo "  $TOOL_NAME \"MyClass\" --exact-match"
echo "  $TOOL_NAME Controller --include-references"
echo ""
echo "For help: $TOOL_NAME --help"