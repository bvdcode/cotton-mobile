#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

COTTON_NODE_MATCH_MODE_DEFAULT=exact

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev/"
destination_name="Mobile smoke folder"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
grant_media=1
launch_app=1
choose_destination=1
preflight_only=0
queue_wait_seconds=10
media_name=""
files_root_xml=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a camera-backup media-scan smoke and captures UI, MediaStore, permission,
and transfer-queue evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --destination NAME        Folder to choose as the backup destination.
  --media-name NAME         Seeded image display name. Defaults to a timestamped PNG.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --no-grant-media          Do not grant Android media permissions with pm grant.
  --skip-destination        Use the existing camera-backup destination.
  --queue-wait SECONDS      Seconds to wait after tapping Queue now. Defaults to 10.
  --preflight-only          Capture package/permission state and exit.
  --no-launch               Do not launch the app before capture.
  --help, -h                Show this help.

The app must already have a signed-in session and a cached root listing for the
selected instance. The smoke validates that a real MediaStore image is visible
to the app, Camera Backup shows full media access, Queue now runs, and the
seeded image appears in the transfer queue as a Camera Backup source.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--instance:instance_uri"
  "--destination:destination_name"
  "--media-name:media_name"
  "--evidence-dir:evidence_dir"
  "--queue-wait:queue_wait_seconds"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--no-grant-media:grant_media:0"
  "--skip-destination:choose_destination:0"
  "--preflight-only:preflight_only:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"

if [[ -z "$media_name" ]]; then
  media_name="cotton-camera-backup-stage15-$timestamp.png"
fi

if [[ -z "${destination_name//[[:space:]]/}" ]]; then
  printf 'Destination name must not be blank.\n' >&2
  exit 64
fi

if [[ -z "${media_name//[[:space:]]/}" || "$media_name" == *"/"* ]]; then
  printf 'Media name must not be blank and must not contain a slash.\n' >&2
  exit 64
fi

if ! [[ "$queue_wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Queue wait must be a non-negative integer.\n' >&2
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

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-camera-backup-media-scan"
fi

mkdir -p "$evidence_dir"







# shellcheck source=smoke-camera-backup-media-support.sh
source "$SCRIPT_DIR/smoke-camera-backup-media-support.sh"

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

if [[ "$grant_media" -eq 1 ]]; then
  grant_media_permissions
fi

cotton_capture_text_best_effort "03-package.txt" cotton_adb shell dumpsys package "$package_id"
cotton_capture_text_best_effort "04-appops.txt" cotton_adb shell cmd appops get --uid "$package_id"

local_seed_dir="$(mktemp -d "${TMPDIR:-/tmp}/cotton-camera-backup-media.XXXXXX")"
trap 'rm -rf "$local_seed_dir"' EXIT
local_media_file="$local_seed_dir/$media_name"
generate_media_file "$local_media_file"
seed_media_store "$local_media_file"

if [[ "$preflight_only" -eq 1 ]]; then
  printf 'Preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c >/dev/null 2>&1 || true
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/19-launch.txt"
  sleep 4
fi

cotton_wait_for_files_root
tap_text "$files_root_xml" "Backup"
sleep 4
cotton_capture_screen "30-backup"
cotton_require_xml_text "$evidence_dir/30-backup.xml" "Camera Backup" "Camera Backup page did not open."
cotton_require_xml_text "$evidence_dir/30-backup.xml" "Media Access" "Media Access state is not visible."
cotton_require_xml_text "$evidence_dir/30-backup.xml" "Allowed" "Camera Backup does not have full media access."

if [[ "$choose_destination" -eq 1 ]]; then
  choose_backup_destination
  backup_xml="$evidence_dir/42-destination-saved.xml"
else
  backup_xml="$evidence_dir/30-backup.xml"
fi

tap_text "$backup_xml" "Queue now"
sleep "$queue_wait_seconds"
cotton_capture_screen "50-queue-now"
cotton_require_xml_text "$evidence_dir/50-queue-now.xml" "Camera Backup" "Camera Backup page was lost after Queue now."
require_xml_any_text "$evidence_dir/50-queue-now.xml" \
  "Queue status was not visible after Queue now." \
  "camera backup upload" \
  "camera backup uploads"

validate_queue_item "$instance_key" | tee "$evidence_dir/63-queue-smoke-item-summary.json"

cotton_capture_text_best_effort "90-logcat-raw.txt" cotton_adb logcat -d -v time
grep -E 'Cotton|WorkManager|SystemJobService|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true

{
  printf 'Camera backup media-scan smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Destination: %s\n' "$destination_name"
  printf 'Media: %s\n' "$media_name"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
