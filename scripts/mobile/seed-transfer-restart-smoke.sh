#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

instance_uri="https://app.cottoncloud.dev"
launch_app=1

usage() {
  cat <<EOF
Usage: $(basename "$0") [--instance URI] [--no-launch]

Seeds app-private transfer metadata for an emulator restart smoke:
  - one queued upload with staged content
  - one failed upload with staged content
  - one completed upload with stale staged content that startup cleanup should remove

The script preserves the signed-in app session and only replaces transfer state for
the selected instance scope.
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

queued_id="11111111-1111-1111-1111-111111111111"
failed_id="22222222-2222-2222-2222-222222222222"
completed_id="33333333-3333-3333-3333-333333333333"
queued_id_n="${queued_id//-/}"
failed_id_n="${failed_id//-/}"
completed_id_n="${completed_id//-/}"

local_seed_dir="$(mktemp -d "${TMPDIR:-/tmp}/cotton-transfer-smoke.XXXXXX")"
remote_seed_dir="/data/local/tmp/cotton-transfer-smoke"
trap 'rm -rf "$local_seed_dir"' EXIT

queue_json="$local_seed_dir/queue.json"
queued_file="$local_seed_dir/queued-upload.jpg"
failed_file="$local_seed_dir/failed-upload.jpg"
completed_file="$local_seed_dir/completed-upload.jpg"

printf 'queued transfer restart smoke\n' > "$queued_file"
printf 'failed transfer restart smoke\n' > "$failed_file"
printf 'completed transfer cleanup smoke\n' > "$completed_file"

cat > "$queue_json" <<EOF
{
  "schemaVersion": 1,
  "savedAtUtc": "2026-06-19T12:00:00Z",
  "items": [
    {
      "id": "$queued_id",
      "kind": 0,
      "displayName": "queued-restart-smoke.jpg",
      "status": 0,
      "transferredBytes": 0,
      "totalBytes": 100,
      "attemptCount": 0,
      "failureMessage": null,
      "createdAtUtc": "2026-06-19T12:00:00Z",
      "updatedAtUtc": "2026-06-19T12:01:00Z"
    },
    {
      "id": "$failed_id",
      "kind": 0,
      "displayName": "failed-restart-smoke.jpg",
      "status": 4,
      "transferredBytes": 50,
      "totalBytes": 100,
      "attemptCount": 1,
      "failureMessage": "Offline",
      "createdAtUtc": "2026-06-19T12:02:00Z",
      "updatedAtUtc": "2026-06-19T12:03:00Z"
    },
    {
      "id": "$completed_id",
      "kind": 0,
      "displayName": "completed-restart-smoke.jpg",
      "status": 3,
      "transferredBytes": 100,
      "totalBytes": 100,
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
  "$staged_root/$queued_id_n/queued-restart-smoke.jpg"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cp \
  "$remote_seed_dir/failed-upload.jpg" \
  "$staged_root/$failed_id_n/failed-restart-smoke.jpg"
adb -s "$COTTON_ADB_SERIAL" shell run-as "$COTTON_ANDROID_PACKAGE_ID" cp \
  "$remote_seed_dir/completed-upload.jpg" \
  "$staged_root/$completed_id_n/completed-restart-smoke.jpg"
adb -s "$COTTON_ADB_SERIAL" shell rm -rf "$remote_seed_dir"

printf 'Seeded transfer restart smoke for %s (%s).\n' "$instance_uri" "$instance_key"
printf 'Queued:    %s\n' "$queued_id"
printf 'Failed:    %s\n' "$failed_id"
printf 'Completed: %s\n' "$completed_id"

if [[ "$launch_app" -eq 1 ]]; then
  adb -s "$COTTON_ADB_SERIAL" shell am start -n "$COTTON_ANDROID_PACKAGE_ID/crc647f4f3c52a3509f5a.MainActivity"
fi
