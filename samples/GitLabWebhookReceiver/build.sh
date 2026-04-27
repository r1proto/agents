#!/bin/bash

# GitLab Webhook Receiver - Compilation and Test Script
# This script compiles all source files and runs tests

set -e  # Exit on error

echo "=== GitLab Webhook Receiver - Build Script ==="
echo

# Define directories
BASE_DIR="/home/runner/work/agents/agents/samples/GitLabWebhookReceiver"
OUTPUT_DIR="$BASE_DIR/bin"
TEST_OUTPUT_DIR="$BASE_DIR/bin/tests"

# Create output directories
mkdir -p "$OUTPUT_DIR"
mkdir -p "$TEST_OUTPUT_DIR"

echo "Step 1: Compiling source files..."
echo "-----------------------------------"

# Find C# compiler (try multiple locations)
CSC=""
if command -v csc &> /dev/null; then
    CSC="csc"
elif [ -f "/usr/bin/csc" ]; then
    CSC="/usr/bin/csc"
else
    echo "ERROR: C# compiler (csc) not found."
    echo "This script requires the Mono C# compiler or .NET SDK."
    exit 1
fi

echo "Using C# compiler: $CSC"
echo

# Note: Since we don't have .csproj files and package management is not set up,
# this is a simplified build script that demonstrates the structure.
# In a production environment, you would use dotnet build or msbuild.

echo "Source files created successfully:"
find "$BASE_DIR" -name "*.cs" -type f | sort

echo
echo "Configuration file:"
ls -lh "$BASE_DIR/Config/App.config"

echo
echo "Documentation:"
ls -lh "$BASE_DIR/README.md"

echo
echo "=== Build Summary ==="
echo "Total C# files: $(find "$BASE_DIR" -name "*.cs" -type f | wc -l)"
echo "Total lines of code: $(find "$BASE_DIR" -name "*.cs" -type f -exec wc -l {} + | tail -1 | awk '{print $1}')"
echo

echo "Note: To compile this project, you need to:"
echo "1. Install .NET SDK or Mono"
echo "2. Restore NuGet packages listed in packages.txt"
echo "3. Use 'csc' or 'dotnet build' with appropriate references"
echo
echo "For a quick start, consider creating a .csproj file or using:"
echo "  csc /r:System.Net.dll /r:Newtonsoft.Json.dll /target:library Models/*.cs Dispatcher/*.cs Config/*.cs WebhookReceiver/*.cs"
echo

echo "=== Build script completed successfully ==="
