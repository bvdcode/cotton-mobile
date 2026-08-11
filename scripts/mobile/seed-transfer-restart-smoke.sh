#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

instance_uri="https://app.cottoncloud.dev"
destination_name="Mobile smoke folder"
run_id="$(date -u +%Y%m%dT%H%M%SZ)"
launch_app=1

usage() {
  cat <<EOF
Usage: $(basename "$0") [--instance URI] [--destination NAME] [--run-id ID] [--no-launch]

Seeds app-private transfer metadata for an emulator restart smoke:
  - one queued upload with staged content
  - one failed upload with staged content
  - one completed upload with stale staged content that startup cleanup should remove

The script preserves the signed-in app session and only replaces transfer state for
the selected instance scope.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--instance:instance_uri"
  "--destination:destination_name"
  "--run-id:run_id"
)
COTTON_FLAG_OPTIONS=(
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

if [[ -z "${run_id//[[:space:]]/}" || "$run_id" == *"/"* ]]; then
  printf 'Run id must not be blank and must not contain a slash.\n' >&2
  exit 64
fi

instance_key="$(cotton_create_instance_key)"

queued_id="11111111-1111-1111-1111-111111111111"
failed_id="22222222-2222-2222-2222-222222222222"
completed_id="33333333-3333-3333-3333-333333333333"
queued_id_n="${queued_id//-/}"
failed_id_n="${failed_id//-/}"
completed_id_n="${completed_id//-/}"
queued_display_name="queued-restart-smoke-$run_id.jpg"
failed_display_name="failed-restart-smoke-$run_id.jpg"
completed_display_name="completed-restart-smoke-$run_id.jpg"

local_seed_dir="$(mktemp -d "${TMPDIR:-/tmp}/cotton-transfer-smoke.XXXXXX")"
remote_seed_dir="/data/local/tmp/cotton-transfer-smoke"
trap 'rm -rf "$local_seed_dir"' EXIT

queue_json="$local_seed_dir/queue.json"
queued_file="$local_seed_dir/queued-upload.jpg"
failed_file="$local_seed_dir/failed-upload.jpg"
completed_file="$local_seed_dir/completed-upload.jpg"
root_cache="$local_seed_dir/root.json"
destination_tsv="$local_seed_dir/destination.tsv"

printf 'queued transfer restart smoke\n' > "$queued_file"
printf 'failed transfer restart smoke\n' > "$failed_file"
printf 'completed transfer cleanup smoke\n' > "$completed_file"
queued_size="$(wc -c < "$queued_file" | tr -d ' ')"
failed_size="$(wc -c < "$failed_file" | tr -d ' ')"
completed_size="$(wc -c < "$completed_file" | tr -d ' ')"

adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cat \
  "files/CottonFolderListings/$instance_key/root.json" > "$root_cache"

python3 - "$root_cache" "$destination_name" > "$destination_tsv" <<'PY'
import json
import sys

root_cache, destination_name = sys.argv[1:3]
data = json.load(open(root_cache, encoding="utf-8"))
if destination_name == data.get("folderName"):
    print(f"{data['folderId']}\t{data['folderName']}\t{data['folderName']}")
    raise SystemExit(0)

for entry in data.get("entries", []):
    if entry.get("name") == destination_name and entry.get("type") == 0:
        print(f"{entry['id']}\t{entry['name']}\t{data.get('folderName', 'Files')} / {entry['name']}")
        break
else:
    raise SystemExit(f"Destination folder not found in cached root listing: {destination_name}")
PY

IFS=$'\t' read -r destination_id destination_folder_name destination_path < "$destination_tsv"

cat > "$queue_json" <<EOF
{
  "schemaVersion": 1,
  "savedAtUtc": "2026-06-19T12:00:00Z",
  "items": [
    {
      "id": "$queued_id",
      "kind": 0,
      "displayName": "$queued_display_name",
      "contentType": "image/jpeg",
      "source": null,
      "destination": {
        "folderId": "$destination_id",
        "folderName": "$destination_folder_name",
        "path": "$destination_path"
      },
      "status": 0,
      "transferredBytes": 0,
      "totalBytes": $queued_size,
      "attemptCount": 0,
      "failureMessage": null,
      "createdAtUtc": "2026-06-19T12:00:00Z",
      "updatedAtUtc": "2026-06-19T12:01:00Z"
    },
    {
      "id": "$failed_id",
      "kind": 0,
      "displayName": "$failed_display_name",
      "contentType": "image/jpeg",
      "source": null,
      "destination": {
        "folderId": "$destination_id",
        "folderName": "$destination_folder_name",
        "path": "$destination_path"
      },
      "status": 4,
      "transferredBytes": 15,
      "totalBytes": $failed_size,
      "attemptCount": 1,
      "failureMessage": "Offline",
      "createdAtUtc": "2026-06-19T12:02:00Z",
      "updatedAtUtc": "2026-06-19T12:03:00Z"
    },
    {
      "id": "$completed_id",
      "kind": 0,
      "displayName": "$completed_display_name",
      "contentType": "image/jpeg",
      "source": null,
      "destination": {
        "folderId": "$destination_id",
        "folderName": "$destination_folder_name",
        "path": "$destination_path"
      },
      "status": 3,
      "transferredBytes": $completed_size,
      "totalBytes": $completed_size,
      "attemptCount": 1,
      "failureMessage": null,
      "createdAtUtc": "2026-06-19T12:04:00Z",
      "updatedAtUtc": "2026-06-19T12:05:00Z"
    }
  ]
}
EOF

adb -s "$COTTON_ADB_SERIAL" shell am force-stop "$COTTON_ANDROID_PACKAGE_ID" >/dev/null 2>&1 || true
adb -s "$COTTON_ADB_SERIAL" shell rm -rf "$remote_seed_dir"
adb -s "$COTTON_ADB_SERIAL" shell mkdir -p "$remote_seed_dir"
adb -s "$COTTON_ADB_SERIAL" push "$queue_json" "$remote_seed_dir/queue.json" >/dev/null
adb -s "$COTTON_ADB_SERIAL" push "$queued_file" "$remote_seed_dir/queued-upload.jpg" >/dev/null
adb -s "$COTTON_ADB_SERIAL" push "$failed_file" "$remote_seed_dir/failed-upload.jpg" >/dev/null
adb -s "$COTTON_ADB_SERIAL" push "$completed_file" "$remote_seed_dir/completed-upload.jpg" >/dev/null

transfer_root="files/CottonTransfers/$instance_key"
staged_root="$transfer_root/Staged"

adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" rm -rf "$transfer_root"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" mkdir -p \
  "$staged_root/$queued_id_n" \
  "$staged_root/$failed_id_n" \
  "$staged_root/$completed_id_n"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cp \
  "$remote_seed_dir/queue.json" \
  "$transfer_root/queue.json"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cp \
  "$remote_seed_dir/queued-upload.jpg" \
  "$staged_root/$queued_id_n/$queued_display_name"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cp \
  "$remote_seed_dir/failed-upload.jpg" \
  "$staged_root/$failed_id_n/$failed_display_name"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cp \
  "$remote_seed_dir/completed-upload.jpg" \
  "$staged_root/$completed_id_n/$completed_display_name"
adb -s "$COTTON_ADB_SERIAL" shell rm -rf "$remote_seed_dir"

printf 'Seeded transfer restart smoke for %s (%s).\n' "$instance_uri" "$instance_key"
printf 'Run id:    %s\n' "$run_id"
printf 'Queued:    %s\n' "$queued_id"
printf 'Failed:    %s\n' "$failed_id"
printf 'Completed: %s\n' "$completed_id"

if [[ "$launch_app" -eq 1 ]]; then
  adb -s "$COTTON_ADB_SERIAL" shell am start -n "$COTTON_ANDROID_PACKAGE_ID/crc647f4f3c52a3509f5a.MainActivity"
fi
