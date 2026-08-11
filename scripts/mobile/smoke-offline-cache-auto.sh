#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

COTTON_CAPTURE_CONNECTIVITY=1

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
folder_name="Mobile smoke folder"
offline_file_name=""
nested_folder_name=""
nested_file_name=""
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
leave_network_disabled=0
network_disabled=0
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a non-interactive offline file/folder cache smoke for the current Android
build. It refreshes a known folder online, validates app-private cached listings
and local offline bytes, disables network, verifies cached root/folder UI, and
opens an on-device file while offline.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Cotton instance URI. Defaults to $instance_uri.
  --folder NAME             Cached folder to navigate. Defaults to "$folder_name".
  --nested-folder NAME      Optional cached child folder to navigate inside --folder.
  --offline-file NAME       On-device file to open offline. Defaults to a pinned root file.
  --nested-file NAME        Optional file expected inside --nested-folder cache.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --leave-network-disabled  Do not restore Wi-Fi/mobile data at the end.
  --help, -h                Show this help.

The app must already have a signed-in session and cached root listing for the
selected instance.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--instance:instance_uri"
  "--folder:folder_name"
  "--nested-folder:nested_folder_name"
  "--offline-file:offline_file_name"
  "--nested-file:nested_file_name"
  "--evidence-dir:evidence_dir"
  "--expected-version-code:expected_version_code"
  "--expected-version-name:expected_version_name"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--leave-network-disabled:leave_network_disabled:1"
)
cotton_parse_arguments "$@"

validate_plain_name() {
  local label="$1"
  local value="$2"
  local is_required="$3"

  if [[ -z "${value//[[:space:]]/}" ]]; then
    if [[ "$is_required" -eq 1 ]]; then
      printf '%s must not be blank.\n' "$label" >&2
      exit 64
    fi

    return
  fi

  if [[ "$value" == *"/"* ]]; then
    printf '%s must not contain a slash.\n' "$label" >&2
    exit 64
  fi
}

validate_plain_name "Folder name" "$folder_name" 1
validate_plain_name "Nested folder name" "$nested_folder_name" 0
validate_plain_name "Offline file name" "$offline_file_name" 0
validate_plain_name "Nested file name" "$nested_file_name" 0

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
  evidence_dir="$evidence_root/$timestamp-offline-cache-auto"
fi

mkdir -p "$evidence_dir"

# shellcheck source=smoke-offline-cache-support.sh
source "$SCRIPT_DIR/smoke-offline-cache-support.sh"

trap restore_network EXIT

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
cotton_capture_text_best_effort "03-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
cotton_verify_expected_version_file "$evidence_dir/03-package-version.txt"

cotton_adb shell svc wifi enable >/dev/null 2>&1 || true
cotton_adb shell svc data enable >/dev/null 2>&1 || true
sleep 3
cotton_capture_text_best_effort "04-connectivity-online.txt" cotton_adb shell dumpsys connectivity

cotton_adb logcat -c >/dev/null 2>&1 || true
cotton_adb shell am force-stop "$package_id" >/dev/null 2>&1 || true
cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/05-launch-online.txt"
sleep 5

waited_xml=""
cotton_wait_for_text "20-online-root" "Files"
online_root_xml="$waited_xml"
cotton_require_xml_text "$online_root_xml" "$folder_name" "Selected folder is not visible in online root Files."

pull_app_file "files/CottonFolderListings/$instance_key/root.json" "$evidence_dir/10-root-cache.json"
pull_app_file "files/CottonOfflineFiles/$instance_key/offline-files.json" "$evidence_dir/11-offline-files.json"
cotton_capture_text_best_effort "12-download-files.txt" \
  cotton_adb shell run-as "$package_id" find "files/CottonDownloads/$instance_key" -maxdepth 2 -type f
select_smoke_targets
write_metadata
validate_local_file_bytes
cotton_require_xml_text "$online_root_xml" "$selected_file_name" "Selected offline file is not visible in online root Files."
cotton_require_xml_text "$online_root_xml" "On device" "Online root does not show any On device marker."

cotton_tap_node_from_xml "$online_root_xml" "$folder_name"
sleep 5
cotton_capture_screen "25-online-folder"
cotton_require_xml_text "$evidence_dir/25-online-folder.xml" "Files / $folder_name" \
  "Online folder navigation did not open the selected folder."
online_folder_xml="$evidence_dir/25-online-folder.xml"

folder_cache_name="${folder_id//-/}.json"
pull_app_file "files/CottonFolderListings/$instance_key/$folder_cache_name" "$evidence_dir/30-folder-cache.json"
validate_folder_cache > "$evidence_dir/31-folder-cache-summary.json"

if [[ -n "${nested_folder_name//[[:space:]]/}" ]]; then
  select_nested_folder_target
  write_metadata
  cotton_require_xml_text "$online_folder_xml" "$nested_folder_name" \
    "Selected nested folder is not visible in the online parent folder."

  cotton_tap_node_from_xml "$online_folder_xml" "$nested_folder_name"
  sleep 5
  cotton_capture_screen "32-online-nested-folder"
  cotton_require_xml_text "$evidence_dir/32-online-nested-folder.xml" "Files /" \
    "Online nested folder navigation did not show a Files breadcrumb."
  cotton_require_xml_text "$evidence_dir/32-online-nested-folder.xml" "$nested_folder_name" \
    "Online nested folder navigation did not open the selected child folder."

  nested_folder_cache_name="${nested_folder_id//-/}.json"
  pull_app_file \
    "files/CottonFolderListings/$instance_key/$nested_folder_cache_name" \
    "$evidence_dir/33-nested-folder-cache.json"
  validate_nested_folder_cache > "$evidence_dir/34-nested-folder-cache-summary.json"

  cotton_tap_node_from_xml "$evidence_dir/32-online-nested-folder.xml" "Up"
  cotton_wait_for_text "34-online-parent-return" "$folder_name"
  online_folder_xml="$waited_xml"
  cotton_require_xml_text "$online_folder_xml" "$nested_folder_name" \
    "Online up navigation did not return to the parent folder."
