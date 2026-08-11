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
configuration="$COTTON_ANDROID_CONFIGURATION"
config_file="$COTTON_REPO_ROOT/src/Cotton.Mobile/Platforms/Android/google-services.json"
config_source_file=""
config_source_env_name=""
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
preflight_only=0
require_registered=1
capture_diagnostics_ui=0
wait_seconds=10
expected_version_code=""
expected_version_name=""
diagnostics_xml=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs the Android remote-push token registration smoke for a Firebase-configured
Cotton build. The script does not print or store the FCM token.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --configuration NAME      Android build configuration. Defaults to COTTON_ANDROID_CONFIGURATION.
  --config-file PATH        Firebase google-services.json path.
  --config-source-file PATH Restore google-services.json from this local source before preflight.
  --config-source-env NAME  Restore google-services.json from this environment variable before preflight.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --wait-seconds N          Seconds to wait after launch for session restore and token registration.
  --diagnostics-ui          Open Diagnostics and validate the Remote push section after launch.
  --allow-unregistered      Capture evidence without failing when registration is not proven.
  --preflight-only          Validate package/config/device state and exit before launching.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

Preconditions for a passing registration smoke:
  - google-services.json contains a client for the tested package id.
  - Google Play services are available on the device.
  - The app has a restorable signed-in session.
  - The backend profile exposes device-token registration.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--configuration:configuration"
  "--config-file:config_file"
  "--config-source-file:config_source_file"
  "--config-source-env:config_source_env_name"
  "--evidence-dir:evidence_dir"
  "--expected-version-code:expected_version_code"
  "--expected-version-name:expected_version_name"
  "--wait-seconds:wait_seconds"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--allow-unregistered:require_registered:0"
  "--diagnostics-ui:capture_diagnostics_ui:1"
  "--preflight-only:preflight_only:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

if [[ ! "$wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Invalid --wait-seconds: %s\n' "$wait_seconds" >&2
  exit 64
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-remote-push-token"
fi

mkdir -p "$evidence_dir"




# shellcheck source=smoke-remote-push-token-support.sh
source "$SCRIPT_DIR/smoke-remote-push-token-support.sh"

write_metadata
restore_firebase_config_if_requested
run_firebase_config_preflight

cotton_capture_text "01-adb-devices.txt" adb devices
if ! cotton_adb get-state > "$evidence_dir/03-device-state.txt" 2>&1; then
  printf 'ADB device is not available for serial %s. Evidence: %s\n' "$serial" "$evidence_dir" >&2
  exit 69
fi

device_state="$(tr -d '\r\n' < "$evidence_dir/03-device-state.txt")"
if [[ "$device_state" != "device" ]]; then
  printf 'ADB serial %s is in state %s, expected device. Evidence: %s\n' \
    "$serial" "$device_state" "$evidence_dir" >&2
  exit 69
fi

if [[ "$install_debug" -eq 1 ]]; then
  "$SCRIPT_DIR/install-android-debug.sh" --no-launch > "$evidence_dir/04-install-debug.txt" 2>&1
fi

if ! cotton_adb shell pm path "$package_id" > "$evidence_dir/05-package.txt" 2>&1; then
  printf 'Package %s is not installed on %s. Use --install-debug or install a Play build first. Evidence: %s\n' \
    "$package_id" "$serial" "$evidence_dir" >&2
  exit 69
fi

cotton_capture_text "06-package-dumpsys.txt" cotton_adb shell dumpsys package "$package_id"
cotton_write_installed_package_version "$evidence_dir/06-package-dumpsys.txt" "$evidence_dir/07-package-version.txt"

cotton_capture_text "08-play-services.txt" cotton_adb shell dumpsys package com.google.android.gms

if [[ "$preflight_only" -eq 1 ]]; then
  printf '\nRemote-push token preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

cotton_adb logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  cotton_capture_text "10-launch.txt" cotton_adb shell monkey -p "$package_id" 1
fi

sleep "$wait_seconds"
capture_window "20-after-launch"

cotton_adb logcat -d -v threadtime |
  awk '/Cotton mobile remote push|remote push token registration|remote push token refresh|Firebase Cloud Messaging|Google Play services/' \
    > "$evidence_dir/90-remote-push-log.txt"

registration_status="no_signal"
if grep -q 'Registered the Cotton mobile remote push token for the current session.' \
  "$evidence_dir/90-remote-push-log.txt"; then
  registration_status="registered"
elif grep -q 'not configured' "$evidence_dir/90-remote-push-log.txt"; then
  registration_status="not_configured"
elif grep -q 'unavailable' "$evidence_dir/90-remote-push-log.txt"; then
  registration_status="unavailable"
fi

write_result "$registration_status"

if [[ "$capture_diagnostics_ui" -eq 1 ]]; then
  capture_and_validate_diagnostics
fi

if [[ "$require_registered" -eq 1 && "$registration_status" != "registered" ]]; then
  printf 'Remote-push token registration was not proven: %s. Evidence: %s\n' \
    "$registration_status" "$evidence_dir" >&2
  exit 65
fi

printf '\nRemote-push token smoke evidence: %s\n' "$evidence_dir"
printf 'Registration status: %s\n' "$registration_status"
