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
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
preflight_only=0
seed_only=0
skip_source_app_file=0
queue_text_share=0
expected_version_code=""
expected_version_name=""
share_text=""
share_file_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a share-to-Cotton smoke and captures Capture Inbox evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --share-text TEXT         Shell-safe text token for the automated ACTION_SEND text share.
  --share-file-name NAME    Seed file name for source-app file share. Defaults to a timestamped txt file.
  --preflight-only          Capture device/package/version state and exit.
  --seed-only               Seed the Android Downloads source-share file and exit.
  --skip-source-app-file    Skip the interactive source-app file-share capture.
  --queue-text-share        After automated text intake, choose the current folder and queue it.
  --no-launch               Do not launch the app before capture.
  --help, -h                Show this help.

The script uses adb to prove Cotton receives ACTION_SEND text shares, captures
known shell URI edge cases, and can optionally pause for a real source-app file
share so Android grants temporary read access like it does for users.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--evidence-dir:evidence_dir"
  "--expected-version-code:expected_version_code"
  "--expected-version-name:expected_version_name"
  "--share-text:share_text"
  "--share-file-name:share_file_name"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--preflight-only:preflight_only:1"
  "--seed-only:seed_only:1"
  "--skip-source-app-file:skip_source_app_file:1"
  "--queue-text-share:queue_text_share:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
if [[ -z "$share_text" ]]; then
  share_text="CottonShareSmoke-$timestamp"
fi

if [[ -z "$share_file_name" ]]; then
  share_file_name="cotton-share-source-$timestamp.txt"
fi

if [[ -z "${share_text//[[:space:]]/}" ]]; then
  printf 'Share text must not be blank.\n' >&2
  exit 64
fi

if [[ "$share_text" =~ [[:space:]] ]]; then
  printf 'Share text must not contain whitespace for deterministic adb automation.\n' >&2
  exit 64
fi

if [[ -z "${share_file_name//[[:space:]]/}" || "$share_file_name" == *"/"* ]]; then
  printf 'Share file name must not be blank and must not contain a slash.\n' >&2
  exit 64
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$queue_text_share" -eq 1 ]] && ! command -v python3 >/dev/null 2>&1; then
  printf 'python3 was not found.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && "$seed_only" -eq 0 && "$skip_source_app_file" -eq 0 && ! -t 0 ]]; then
  printf 'The source-app file-share step requires an interactive terminal.\n' >&2
  printf 'Use --skip-source-app-file to capture automated share evidence only.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-share-to-cotton"
fi

mkdir -p "$evidence_dir"

# shellcheck source=smoke-share-to-cotton-support.sh
source "$SCRIPT_DIR/smoke-share-to-cotton-support.sh"

cotton_capture_text_best_effort "00-device.txt" cotton_adb shell getprop ro.product.model
cotton_capture_text_best_effort "01-adb-devices.txt" adb devices
cotton_capture_text_best_effort "02-window.txt" cotton_adb shell dumpsys window

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/08-install.txt"
fi

cotton_capture_text_best_effort "03-package-path.txt" cotton_adb shell pm path "$package_id"
cotton_capture_text_best_effort "04-package.txt" cotton_adb shell dumpsys package "$package_id"
cotton_capture_text_best_effort "05-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
cotton_verify_expected_version_file "$evidence_dir/05-package-version.txt"

seed_share_file

if [[ "$preflight_only" -eq 1 || "$seed_only" -eq 1 ]]; then
  printf 'Share-to-Cotton preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c || true
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/09-launch.txt"
  sleep 3
fi

cotton_capture_screen "10-launch"

start_text_share
sleep 3
cotton_capture_screen "20-text-share-inbox"
cotton_require_xml_text "$evidence_dir/20-text-share-inbox.xml" "Capture Inbox" "Capture Inbox did not open for text share."
cotton_require_xml_text "$evidence_dir/20-text-share-inbox.xml" "$share_text" "Text share payload is not visible in Capture Inbox."
cotton_require_xml_text "$evidence_dir/20-text-share-inbox.xml" "Text share captured" "Text share detail is not visible."
cotton_require_xml_text "$evidence_dir/20-text-share-inbox.xml" "Choose folder" "Text share is not waiting for a destination."
cotton_require_xml_text "$evidence_dir/20-text-share-inbox.xml" "No destination selected" "Text share destination state is not visible."
cotton_require_xml_text "$evidence_dir/20-text-share-inbox.xml" "Text" "Text share kind is not visible."

