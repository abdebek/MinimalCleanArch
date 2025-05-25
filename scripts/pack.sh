#!/bin/bash

set -e

# Default values
CONFIGURATION="Release"
OUTPUT_PATH="./artifacts/packages"
SKIP_BUILD=false
INCLUDE_SYMBOLS=true

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Print colored output
print_color() {
    printf "${1}%s${NC}\n" "$2"
}

# Check if we're on Windows (Git Bash, WSL, etc.)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || -n "$WSL_DISTRO_NAME" ]]; then
    # Disable colors on Windows to avoid issues
    RED=''
    GREEN=''
    YELLOW=''
    CYAN=''
    GRAY=''
    NC=''
fi

print_usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -c, --configuration CONFIG   Build configuration (default: Release)"
    echo "  -o, --output PATH           Output directory (default: ./artifacts/packages)"
    echo "  --skip-build                Skip build step"
    echo "  --no-symbols                Don't include symbol packages"
    echo "  -h, --help                  Show this help message"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --no-symbols)
            INCLUDE_SYMBOLS=false
            shift
            ;;
        -h|--help)
            print_usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            print_usage
            exit 1
            ;;
    esac
done

print_color $GREEN "Building and packing MinimalCleanArch packages..."
print_color $CYAN "Configuration: $CONFIGURATION"
print_color $CYAN "Output Path: $OUTPUT_PATH"
print_color $CYAN "Skip Build: $SKIP_BUILD"
print_color $CYAN "Include Symbols: $INCLUDE_SYMBOLS"

# Create output directory
mkdir -p "$OUTPUT_PATH"
print_color $YELLOW "Created output directory: $OUTPUT_PATH"

# Clean previous artifacts
print_color $YELLOW "Cleaning previous artifacts..."
rm -f "$OUTPUT_PATH"/*.nupkg
rm -f "$OUTPUT_PATH"/*.snupkg

# Projects to pack (in dependency order - core first, then dependent packages)
projects=(
    "src/MinimalCleanArch/MinimalCleanArch.csproj"
    "src/MinimalCleanArch.DataAccess/MinimalCleanArch.DataAccess.csproj"
    "src/MinimalCleanArch.Extensions/MinimalCleanArch.Extensions.csproj"
    "src/MinimalCleanArch.Validation/MinimalCleanArch.Validation.csproj"
    "src/MinimalCleanArch.Security/MinimalCleanArch.Security.csproj"
)

success_count=0
total_projects=${#projects[@]}

for project in "${projects[@]}"; do
    if [ ! -f "$project" ]; then
        print_color $YELLOW "Warning: Project file not found: $project - Skipping"
        continue
    fi
    
    ((current_index = success_count + 1))
    print_color $YELLOW "Processing ($current_index/$total_projects): $project..."
    
    if [ "$SKIP_BUILD" = false ]; then
        print_color $GRAY "  Building..."
        if ! dotnet build "$project" --configuration "$CONFIGURATION" --verbosity minimal --nologo; then
            print_color $RED "Build failed for $project"
            exit 1
        fi
    fi
    
    print_color $GRAY "  Packing..."
    pack_args=(
        "pack" "$project"
        "--configuration" "$CONFIGURATION"
        "--output" "$OUTPUT_PATH"
        "--verbosity" "minimal"
        "--nologo"
    )
    
    if [ "$SKIP_BUILD" = true ]; then
        pack_args+=("--no-build")
    fi
    
    if [ "$INCLUDE_SYMBOLS" = true ]; then
        pack_args+=("--include-symbols")
    fi
    
    if ! dotnet "${pack_args[@]}"; then
        print_color $RED "Pack failed for $project"
        exit 1
    fi
    
    ((success_count++))
    print_color $GREEN "  Success"
done

echo ""
print_color $GREEN "Packaging completed successfully!"
print_color $CYAN "Processed $success_count/$total_projects projects"

# List created packages
echo ""
print_color $GREEN "Created packages:"
if ! ls "$OUTPUT_PATH"/*.nupkg 1> /dev/null 2>&1; then
    print_color $YELLOW "Warning: No packages were created!"
    exit 1
else
    for package in "$OUTPUT_PATH"/*.nupkg; do
        if [ -f "$package" ]; then
            size=$(du -k "$package" | cut -f1)
            basename_pkg=$(basename "$package")
            print_color $CYAN "  Package: $basename_pkg (${size} KB)"
        fi
    done
    
    package_count=$(ls "$OUTPUT_PATH"/*.nupkg 2>/dev/null | wc -l)
    print_color $GREEN "Total packages: $package_count"
    
    # Show symbols packages if they exist
    if ls "$OUTPUT_PATH"/*.snupkg 1> /dev/null 2>&1; then
        echo ""
        print_color $GREEN "Symbol packages:"
        for symbol_package in "$OUTPUT_PATH"/*.snupkg; do
            if [ -f "$symbol_package" ]; then
                size=$(du -k "$symbol_package" | cut -f1)
                basename_sym=$(basename "$symbol_package")
                print_color $CYAN "  Symbol: $basename_sym (${size} KB)"
            fi
        done
    fi
fi

echo ""
print_color $GREEN "All packages ready for publishing!"
print_color $GRAY "Run './scripts/publish.sh' to publish to NuGet.org"