#!/usr/bin/env bash
set -euo pipefail

manifest_path="src/Cotton.Mobile/Platforms/Android/AndroidManifest.xml"
android_source_path="src/Cotton.Mobile/Platforms/Android"

if [[ ! -f "$manifest_path" ]]; then
  printf 'Android manifest was not found: %s\n' "$manifest_path" >&2
  exit 1
fi

required_permissions=(
  "android.permission.READ_MEDIA_IMAGES"
  "android.permission.READ_MEDIA_VIDEO"
)

for permission in "${required_permissions[@]}"; do
  if ! grep -Fq "$permission" "$manifest_path"; then
    printf 'Media backup manifest must declare %s.\n' "$permission" >&2
    exit 1
  fi
done

if grep -Fq "android.permission.READ_MEDIA_VISUAL_USER_SELECTED" "$manifest_path"; then
  printf 'Media backup manifest must not declare partial-library access.\n' >&2
  exit 1
fi

required_api_references=(
  "Manifest.Permission.ReadMediaImages"
  "Manifest.Permission.ReadMediaVideo"
)

for api_reference in "${required_api_references[@]}"; do
  if ! grep -R -Fq --include='*.cs' "$api_reference" "$android_source_path"; then
    printf 'Android media backup must request %s.\n' "$api_reference" >&2
    exit 1
  fi
done

printf 'Android Play media permission checks passed.\n'
