#!/bin/bash

# Generate GUID for Unity meta files
generate_guid() {
    # Generate a random GUID-like string (32 hex characters)
    cat /dev/urandom | tr -dc 'a-f0-9' | fold -w 32 | head -n 1
}

# Generate meta file for a folder
generate_folder_meta() {
    local path="$1"
    local guid=$(generate_guid)
    cat > "${path}.meta" << EOF
fileFormatVersion: 2
guid: ${guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
EOF
}

# Generate meta file for a C# script
generate_script_meta() {
    local path="$1"
    local guid=$(generate_guid)
    cat > "${path}.meta" << EOF
fileFormatVersion: 2
guid: ${guid}
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
EOF
}

# Generate meta file for text/markdown files
generate_text_meta() {
    local path="$1"
    local guid=$(generate_guid)
    cat > "${path}.meta" << EOF
fileFormatVersion: 2
guid: ${guid}
TextScriptImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
EOF
}

# Generate meta file for JSON files
generate_json_meta() {
    local path="$1"
    local guid=$(generate_guid)
    cat > "${path}.meta" << EOF
fileFormatVersion: 2
guid: ${guid}
TextScriptImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
EOF
}

# Process UnityPackage directory
echo "Generating .meta files for UnityPackage..."

# Generate meta for folders
find UnityPackage -type d | while read dir; do
    if [ ! -f "${dir}.meta" ]; then
        echo "Creating meta for folder: $dir"
        generate_folder_meta "$dir"
    fi
done

# Generate meta for C# scripts
find UnityPackage -type f -name "*.cs" | while read file; do
    if [ ! -f "${file}.meta" ]; then
        echo "Creating meta for script: $file"
        generate_script_meta "$file"
    fi
done

# Generate meta for Markdown files
find UnityPackage -type f -name "*.md" | while read file; do
    if [ ! -f "${file}.meta" ]; then
        echo "Creating meta for markdown: $file"
        generate_text_meta "$file"
    fi
done

# Generate meta for JSON files
find UnityPackage -type f -name "*.json" | while read file; do
    if [ ! -f "${file}.meta" ]; then
        echo "Creating meta for json: $file"
        generate_json_meta "$file"
    fi
done

echo "Meta file generation complete!"
