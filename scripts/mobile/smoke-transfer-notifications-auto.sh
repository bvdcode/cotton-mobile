#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
destination_name="Mobile smoke folder"
permission_state="allowed"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
wait_seconds=10
upload_name=""
upload_body=""
content_type="text/plain"

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a non-interactive transfer-notification permission smoke for the current
Android build. It seeds one queued upload, launches the app so startup restore
resumes the transfer, then validates notification dumpsys output for the chosen
POST_NOTIFICATIONS state.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Cotton instance URI. Defaults to $instance_uri.
  --destination NAME        Destination folder name in cached root. Defaults to "$destination_name".
  --permission-state STATE  Android notification state: allowed or denied.
  --name FILE               Seed upload file name. Defaults to a timestamped txt file.
  --body TEXT               Seed upload body.
  --content-type MIME       Seed upload MIME type. Defaults to "$content_type".
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before seeding.
  --wait-seconds N          Seconds to wait after launch. Defaults to 10.
  --help, -h                Show this help.

The app must already have a signed-in session and a cached root listing for the
selected instance.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --package)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --package.\n' >&2
        exit 64
      fi
      package_id="$2"
      shift 2
      ;;
    --serial)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --serial.\n' >&2
        exit 64
      fi
      serial="$2"
      shift 2
      ;;
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
    --permission-state)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --permission-state.\n' >&2
        exit 64
      fi
      permission_state="$2"
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
    --install-debug)
      install_debug=1
      shift
      ;;
    --wait-seconds)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --wait-seconds.\n' >&2
        exit 64
      fi
      wait_seconds="$2"
      shift 2
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

case "$permission_state" in
  allowed|denied)
    ;;
  *)
    printf 'Invalid --permission-state: %s. Expected allowed or denied.\n' "$permission_state" >&2
    exit 64
    ;;
esac

if ! [[ "$wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Wait seconds must be a non-negative integer.\n' >&2
  exit 64
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if ! command -v python3 >/dev/null 2>&1; then
  printf 'python3 was not found.\n' >&2
  exit 127
fi

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
if [[ -z "$upload_name" ]]; then
  upload_name="notification-$permission_state-smoke-$timestamp.txt"
fi

if [[ -z "$upload_body" ]]; then
  upload_body="Cotton transfer notification $permission_state smoke $timestamp"
fi

if [[ -z "${upload_name//[[:space:]]/}" || "$upload_name" == *"/"* ]]; then
  printf 'Upload name must not be blank and must not contain a slash.\n' >&2
  exit 64
fi

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-transfer-notifications-$permission_state-auto"
fi

mkdir -p "$evidence_dir"

adb_device() {
  adb -s "$serial" "$@"
}

capture_text() {
  local name="$1"
  shift
  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed: %q\n' "$1" >> "$evidence_dir/$name"
  fi
}

create_instance_key() {
  python3 - "$instance_uri" <<'PY'
import hashlib
import sys
from urllib.parse import urlparse

uri = urlparse(sys.argv[1])
if uri.scheme.lower() not in ("http", "https") or not uri.hostname:
    raise SystemExit("Instance URI must include http(s) scheme and host.")

scheme = uri.scheme.lower()
host = uri.hostname.lower()
default_port = (scheme == "http" and uri.port in (None, 80)) or (
    scheme == "https" and uri.port in (None, 443)
)
authority = host if default_port else f"{host}:{uri.port}"
path = "" if uri.path in ("", "/") else uri.path.rstrip("/")
scope = f"{scheme}://{authority}{path}"
print(hashlib.sha256(scope.encode("utf-8")).hexdigest())
PY
}

apply_permission_state() {
  case "$permission_state" in
    allowed)
      {
        adb_device shell pm grant "$package_id" android.permission.POST_NOTIFICATIONS || true
        adb_device shell pm set-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/04-permission-setup.txt" 2>&1
      ;;
    denied)
      {
        adb_device shell pm revoke "$package_id" android.permission.POST_NOTIFICATIONS || true
        adb_device shell pm set-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/04-permission-setup.txt" 2>&1
      ;;
  esac
}

capture_notification_state() {
  local prefix="$1"

  capture_text "$prefix-package-permission.txt" adb_device shell dumpsys package "$package_id"
  capture_text "$prefix-appops.txt" adb_device shell appops get "$package_id" POST_NOTIFICATION
  capture_text "$prefix-notification-dumpsys.txt" adb_device shell dumpsys notification --noredact
  grep -E "$package_id|cotton\\.|Upload complete|Upload failed|$upload_name" \
    "$evidence_dir/$prefix-notification-dumpsys.txt" \
    > "$evidence_dir/$prefix-notification-summary.txt" || true
}

capture_screen() {
  local prefix="$1"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window
  if ! adb_device exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  if adb_device shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    if ! adb_device pull /sdcard/cotton-window.xml "$evidence_dir/$prefix.xml" > "$evidence_dir/$prefix-pull-xml.log" 2>&1; then
      rm -f "$evidence_dir/$prefix.xml"
    fi
    adb_device shell rm -f /sdcard/cotton-window.xml >/dev/null 2>&1 || true
  fi
}

capture_queue() {
  local prefix="$1"
  local transfer_root="files/CottonTransfers/$instance_key"

  adb_device shell run-as "$package_id" cat "$transfer_root/queue.json" \
    > "$evidence_dir/$prefix-queue.json"
  adb_device shell run-as "$package_id" find "$transfer_root/Staged" \
    -maxdepth 2 -type f | sort > "$evidence_dir/$prefix-staged-files.txt" || true
}

validate_permission_state() {
  local package_permission="$evidence_dir/20-after-run-package-permission.txt"
  local appops="$evidence_dir/20-after-run-appops.txt"

  case "$permission_state" in
    allowed)
      if ! grep -Fq "android.permission.POST_NOTIFICATIONS: granted=true" "$package_permission"; then
        printf 'POST_NOTIFICATIONS is not granted in allowed run.\n' >&2
        exit 66
      fi
      ;;
    denied)
      if ! grep -Fq "android.permission.POST_NOTIFICATIONS: granted=false" "$package_permission"; then
        printf 'POST_NOTIFICATIONS is not denied in denied run.\n' >&2
        exit 66
      fi
      if ! grep -Eq 'POST_NOTIFICATION: ignore|POST_NOTIFICATION: deny' "$appops"; then
        printf 'POST_NOTIFICATION appop is not blocked in denied run.\n' >&2
        exit 66
      fi
      ;;
  esac
}

