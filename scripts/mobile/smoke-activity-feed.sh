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
require_load_more=0
tap_load_more=0

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an Android Activity feed smoke:
  1. Restores the signed-in Files shell.
  2. Opens Activity from the account action sheet.
  3. Verifies Activity page chrome, Refresh, and list or empty state.
  4. Taps Refresh and verifies Activity remains usable.
  5. Optionally requires and taps Load more for profiles with multiple pages.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --no-launch               Do not launch automatically.
  --require-load-more       Fail unless the Activity page exposes Load more.
  --tap-load-more           Tap Load more and verify the page remains usable. Implies --require-load-more.
  --help, -h                Show this help.

The app must already have a signed-in session. Use --require-load-more only with
an account that has more Activity entries than the first page size.
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
  "--require-load-more:require_load_more:1"
  "--tap-load-more:require_load_more:1:tap_load_more:1"
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
  evidence_dir="$evidence_root/$timestamp-activity-feed"
fi

mkdir -p "$evidence_dir"






require_activity_content() {
  local xml_file="$1"

  if [[ ! -f "$xml_file" ]]; then
    printf 'Activity XML is missing: %s\n' "$xml_file" >&2
    exit 66
  fi

  if grep -Eq 'No activity yet|[0-9][^"]* items?|[0-9][^"]* of [0-9][^"]* items?' "$xml_file"; then
    return
  fi

  printf 'Activity page did not show an empty state or an item summary.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 66
}




open_account_activity() {
  cotton_tap_node_from_xml "$files_root_xml" "Account" exact
  sleep 2
  cotton_capture_screen "30-account-actions"
  cotton_require_xml_text "$evidence_dir/30-account-actions.xml" "Activity" \
    "Account action sheet did not expose Activity."
  cotton_tap_node_from_xml "$evidence_dir/30-account-actions.xml" "Activity" exact
}

validate_activity_page() {
  local xml_file="$1"
  local must_have_load_more="${2:-$require_load_more}"

  cotton_require_xml_text "$xml_file" "Activity" "Activity page title is missing."
  cotton_require_xml_text "$xml_file" "Refresh" "Activity page did not expose Refresh."
  require_activity_content "$xml_file"
  if [[ "$must_have_load_more" -eq 1 ]]; then
    cotton_require_xml_text "$xml_file" "Load more" "Activity page did not expose Load more."
  fi
}

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'require_load_more=%s\n' "$require_load_more"
    printf 'tap_load_more=%s\n' "$tap_load_more"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
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
open_account_activity
sleep 4
cotton_wait_for_text "40-activity" "Activity"
activity_xml="$waited_xml"
validate_activity_page "$activity_xml" "$require_load_more"

cotton_tap_node_from_xml "$activity_xml" "Refresh" exact
sleep 4
cotton_wait_for_text "50-activity-refresh" "Activity"
activity_refresh_xml="$waited_xml"
validate_activity_page "$activity_refresh_xml" "$require_load_more"

if [[ "$tap_load_more" -eq 1 ]]; then
  cotton_tap_node_from_xml "$activity_refresh_xml" "Load more" exact
  sleep 4
  cotton_wait_for_text "60-activity-load-more" "Activity"
  activity_load_more_xml="$waited_xml"
  validate_activity_page "$activity_load_more_xml" 0
fi

cotton_capture_text_best_effort "90-logcat-raw.txt" cotton_adb logcat -d -v time
grep -E 'Cotton|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true
if grep -E 'FATAL EXCEPTION|mono-rt' "$evidence_dir/91-logcat-cotton.txt" > "$evidence_dir/92-fatal-markers.txt"; then
  printf 'Fatal log markers were found during Activity feed smoke.\n' >&2
  printf 'Evidence: %s/92-fatal-markers.txt\n' "$evidence_dir" >&2
  exit 66
fi

{
  printf 'Activity feed smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Refresh: passed\n'
  printf 'Require load more: %s\n' "$require_load_more"
  printf 'Tapped load more: %s\n' "$tap_load_more"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
