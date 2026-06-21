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
preflight_only=0
cancel_on_timeout=1
wait_seconds=90
expected_version_code=""
expected_version_name=""
target_file=""
target_folder=""
create_disposable_folder=0
restore_from_trash_page=0
delete_forever_from_trash_page=0
restore_bulk_from_trash_page=0
delete_bulk_forever_from_trash_page=0
bulk_second_file=""
bulk_second_folder=""
create_bulk_second_disposable_folder=0
bulk_selection=0
bulk_second_kind=""
bulk_second_name=""
target_kind=""
target_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a Files move-to-trash and restore smoke for an existing test file or folder.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --target-file NAME        Existing Cotton Files file row to move to trash and restore.
  --target-folder NAME      Existing Cotton Files folder row to move to trash and restore.
  --create-disposable-folder NAME
                            Create a root-visible disposable folder first, then trash/restore it.
  --bulk-second-file NAME   Move the target plus this visible file row to trash as one selection.
  --bulk-second-folder NAME Move the target plus this visible folder row to trash as one selection.
  --create-bulk-second-disposable-folder NAME
                            Create a second root-visible disposable folder for bulk selection.
  --restore-from-trash-page Open Account -> Trash after moving the item, then restore it there.
  --delete-forever-from-trash-page
                            Open Account -> Trash after moving a disposable folder, then delete forever.
  --restore-bulk-from-trash-page
                            After bulk move-to-trash proof, select both Trash rows and restore them together.
  --delete-bulk-forever-from-trash-page
                            After disposable bulk move-to-trash proof, select both Trash rows and delete them forever.
  --wait-seconds N          Seconds to wait for each server mutation. Defaults to $wait_seconds.
  --no-cancel-on-timeout    Leave the app in its current state when a mutation times out.
  --preflight-only          Capture device/package/root state and exit.
  --no-launch               Do not launch the app before capture.
  --help, -h                Show this help.

The app must already have a signed-in session. Use a disposable test file/folder
or a known smoke fixture because this script performs a real server
trash/restore cycle when the backend responds successfully. Bulk selection
proof moves both selected items to Trash and then verifies they are recoverable
from the Trash page. Add --restore-bulk-from-trash-page to clean up both
items through Trash page selection restore, or use disposable targets with
--delete-bulk-forever-from-trash-page to prove permanent bulk deletion.
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
    --expected-version-code)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --expected-version-code.\n' >&2
        exit 64
      fi
      expected_version_code="$2"
      shift 2
      ;;
    --expected-version-name)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --expected-version-name.\n' >&2
        exit 64
      fi
      expected_version_name="$2"
      shift 2
      ;;
    --target-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --target-file.\n' >&2
        exit 64
      fi
      target_file="$2"
      shift 2
      ;;
    --target-folder)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --target-folder.\n' >&2
        exit 64
      fi
      if [[ -n "${target_folder//[[:space:]]/}" ]]; then
        printf 'Only one folder target option can be used.\n' >&2
        exit 64
      fi
      target_folder="$2"
      shift 2
      ;;
    --create-disposable-folder)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --create-disposable-folder.\n' >&2
        exit 64
      fi
      if [[ -n "${target_folder//[[:space:]]/}" ]]; then
        printf 'Only one folder target option can be used.\n' >&2
        exit 64
      fi
      create_disposable_folder=1
      target_folder="$2"
      shift 2
      ;;
    --bulk-second-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --bulk-second-file.\n' >&2
        exit 64
      fi
      if [[ -n "$bulk_second_file" || -n "$bulk_second_folder" ]]; then
        printf 'Only one bulk second target option can be used.\n' >&2
        exit 64
      fi
      bulk_second_file="$2"
      shift 2
      ;;
    --bulk-second-folder)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --bulk-second-folder.\n' >&2
        exit 64
      fi
      if [[ -n "$bulk_second_file" || -n "$bulk_second_folder" ]]; then
        printf 'Only one bulk second target option can be used.\n' >&2
        exit 64
      fi
      bulk_second_folder="$2"
      shift 2
      ;;
    --create-bulk-second-disposable-folder)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --create-bulk-second-disposable-folder.\n' >&2
        exit 64
      fi
      if [[ -n "$bulk_second_file" || -n "$bulk_second_folder" ]]; then
        printf 'Only one bulk second target option can be used.\n' >&2
        exit 64
      fi
      create_bulk_second_disposable_folder=1
      bulk_second_folder="$2"
      shift 2
      ;;
    --restore-from-trash-page)
      restore_from_trash_page=1
      shift
      ;;
    --delete-forever-from-trash-page)
      delete_forever_from_trash_page=1
      shift
      ;;
    --restore-bulk-from-trash-page)
      restore_bulk_from_trash_page=1
      shift
      ;;
    --delete-bulk-forever-from-trash-page)
      delete_bulk_forever_from_trash_page=1
      shift
      ;;
    --wait-seconds)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --wait-seconds.\n' >&2
        exit 64
      fi
      wait_seconds="$2"
      shift 2
      ;;
    --no-cancel-on-timeout)
      cancel_on_timeout=0
      shift
      ;;
    --preflight-only)
      preflight_only=1
      shift
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

