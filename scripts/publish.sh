#!/bin/bash

set -e

# Default values
API_KEY="${NUGET_API_KEY}"
SOURCE="https://api.nuget.org/v3/index.json"
PACKAGES_PATH="./artifacts/packages"
WHAT_IF=false
SKIP_DUPLICATE=true
TIMEOUT_SECONDS=300

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
    echo "  -k, --api-key KEY           NuGet API key (or set NUGET_API_KEY env var)"
    echo "  -s, --source URL            NuGet source URL (default: nuget.org)"
    echo "  -p, --packages-path PATH    Path to packages directory"
    echo "  -w, --what-if               Show what would be published without actually publishing"
    echo "  --no-skip-duplicate         Don't skip duplicate packages"
    echo "  -t, --timeout SECONDS       Timeout in seconds (default: 300)"
    echo "  -h, --help                  Show this help message"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -k|--api-key)
            API_KEY="$2"
            shift 2
            ;;
        -s|--source)
            SOURCE="$2"
            shift 2
            ;;
        -p|--packages-path)
            PACKAGES_PATH="$2"
            shift 2
            ;;
        -w|--what-if)
            WHAT_IF=true
            shift
            ;;
        --no-skip-duplicate)
            SKIP_DUPLICATE=false
            shift
            ;;
        -t|--timeout)
            TIMEOUT_SECONDS="$2"
            shift 2
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

print_color $GREEN "Publishing MinimalCleanArch packages..."
print_color $CYAN "Source: $SOURCE"
print_color $CYAN "Packages Path: $PACKAGES_PATH"
print_color $CYAN "What-If Mode: $WHAT_IF"

# Validate API key
if [ -z "$API_KEY" ]; then
    if [ "$WHAT_IF" = true ]; then
        print_color $YELLOW "API key not set - continuing in What-If mode"
    else
        print_color $RED "API key is required!"
        print_color $YELLOW "Solutions:"
        print_color $GRAY "  1. Set NUGET_API_KEY environment variable"
        print_color $GRAY "  2. Pass -k/--api-key parameter"
        print_color $GRAY "  3. Get API key from: https://www.nuget.org/account/apikeys"
        exit 1
    fi
fi

# Validate packages directory
if [ ! -d "$PACKAGES_PATH" ]; then
    print_color $RED "Packages directory not found: $PACKAGES_PATH"
    exit 1
fi

# Find packages (exclude symbol packages)
packages=()
while IFS= read -r file; do
    if [[ $(basename "$file") != *.symbols.nupkg ]]; then
        packages+=("$file")
    fi
done < <(find "$PACKAGES_PATH" -maxdepth 1 -name "*.nupkg" -print 2>/dev/null | sort)

if [ ${#packages[@]} -eq 0 ]; then
    print_color $RED "No packages found in $PACKAGES_PATH"
    print_color $YELLOW "Expected .nupkg files in the packages directory"
    exit 1
fi

echo ""
print_color $GREEN "Found ${#packages[@]} packages to publish:"
for package in "${packages[@]}"; do
    size=$(du -k "$package" | cut -f1)
    basename_pkg=$(basename "$package")
    print_color $CYAN "  Package: $basename_pkg (${size} KB)"
done

if [ "$WHAT_IF" = true ]; then
    echo ""
    print_color $YELLOW "WHAT-IF MODE - No packages will be published"
    print_color $GRAY "Remove -w/--what-if to actually publish packages"
fi

echo ""

success_count=0
skipped_count=0
error_count=0

for package in "${packages[@]}"; do
    basename_pkg=$(basename "$package")
    print_color $YELLOW "Publishing: $basename_pkg..."
    
    if [ "$WHAT_IF" = true ]; then
        print_color $CYAN "  WHAT-IF: Would publish $package"
        ((success_count++))
        continue
    fi
    
    push_args=(
        "nuget" "push" "$package"
        "--api-key" "$API_KEY"
        "--source" "$SOURCE"
        "--timeout" "$TIMEOUT_SECONDS"
    )
    
    if [ "$SKIP_DUPLICATE" = true ]; then
        push_args+=("--skip-duplicate")
    fi
    
    # Capture output and exit code
    if output=$(dotnet "${push_args[@]}" 2>&1); then
        print_color $GREEN "  Published successfully"
        ((success_count++))
    else
        exit_code=$?
        if [ $exit_code -eq 409 ] && [ "$SKIP_DUPLICATE" = true ]; then
            print_color $YELLOW "  Package already exists (skipped)"
            ((skipped_count++))
        else
            print_color $RED "  Failed: $output"
            ((error_count++))
            
            if [ "$SKIP_DUPLICATE" = false ]; then
                print_color $RED "Failed to publish $basename_pkg"
                exit 1
            fi
        fi
    fi
    
    echo ""
done

# Summary
print_color $GREEN "Publishing Summary:"
print_color $GREEN "  Successful: $success_count"
if [ $skipped_count -gt 0 ]; then
    print_color $YELLOW "  Skipped: $skipped_count"
fi
if [ $error_count -gt 0 ]; then
    print_color $RED "  Errors: $error_count"
fi
print_color $CYAN "  Total: ${#packages[@]}"

echo ""
if [ "$WHAT_IF" = true ]; then
    print_color $YELLOW "What-If mode completed - no packages were actually published"
    print_color $GRAY "Run without --what-if to actually publish packages"
elif [ $error_count -eq 0 ]; then
    print_color $GREEN "All packages published successfully!"
    print_color $GRAY "Visit https://www.nuget.org/profiles/[YourProfile] to view your packages"
else
    print_color $YELLOW "Some packages failed to publish. Check the output above."
    if [ "$SKIP_DUPLICATE" = false ]; then
        exit 1
    fi
fi

# Exit successfully in What-If mode or if we have any successful publishes
if [ "$WHAT_IF" = true ] || [ $success_count -gt 0 ]; then
    exit 0
fi
