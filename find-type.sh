#!/bin/bash

# TypeFinder wrapper script for easier usage by coding agents
# Usage: ./find-type.sh <type-name> [options]

if [ $# -eq 0 ] || [ "$1" = "--help" ] || [ "$1" = "-h" ]; then
    echo "Usage: ./find-type.sh <type-name> [options]"
    echo ""
    echo "Options:"
    echo "  --exact-match    Search for exact type name match"
    echo "  --case-sensitive Use case-sensitive search"
    echo "  --file-types     Comma-separated list of file extensions to search"
    echo "  --max-results    Maximum number of results to return"
    echo "  --workspace      Specify workspace path (default: current directory)"
    echo "  --help, -h       Show this help message"
    echo ""
    echo "Examples:"
    echo "  ./find-type.sh User"
    echo "  ./find-type.sh \"MyClass\" --exact-match"
    echo "  ./find-type.sh Controller --file-types .cs,.ts"
    exit 1
fi

# Default workspace to current directory
WORKSPACE_PATH="."
TYPE_NAME="$1"
shift

# Parse options
OPTIONS=""
while [[ $# -gt 0 ]]; do
    case $1 in
        --workspace)
            WORKSPACE_PATH="$2"
            shift 2
            ;;
        *)
            OPTIONS="$OPTIONS $1"
            shift
            ;;
    esac
done

# Ensure .NET is available
if ! command -v dotnet &> /dev/null; then
    if [ -d "$HOME/.dotnet" ]; then
        export PATH="$HOME/.dotnet:$PATH"
    else
        echo "Error: .NET is not installed. Please install .NET 8.0 or later."
        exit 1
    fi
fi

# Run the TypeFinder
cd "$(dirname "$0")/src/TypeFinder"
dotnet run -- "$WORKSPACE_PATH" "$TYPE_NAME" $OPTIONS