if [[ "$queue_text_share" -eq 1 ]]; then
  cotton_tap_node_from_xml "$evidence_dir/20-text-share-inbox.xml" "Destination" exact
  wait_for_text_capture \
    "21-text-share-destination" \
    "Choose Destination" \
    "Destination picker did not open for text share."
  cotton_require_xml_text "$evidence_dir/21-text-share-destination.xml" "Choose this folder" "Destination picker did not expose current-folder selection."

  cotton_tap_node_from_xml "$evidence_dir/21-text-share-destination.xml" "Choose this folder" exact
  wait_for_text_capture \
    "22-text-share-destination-saved" \
    "Destination:" \
    "Capture Inbox did not show saved text-share destination."
  cotton_require_xml_text "$evidence_dir/22-text-share-destination-saved.xml" "$share_text" "Text share payload was lost after destination selection."
  cotton_require_xml_text "$evidence_dir/22-text-share-destination-saved.xml" "Ready" "Text share was not ready after destination selection."

  cotton_tap_node_from_xml "$evidence_dir/22-text-share-destination-saved.xml" "Queue" exact
  wait_for_text_capture \
    "23-text-share-queued" \
    "Queued" \
    "Text share did not show queued upload status."
fi

seed_content_uri="$(content_uri_for_seeded_file)"
start_content_uri_edge_share "$seed_content_uri"
sleep 3
cotton_capture_screen "30-shell-content-uri-edge"
cotton_require_xml_text "$evidence_dir/30-shell-content-uri-edge.xml" "Capture Inbox" "Capture Inbox did not stay visible for shell content URI edge case."
cotton_require_xml_text "$evidence_dir/30-shell-content-uri-edge.xml" "Needs access" "Shell content URI edge case did not surface missing access."
cotton_require_xml_text "$evidence_dir/30-shell-content-uri-edge.xml" "Android revoked access to the shared content." "Missing-permission message is not visible."

start_file_uri_edge_share
sleep 3
cotton_capture_screen "40-file-uri-edge"
cotton_require_xml_text "$evidence_dir/40-file-uri-edge.xml" "Capture Inbox" "Capture Inbox did not stay visible for file URI edge case."
cotton_require_xml_text "$evidence_dir/40-file-uri-edge.xml" "$share_file_name" "File URI edge case did not show the source file name."
cotton_require_xml_text "$evidence_dir/40-file-uri-edge.xml" "Unsupported" "File URI edge case did not surface unsupported status."
cotton_require_xml_text "$evidence_dir/40-file-uri-edge.xml" "Android could not open the shared content." "Unsupported-content message is not visible."

if [[ "$skip_source_app_file" -eq 0 ]]; then
  cotton_wait_for_operator "Share $share_file_name from Android Files, Photos, Drive, or another source app to Cotton."
  cotton_capture_screen "50-source-app-file-share"
  cotton_require_xml_text "$evidence_dir/50-source-app-file-share.xml" "Capture Inbox" "Capture Inbox is not visible after source-app file share."
  cotton_require_xml_text "$evidence_dir/50-source-app-file-share.xml" "$share_file_name" "Source-app shared file name is not visible."
  cotton_require_xml_text "$evidence_dir/50-source-app-file-share.xml" "Copied to this device" "Source-app file was not copied to local staging."
  cotton_require_xml_text "$evidence_dir/50-source-app-file-share.xml" "Choose folder" "Source-app shared file is not waiting for destination selection."
fi

cotton_capture_text_best_effort "90-logcat.txt" cotton_adb logcat -d -v time

printf 'Share-to-Cotton evidence captured in %s\n' "$evidence_dir"
