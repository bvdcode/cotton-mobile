#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

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

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--instance:instance_uri"
  "--destination:destination_name"
  "--permission-state:permission_state"
  "--name:upload_name"
  "--body:upload_body"
  "--content-type:content_type"
  "--evidence-dir:evidence_dir"
  "--wait-seconds:wait_seconds"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
)
cotton_parse_arguments "$@"

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

apply_permission_state() {
  case "$permission_state" in
    allowed)
      {
        cotton_adb shell pm grant "$package_id" android.permission.POST_NOTIFICATIONS || true
        cotton_adb shell pm set-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        cotton_adb shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/04-permission-setup.txt" 2>&1
      ;;
    denied)
      {
        cotton_adb shell pm revoke "$package_id" android.permission.POST_NOTIFICATIONS || true
        cotton_adb shell pm set-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        cotton_adb shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/04-permission-setup.txt" 2>&1
      ;;
  esac
}

capture_notification_state() {
  local prefix="$1"

  cotton_capture_text_best_effort "$prefix-package-permission.txt" cotton_adb shell dumpsys package "$package_id"
  cotton_capture_text_best_effort "$prefix-appops.txt" cotton_adb shell appops get "$package_id" POST_NOTIFICATION
  cotton_capture_text_best_effort "$prefix-notification-dumpsys.txt" cotton_adb shell dumpsys notification --noredact
  grep -E "$package_id|cotton\\.|Upload complete|Upload failed|$upload_name" \
    "$evidence_dir/$prefix-notification-dumpsys.txt" \
    > "$evidence_dir/$prefix-notification-summary.txt" || true
}

capture_queue() {
  local prefix="$1"
  local transfer_root="files/CottonTransfers/$instance_key"

  cotton_adb shell run-as "$package_id" cat "$transfer_root/queue.json" \
    > "$evidence_dir/$prefix-queue.json"
  cotton_adb shell run-as "$package_id" find "$transfer_root/Staged" \
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

instance_key="$(cotton_create_instance_key)"
write_metadata
cotton_capture_text_best_effort "01-adb-devices.txt" adb devices

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/02-install.txt"
fi

cotton_capture_text_best_effort "03-package.txt" cotton_adb shell dumpsys package "$package_id"
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
cotton_adb logcat -c >/dev/null 2>&1 || true
cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/13-launch.txt"
sleep "$wait_seconds"

capture_queue "20-after-run"
capture_notification_state "20-after-run"
cotton_capture_screen "22-after-run"
cotton_capture_text_best_effort "90-logcat.txt" cotton_adb logcat -d -v threadtime

validate_permission_state
validate_queue
validate_notification_result

{
  printf 'Transfer notification %s smoke passed.\n' "$permission_state"
  printf 'Package: %s\n' "$package_id"
  printf 'Upload: %s\n' "$upload_name"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
