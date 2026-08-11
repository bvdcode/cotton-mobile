#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

COTTON_NODE_MATCH_MODE_DEFAULT=exact
COTTON_WAIT_ATTEMPTS=10

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
first_file="242.mp4"
second_file="238.png"
mixed_folder=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a Files multi-select local-share smoke and captures evidence.

Options:
  --package ID          Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL       ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR    Evidence directory. Defaults to a timestamped directory.
  --install-debug       Install the current debug APK with -r before launch.
  --first-file NAME     First visible Files row to select. Defaults to "$first_file".
  --second-file NAME    Second visible Files row to select. Defaults to "$second_file".
  --mixed-folder NAME   Optional folder row to select with --first-file for mixed-selection actions.
  --no-launch           Do not launch the app before capture.
  --help, -h            Show this help.

The app must already have a signed-in session and a Files root containing the
two selected file rows. The smoke downloads the selected files if needed, then
verifies the multi-file local Share files action and Android share UI handoff.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--evidence-dir:evidence_dir"
  "--first-file:first_file"
  "--second-file:second_file"
  "--mixed-folder:mixed_folder"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

if [[ -z "${first_file//[[:space:]]/}" || -z "${second_file//[[:space:]]/}" ]]; then
  printf 'Selected file names must not be blank.\n' >&2
  exit 64
fi

if [[ "$first_file" == "$second_file" ]]; then
  printf 'Selected file names must be different.\n' >&2
  exit 64
fi

if [[ -n "$mixed_folder" && -z "${mixed_folder//[[:space:]]/}" ]]; then
  printf 'Mixed-selection folder name must not be blank.\n' >&2
  exit 64
fi

if [[ -n "$mixed_folder" && ("$mixed_folder" == "$first_file" || "$mixed_folder" == "$second_file") ]]; then
  printf 'Mixed-selection folder name must be different from selected file names.\n' >&2
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
if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-selection-share-local"
fi

mkdir -p "$evidence_dir"







# shellcheck source=smoke-selection-share-support.sh
source "$SCRIPT_DIR/smoke-selection-share-support.sh"

write_metadata
write_checklist

cotton_capture_text_best_effort "00-device.txt" cotton_adb shell getprop ro.product.model
cotton_capture_text_best_effort "01-adb-devices.txt" adb devices
cotton_capture_text_best_effort "02-package.txt" cotton_adb shell dumpsys package "$package_id"
cotton_capture_text_best_effort "03-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/04-install.txt"
fi

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c >/dev/null 2>&1 || true
  cotton_adb shell am force-stop "$package_id" >/dev/null 2>&1 || true
  sleep 1
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/05-launch.txt"
  sleep 4
fi

cotton_wait_for_files_root
ensure_selected_files_local
cotton_wait_for_files_root
validate_mixed_selection_actions
cotton_wait_for_files_root
select_two_files "70"
open_selection_actions "80-share-files-sheet"
cotton_require_xml_text "$actions_xml" "Share files" "Selection action sheet did not expose Share files for local files."

cotton_tap_node_from_xml "$actions_xml" "Share files" exact
sleep 3
wait_for_share_handoff
cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
sleep 1

cotton_capture_text_best_effort "99-logcat.txt" cotton_adb logcat -d -v time
if grep -E "ANR|FATAL EXCEPTION|Input dispatching timed out" "$evidence_dir/99-logcat.txt" > "$evidence_dir/99-logcat-fatal-markers.txt"; then
  printf 'Fatal or ANR markers were found in logcat.\n' >&2
  printf 'Evidence: %s/99-logcat-fatal-markers.txt\n' "$evidence_dir" >&2
  exit 66
fi

{
  printf 'first_file=%s\n' "$first_file"
  printf 'second_file=%s\n' "$second_file"
  printf 'mixed_folder=%s\n' "$mixed_folder"
  printf 'share_handoff=%s\n' "$share_handoff"
} > "$evidence_dir/result.env"

printf 'Selection local share evidence captured in %s\n' "$evidence_dir"
