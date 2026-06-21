#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

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
    --no-launch)
      launch_app=0
      shift
      ;;
    --require-load-more)
      require_load_more=1
      shift
      ;;
    --tap-load-more)
      require_load_more=1
      tap_load_more=1
      shift
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

capture_screen() {
  local prefix="$1"
  local remote_xml="/sdcard/cotton-activity-feed-window.xml"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window

  if ! adb_device exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  adb_device shell rm -f "$remote_xml" >/dev/null 2>&1 || true
  if adb_device shell uiautomator dump "$remote_xml" > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    if ! adb_device pull "$remote_xml" "$evidence_dir/$prefix.xml" > "$evidence_dir/$prefix-pull-xml.log" 2>&1; then
      rm -f "$evidence_dir/$prefix.xml"
    fi
    adb_device shell rm -f "$remote_xml" >/dev/null 2>&1 || true
  else
    rm -f "$evidence_dir/$prefix.xml"
  fi
}

xml_has_text() {
  local xml_file="$1"
  local needle="$2"

  [[ -f "$xml_file" ]] && grep -Fq "$needle" "$xml_file"
}

require_xml_text() {
  local xml_file="$1"
  local needle="$2"
  local message="$3"

  if ! xml_has_text "$xml_file" "$needle"; then
    printf '%s\n' "$message" >&2
    printf 'Missing text: %s\n' "$needle" >&2
    printf 'Evidence: %s\n' "$xml_file" >&2
    exit 66
  fi
}

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

tap_node_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local mode="${3:-contains}"
  local point_file="$evidence_dir/tap-point.txt"

  python3 - "$xml_file" "$needle" "$mode" > "$point_file" <<'PY'
import re
import sys
from xml.etree import ElementTree

xml_file, needle, mode = sys.argv[1:4]
root = ElementTree.parse(xml_file).getroot()

def center(bounds: str) -> tuple[int, int]:
    match = re.fullmatch(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", bounds)
    if match is None:
        raise ValueError(bounds)
    left, top, right, bottom = [int(value) for value in match.groups()]
    return ((left + right) // 2, (top + bottom) // 2)

def matches(value: str) -> bool:
    return value == needle if mode == "exact" else needle in value

for node in root.iter("node"):
    values = (
        node.attrib.get("text", ""),
        node.attrib.get("content-desc", ""),
        node.attrib.get("hint", ""),
    )
    if any(matches(value) for value in values):
        print(*center(node.attrib["bounds"]))
        raise SystemExit(0)

raise SystemExit(f"Could not find UI node: {needle}")
PY

  read -r tap_x tap_y < "$point_file"
  adb_device shell input tap "$tap_x" "$tap_y"
}

wait_for_text() {
  local prefix="$1"
  local needle="$2"
  local attempt
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    capture_screen "$prefix-$attempt"
    xml_file="$evidence_dir/$prefix-$attempt.xml"
    if xml_has_text "$xml_file" "$needle"; then
      waited_xml="$xml_file"
      return
    fi
    sleep 2
  done

  printf 'Timed out waiting for UI text: %s\n' "$needle" >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="20-files-root-$attempt"
    capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if xml_has_text "$xml_file" "Files" && xml_has_text "$xml_file" "Account"; then
      files_root_xml="$xml_file"
      return
    fi

    if xml_has_text "$xml_file" "Navigate up"; then
      tap_node_from_xml "$xml_file" "Navigate up" exact
      sleep 2
      continue
    fi

    adb_device shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 1
    adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/20-relaunch-$attempt.txt" || true
    sleep 2
  done

  printf 'Files root with Account navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

open_account_activity() {
  tap_node_from_xml "$files_root_xml" "Account" exact
  sleep 2
  capture_screen "30-account-actions"
  require_xml_text "$evidence_dir/30-account-actions.xml" "Activity" \
    "Account action sheet did not expose Activity."
  tap_node_from_xml "$evidence_dir/30-account-actions.xml" "Activity" exact
}

validate_activity_page() {
  local xml_file="$1"
  local must_have_load_more="${2:-$require_load_more}"

  require_xml_text "$xml_file" "Activity" "Activity page title is missing."
  require_xml_text "$xml_file" "Refresh" "Activity page did not expose Refresh."
  require_activity_content "$xml_file"
  if [[ "$must_have_load_more" -eq 1 ]]; then
    require_xml_text "$xml_file" "Load more" "Activity page did not expose Load more."
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
capture_text "01-adb-devices.txt" adb devices

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/02-install.txt"
fi

capture_text "03-package.txt" adb_device shell dumpsys package "$package_id"

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c >/dev/null 2>&1 || true
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/04-launch.txt"
  sleep 4
fi

wait_for_files_root
open_account_activity
sleep 4
wait_for_text "40-activity" "Activity"
activity_xml="$waited_xml"
validate_activity_page "$activity_xml" "$require_load_more"

tap_node_from_xml "$activity_xml" "Refresh" exact
sleep 4
wait_for_text "50-activity-refresh" "Activity"
activity_refresh_xml="$waited_xml"
validate_activity_page "$activity_refresh_xml" "$require_load_more"

if [[ "$tap_load_more" -eq 1 ]]; then
  tap_node_from_xml "$activity_refresh_xml" "Load more" exact
  sleep 4
  wait_for_text "60-activity-load-more" "Activity"
  activity_load_more_xml="$waited_xml"
  validate_activity_page "$activity_load_more_xml" 0
fi

capture_text "90-logcat-raw.txt" adb_device logcat -d -v time
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
