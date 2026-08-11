#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

instance_uri="https://app.cottoncloud.dev"
destination_name="Mobile smoke folder"
upload_name="queued-run-smoke.txt"
upload_body="queued upload foreground smoke"
content_type="text/plain"
launch_app=1

usage() {
  cat <<EOF
Usage: $(basename "$0") [--instance URI] [--destination NAME] [--name FILE] [--body TEXT] [--content-type MIME] [--no-launch]

Seeds app-private transfer metadata for one destination-backed queued upload.
The destination folder id is read from the app's cached root listing, so open Files
online at least once before running this smoke.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--instance:instance_uri"
  "--destination:destination_name"
  "--name:upload_name"
  "--body:upload_body"
  "--content-type:content_type"
)
COTTON_FLAG_OPTIONS=(
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

instance_key="$(cotton_create_instance_key)"

local_seed_dir="$(mktemp -d "${TMPDIR:-/tmp}/cotton-queued-upload-smoke.XXXXXX")"
remote_seed_dir="/data/local/tmp/cotton-queued-upload-smoke"
trap 'rm -rf "$local_seed_dir"' EXIT

root_cache="$local_seed_dir/root.json"
queue_json="$local_seed_dir/queue.json"
upload_file="$local_seed_dir/$upload_name"
destination_tsv="$local_seed_dir/destination.tsv"

adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cat \
  "files/CottonFolderListings/$instance_key/root.json" > "$root_cache"

cotton_resolve_cached_destination "$root_cache" "$destination_name" "$destination_tsv"
transfer_id="$(cotton_new_uuid)"
transfer_id_n="${transfer_id//-/}"
printf '%s\n' "$upload_body" > "$upload_file"
upload_size="$(wc -c < "$upload_file" | tr -d ' ')"

cat > "$queue_json" <<EOF
{
  "schemaVersion": 1,
  "savedAtUtc": "2026-06-19T21:00:00Z",
  "items": [
    {
      "id": "$transfer_id",
      "kind": 0,
      "displayName": "$upload_name",
      "contentType": "$content_type",
      "destination": {
        "folderId": "$destination_id",
        "folderName": "$destination_folder_name",
        "path": "Default / $destination_folder_name"
      },
      "status": 0,
      "transferredBytes": 0,
      "totalBytes": $upload_size,
      "attemptCount": 0,
      "failureMessage": null,
      "createdAtUtc": "2026-06-19T21:00:00Z",
      "updatedAtUtc": "2026-06-19T21:00:00Z"
    }
  ]
}
EOF

cotton_stage_queued_upload \
  "$COTTON_ADB_SERIAL" \
  "$COTTON_ANDROID_PACKAGE_ID" \
  "$remote_seed_dir" \
  "$queue_json" \
  "$upload_file" \
  "$upload_name" \
  "$instance_key" \
  "$transfer_id_n"

printf 'Seeded queued upload smoke for %s (%s).\n' "$instance_uri" "$instance_key"
printf 'Transfer:    %s\n' "$transfer_id"
printf 'Destination: %s (%s)\n' "$destination_folder_name" "$destination_id"
printf 'File:        %s (%s bytes)\n' "$upload_name" "$upload_size"
printf 'ContentType: %s\n' "$content_type"

if [[ "$launch_app" -eq 1 ]]; then
  adb -s "$COTTON_ADB_SERIAL" shell am start -n "$COTTON_ANDROID_PACKAGE_ID/crc647f4f3c52a3509f5a.MainActivity"
fi
