#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

instance_uri="https://app.cottoncloud.dev"
destination_name="Mobile smoke folder"
upload_name="camera-backup-workmanager-smoke.jpg"
upload_body="camera backup workmanager smoke"
content_type="image/jpeg"
evidence_dir="${TMPDIR:-/tmp}/cotton-mobile-evidence/camera-backup-workmanager"
launch_app=1

usage() {
  cat <<EOF
Usage: $(basename "$0") [--instance URI] [--destination NAME] [--name FILE] [--body TEXT] [--content-type MIME] [--evidence-dir DIR] [--no-launch]

Seeds one Camera Backup queued upload, force-stops the app, optionally launches it,
and captures WorkManager/jobscheduler/logcat/queue evidence. The app must already
have a signed-in session and a cached root listing for the selected instance.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--instance:instance_uri"
  "--destination:destination_name"
  "--name:upload_name"
  "--body:upload_body"
  "--content-type:content_type"
  "--evidence-dir:evidence_dir"
)
COTTON_FLAG_OPTIONS=(
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

mkdir -p "$evidence_dir"

instance_key="$(cotton_create_instance_key)"

local_seed_dir="$(mktemp -d "${TMPDIR:-/tmp}/cotton-camera-backup-workmanager.XXXXXX")"
remote_seed_dir="/data/local/tmp/cotton-camera-backup-workmanager"
trap 'rm -rf "$local_seed_dir"' EXIT

root_cache="$local_seed_dir/root.json"
queue_json="$local_seed_dir/queue.json"
upload_file="$local_seed_dir/$upload_name"
destination_tsv="$local_seed_dir/destination.tsv"

adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cat \
  "files/CottonFolderListings/$instance_key/root.json" > "$root_cache"
cp "$root_cache" "$evidence_dir/root-cache.json"

cotton_resolve_cached_destination "$root_cache" "$destination_name" "$destination_tsv"
transfer_id="$(cotton_new_uuid)"
transfer_id_n="${transfer_id//-/}"
printf '%s\n' "$upload_body" > "$upload_file"
upload_size="$(wc -c < "$upload_file" | tr -d ' ')"
source_id="content://media/external/images/media/cotton-workmanager-smoke-$transfer_id_n"

cat > "$queue_json" <<EOF
{
  "schemaVersion": 1,
  "savedAtUtc": "2026-06-19T22:00:00Z",
  "items": [
    {
      "id": "$transfer_id",
      "kind": 0,
      "displayName": "$upload_name",
      "contentType": "$content_type",
      "source": {
        "kind": 1,
        "sourceId": "$source_id",
        "lastModifiedUtc": "2026-06-19T21:55:00Z",
        "sizeBytes": $upload_size,
        "capturedAtUtc": "2026-06-19T21:54:00Z"
      },
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
      "createdAtUtc": "2026-06-19T22:00:00Z",
      "updatedAtUtc": "2026-06-19T22:00:00Z"
    }
  ]
}
EOF
cp "$queue_json" "$evidence_dir/seed-queue.json"

adb -s "$COTTON_ADB_SERIAL" logcat -c >/dev/null
cotton_stage_queued_upload \
  "$COTTON_ADB_SERIAL" \
  "$COTTON_ANDROID_PACKAGE_ID" \
  "$remote_seed_dir" \
  "$queue_json" \
  "$upload_file" \
  "$upload_name" \
  "$instance_key" \
  "$transfer_id_n"

adb -s "$COTTON_ADB_SERIAL" shell dumpsys jobscheduler "$COTTON_ANDROID_PACKAGE_ID" \
  > "$evidence_dir/10-jobs-before-launch.txt" || true

if [[ "$launch_app" -eq 1 ]]; then
  adb -s "$COTTON_ADB_SERIAL" shell am start -n "$COTTON_ANDROID_PACKAGE_ID/crc647f4f3c52a3509f5a.MainActivity" \
    > "$evidence_dir/20-launch.txt"
  sleep 12
fi

adb -s "$COTTON_ADB_SERIAL" shell dumpsys jobscheduler "$COTTON_ANDROID_PACKAGE_ID" \
  > "$evidence_dir/30-jobs-after-launch.txt" || true
adb -s "$COTTON_ADB_SERIAL" shell pidof "$COTTON_ANDROID_PACKAGE_ID" \
  > "$evidence_dir/31-pidof-after-launch.txt" || true
adb -s "$COTTON_ADB_SERIAL" shell dumpsys activity top \
  > "$evidence_dir/32-activity-top-after-launch.txt" || true
adb -s "$COTTON_ADB_SERIAL" shell dumpsys window \
  > "$evidence_dir/33-window-after-launch.txt" || true
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" ls -la databases \
  > "$evidence_dir/34-databases-after-launch.txt" || true
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" ls -la no_backup \
  > "$evidence_dir/35-no-backup-after-launch.txt" || true
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cat "$transfer_root/queue.json" \
  > "$evidence_dir/40-queue-after-launch.json" || true
adb -s "$COTTON_ADB_SERIAL" logcat -d -v time \
  > "$evidence_dir/49-logcat-raw.txt" || true
grep -E 'Cotton|WorkManager|SystemJobService|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/49-logcat-raw.txt" \
  > "$evidence_dir/50-logcat-workmanager.txt" || true

{
  printf 'Instance: %s\n' "$instance_uri"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Package: %s\n' "$COTTON_ANDROID_PACKAGE_ID"
  printf 'Transfer: %s\n' "$transfer_id"
  printf 'Destination: %s (%s)\n' "$destination_folder_name" "$destination_id"
  printf 'Source: %s\n' "$source_id"
  printf 'File: %s (%s bytes)\n' "$upload_name" "$upload_size"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/00-summary.txt"
