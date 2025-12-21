#!/bin/bash

set -e

# Default values
CONFIGURATION="Release"
OUTPUT_PATH="./artifacts/packages"
SKIP_BUILD=false
PACKAGE_VERSION=""

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
        --package-version)
            PACKAGE_VERSION="$2"
            shift 2
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
if [ -n "$PACKAGE_VERSION" ]; then
    echo "Package Version: $PACKAGE_VERSION"
fi

# Create output directory
mkdir -p "$OUTPUT_PATH"
echo "Created output directory: $OUTPUT_PATH"

# Clean previous artifacts
echo "Cleaning previous artifacts..."
rm -f "$OUTPUT_PATH"/*.nupkg
rm -f "$OUTPUT_PATH"/*.snupkg

# Projects to pack (in dependency order - core first, then dependent packages)
projects=(
    "src/MinimalCleanArch/MinimalCleanArch.csproj"
    "src/MinimalCleanArch.Audit/MinimalCleanArch.Audit.csproj"
    "src/MinimalCleanArch.Messaging/MinimalCleanArch.Messaging.csproj"
    "src/MinimalCleanArch.DataAccess/MinimalCleanArch.DataAccess.csproj"
    "src/MinimalCleanArch.Extensions/MinimalCleanArch.Extensions.csproj"
    "src/MinimalCleanArch.Validation/MinimalCleanArch.Validation.csproj"
    "src/MinimalCleanArch.Security/MinimalCleanArch.Security.csproj"
    "templates/MinimalCleanArch.Templates.csproj"
)

echo "Found ${#projects[@]} projects to build:"
for project in "${projects[@]}"; do
    echo "  - $project"
done
echo ""

success_count=0

for project in "${projects[@]}"; do
    if [ ! -f "$project" ]; then
        echo "Project file not found: $project - skipping"
        continue
    fi

    project_name=$(basename "$project" .csproj)
    echo "Processing: $project_name..."
    
    if [ "$SKIP_BUILD" = false ]; then
        echo "  Building..."
        build_props=(
            "/p:UseSharedCompilation=false"
            "/p:BuildInParallel=false"
        )
        build_args=(
            "$project"
            --configuration "$CONFIGURATION"
            --verbosity minimal
            --nologo
        )
        build_args+=("${build_props[@]}")
        if [ -n "$PACKAGE_VERSION" ]; then
            build_args+=("/p:PackageVersion=$PACKAGE_VERSION")
        fi

        if ! dotnet build "${build_args[@]}"; then
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
    pack_args+=("/p:UseSharedCompilation=false" "/p:BuildInParallel=false")

    if [ -n "$PACKAGE_VERSION" ]; then
        pack_args+=("/p:PackageVersion=$PACKAGE_VERSION")
        if [ "$project" = "templates/MinimalCleanArch.Templates.csproj" ]; then
            pack_args+=("/p:Version=$PACKAGE_VERSION")
        fi
    fi
    
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