validate_queue() {
  python3 - "$evidence_dir/20-after-run-queue.json" "$upload_name" \
    > "$evidence_dir/21-transfer-summary.json" <<'PY'
import json
import sys

queue_path, upload_name = sys.argv[1:3]
data = json.load(open(queue_path, encoding="utf-8"))
items = [item for item in data.get("items", []) if item.get("displayName") == upload_name]
if not items:
    raise SystemExit(f"Missing transfer for {upload_name}")
item = items[-1]
if item.get("status") not in (3, 4):
    raise SystemExit(f"Transfer did not reach terminal state: {item.get('status')}")
if item.get("failureMessage") and "Object already exists" in item["failureMessage"]:
    raise SystemExit(f"Upload hit stale server-name conflict: {item['failureMessage']}")
print(json.dumps(
    {
        "displayName": item.get("displayName"),
        "status": item.get("status"),
        "transferredBytes": item.get("transferredBytes"),
        "totalBytes": item.get("totalBytes"),
        "failureMessage": item.get("failureMessage"),
    },
    indent=2,
))
PY
}

validate_notification_result() {
  local summary="$evidence_dir/20-after-run-notification-summary.txt"
  local dumpsys="$evidence_dir/20-after-run-notification-dumpsys.txt"

  case "$permission_state" in
    allowed)
      if ! grep -Fq "$upload_name" "$dumpsys"; then
        printf 'Allowed run did not post a notification for %s.\n' "$upload_name" >&2
        exit 66
      fi
      if ! grep -Eq 'Upload complete|Upload failed' "$summary"; then
        printf 'Allowed run did not show a transfer outcome notification.\n' >&2
        exit 66
      fi
      ;;
    denied)
      if grep -Fq "$upload_name" "$dumpsys"; then
        printf 'Denied run still posted a notification for %s.\n' "$upload_name" >&2
        exit 66
      fi
      ;;
  esac
}

write_metadata() {
  {
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'destination=%s\n' "$destination_name"
    printf 'permission_state=%s\n' "$permission_state"
    printf 'upload_name=%s\n' "$upload_name"
    printf 'content_type=%s\n' "$content_type"
    printf 'android_notification_permission_docs=https://developer.android.com/develop/ui/views/notifications/notification-permission\n'
  } > "$evidence_dir/00-metadata.txt"
}

instance_key="$(create_instance_key)"
write_metadata
capture_text "01-adb-devices.txt" adb devices

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  adb_device install --no-incremental -r "$COTTON_ANDROID_APK" > "$evidence_dir/02-install.txt"
fi

capture_text "03-package.txt" adb_device shell dumpsys package "$package_id"
apply_permission_state
capture_notification_state "10-before-run"

COTTON_ANDROID_PACKAGE_ID="$package_id" \
COTTON_ADB_SERIAL="$serial" \
  "$SCRIPT_DIR/seed-queued-upload-smoke.sh" \
    --instance "$instance_uri" \
    --destination "$destination_name" \
    --name "$upload_name" \
    --body "$upload_body" \
    --content-type "$content_type" \
    --no-launch \
    > "$evidence_dir/11-seed-upload.txt" 2>&1

capture_queue "12-before-launch"
adb_device logcat -c >/dev/null 2>&1 || true
adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/13-launch.txt"
sleep "$wait_seconds"

capture_queue "20-after-run"
capture_notification_state "20-after-run"
capture_screen "22-after-run"
capture_text "90-logcat.txt" adb_device logcat -d -v threadtime

validate_permission_state
validate_queue
validate_notification_result

{
  printf 'Transfer notification %s smoke passed.\n' "$permission_state"
  printf 'Package: %s\n' "$package_id"
  printf 'Upload: %s\n' "$upload_name"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