if [[ ! "$wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Wait seconds must be a positive integer.\n' >&2
  exit 64
fi

if [[ "$wait_seconds" -le 0 ]]; then
  printf 'Wait seconds must be greater than zero.\n' >&2
  exit 64
fi

if [[ -n "${target_file//[[:space:]]/}" && -n "${target_folder//[[:space:]]/}" ]]; then
  printf 'Choose either --target-file or --target-folder, not both.\n' >&2
  exit 64
fi

if [[ "$create_disposable_folder" -eq 1 && "$preflight_only" -eq 1 ]]; then
  printf '%s\n' '--create-disposable-folder cannot be combined with --preflight-only.' >&2
  exit 64
fi

if [[ "$restore_from_trash_page" -eq 1 && "$delete_forever_from_trash_page" -eq 1 ]]; then
  printf '%s\n' '--restore-from-trash-page and --delete-forever-from-trash-page cannot be combined.' >&2
  exit 64
fi

if [[ "$preflight_only" -eq 1 \
  && ( "$restore_from_trash_page" -eq 1 \
    || "$delete_forever_from_trash_page" -eq 1 \
    || "$restore_bulk_from_trash_page" -eq 1 \
    || "$delete_bulk_forever_from_trash_page" -eq 1 ) ]]; then
  printf '%s\n' 'Trash page action options cannot be combined with --preflight-only.' >&2
  exit 64
fi

if [[ "$restore_bulk_from_trash_page" -eq 1 && "$delete_bulk_forever_from_trash_page" -eq 1 ]]; then
  printf '%s\n' '--restore-bulk-from-trash-page and --delete-bulk-forever-from-trash-page cannot be combined.' >&2
  exit 64
fi

if [[ -n "$bulk_second_file" ]]; then
  bulk_selection=1
  bulk_second_kind="file"
  bulk_second_name="$bulk_second_file"
elif [[ -n "$bulk_second_folder" ]]; then
  bulk_selection=1
  bulk_second_kind="folder"
  bulk_second_name="$bulk_second_folder"
fi

if [[ "$bulk_selection" -eq 1 && "$preflight_only" -eq 1 ]]; then
  printf '%s\n' 'Bulk selection options cannot be combined with --preflight-only.' >&2
  exit 64
fi

if [[ "$bulk_selection" -eq 1 \
  && ( "$restore_from_trash_page" -eq 1 || "$delete_forever_from_trash_page" -eq 1 ) ]]; then
  printf '%s\n' 'Bulk selection smoke verifies Trash page recoverability and cannot be combined with single-item Trash page actions.' >&2
  exit 64
fi

if [[ "$restore_bulk_from_trash_page" -eq 1 && "$bulk_selection" -ne 1 ]]; then
  printf '%s\n' '--restore-bulk-from-trash-page requires a bulk selection target.' >&2
  exit 64
fi

if [[ "$delete_bulk_forever_from_trash_page" -eq 1 && "$bulk_selection" -ne 1 ]]; then
  printf '%s\n' '--delete-bulk-forever-from-trash-page requires a bulk selection target.' >&2
  exit 64
fi

if [[ "$create_bulk_second_disposable_folder" -eq 1 && "$create_disposable_folder" -ne 1 ]]; then
  printf '%s\n' '--create-bulk-second-disposable-folder requires --create-disposable-folder.' >&2
  exit 64
fi

if [[ "$delete_bulk_forever_from_trash_page" -eq 1 \
  && ( "$create_disposable_folder" -ne 1 || "$create_bulk_second_disposable_folder" -ne 1 ) ]]; then
  printf '%s\n' '--delete-bulk-forever-from-trash-page requires both disposable folder creation options.' >&2
  exit 64
fi

if [[ "$delete_forever_from_trash_page" -eq 1 && "$create_disposable_folder" -ne 1 ]]; then
  printf '%s\n' '--delete-forever-from-trash-page requires --create-disposable-folder.' >&2
  exit 64
fi

if [[ -n "${target_file//[[:space:]]/}" ]]; then
  target_kind="file"
  target_name="$target_file"
elif [[ -n "${target_folder//[[:space:]]/}" ]]; then
  target_kind="folder"
  target_name="$target_folder"
fi

if [[ -z "$target_kind" && "$preflight_only" -eq 0 ]]; then
  printf 'Target file or folder is required unless --preflight-only is used.\n' >&2
  exit 64
fi

if [[ -n "$target_kind" ]]; then
  if [[ -z "${target_name//[[:space:]]/}" || "$target_name" == *"/"* ]]; then
    printf 'Target %s name must not be blank and must not contain a slash.\n' "$target_kind" >&2
    exit 64
  fi
fi

if [[ "$bulk_selection" -eq 1 ]]; then
  if [[ -z "${bulk_second_name//[[:space:]]/}" || "$bulk_second_name" == *"/"* ]]; then
    printf 'Bulk second %s name must not be blank and must not contain a slash.\n' "$bulk_second_kind" >&2
    exit 64
  fi

  if [[ "$bulk_second_name" == "$target_name" ]]; then
    printf 'Bulk second target name must be different from the primary target name.\n' >&2
    exit 64
  fi
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
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  if [[ -n "$target_kind" ]]; then
    if [[ "$bulk_selection" -eq 1 ]]; then
      evidence_dir="$evidence_root/$timestamp-selection-trash"
    elif [[ "$delete_forever_from_trash_page" -eq 1 ]]; then
      evidence_dir="$evidence_root/$timestamp-$target_kind-trash-delete-forever"
    elif [[ "$restore_from_trash_page" -eq 1 ]]; then
      evidence_dir="$evidence_root/$timestamp-$target_kind-trash-page-restore"
    else
      evidence_dir="$evidence_root/$timestamp-$target_kind-trash-restore"
    fi
  else
    evidence_dir="$evidence_root/$timestamp-trash-restore-preflight"
  fi
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

capture_failure_evidence() {
  local exit_code=$?

  if [[ "$exit_code" -ne 0 && -d "$evidence_dir" ]]; then
    capture_screen "98-failure" || true
    capture_text "99-logcat.txt" adb_device logcat -d -v time || true
  fi

  exit "$exit_code"
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

point_for_clickable_node_text() {
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
    if node.attrib.get("clickable") != "true":
        continue
    values = (
        node.attrib.get("text", ""),
        node.attrib.get("content-desc", ""),
        node.attrib.get("hint", ""),
    )
    if any(matches(value) for value in values):
        print(*center(parse_bounds(node.attrib["bounds"])))
        raise SystemExit(0)

for node in root.iter("node"):
    if node.attrib.get("enabled") != "true":
        continue
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

point_for_editable_node() {
  local xml_file="$1"
  local point_file="$evidence_dir/edit-point.txt"

  python3 - "$xml_file" > "$point_file" <<'PY'
import re
import sys
from xml.etree import ElementTree

xml_file = sys.argv[1]
root = ElementTree.parse(xml_file).getroot()

def parse_bounds(bounds: str) -> tuple[int, int, int, int]:
    match = re.fullmatch(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", bounds)
    if match is None:
        raise ValueError(bounds)
    return tuple(int(value) for value in match.groups())

def center(bounds: tuple[int, int, int, int]) -> tuple[int, int]:
    left, top, right, bottom = bounds
    return ((left + right) // 2, (top + bottom) // 2)

for node in root.iter("node"):
    class_name = node.attrib.get("class", "")
    if "EditText" in class_name or node.attrib.get("focused") == "true":
        print(*center(parse_bounds(node.attrib["bounds"])))
        raise SystemExit(0)

raise SystemExit("Could not find editable UI node")
PY

  read -r tap_x tap_y < "$point_file"
}

tap_clickable_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local mode="${3:-exact}"

  point_for_clickable_node_text "$xml_file" "$needle" "$mode"
  adb_device shell input tap "$tap_x" "$tap_y"
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

point_for_target_row_action() {
  local xml_file="$1"
  local item_name="$2"
  local action_text="$3"
  local point_file="$evidence_dir/row-action-point.txt"

  python3 - "$xml_file" "$item_name" "$action_text" > "$point_file" <<'PY'
import re
import sys
from xml.etree import ElementTree

xml_file, item_name, action_text = sys.argv[1:4]
root = ElementTree.parse(xml_file).getroot()

def parse_bounds(bounds: str) -> tuple[int, int, int, int]:
    match = re.fullmatch(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", bounds)
    if match is None:
        raise ValueError(bounds)
    return tuple(int(value) for value in match.groups())

def center(bounds: tuple[int, int, int, int]) -> tuple[int, int]:
    left, top, right, bottom = bounds
    return ((left + right) // 2, (top + bottom) // 2)

def values_for(node: ElementTree.Element) -> tuple[str, str, str]:
    return (
        node.attrib.get("text", ""),
        node.attrib.get("content-desc", ""),
        node.attrib.get("hint", ""),
    )

nodes: list[tuple[ElementTree.Element, tuple[int, int, int, int]]] = []
for node in root.iter("node"):
    bounds_value = node.attrib.get("bounds")
    if bounds_value:
        nodes.append((node, parse_bounds(bounds_value)))

target_nodes = [
    (node, bounds)
    for node, bounds in nodes
    if any(value == item_name for value in values_for(node))
]
if not target_nodes:
    target_nodes = [
        (node, bounds)
        for node, bounds in nodes
        if any(item_name in value for value in values_for(node))
    ]

action_nodes = [
    (node, bounds)
    for node, bounds in nodes
    if node.attrib.get("enabled", "true") == "true"
    and any(value == action_text for value in values_for(node))
]

matches: list[tuple[int, int, tuple[int, int]]] = []
for _target_node, target_bounds in target_nodes:
    target_x, target_y = center(target_bounds)
    for _action_node, action_bounds in action_nodes:
        action_x, action_y = center(action_bounds)
        vertical_distance = abs(action_y - target_y)
        if action_y < target_bounds[1] - 24:
            continue
        if vertical_distance > 520:
            continue
        horizontal_distance = abs(action_x - target_x)
        matches.append((vertical_distance, horizontal_distance, (action_x, action_y)))

if not matches:
    raise SystemExit(f"Could not find {action_text} for row: {item_name}")

matches.sort()
print(*matches[0][2])
PY

  read -r tap_x tap_y < "$point_file"
}

tap_target_row_action_from_xml() {
  local xml_file="$1"
  local item_name="$2"
  local action_text="$3"

  point_for_target_row_action "$xml_file" "$item_name" "$action_text"
  adb_device shell input tap "$tap_x" "$tap_y"
}

tap_editable_from_xml() {
  local xml_file="$1"

  point_for_editable_node "$xml_file"
  adb_device shell input tap "$tap_x" "$tap_y"
}

adb_input_text() {
  local text="$1"
  text="${text// /%s}"
  adb_device shell input text "$text"
}

verify_expected_version() {
  if [[ -n "$expected_version_code" ]] \
    && ! grep -Fq "versionCode=$expected_version_code" "$evidence_dir/05-package-version.txt"; then
    printf 'Installed versionCode does not match expected value %s.\n' "$expected_version_code" >&2
    exit 67
  fi

  if [[ -n "$expected_version_name" ]] \
    && ! grep -Fq "versionName=$expected_version_name" "$evidence_dir/05-package-version.txt"; then
    printf 'Installed versionName does not match expected value %s.\n' "$expected_version_name" >&2
    exit 67
  fi
}

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'repo=%s\n' "$COTTON_REPO_ROOT"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'preflight_only=%s\n' "$preflight_only"
    printf 'wait_seconds=%s\n' "$wait_seconds"
    printf 'cancel_on_timeout=%s\n' "$cancel_on_timeout"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'create_disposable_folder=%s\n' "$create_disposable_folder"
    printf 'restore_from_trash_page=%s\n' "$restore_from_trash_page"
    printf 'delete_forever_from_trash_page=%s\n' "$delete_forever_from_trash_page"
    printf 'restore_bulk_from_trash_page=%s\n' "$restore_bulk_from_trash_page"
    printf 'delete_bulk_forever_from_trash_page=%s\n' "$delete_bulk_forever_from_trash_page"
    printf 'bulk_selection=%s\n' "$bulk_selection"
    printf 'create_bulk_second_disposable_folder=%s\n' "$create_bulk_second_disposable_folder"
    printf 'bulk_second_kind=%s\n' "$bulk_second_kind"
    printf 'bulk_second_name=%s\n' "$bulk_second_name"
    printf 'bulk_second_file=%s\n' "$bulk_second_file"
    printf 'bulk_second_folder=%s\n' "$bulk_second_folder"
    printf 'target_kind=%s\n' "$target_kind"
    printf 'target_name=%s\n' "$target_name"
    printf 'target_file=%s\n' "$target_file"
    printf 'target_folder=%s\n' "$target_folder"
    printf 'maui_popups_docs=https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups\n'
    printf 'maui_toolbar_docs=https://learn.microsoft.com/en-us/dotnet/maui/user-interface/toolbaritem\n'
    printf 'maui_commanding_docs=https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/commanding\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  {
    if [[ "$bulk_selection" -eq 1 ]]; then
      cat <<EOF
# Files Bulk Trash Smoke

Package: \`$package_id\`
Device: \`$serial\`
Primary target kind: \`$target_kind\`
Primary target name: \`$target_name\`
Second target kind: \`$bulk_second_kind\`
Second target name: \`$bulk_second_name\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Both target rows are disposable or safe to leave in Trash after this run.
- [ ] Both target rows are visible together in Files root before the selection begins.
- [ ] If \`create_disposable_folder=1\`, \`28-created-folder.xml\` shows the primary disposable folder.
- [ ] If \`create_bulk_second_disposable_folder=1\`, \`29-created-bulk-second-folder.xml\` shows the second disposable folder.

## Bulk Move To Trash

- [ ] \`20-files-root-ready.xml\` shows Files root chrome.
- [ ] \`30-bulk-targets-visible.xml\` shows both target item rows.
- [ ] \`35-bulk-first-selected.xml\` shows \`1 selected\`.
- [ ] \`36-bulk-two-selected.xml\` shows \`2 selected\`.
- [ ] \`40-file-actions.xml\` shows the selection action sheet and \`Move to trash\`.
- [ ] \`50-trash-confirm.xml\` shows \`Move selection to trash?\` and names the selected item kinds.
- [ ] \`60-after-trash.xml\` shows \`2 items moved to trash.\`.

## Trash Page Recoverability

- [ ] \`65-account-actions.xml\` shows the \`Trash\` account action.
- [ ] \`66-trash-page.xml\` shows the Trash page chrome, both target items, \`Restore\`, and \`Delete forever\`.
- [ ] \`66-trash-overflow.xml\` shows the \`Empty\` toolbar overflow action without executing it.
- [ ] If \`restore_bulk_from_trash_page=1\`, \`69-trash-bulk-two-selected.xml\` shows \`2 selected\`, \`70-trash-bulk-restore-confirm.xml\` shows the restore confirmation, and \`80-after-trash-bulk-restore.xml\` shows \`2 selected items restored.\`.
- [ ] If \`delete_bulk_forever_from_trash_page=1\`, \`69-trash-bulk-two-selected.xml\` shows \`2 selected\`, \`70-trash-bulk-delete-forever-confirm.xml\` shows the permanent-delete confirmation, and \`80-after-trash-bulk-delete-forever.xml\` shows \`2 selected items deleted forever.\`.
- [ ] \`99-logcat.txt\` has no ANR/FATAL markers.
EOF
      return
    fi

    cat <<EOF
# Files Trash Smoke

Package: \`$package_id\`
Device: \`$serial\`
Target kind: \`$target_kind\`
Target name: \`$target_name\`
Restore from Trash page: \`$restore_from_trash_page\`
Delete forever from Trash page: \`$delete_forever_from_trash_page\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Target item is disposable or safe to restore after a trash cycle.
- [ ] Permanent delete is only run with \`create_disposable_folder=1\`.
- [ ] If \`create_disposable_folder=1\`, \`28-created-folder.xml\` shows the new folder before trash.

## Trash

- [ ] \`20-files-root-ready.xml\` shows Files root chrome.
- [ ] \`30-target-visible.xml\` shows the target item row.
- [ ] \`40-file-actions.xml\` shows the target action sheet and \`Move to trash\`.
- [ ] \`50-trash-confirm.xml\` shows the move-to-trash confirmation.
- [ ] \`60-after-trash.xml\` shows the moved-to-trash status and \`Restore\`.

EOF

    if [[ "$restore_from_trash_page" -eq 1 || "$delete_forever_from_trash_page" -eq 1 ]]; then
      cat <<EOF
## Trash Page

- [ ] \`65-account-actions.xml\` shows the \`Trash\` account action.
- [ ] \`66-trash-page.xml\` shows the Trash page chrome, target item, \`Restore\`, and \`Delete forever\`.
- [ ] \`66-trash-overflow.xml\` shows the \`Empty\` toolbar overflow action without executing it.

EOF
    fi

    if [[ "$delete_forever_from_trash_page" -eq 1 ]]; then
      cat <<EOF
## Delete Forever

- [ ] \`70-delete-forever-confirm.xml\` shows \`Delete forever?\`.
- [ ] \`80-after-delete-forever.xml\` shows the permanent delete result.
- [ ] \`99-logcat.txt\` has no ANR/FATAL markers.
EOF
    else
      cat <<EOF
## Restore

- [ ] \`70-restore-confirm.xml\` shows \`Restore item?\`.
- [ ] \`80-after-restore.xml\` shows the restored status or the target row restored in Files.
- [ ] \`99-logcat.txt\` has no ANR/FATAL markers.
EOF
    fi
  } > "$evidence_dir/checklist.md"
}

trap capture_failure_evidence EXIT

wait_for_files_root() {
  local attempt
  local attempt_limit
  local xml_file

  attempt_limit=$(( (wait_seconds + 2) / 3 ))
  for attempt in $(seq 0 "$((attempt_limit - 1))"); do
    capture_screen "20-files-root-$attempt"
    xml_file="$evidence_dir/20-files-root-$attempt.xml"
    if xml_has_text "$xml_file" "Files" \
      && xml_has_text "$xml_file" "Account" \
      && xml_has_text "$xml_file" "Add files"; then
      cp "$xml_file" "$evidence_dir/20-files-root-ready.xml"
      if [[ -f "$evidence_dir/20-files-root-$attempt.png" ]]; then
        cp "$evidence_dir/20-files-root-$attempt.png" "$evidence_dir/20-files-root-ready.png"
      fi
      files_root_xml="$xml_file"
      return
    fi

    sleep 3
  done

  printf 'Files root with signed-in chrome is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

create_disposable_target_folder() {
  create_disposable_folder_named \
    "$target_name" \
    "25-add-actions" \
    "26-new-folder-prompt" \
    "27-new-folder-filled" \
    "28-created-folder"
}

create_bulk_second_disposable_target_folder() {
  create_disposable_folder_named \
    "$bulk_second_name" \
    "29-bulk-second-add-actions" \
    "29-bulk-second-new-folder-prompt" \
    "29-bulk-second-new-folder-filled" \
    "29-created-bulk-second-folder"
}

create_disposable_folder_named() {
  local folder_name="$1"
  local add_actions_prefix="$2"
  local prompt_prefix="$3"
  local filled_prefix="$4"
  local created_prefix="$5"

  tap_clickable_from_xml "$files_root_xml" "Add files" exact
  sleep 1
  capture_screen "$add_actions_prefix"
  require_xml_text "$evidence_dir/$add_actions_prefix.xml" "New folder" \
    "Add action sheet did not expose New folder."

  tap_clickable_from_xml "$evidence_dir/$add_actions_prefix.xml" "New folder" exact
  sleep 1
  capture_screen "$prompt_prefix"
  require_xml_text "$evidence_dir/$prompt_prefix.xml" "New folder" "New-folder prompt did not open."
  require_xml_text "$evidence_dir/$prompt_prefix.xml" "Folder name" \
    "New-folder prompt did not show the folder-name field."

  tap_editable_from_xml "$evidence_dir/$prompt_prefix.xml"
  adb_input_text "$folder_name"
  sleep 1
  capture_screen "$filled_prefix"
  tap_clickable_from_xml "$evidence_dir/$filled_prefix.xml" "Create" exact
  wait_for_created_folder "$folder_name" "$created_prefix"
}

wait_for_created_folder() {
  local folder_name="$1"
  local created_prefix="$2"
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    capture_screen "$created_prefix-$attempt"
    xml_file="$evidence_dir/$created_prefix-$attempt.xml"

    if xml_has_text "$xml_file" "Actions for $folder_name"; then
      cp "$xml_file" "$evidence_dir/$created_prefix.xml"
      if [[ -f "$evidence_dir/$created_prefix-$attempt.png" ]]; then
        cp "$evidence_dir/$created_prefix-$attempt.png" "$evidence_dir/$created_prefix.png"
      fi
      files_root_xml="$xml_file"
      return
    fi

    if xml_has_text "$xml_file" "An item with that name already exists." \
      || xml_has_text "$xml_file" "Could not create folder." \
      || xml_has_text "$xml_file" "Offline. New folder needs internet." \
      || xml_has_text "$xml_file" "New folder cancelled."; then
      printf 'Disposable folder creation did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "$created_prefix-timeout"
  printf 'Timed out waiting for disposable folder creation.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

ensure_target_visible() {
  local xml_file="$files_root_xml"

  if ! xml_has_text "$xml_file" "$target_name"; then
    tap_clickable_from_xml "$xml_file" "Search files" exact
    sleep 1
    capture_screen "30-search-open"
    adb_input_text "$target_name"
    sleep 2
    capture_screen "30-target-visible"
    xml_file="$evidence_dir/30-target-visible.xml"
  else
    capture_screen "30-target-visible"
    xml_file="$evidence_dir/30-target-visible.xml"
  fi

  require_xml_text "$xml_file" "Actions for $target_name" "Target $target_kind row is not visible in Files."
  target_xml="$xml_file"
}

bulk_file_count() {
  local count=0

  if [[ "$target_kind" == "file" ]]; then
    count=$((count + 1))
  fi

  if [[ "$bulk_second_kind" == "file" ]]; then
    count=$((count + 1))
  fi

  printf '%s\n' "$count"
}

bulk_folder_count() {
  local count=0

  if [[ "$target_kind" == "folder" ]]; then
    count=$((count + 1))
  fi

  if [[ "$bulk_second_kind" == "folder" ]]; then
    count=$((count + 1))
  fi

  printf '%s\n' "$count"
}

format_bulk_selection_text() {
  local file_count
  local folder_count
  local parts=()

  file_count="$(bulk_file_count)"
  folder_count="$(bulk_folder_count)"
  if [[ "$file_count" -gt 0 ]]; then
    if [[ "$file_count" -eq 1 ]]; then
      parts+=("1 file")
    else
      parts+=("$file_count files")
    fi
  fi

  if [[ "$folder_count" -gt 0 ]]; then
    if [[ "$folder_count" -eq 1 ]]; then
      parts+=("1 folder")
    else
      parts+=("$folder_count folders")
    fi
  fi

  if [[ "${#parts[@]}" -eq 1 ]]; then
    printf '%s\n' "${parts[0]}"
  else
    printf '%s and %s\n' "${parts[0]}" "${parts[1]}"
  fi
}

ensure_bulk_targets_visible() {
  capture_screen "30-bulk-targets-visible"
  bulk_target_xml="$evidence_dir/30-bulk-targets-visible.xml"

  require_xml_text "$bulk_target_xml" "Actions for $target_name" \
    "Primary bulk target $target_kind row is not visible in Files."
  require_xml_text "$bulk_target_xml" "Actions for $bulk_second_name" \
    "Second bulk target $bulk_second_kind row is not visible in Files."
}

select_bulk_targets() {
  long_press_row_from_xml "$bulk_target_xml" "$target_name"
  sleep 2
  capture_screen "35-bulk-first-selected"
  require_xml_text "$evidence_dir/35-bulk-first-selected.xml" "1 selected" \
    "Long press did not start bulk file selection."

  tap_row_from_xml "$evidence_dir/35-bulk-first-selected.xml" "$bulk_second_name"
  sleep 1
  capture_screen "36-bulk-two-selected"
  require_xml_text "$evidence_dir/36-bulk-two-selected.xml" "2 selected" \
    "Second bulk target did not join the selection."

  local file_count
  local folder_count
  file_count="$(bulk_file_count)"
  folder_count="$(bulk_folder_count)"
  if [[ "$file_count" -gt 0 ]]; then
    require_xml_text "$evidence_dir/36-bulk-two-selected.xml" \
      "$file_count file" \
      "Bulk selection detail did not show the expected file count."
  fi

  if [[ "$folder_count" -gt 0 ]]; then
    require_xml_text "$evidence_dir/36-bulk-two-selected.xml" \
      "$folder_count folder" \
      "Bulk selection detail did not show the expected folder count."
  fi

  bulk_selected_xml="$evidence_dir/36-bulk-two-selected.xml"
}

open_bulk_selection_actions() {
  tap_clickable_from_xml "$bulk_selected_xml" "Actions" exact
  sleep 1
  capture_screen "40-file-actions"
  require_xml_text "$evidence_dir/40-file-actions.xml" "2 selected" \
    "Bulk selection action sheet did not open."
  require_xml_text "$evidence_dir/40-file-actions.xml" "Move to trash" \
    "Bulk selection action sheet did not expose Move to trash."
}

confirm_bulk_move_to_trash() {
  local selection_text

  selection_text="$(format_bulk_selection_text)"
  tap_clickable_from_xml "$evidence_dir/40-file-actions.xml" "Move to trash" exact
  sleep 1
  capture_screen "50-trash-confirm"
  require_xml_text "$evidence_dir/50-trash-confirm.xml" "Move selection to trash?" \
    "Bulk move-to-trash confirmation did not open."
  require_xml_text "$evidence_dir/50-trash-confirm.xml" \
    "$selection_text will be removed from this folder and can be restored from trash." \
    "Bulk move-to-trash confirmation did not describe the selected item kinds."
  tap_clickable_from_xml "$evidence_dir/50-trash-confirm.xml" "Move to trash" exact
}

wait_for_bulk_trash_completion() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    capture_screen "60-after-trash-$attempt"
    xml_file="$evidence_dir/60-after-trash-$attempt.xml"

    if xml_has_text "$xml_file" "2 items moved to trash."; then
      cp "$xml_file" "$evidence_dir/60-after-trash.xml"
      if [[ -f "$evidence_dir/60-after-trash-$attempt.png" ]]; then
        cp "$evidence_dir/60-after-trash-$attempt.png" "$evidence_dir/60-after-trash.png"
      fi
      trash_xml="$xml_file"
      return
    fi

    if xml_has_text "$xml_file" "Could not move selection to trash." \
      || xml_has_text "$xml_file" "Refresh this folder before moving selected files to trash." \
      || xml_has_text "$xml_file" "Offline. Move to trash needs internet." \
      || xml_has_text "$xml_file" "Move to trash is taking longer than expected. Refresh and try again." \
      || xml_has_text "$xml_file" "Move to trash failed after" \
      || xml_has_text "$xml_file" "Move to trash cancelled"; then
      printf 'Bulk move to trash did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "60-after-trash-timeout"
  printf 'Timed out waiting for bulk move-to-trash completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

open_bulk_trash_page() {
  tap_clickable_from_xml "$trash_xml" "Account" exact
  sleep 1
  capture_screen "65-account-actions"
  require_xml_text "$evidence_dir/65-account-actions.xml" "Trash" \
    "Account action sheet did not expose Trash."
  tap_clickable_from_xml "$evidence_dir/65-account-actions.xml" "Trash" exact
  sleep 2
  wait_for_bulk_trash_page_items
  verify_empty_trash_overflow_action
}

wait_for_bulk_trash_page_items() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    capture_screen "66-trash-page-$attempt"
    xml_file="$evidence_dir/66-trash-page-$attempt.xml"

    if xml_has_text "$xml_file" "Trash" \
      && xml_has_text "$xml_file" "Search trash" \
      && xml_has_text "$xml_file" "Sort trash" \
      && xml_has_text "$xml_file" "Change trash view" \
      && xml_has_text "$xml_file" "$target_name" \
      && xml_has_text "$xml_file" "$bulk_second_name" \
      && xml_has_text "$xml_file" "Restore" \
      && xml_has_text "$xml_file" "Delete forever"; then
      cp "$xml_file" "$evidence_dir/66-trash-page.xml"
      if [[ -f "$evidence_dir/66-trash-page-$attempt.png" ]]; then
        cp "$evidence_dir/66-trash-page-$attempt.png" "$evidence_dir/66-trash-page.png"
      fi
      return
    fi

    if xml_has_text "$xml_file" "Could not load trash." \
      || xml_has_text "$xml_file" "Offline. Trash needs internet." \
      || xml_has_text "$xml_file" "Trash refresh cancelled."; then
      printf 'Trash page did not load successfully after bulk move to trash.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    adb_device shell input swipe 540 1700 540 650 350 >/dev/null 2>&1 || true
    sleep 3
    attempt=$((attempt + 1))
  done

  printf 'Timed out waiting for both bulk target rows on Trash page.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

verify_empty_trash_overflow_action() {
  require_xml_text "$evidence_dir/66-trash-page.xml" "More" \
    "Trash page did not expose the toolbar overflow for Empty."
  tap_clickable_from_xml "$evidence_dir/66-trash-page.xml" "More" contains
  sleep 1
  capture_screen "66-trash-overflow"
  require_xml_text "$evidence_dir/66-trash-overflow.xml" "Empty" \
    "Trash page overflow did not expose Empty."
  adb_device shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
  sleep 1
}

restore_bulk_from_trash_page() {
  local selection_text

  tap_clickable_from_xml "$evidence_dir/66-trash-page.xml" "Select" exact
  sleep 1
  capture_screen "67-trash-bulk-select-mode"
  require_xml_text "$evidence_dir/67-trash-bulk-select-mode.xml" "Select trash items" \
    "Trash page selection mode did not open."
  require_xml_text "$evidence_dir/67-trash-bulk-select-mode.xml" "Tap items to select them." \
    "Trash page selection mode did not explain item selection."

  tap_row_from_xml "$evidence_dir/67-trash-bulk-select-mode.xml" "$target_name"
  sleep 1
  capture_screen "68-trash-bulk-first-selected"
  require_xml_text "$evidence_dir/68-trash-bulk-first-selected.xml" "1 selected" \
    "Primary Trash row did not become selected."

  tap_row_from_xml "$evidence_dir/68-trash-bulk-first-selected.xml" "$bulk_second_name"
  sleep 1
  capture_screen "69-trash-bulk-two-selected"
  require_xml_text "$evidence_dir/69-trash-bulk-two-selected.xml" "2 selected" \
    "Second Trash row did not join the selection."
  require_xml_text "$evidence_dir/69-trash-bulk-two-selected.xml" "Restore" \
    "Trash selection bar did not expose Restore."
  require_xml_text "$evidence_dir/69-trash-bulk-two-selected.xml" "Delete forever" \
    "Trash selection bar did not expose Delete forever."

  tap_clickable_from_xml "$evidence_dir/69-trash-bulk-two-selected.xml" "Restore" exact
  sleep 1
  capture_screen "70-trash-bulk-restore-confirm"
  selection_text="$(format_bulk_selection_text)"
  require_xml_text "$evidence_dir/70-trash-bulk-restore-confirm.xml" "Restore selected items?" \
    "Trash bulk restore confirmation did not open."
  require_xml_text "$evidence_dir/70-trash-bulk-restore-confirm.xml" \
    "Restore 2 selected items to their original locations?" \
    "Trash bulk restore confirmation did not describe the selected item count."
  tap_clickable_from_xml "$evidence_dir/70-trash-bulk-restore-confirm.xml" "Restore" exact

  wait_for_bulk_trash_restore_completion "$selection_text"
}

wait_for_bulk_trash_restore_completion() {
  local selection_text="$1"
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    capture_screen "80-after-trash-bulk-restore-$attempt"
    xml_file="$evidence_dir/80-after-trash-bulk-restore-$attempt.xml"

    if xml_has_text "$xml_file" "2 selected items restored."; then
      cp "$xml_file" "$evidence_dir/80-after-trash-bulk-restore.xml"
      if [[ -f "$evidence_dir/80-after-trash-bulk-restore-$attempt.png" ]]; then
        cp "$evidence_dir/80-after-trash-bulk-restore-$attempt.png" \
          "$evidence_dir/80-after-trash-bulk-restore.png"
      fi
      return
    fi

    if xml_has_text "$xml_file" "Could not restore selected items." \
      || xml_has_text "$xml_file" "Offline. Restore needs internet." \
      || xml_has_text "$xml_file" "Selection action cancelled." \
      || xml_has_text "$xml_file" "0 of 2 selected items restored."; then
      printf 'Trash page bulk restore did not complete successfully for %s.\n' "$selection_text" >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  printf 'Timed out waiting for Trash page bulk restore completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

delete_bulk_forever_from_trash_page() {
  tap_clickable_from_xml "$evidence_dir/66-trash-page.xml" "Select" exact
  sleep 1
  capture_screen "67-trash-bulk-select-mode"
  require_xml_text "$evidence_dir/67-trash-bulk-select-mode.xml" "Select trash items" \
    "Trash page selection mode did not open."
  require_xml_text "$evidence_dir/67-trash-bulk-select-mode.xml" "Tap items to select them." \
    "Trash page selection mode did not explain item selection."

  tap_row_from_xml "$evidence_dir/67-trash-bulk-select-mode.xml" "$target_name"
  sleep 1
  capture_screen "68-trash-bulk-first-selected"
  require_xml_text "$evidence_dir/68-trash-bulk-first-selected.xml" "1 selected" \
    "Primary Trash row did not become selected."

  tap_row_from_xml "$evidence_dir/68-trash-bulk-first-selected.xml" "$bulk_second_name"
  sleep 1
  capture_screen "69-trash-bulk-two-selected"
  require_xml_text "$evidence_dir/69-trash-bulk-two-selected.xml" "2 selected" \
    "Second Trash row did not join the selection."
  require_xml_text "$evidence_dir/69-trash-bulk-two-selected.xml" "Restore" \
    "Trash selection bar did not expose Restore."
  require_xml_text "$evidence_dir/69-trash-bulk-two-selected.xml" "Delete forever" \
    "Trash selection bar did not expose Delete forever."

  tap_clickable_from_xml "$evidence_dir/69-trash-bulk-two-selected.xml" "Delete forever" exact
  sleep 1
  capture_screen "70-trash-bulk-delete-forever-confirm"
  require_xml_text "$evidence_dir/70-trash-bulk-delete-forever-confirm.xml" "Delete selected forever?" \
    "Trash bulk delete-forever confirmation did not open."
  require_xml_text "$evidence_dir/70-trash-bulk-delete-forever-confirm.xml" \
    "Permanently delete 2 selected items? This cannot be undone." \
    "Trash bulk delete-forever confirmation did not describe the selected item count."
  tap_clickable_from_xml "$evidence_dir/70-trash-bulk-delete-forever-confirm.xml" "Delete forever" exact

  wait_for_bulk_trash_delete_forever_completion
}

wait_for_bulk_trash_delete_forever_completion() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    capture_screen "80-after-trash-bulk-delete-forever-$attempt"
    xml_file="$evidence_dir/80-after-trash-bulk-delete-forever-$attempt.xml"

    if xml_has_text "$xml_file" "2 selected items deleted forever."; then
      cp "$xml_file" "$evidence_dir/80-after-trash-bulk-delete-forever.xml"
      if [[ -f "$evidence_dir/80-after-trash-bulk-delete-forever-$attempt.png" ]]; then
        cp "$evidence_dir/80-after-trash-bulk-delete-forever-$attempt.png" \
          "$evidence_dir/80-after-trash-bulk-delete-forever.png"
      fi
      return
    fi

    if xml_has_text "$xml_file" "Could not delete selected items." \
      || xml_has_text "$xml_file" "Offline. Delete forever needs internet." \
      || xml_has_text "$xml_file" "Selection action cancelled." \
      || xml_has_text "$xml_file" "0 of 2 selected items deleted forever."; then
      printf 'Trash page bulk delete-forever did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  printf 'Timed out waiting for Trash page bulk delete-forever completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

open_target_actions() {
  tap_clickable_from_xml "$target_xml" "Actions for $target_name" exact
  sleep 1
  capture_screen "40-file-actions"
  require_xml_text "$evidence_dir/40-file-actions.xml" "$target_name" "Target action sheet did not open."
  require_xml_text "$evidence_dir/40-file-actions.xml" "Move to trash" "Target action sheet did not expose Move to trash."
}

confirm_move_to_trash() {
  local confirm_title="Move to trash?"
  local confirm_message="$target_name will be removed"

  if [[ "$target_kind" == "folder" ]]; then
    confirm_title="Move folder to trash?"
    confirm_message="$target_name and its contents will be removed"
  fi

  tap_clickable_from_xml "$evidence_dir/40-file-actions.xml" "Move to trash" exact
  sleep 1
  capture_screen "50-trash-confirm"
  require_xml_text "$evidence_dir/50-trash-confirm.xml" "$confirm_title" "Move-to-trash confirmation did not open."
  require_xml_text "$evidence_dir/50-trash-confirm.xml" "$confirm_message" "Move-to-trash confirmation did not name the target $target_kind."
  tap_clickable_from_xml "$evidence_dir/50-trash-confirm.xml" "Move to trash" exact
}

cancel_after_timeout() {
  local xml_file="$1"
  local prefix="$2"

  if [[ "$cancel_on_timeout" -eq 1 ]] && xml_has_text "$xml_file" "Cancel"; then
    tap_clickable_from_xml "$xml_file" "Cancel" exact
    sleep 1
    capture_screen "$prefix-cancelled"
  fi
}

wait_for_trash_follow_up() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    capture_screen "60-after-trash-$attempt"
    xml_file="$evidence_dir/60-after-trash-$attempt.xml"

    if xml_has_text "$xml_file" "$target_name moved to trash." \
      && xml_has_text "$xml_file" "Restore"; then
      cp "$xml_file" "$evidence_dir/60-after-trash.xml"
      if [[ -f "$evidence_dir/60-after-trash-$attempt.png" ]]; then
        cp "$evidence_dir/60-after-trash-$attempt.png" "$evidence_dir/60-after-trash.png"
      fi
      trash_xml="$xml_file"
      return
    fi

    if xml_has_text "$xml_file" "Could not move file to trash." \
      || xml_has_text "$xml_file" "Could not move folder to trash." \
      || xml_has_text "$xml_file" "Offline. Move to trash needs internet." \
      || xml_has_text "$xml_file" "Move to trash is taking longer than expected. Refresh and try again." \
      || xml_has_text "$xml_file" "Move to trash cancelled."; then
      printf 'Move to trash did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "60-after-trash-timeout"
  printf 'Timed out waiting for move-to-trash completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

open_trash_page() {
  tap_clickable_from_xml "$trash_xml" "Account" exact
  sleep 1
  capture_screen "65-account-actions"
  require_xml_text "$evidence_dir/65-account-actions.xml" "Trash" \
    "Account action sheet did not expose Trash."
  tap_clickable_from_xml "$evidence_dir/65-account-actions.xml" "Trash" exact
  sleep 2
  wait_for_trash_page_item
  verify_empty_trash_overflow_action
}

wait_for_trash_page_item() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    capture_screen "66-trash-page-$attempt"
    xml_file="$evidence_dir/66-trash-page-$attempt.xml"

    if xml_has_text "$xml_file" "Trash" \
      && xml_has_text "$xml_file" "Search trash" \
      && xml_has_text "$xml_file" "Sort trash" \
      && xml_has_text "$xml_file" "Change trash view" \
      && xml_has_text "$xml_file" "$target_name" \
      && xml_has_text "$xml_file" "Restore" \
      && xml_has_text "$xml_file" "Delete forever"; then
      cp "$xml_file" "$evidence_dir/66-trash-page.xml"
      if [[ -f "$evidence_dir/66-trash-page-$attempt.png" ]]; then
        cp "$evidence_dir/66-trash-page-$attempt.png" "$evidence_dir/66-trash-page.png"
      fi
      trash_page_xml="$xml_file"
      return
    fi

    if xml_has_text "$xml_file" "Could not load trash." \
      || xml_has_text "$xml_file" "Offline. Trash needs internet." \
      || xml_has_text "$xml_file" "Trash refresh cancelled."; then
      printf 'Trash page did not load successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    adb_device shell input swipe 540 1700 540 650 350 >/dev/null 2>&1 || true
    sleep 3
    attempt=$((attempt + 1))
  done

  printf 'Timed out waiting for the target row on Trash page.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

confirm_restore() {
  tap_clickable_from_xml "$trash_xml" "Restore" exact
  sleep 1
  capture_screen "70-restore-confirm"
  require_xml_text "$evidence_dir/70-restore-confirm.xml" "Restore item?" "Restore confirmation did not open."
  require_xml_text "$evidence_dir/70-restore-confirm.xml" "Restore $target_name" "Restore confirmation did not name the target $target_kind."
  tap_clickable_from_xml "$evidence_dir/70-restore-confirm.xml" "Restore" exact
}

confirm_trash_page_restore() {
  tap_target_row_action_from_xml "$trash_page_xml" "$target_name" "Restore"
  sleep 1
  capture_screen "70-restore-confirm"
  require_xml_text "$evidence_dir/70-restore-confirm.xml" "Restore item?" "Restore confirmation did not open."
  require_xml_text "$evidence_dir/70-restore-confirm.xml" "Restore $target_name" "Restore confirmation did not name the target $target_kind."
  tap_clickable_from_xml "$evidence_dir/70-restore-confirm.xml" "Restore" exact
}

wait_for_restore_completion() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    capture_screen "80-after-restore-$attempt"
    xml_file="$evidence_dir/80-after-restore-$attempt.xml"

    if xml_has_text "$xml_file" "$target_name restored." \
      || xml_has_text "$xml_file" "Actions for $target_name"; then
      cp "$xml_file" "$evidence_dir/80-after-restore.xml"
      if [[ -f "$evidence_dir/80-after-restore-$attempt.png" ]]; then
        cp "$evidence_dir/80-after-restore-$attempt.png" "$evidence_dir/80-after-restore.png"
      fi
      return
    fi

    if xml_has_text "$xml_file" "Could not restore item." \
      || xml_has_text "$xml_file" "Offline. Restore needs internet." \
      || xml_has_text "$xml_file" "Restore is taking longer than expected. Refresh and try again." \
      || xml_has_text "$xml_file" "Restore cancelled."; then
      printf 'Restore did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "80-after-restore-timeout"
  printf 'Timed out waiting for restore completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

confirm_trash_page_delete_forever() {
  tap_target_row_action_from_xml "$trash_page_xml" "$target_name" "Delete forever"
  sleep 1
  capture_screen "70-delete-forever-confirm"
  require_xml_text "$evidence_dir/70-delete-forever-confirm.xml" "Delete forever?" \
    "Delete-forever confirmation did not open."
  require_xml_text "$evidence_dir/70-delete-forever-confirm.xml" "Permanently delete $target_name" \
    "Delete-forever confirmation did not name the target $target_kind."
  require_xml_text "$evidence_dir/70-delete-forever-confirm.xml" "This cannot be undone." \
    "Delete-forever confirmation did not explain the permanent action."
  tap_clickable_from_xml "$evidence_dir/70-delete-forever-confirm.xml" "Delete forever" exact
}

wait_for_delete_forever_completion() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    capture_screen "80-after-delete-forever-$attempt"
    xml_file="$evidence_dir/80-after-delete-forever-$attempt.xml"

    if xml_has_text "$xml_file" "$target_name permanently deleted."; then
      cp "$xml_file" "$evidence_dir/80-after-delete-forever.xml"
      if [[ -f "$evidence_dir/80-after-delete-forever-$attempt.png" ]]; then
        cp "$evidence_dir/80-after-delete-forever-$attempt.png" "$evidence_dir/80-after-delete-forever.png"
      fi
      return
    fi

    if xml_has_text "$xml_file" "Could not permanently delete item." \
      || xml_has_text "$xml_file" "Offline. Delete forever needs internet." \
      || xml_has_text "$xml_file" "Refresh trash before permanently deleting this file." \
      || xml_has_text "$xml_file" "Delete forever cancelled."; then
      printf 'Delete forever did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "80-after-delete-forever-timeout"
  printf 'Timed out waiting for delete-forever completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

write_metadata
write_checklist

capture_text "00-device.txt" adb_device shell getprop ro.product.model
capture_text "01-adb-devices.txt" adb devices
capture_text "02-window.txt" adb_device shell dumpsys window

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/03-install.txt"
fi

capture_text "04-package.txt" adb_device shell dumpsys package "$package_id"
capture_text "05-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
verify_expected_version

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c || true
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/06-launch.txt"
  sleep 3
fi

wait_for_files_root

if [[ "$preflight_only" -eq 1 ]]; then
  capture_text "99-logcat.txt" adb_device logcat -d -v time
  printf 'Files trash/restore preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$create_disposable_folder" -eq 1 ]]; then
  create_disposable_target_folder
fi

if [[ "$create_bulk_second_disposable_folder" -eq 1 ]]; then
  create_bulk_second_disposable_target_folder
fi

if [[ "$bulk_selection" -eq 1 ]]; then
  ensure_bulk_targets_visible
  select_bulk_targets
  open_bulk_selection_actions
  confirm_bulk_move_to_trash
  wait_for_bulk_trash_completion
  open_bulk_trash_page
  if [[ "$restore_bulk_from_trash_page" -eq 1 ]]; then
    restore_bulk_from_trash_page
  elif [[ "$delete_bulk_forever_from_trash_page" -eq 1 ]]; then
    delete_bulk_forever_from_trash_page
  fi
  capture_text "99-logcat.txt" adb_device logcat -d -v time
  if [[ "$restore_bulk_from_trash_page" -eq 1 ]]; then
    printf 'Files bulk selection trash and Trash restore evidence captured in %s\n' "$evidence_dir"
  elif [[ "$delete_bulk_forever_from_trash_page" -eq 1 ]]; then
    printf 'Files bulk selection trash and Trash delete-forever evidence captured in %s\n' "$evidence_dir"
  else
    printf 'Files bulk selection trash evidence captured in %s\n' "$evidence_dir"
  fi
  exit 0
fi

ensure_target_visible
open_target_actions
confirm_move_to_trash
wait_for_trash_follow_up
if [[ "$delete_forever_from_trash_page" -eq 1 ]]; then
  open_trash_page
  confirm_trash_page_delete_forever
  wait_for_delete_forever_completion
elif [[ "$restore_from_trash_page" -eq 1 ]]; then
  open_trash_page
  confirm_trash_page_restore
  wait_for_restore_completion
else
  confirm_restore
  wait_for_restore_completion
fi
capture_text "99-logcat.txt" adb_device logcat -d -v time

if [[ "$delete_forever_from_trash_page" -eq 1 ]]; then
  printf 'Files %s trash/delete-forever evidence captured in %s\n' "$target_kind" "$evidence_dir"
else
  printf 'Files %s trash/restore evidence captured in %s\n' "$target_kind" "$evidence_dir"
fi
