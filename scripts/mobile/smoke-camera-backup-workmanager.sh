#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

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

while [[ $# -gt 0 ]]; do
  case "$1" in
    --instance)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --instance.\n' >&2
        exit 64
      fi
      instance_uri="$2"
      shift 2
      ;;
    --destination)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --destination.\n' >&2
        exit 64
      fi
      destination_name="$2"
      shift 2
      ;;
    --name)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --name.\n' >&2
        exit 64
      fi
      upload_name="$2"
      shift 2
      ;;
    --body)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --body.\n' >&2
        exit 64
      fi
      upload_body="$2"
      shift 2
      ;;
    --content-type)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --content-type.\n' >&2
        exit 64
      fi
      content_type="$2"
      shift 2
      ;;
    --evidence-dir)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --evidence-dir.\n' >&2
        exit 64
      fi
      evidence_dir="$2"
      shift 2
      ;;
    --no-launch)
      launch_app=0
      shift
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      printf 'Unknown argument: %s\n' "$1" >&2
      exit 64
      ;;
  esac
done

mkdir -p "$evidence_dir"

instance_key="$(
  python3 - "$instance_uri" <<'PY'
import hashlib
import sys
from urllib.parse import urlparse

uri = urlparse(sys.argv[1])
if uri.scheme.lower() not in ("http", "https") or not uri.hostname:
    raise SystemExit("Instance URI must include http(s) scheme and host.")

scheme = uri.scheme.lower()
host = uri.hostname.lower()
default_port = (scheme == "http" and uri.port in (None, 80)) or (scheme == "https" and uri.port in (None, 443))
authority = host if default_port else f"{host}:{uri.port}"
path = "" if uri.path in ("", "/") else uri.path.rstrip("/")
scope = f"{scheme}://{authority}{path}"
print(hashlib.sha256(scope.encode("utf-8")).hexdigest())
PY
)"

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

python3 - "$root_cache" "$destination_name" > "$destination_tsv" <<'PY'
import json
import sys

root_cache, destination_name = sys.argv[1:3]
data = json.load(open(root_cache, encoding="utf-8"))
for entry in data.get("entries", []):
    if entry.get("name") == destination_name and entry.get("type") == 0:
        print(f"{entry['id']}\t{entry['name']}")
        break
else:
    raise SystemExit(f"Destination folder not found in cached root listing: {destination_name}")
PY

IFS=$'\t' read -r destination_id destination_folder_name < "$destination_tsv"
transfer_id="$(python3 - <<'PY'
import uuid
print(uuid.uuid4())
PY
)"
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
adb -s "$COTTON_ADB_SERIAL" shell am force-stop "$COTTON_ANDROID_PACKAGE_ID" >/dev/null 2>&1 || true
adb -s "$COTTON_ADB_SERIAL" shell rm -rf "$remote_seed_dir"
adb -s "$COTTON_ADB_SERIAL" shell mkdir -p "$remote_seed_dir"
adb -s "$COTTON_ADB_SERIAL" push "$queue_json" "$remote_seed_dir/queue.json" >/dev/null
adb -s "$COTTON_ADB_SERIAL" push "$upload_file" "$remote_seed_dir/$upload_name" >/dev/null

transfer_root="files/CottonTransfers/$instance_key"
staged_root="$transfer_root/Staged"

adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" rm -rf "$transfer_root"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" mkdir -p "$staged_root/$transfer_id_n"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cp \
  "$remote_seed_dir/queue.json" \
  "$transfer_root/queue.json"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cp \
  "$remote_seed_dir/$upload_name" \
  "$staged_root/$transfer_id_n/$upload_name"
adb -s "$COTTON_ADB_SERIAL" shell rm -rf "$remote_seed_dir"

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
