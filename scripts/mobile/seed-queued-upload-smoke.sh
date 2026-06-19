#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

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

local_seed_dir="$(mktemp -d "${TMPDIR:-/tmp}/cotton-queued-upload-smoke.XXXXXX")"
remote_seed_dir="/data/local/tmp/cotton-queued-upload-smoke"
trap 'rm -rf "$local_seed_dir"' EXIT

root_cache="$local_seed_dir/root.json"
queue_json="$local_seed_dir/queue.json"
upload_file="$local_seed_dir/$upload_name"
destination_tsv="$local_seed_dir/destination.tsv"

adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cat \
  "files/CottonFolderListings/$instance_key/root.json" > "$root_cache"

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

printf 'Seeded queued upload smoke for %s (%s).\n' "$instance_uri" "$instance_key"
printf 'Transfer:    %s\n' "$transfer_id"
printf 'Destination: %s (%s)\n' "$destination_folder_name" "$destination_id"
printf 'File:        %s (%s bytes)\n' "$upload_name" "$upload_size"
printf 'ContentType: %s\n' "$content_type"

if [[ "$launch_app" -eq 1 ]]; then
  adb -s "$COTTON_ADB_SERIAL" shell am start -n "$COTTON_ANDROID_PACKAGE_ID/crc647f4f3c52a3509f5a.MainActivity"
fi
