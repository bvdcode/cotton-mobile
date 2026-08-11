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
kind="photo"
count=2
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
preflight_only=0
seed_only=0
wait_seconds=6
expected_version_code=""
expected_version_name=""
files_root_xml=""

declare -a media_names=()

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a selected-photo/video upload smoke:
  1. Seeds real Android shared-media items.
  2. Opens Cotton Files and the selected-media picker flow.
  3. Captures picker, Files, Transfers, MediaStore, queue, staging, and logcat evidence.
  4. Validates queued Transfers records use the SelectedMedia source kind.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --kind photo|video        Selected media kind to test. Defaults to photo.
  --count N                 Number of seeded items to select. Defaults to 2.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --expected-version-code N Require the installed package versionCode in dumpsys.
  --expected-version-name N Require the installed package versionName in dumpsys.
  --wait-seconds N          Seconds to wait after returning from the picker. Defaults to 6.
  --preflight-only          Capture package and seeded-media state, then exit.
  --seed-only               Seed shared-media items and exit after MediaStore validation.
  --no-launch               Do not launch the app before capture.
  --help, -h                Show this help.

The app must already have a signed-in session and a cached Files root for the
selected instance. Full mode is intentionally interactive because Android's
system photo/video picker requires user selection.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--instance:instance_uri"
  "--kind:kind"
  "--count:count"
  "--evidence-dir:evidence_dir"
  "--expected-version-code:expected_version_code"
  "--expected-version-name:expected_version_name"
  "--wait-seconds:wait_seconds"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--preflight-only:preflight_only:1"
  "--seed-only:seed_only:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

if [[ "$kind" != "photo" && "$kind" != "video" ]]; then
  printf 'Kind must be either photo or video.\n' >&2
  exit 64
fi

if ! [[ "$count" =~ ^[0-9]+$ ]] || [[ "$count" -lt 1 ]]; then
  printf 'Count must be a positive integer.\n' >&2
  exit 64
fi

if [[ "$kind" == "photo" && "$count" -gt 20 ]]; then
  printf 'Photo count must not exceed 20.\n' >&2
  exit 64
fi

if [[ "$kind" == "video" && "$count" -gt 10 ]]; then
  printf 'Video count must not exceed 10.\n' >&2
  exit 64
fi

if ! [[ "$wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Wait seconds must be a non-negative integer.\n' >&2
  exit 64
fi

if [[ "$preflight_only" -eq 0 && "$seed_only" -eq 0 && ! -t 0 ]]; then
  printf 'Full selected-media smoke requires an interactive terminal.\n' >&2
  printf 'Use --preflight-only or --seed-only for non-interactive evidence.\n' >&2
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
run_id="cotton-selected-media-$kind-$timestamp"

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-selected-media-upload-$kind"
fi

mkdir -p "$evidence_dir"








# shellcheck source=smoke-selected-media-seed.sh
source "$SCRIPT_DIR/smoke-selected-media-seed.sh"
# shellcheck source=smoke-selected-media-flow.sh
source "$SCRIPT_DIR/smoke-selected-media-flow.sh"

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
cotton_capture_text_best_effort "04-version.txt" cotton_adb shell getprop ro.build.version.sdk
validate_package_version

seed_shared_media
write_metadata
write_checklist

if [[ "$seed_only" -eq 1 ]]; then
  printf 'Selected-media seed evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$preflight_only" -eq 1 ]]; then
  printf 'Selected-media preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

instance_key="$(cotton_create_instance_key)"

cotton_adb logcat -c >/dev/null 2>&1 || true
if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/19-launch.txt"
  sleep 4
fi

cotton_wait_for_files_root "20-files-root"
cotton_require_xml_text "$files_root_xml" "Add files" "Files Add action is not visible."

tap_text "$files_root_xml" "Add files"
sleep 2
cotton_capture_screen "30-add-actions"
cotton_require_xml_text "$evidence_dir/30-add-actions.xml" "Upload..." "Upload action is not visible."
tap_text "$evidence_dir/30-add-actions.xml" "Upload..."
sleep 2
cotton_capture_screen "31-upload-actions"
if [[ "$kind" == "photo" ]]; then
  cotton_require_xml_text "$evidence_dir/31-upload-actions.xml" "Upload photo" "Upload photo action is not visible."
  tap_text "$evidence_dir/31-upload-actions.xml" "Upload photo"
else
  cotton_require_xml_text "$evidence_dir/31-upload-actions.xml" "Upload video" "Upload video action is not visible."
  tap_text "$evidence_dir/31-upload-actions.xml" "Upload video"
fi

sleep 3
cotton_capture_screen "40-selected-media-picker"

printf '\nSeeded %s items to select:\n' "$kind"
printf '  %s\n' "${media_names[@]}"
cotton_wait_for_operator "Select all seeded items in the Android picker, then tap the picker confirmation button."

sleep "$wait_seconds"
cotton_wait_for_files_root "50-files-after-picker"
capture_transfer_state "60-after-picker" "$instance_key"
validate_selected_media_queue > "$evidence_dir/62-validation-summary.json"

open_transfers_page
sleep 3
cotton_capture_screen "70-transfers"
cotton_require_xml_text "$evidence_dir/70-transfers.xml" "Transfers" "Transfers page did not open."

cotton_capture_text_best_effort "90-logcat-raw.txt" cotton_adb logcat -d -v time
grep -E 'Cotton|WorkManager|SystemJobService|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true
if grep -E 'FATAL EXCEPTION|AndroidRuntime.*FATAL|mono-rt.*SIG' "$evidence_dir/90-logcat-raw.txt" \
    > "$evidence_dir/92-fatal-logcat.txt"; then
  printf 'Fatal runtime crash found in logcat.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir/92-fatal-logcat.txt" >&2
  exit 66
fi

{
  printf 'Selected-media upload smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Kind: %s\n' "$kind"
  printf 'Count: %s\n' "$count"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