fi

cotton_tap_node_from_xml "$online_folder_xml" "Up"
cotton_wait_for_text "35-online-root-return" "$selected_file_name"
online_root_return_xml="$waited_xml"
cotton_require_xml_text "$online_root_return_xml" "$folder_name" "Online up navigation did not return to root."

cotton_capture_text_best_effort "39-network-before-offline.txt" cotton_adb shell dumpsys connectivity
cotton_adb shell svc wifi disable >/dev/null 2>&1 || true
cotton_adb shell svc data disable >/dev/null 2>&1 || true
network_disabled=1
sleep 4
cotton_capture_text_best_effort "40-network-disabled.txt" cotton_adb shell dumpsys connectivity

cotton_adb shell am force-stop "$package_id" >/dev/null 2>&1 || true
cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/41-launch-offline.txt"
sleep 6

cotton_wait_for_text "45-offline-root" "Offline"
offline_root_xml="$waited_xml"
cotton_require_xml_text "$offline_root_xml" "Files" "Offline root did not show Files."
cotton_require_xml_text "$offline_root_xml" "$folder_name" "Offline root did not show the selected cached folder."
cotton_require_xml_text "$offline_root_xml" "$selected_file_name" "Offline root did not show the selected on-device file."
cotton_require_xml_text "$offline_root_xml" "On device" "Offline root did not show an On device marker."

cotton_tap_node_from_xml "$offline_root_xml" "$folder_name"
sleep 4
cotton_capture_screen "50-offline-folder"
cotton_require_xml_text "$evidence_dir/50-offline-folder.xml" "Files / $folder_name" \
  "Offline folder navigation did not open cached folder."
cotton_require_xml_text "$evidence_dir/50-offline-folder.xml" "Saved folder list cached" "Offline folder did not show cached-listing notice."
cotton_require_xml_text "$evidence_dir/50-offline-folder.xml" "Files marked On device can still open" \
  "Offline folder did not show on-device-open guidance."
offline_folder_xml="$evidence_dir/50-offline-folder.xml"

if [[ -n "${nested_folder_name//[[:space:]]/}" ]]; then
  cotton_require_xml_text "$offline_folder_xml" "$nested_folder_name" \
    "Selected nested folder is not visible in the offline parent folder."
  cotton_tap_node_from_xml "$offline_folder_xml" "$nested_folder_name"
  sleep 4
  cotton_capture_screen "52-offline-nested-folder"
  cotton_require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "Files /" \
    "Offline nested folder navigation did not show a Files breadcrumb."
  cotton_require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "$nested_folder_name" \
    "Offline nested folder navigation did not open the cached child folder."
  cotton_require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "Saved folder list cached" \
    "Offline nested folder did not show cached-listing notice."
  cotton_require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "Files marked On device can still open" \
    "Offline nested folder did not show on-device-open guidance."
  if [[ -n "${nested_file_name//[[:space:]]/}" ]]; then
    cotton_require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "$nested_file_name" \
      "Expected nested file is not visible in the offline child folder."
  fi

  cotton_tap_node_from_xml "$evidence_dir/52-offline-nested-folder.xml" "Up"
  cotton_wait_for_text "54-offline-parent-return" "$folder_name"
  offline_folder_xml="$waited_xml"
  cotton_require_xml_text "$offline_folder_xml" "$nested_folder_name" \
    "Offline up navigation did not return to the parent cached folder."
fi

cotton_tap_node_from_xml "$offline_folder_xml" "Up"
cotton_wait_for_text "55-offline-root-return" "$selected_file_name"
offline_root_return_xml="$waited_xml"
cotton_require_xml_text "$offline_root_return_xml" "$folder_name" \
  "Offline up navigation did not return to the root file list."

cotton_tap_node_from_xml "$offline_root_return_xml" "$selected_file_name"
sleep 5
cotton_capture_screen "60-offline-file-open"
cotton_require_xml_text "$evidence_dir/60-offline-file-open.xml" "$selected_file_name" \
  "Offline local file did not open in the app viewer."
cotton_require_xml_text "$evidence_dir/60-offline-file-open.xml" "Open" \
  "Offline local file viewer did not expose the external open action."

cotton_capture_text_best_effort "90-logcat-raw.txt" cotton_adb logcat -d -v time
grep -E 'Cotton|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true

restore_network
sleep 5
cotton_capture_text_best_effort "92-connectivity-after-restore.txt" cotton_adb shell dumpsys connectivity

{
  printf 'Offline cache smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Folder: %s\n' "$folder_name"
  if [[ -n "${nested_folder_name//[[:space:]]/}" ]]; then
    printf 'Nested folder: %s\n' "$nested_folder_name"
  fi
  printf 'Offline file: %s\n' "$selected_file_name"
  if [[ -n "${nested_file_name//[[:space:]]/}" ]]; then
    printf 'Nested file: %s\n' "$nested_file_name"
  fi
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
