#!/usr/bin/env bash
set -euo pipefail

manifest_path="src/Cotton.Mobile/Platforms/Android/AndroidManifest.xml"
policy_path="src/Cotton.Mobile/Platforms/Android/AndroidCameraBackupMediaAccessPolicy.cs"

if [[ ! -f "$manifest_path" ]]; then
  printf 'Android manifest was not found: %s\n' "$manifest_path" >&2
  exit 1
fi

blocked_permissions=(
  "android.permission.READ_MEDIA_IMAGES"
  "android.permission.READ_MEDIA_VIDEO"
  "android.permission.READ_MEDIA_VISUAL_USER_SELECTED"
)

for permission in "${blocked_permissions[@]}"; do
  if grep -Fq "$permission" "$manifest_path"; then
    printf 'Play release manifest must not declare %s.\n' "$permission" >&2
    exit 1
  fi
done

blocked_api_references=(
  "Manifest.Permission.ReadMediaImages"
  "Manifest.Permission.ReadMediaVideo"
)

for api_reference in "${blocked_api_references[@]}"; do
  if grep -Fq "$api_reference" "$policy_path"; then
    printf 'Camera backup media policy must not request %s.\n' "$api_reference" >&2
    exit 1
  fi
done

printf 'Android Play media permission checks passed.\n'
