#!/bin/bash

# Define paths
PLUGIN_FILE="plugin.json"
TMP_DIR="./tmp"
WWWROOT_DIR="$TMP_DIR/wwwroot"
ASSEMBLY_BASE_DIR="./MoonlightServers.ApiServer/bin/Debug/net8.0"
STATIC_BASE_DIR="./MoonlightServers.Frontend/wwwroot"

# Create tmp and wwwroot directories
mkdir -p "$TMP_DIR"
mkdir -p "$WWWROOT_DIR"

# Function to copy files with preserved structure
copy_files_with_structure() {
    local base_dir=$1
    local jq_query=$2
    local dest_root=$3
    local file_type=$4

    echo "Copying $file_type..."
    jq -r "$jq_query[]" "$PLUGIN_FILE" | while read -r rel_path; do
        src_path="$base_dir/$rel_path"
        dest_path="$dest_root/$rel_path"

        if [ -f "$src_path" ]; then
            mkdir -p "$(dirname "$dest_path")"
            cp "$src_path" "$dest_path"
            echo "Copied $src_path -> $dest_path"
        else
            echo "Warning: $src_path not found"
        fi
    done
}

# Copy assemblies to tmp root
copy_files_with_structure "$ASSEMBLY_BASE_DIR" ".assemblies.apiServer" "$TMP_DIR" "apiServer assemblies"
copy_files_with_structure "$ASSEMBLY_BASE_DIR" ".assemblies.client" "$TMP_DIR" "client assemblies"

# Copy static files to tmp/wwwroot
copy_files_with_structure "$STATIC_BASE_DIR" ".bundledStyles" "$WWWROOT_DIR" "bundled styles"
copy_files_with_structure "$STATIC_BASE_DIR" ".scripts" "$WWWROOT_DIR" "scripts"

# Copy the plugin.json into tmp
cp "$PLUGIN_FILE" "$TMP_DIR/plugin.json"
echo "Copied $PLUGIN_FILE to $TMP_DIR/plugin.json"

# Extract name from plugin.json for the zip file
PLUGIN_NAME=$(jq -r '.name' "$PLUGIN_FILE")
ZIP_NAME="${PLUGIN_NAME}.zip"

# Create zip file (placed next to plugin.json)
echo "Creating zip file: $ZIP_NAME"
(cd "$TMP_DIR" && zip -r "../$ZIP_NAME" .)

# Cleanup
rm -rf "$TMP_DIR"
echo "Cleaned up tmp directory."

echo "Done! Created $ZIP_NAME"