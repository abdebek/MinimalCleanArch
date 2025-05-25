#!/bin/bash

set -e

# Default values
CONFIGURATION="Release"
OUTPUT_PATH="./artifacts/packages"
SKIP_BUILD=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        *)
            shift
            ;;
    esac
done

echo "Building and packing MinimalCleanArch packages..."
echo "Configuration: $CONFIGURATION"
echo "Output Path: $OUTPUT_PATH"
echo "Skip Build: $SKIP_BUILD"

# Create output directory
mkdir -p "$OUTPUT_PATH"
echo "Created output directory: $OUTPUT_PATH"

# Clean previous artifacts
echo "Cleaning previous artifacts..."
rm -f "$OUTPUT_PATH"/*.nupkg
rm -f "$OUTPUT_PATH"/*.snupkg

# Find all .csproj files in src/ directory
projects=()
while IFS= read -r -d '' project; do
    projects+=("$project")
done < <(find src/ -name "*.csproj" -print0 2>/dev/null)

if [ ${#projects[@]} -eq 0 ]; then
    echo "No projects found in src/ directory"
    exit 1
fi

echo "Found ${#projects[@]} projects to build:"
for project in "${projects[@]}"; do
    echo "  - $project"
done
echo ""

success_count=0

for project in "${projects[@]}"; do
    project_name=$(basename "$project" .csproj)
    echo "Processing: $project_name..."
    
    if [ "$SKIP_BUILD" = false ]; then
        echo "  Building..."
        if ! dotnet build "$project" --configuration "$CONFIGURATION" --verbosity minimal --nologo; then
            echo "  Build failed for $project - skipping"
            continue
        fi
    fi
    
    echo "  Packing..."
    pack_args=(
        "pack" "$project"
        "--configuration" "$CONFIGURATION"
        "--output" "$OUTPUT_PATH"
        "--verbosity" "minimal"
        "--nologo"
        "--include-symbols"
    )
    
    if [ "$SKIP_BUILD" = true ]; then
        pack_args+=("--no-build")
    fi
    
    if ! dotnet "${pack_args[@]}"; then
        echo "  Pack failed for $project - skipping"
        continue
    fi
    
    ((success_count++))
    echo "  Success"
done

echo ""
echo "Packaging completed!"
echo "Processed $success_count/${#projects[@]} projects successfully"

# List created packages
echo ""
echo "Created packages:"
if ! ls "$OUTPUT_PATH"/*.nupkg 1> /dev/null 2>&1; then
    echo "  No packages were created!"
    exit 1
else
    for package in "$OUTPUT_PATH"/*.nupkg; do
        if [ -f "$package" ]; then
            size=$(du -k "$package" | cut -f1)
            basename_pkg=$(basename "$package")
            echo "  Package: $basename_pkg (${size} KB)"
        fi
    done
    
    package_count=$(ls "$OUTPUT_PATH"/*.nupkg 2>/dev/null | wc -l)
    echo "Total packages: $package_count"
    
    # Show symbols packages if they exist
    if ls "$OUTPUT_PATH"/*.snupkg 1> /dev/null 2>&1; then
        echo ""
        echo "Symbol packages:"
        for symbol_package in "$OUTPUT_PATH"/*.snupkg; do
            if [ -f "$symbol_package" ]; then
                size=$(du -k "$symbol_package" | cut -f1)
                basename_sym=$(basename "$symbol_package")
                echo "  Symbol: $basename_sym (${size} KB)"
            fi
        done
    fi
fi

echo ""
echo "All packages ready for publishing!"

if [ $success_count -eq 0 ]; then
    echo "Error: No packages were created successfully"
    exit 1
fi