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
first_file="242.mp4"
second_file="238.png"

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
  --no-launch           Do not launch the app before capture.
  --help, -h            Show this help.

The app must already have a signed-in session and a Files root containing the
two selected file rows. The smoke downloads the selected files if needed, then
verifies the multi-file local Share files action and Android share UI handoff.
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
    --first-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --first-file.\n' >&2
        exit 64
      fi
      first_file="$2"
      shift 2
      ;;
    --second-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --second-file.\n' >&2
        exit 64
      fi
      second_file="$2"
      shift 2
      ;;
    --no-launch)
      launch_app=0
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

if [[ -z "${first_file//[[:space:]]/}" || -z "${second_file//[[:space:]]/}" ]]; then
  printf 'Selected file names must not be blank.\n' >&2
  exit 64
fi

if [[ "$first_file" == "$second_file" ]]; then
  printf 'Selected file names must be different.\n' >&2
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
  local remote_xml="/sdcard/cotton-window.xml"

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

xml_has_selection_banner() {
  local xml_file="$1"

  [[ -f "$xml_file" ]] && grep -Eq 'text="[0-9]+ selected"' "$xml_file"
}

point_for_node_text() {
  local xml_file="$1"
  local needle="$2"
  local mode="${3:-exact}"
  local point_file="$evidence_dir/tap-point.txt"

  python3 - "$xml_file" "$needle" "$mode" > "$point_file" <<'PY'
import re
import sys
from xml.etree import ElementTree

xml_file, needle, mode = sys.argv[1:4]
root = ElementTree.parse(xml_file).getroot()

def parse_bounds(bounds: str) -> tuple[int, int, int, int]:
    match = re.fullmatch(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", bounds)
    if match is None:
        raise ValueError(bounds)
    return tuple(int(value) for value in match.groups())

def center(bounds: tuple[int, int, int, int]) -> tuple[int, int]:
    left, top, right, bottom = bounds
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
        print(*center(parse_bounds(node.attrib["bounds"])))
        raise SystemExit(0)

raise SystemExit(f"Could not find UI node: {needle}")
PY

  read -r tap_x tap_y < "$point_file"
}

point_for_row_text() {
  local xml_file="$1"
  local needle="$2"
  local point_file="$evidence_dir/row-point.txt"

  python3 - "$xml_file" "$needle" > "$point_file" <<'PY'
import re
import sys
from xml.etree import ElementTree

xml_file, needle = sys.argv[1:3]
root = ElementTree.parse(xml_file).getroot()

def parse_bounds(bounds: str) -> tuple[int, int, int, int]:
    match = re.fullmatch(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", bounds)
    if match is None:
        raise ValueError(bounds)
    return tuple(int(value) for value in match.groups())

target_bounds = None
for node in root.iter("node"):
    if node.attrib.get("text", "") == needle:
        target_bounds = parse_bounds(node.attrib["bounds"])
        break

if target_bounds is None:
    raise SystemExit(f"Could not find row text: {needle}")

target_left, target_top, target_right, target_bottom = target_bounds
target_y = (target_top + target_bottom) // 2
target_x = (target_left + target_right) // 2
best = None

for node in root.iter("node"):
    if node.attrib.get("long-clickable") != "true" and node.attrib.get("clickable") != "true":
        continue

    left, top, right, bottom = parse_bounds(node.attrib["bounds"])
    if top <= target_y <= bottom and left <= target_x <= right:
        height = bottom - top
        width = right - left
        candidate = (height, width, left, top, right, bottom)
        if best is None or candidate < best:
            best = candidate

if best is None:
    print(target_x, target_y)
    raise SystemExit(0)

_, _, left, top, right, bottom = best
row_y = (top + bottom) // 2
row_x = min(max(left + 88, left + 1), right - 1)
print(row_x, row_y)
PY

  read -r tap_x tap_y < "$point_file"
}

tap_node_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local mode="${3:-exact}"

  point_for_node_text "$xml_file" "$needle" "$mode"
  adb_device shell input tap "$tap_x" "$tap_y"
}

tap_row_from_xml() {
  local xml_file="$1"
  local needle="$2"

  point_for_row_text "$xml_file" "$needle"
  adb_device shell input tap "$tap_x" "$tap_y"
}

long_press_row_from_xml() {
  local xml_file="$1"
  local needle="$2"

  point_for_row_text "$xml_file" "$needle"
  adb_device shell input touchscreen swipe "$tap_x" "$tap_y" "$tap_x" "$tap_y" 1800
}

wait_for_text() {
  local prefix="$1"
  local needle="$2"
  local attempt
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7 8 9; do
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

is_cotton_focused() {
  local window_file="$1"
  grep -Fq "$package_id/" "$window_file"
}

wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="20-files-root-$attempt"
    capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if xml_has_text "$xml_file" "Files" \
      && xml_has_text "$xml_file" "Account" \
      && ! xml_has_selection_banner "$xml_file"; then
      files_root_xml="$xml_file"
      return
    fi

    if xml_has_selection_banner "$xml_file" && xml_has_text "$xml_file" "Cancel"; then
      tap_node_from_xml "$xml_file" "Cancel" exact
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

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'repo=%s\n' "$COTTON_REPO_ROOT"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'first_file=%s\n' "$first_file"
    printf 'second_file=%s\n' "$second_file"
    printf 'maui_share_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/data/share\n'
    printf 'maui_share_request_docs=https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.applicationmodel.datatransfer.share.requestasync\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Selection Local Share Smoke

Package: \`$package_id\`
Device: \`$serial\`
Files: \`$first_file\`, \`$second_file\`

## Preconditions

- [ ] Signed-in session is restored without clearing app data.
- [ ] Files root shows both selected file rows.

## Selection And Local State

- [ ] \`30-two-selected.xml\` shows \`2 selected\` and \`2 files\`.
- [ ] If the files were not local, \`40-actions-before-local.xml\` omits \`Share files\`.
- [ ] \`50-after-download.xml\` shows both files and \`On device\`.

## Share Files

- [ ] \`80-share-files-sheet.xml\` shows \`Share files\`.
- [ ] \`90-share-handoff-*.txt\` or \`90-share-handoff-*.xml\` shows Android system share UI handoff.
- [ ] \`99-logcat.txt\` has no ANR/FATAL markers.
EOF
}

select_two_files() {
  local prefix="$1"

  capture_screen "$prefix-root"
  require_xml_text "$evidence_dir/$prefix-root.xml" "$first_file" "First selected file is not visible in Files."
  require_xml_text "$evidence_dir/$prefix-root.xml" "$second_file" "Second selected file is not visible in Files."

  long_press_row_from_xml "$evidence_dir/$prefix-root.xml" "$first_file"
  sleep 2
  capture_screen "$prefix-first-selected"
  require_xml_text "$evidence_dir/$prefix-first-selected.xml" "1 selected" "Long press did not start file selection."

  tap_row_from_xml "$evidence_dir/$prefix-first-selected.xml" "$second_file"
  sleep 1
  capture_screen "$prefix-two-selected"
  require_xml_text "$evidence_dir/$prefix-two-selected.xml" "2 selected" "Second file did not join the selection."
  require_xml_text "$evidence_dir/$prefix-two-selected.xml" "2 files" "Selection detail did not show two files."

  selected_xml="$evidence_dir/$prefix-two-selected.xml"
}

open_selection_actions() {
  local prefix="$1"

  tap_node_from_xml "$selected_xml" "Actions" exact
  sleep 1
  capture_screen "$prefix"
  require_xml_text "$evidence_dir/$prefix.xml" "2 selected" "Selection action sheet did not open."
  require_xml_text "$evidence_dir/$prefix.xml" "Download files" "Selection action sheet did not expose Download files."
  require_xml_text "$evidence_dir/$prefix.xml" "Keep offline" "Selection action sheet did not expose Keep offline."
  actions_xml="$evidence_dir/$prefix.xml"
}

wait_for_local_files() {
  local attempt
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7 8 9; do
    capture_screen "50-after-download-$attempt"
    xml_file="$evidence_dir/50-after-download-$attempt.xml"

    if xml_has_text "$xml_file" "$first_file" \
      && xml_has_text "$xml_file" "$second_file" \
      && xml_has_text "$xml_file" "On device"; then
      cp "$xml_file" "$evidence_dir/50-after-download.xml"
      if [[ -f "$evidence_dir/50-after-download-$attempt.png" ]]; then
        cp "$evidence_dir/50-after-download-$attempt.png" "$evidence_dir/50-after-download.png"
      fi
      return
    fi

    adb_device shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 2
  done

  printf 'Selected files did not return to Files with an On device marker.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

ensure_selected_files_local() {
  select_two_files "30"
  open_selection_actions "40-actions-before-local"

  if xml_has_text "$actions_xml" "Share files"; then
    tap_node_from_xml "$actions_xml" "Cancel" exact
    sleep 1
    wait_for_files_root
    return
  fi

  tap_node_from_xml "$actions_xml" "Download files" exact
  sleep 4
  wait_for_local_files
  wait_for_files_root
}

wait_for_share_handoff() {
  local attempt
  local prefix
  local xml_file
  local window_file

  for attempt in 0 1 2 3 4 5; do
    prefix="90-share-handoff-$attempt"
    capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"
    window_file="$evidence_dir/$prefix-window.txt"

    if [[ -f "$window_file" ]] && ! is_cotton_focused "$window_file"; then
      share_handoff="external-window"
      return
    fi

    if xml_has_text "$xml_file" "Share" \
      || xml_has_text "$xml_file" "Nearby" \
      || xml_has_text "$xml_file" "Complete action"; then
      share_handoff="system-share-ui"
      return
    fi

    sleep 2
  done

  printf 'Android share UI handoff was not observed after tapping Share files.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

write_metadata
write_checklist

capture_text "00-device.txt" adb_device shell getprop ro.product.model
capture_text "01-adb-devices.txt" adb devices
capture_text "02-package.txt" adb_device shell dumpsys package "$package_id"
capture_text "03-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/04-install.txt"
fi

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c >/dev/null 2>&1 || true
  adb_device shell am force-stop "$package_id" >/dev/null 2>&1 || true
  sleep 1
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/05-launch.txt"
  sleep 4
fi

wait_for_files_root
ensure_selected_files_local
wait_for_files_root
select_two_files "70"
open_selection_actions "80-share-files-sheet"
require_xml_text "$actions_xml" "Share files" "Selection action sheet did not expose Share files for local files."

tap_node_from_xml "$actions_xml" "Share files" exact
sleep 3
wait_for_share_handoff
adb_device shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
sleep 1

capture_text "99-logcat.txt" adb_device logcat -d -v time
if grep -E "ANR|FATAL EXCEPTION|Input dispatching timed out" "$evidence_dir/99-logcat.txt" > "$evidence_dir/99-logcat-fatal-markers.txt"; then
  printf 'Fatal or ANR markers were found in logcat.\n' >&2
  printf 'Evidence: %s/99-logcat-fatal-markers.txt\n' "$evidence_dir" >&2
  exit 66
fi

{
  printf 'first_file=%s\n' "$first_file"
  printf 'second_file=%s\n' "$second_file"
  printf 'share_handoff=%s\n' "$share_handoff"
} > "$evidence_dir/result.env"

printf 'Selection local share evidence captured in %s\n' "$evidence_dir"
