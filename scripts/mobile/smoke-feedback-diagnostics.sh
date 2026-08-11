#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an Android feedback/diagnostics smoke:
  1. Restores the signed-in Files shell.
  2. Opens Diagnostics from the account action sheet.
  3. Verifies diagnostics sections and Copy status.
  4. Opens Send feedback from the account action sheet.
  5. Accepts either an external email composer or the in-app clipboard fallback.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --no-launch               Do not launch automatically.
  --help, -h                Show this help.

The app must already have a signed-in session.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--evidence-dir:evidence_dir"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if ! command -v python3 >/dev/null 2>&1; then
  printf 'python3 was not found.\n' >&2
  exit 127
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-feedback-diagnostics"
fi

mkdir -p "$evidence_dir"

open_account_action() {
  local action="$1"
  local prefix="$2"

  cotton_tap_node_from_xml "$files_root_xml" "Account" exact
  sleep 2
  cotton_capture_screen "$prefix-account-actions"
  cotton_require_xml_text "$evidence_dir/$prefix-account-actions.xml" "$action" \
    "Account action sheet did not expose $action."
  cotton_tap_node_from_xml "$evidence_dir/$prefix-account-actions.xml" "$action" exact
}

ensure_text_with_scroll() {
  local start_xml="$1"
  local prefix="$2"
  local needle="$3"
  local xml_file="$start_xml"
  local attempt

  if cotton_xml_has_text "$xml_file" "$needle"; then
    ensured_xml="$xml_file"
    return
  fi

  for attempt in 0 1 2 3; do
    cotton_adb shell input swipe 540 1700 540 650 350 >/dev/null 2>&1 || true
    sleep 1
    cotton_capture_screen "$prefix-scroll-$attempt"
    xml_file="$evidence_dir/$prefix-scroll-$attempt.xml"
    if cotton_xml_has_text "$xml_file" "$needle"; then
      ensured_xml="$xml_file"
      return
    fi
  done

  printf 'Could not find text after scrolling: %s\n' "$needle" >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

read_focused_window() {
  cotton_adb shell dumpsys window 2>/dev/null \
    | tr -d '\r' \
    | grep -E 'mCurrentFocus|mFocusedApp|mTopFullscreenOpaqueWindowState' \
    | sed -n '1,20p'
}

validate_feedback_result() {
  cotton_capture_screen "80-feedback-result"
  read_focused_window > "$evidence_dir/81-feedback-focused-window.txt" || true

  if cotton_xml_has_text "$evidence_dir/80-feedback-result.xml" "Feedback details were copied"; then
    cotton_tap_node_from_xml "$evidence_dir/80-feedback-result.xml" "OK" exact
    feedback_result="clipboard-fallback"
    return
  fi

  if cotton_xml_has_text "$evidence_dir/80-feedback-result.xml" "Could not open an email app. Send feedback to"; then
    printf 'Feedback composer and clipboard fallback both failed.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir/80-feedback-result.xml" >&2
    exit 66
  fi

  if ! grep -Fq "$package_id" "$evidence_dir/81-feedback-focused-window.txt"; then
    cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    feedback_result="external-composer"
    return
  fi

  printf 'Feedback did not open a composer or show the clipboard fallback.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'maui_clipboard_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/data/clipboard\n'
    printf 'maui_launcher_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/launcher\n'
    printf 'maui_device_display_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/display\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/00-metadata.txt"
}

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

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c >/dev/null 2>&1 || true
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/04-launch.txt"
  sleep 4
fi

cotton_wait_for_files_root

open_account_action "Diagnostics" "30"
sleep 4
cotton_wait_for_text "40-diagnostics" "Diagnostics"
diagnostics_xml="$waited_xml"
cotton_require_xml_text "$diagnostics_xml" "App" "Diagnostics App section is missing."
cotton_require_xml_text "$diagnostics_xml" "Device" "Diagnostics Device section is missing."
cotton_require_xml_text "$diagnostics_xml" "Session" "Diagnostics Session section is missing."

cotton_tap_node_from_xml "$diagnostics_xml" "Copy" exact
sleep 3
cotton_wait_for_text "50-diagnostics-copy" "Diagnostics copied"
diagnostics_copy_xml="$waited_xml"
cotton_require_xml_text "$diagnostics_copy_xml" "Diagnostics copied" "Diagnostics copy status was not shown."

ensure_text_with_scroll "$diagnostics_copy_xml" "60-diagnostics-local-cache" "Local cache"
local_cache_xml="$ensured_xml"
ensure_text_with_scroll "$local_cache_xml" "61-diagnostics-account-storage" "Account storage"
account_storage_xml="$ensured_xml"
ensure_text_with_scroll "$account_storage_xml" "62-diagnostics-pending-uploads" "Pending uploads"
pending_uploads_xml="$ensured_xml"
ensure_text_with_scroll "$pending_uploads_xml" "63-diagnostics-remote-push" "Remote push"
remote_push_xml="$ensured_xml"
cotton_require_xml_text "$remote_push_xml" "Token" "Diagnostics remote push section omits token status."
cotton_require_xml_text "$remote_push_xml" "Registration" "Diagnostics remote push section omits registration status."

cotton_tap_node_from_xml "$remote_push_xml" "Navigate up" exact
sleep 3
cotton_wait_for_files_root

open_account_action "Send feedback" "70"
sleep 5
feedback_result="unknown"
validate_feedback_result

cotton_capture_text_best_effort "90-logcat-raw.txt" cotton_adb logcat -d -v time
grep -E 'Cotton|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true
if grep -E 'FATAL EXCEPTION|mono-rt' "$evidence_dir/91-logcat-cotton.txt" > "$evidence_dir/92-fatal-markers.txt"; then
  printf 'Fatal log markers were found during feedback/diagnostics smoke.\n' >&2
  printf 'Evidence: %s/92-fatal-markers.txt\n' "$evidence_dir" >&2
  exit 66
fi

{
  printf 'Feedback/diagnostics smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Diagnostics copy: passed\n'
  printf 'Feedback result: %s\n' "$feedback_result"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
