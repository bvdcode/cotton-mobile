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
  --wait-seconds N          Seconds to wait for each server mutation. Defaults to $wait_seconds.
  --no-cancel-on-timeout    Leave the app in its current state when a mutation times out.
  --preflight-only          Capture device/package/root state and exit.
  --no-launch               Do not launch the app before capture.
  --help, -h                Show this help.

The app must already have a signed-in session. Use a disposable test file/folder
or a known smoke fixture because this script performs a real server
trash/restore cycle when the backend responds successfully.
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
    evidence_dir="$evidence_root/$timestamp-$target_kind-trash-restore"
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
    printf 'target_kind=%s\n' "$target_kind"
    printf 'target_name=%s\n' "$target_name"
    printf 'target_file=%s\n' "$target_file"
    printf 'target_folder=%s\n' "$target_folder"
    printf 'maui_popups_docs=https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Files Trash Restore Smoke

Package: \`$package_id\`
Device: \`$serial\`
Target kind: \`$target_kind\`
Target name: \`$target_name\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Target item is disposable or safe to restore after a trash cycle.
- [ ] If \`create_disposable_folder=1\`, \`28-created-folder.xml\` shows the new folder before trash.

## Trash

- [ ] \`20-files-root-ready.xml\` shows Files root chrome.
- [ ] \`30-target-visible.xml\` shows the target item row.
- [ ] \`40-file-actions.xml\` shows the target action sheet and \`Move to trash\`.
- [ ] \`50-trash-confirm.xml\` shows the move-to-trash confirmation.
- [ ] \`60-after-trash.xml\` shows the moved-to-trash status and \`Restore\`.

## Restore

- [ ] \`70-restore-confirm.xml\` shows \`Restore item?\`.
- [ ] \`80-after-restore.xml\` shows the restored status or the target row restored in Files.
- [ ] \`99-logcat.txt\` has no ANR/FATAL markers.
EOF
}

trap capture_failure_evidence EXIT

wait_for_files_root() {
  local attempt
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7 8 9; do
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

    adb_device shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 1
    adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/20-relaunch-$attempt.txt" || true
    sleep 2
  done

  printf 'Files root with signed-in chrome is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

create_disposable_target_folder() {
  tap_clickable_from_xml "$files_root_xml" "Add files" exact
  sleep 1
  capture_screen "25-add-actions"
  require_xml_text "$evidence_dir/25-add-actions.xml" "New folder" "Add action sheet did not expose New folder."

  tap_clickable_from_xml "$evidence_dir/25-add-actions.xml" "New folder" exact
  sleep 1
  capture_screen "26-new-folder-prompt"
  require_xml_text "$evidence_dir/26-new-folder-prompt.xml" "New folder" "New-folder prompt did not open."
  require_xml_text "$evidence_dir/26-new-folder-prompt.xml" "Folder name" "New-folder prompt did not show the folder-name field."

  tap_editable_from_xml "$evidence_dir/26-new-folder-prompt.xml"
  adb_input_text "$target_name"
  sleep 1
  capture_screen "27-new-folder-filled"
  tap_clickable_from_xml "$evidence_dir/27-new-folder-filled.xml" "Create" exact
  wait_for_created_folder
}

wait_for_created_folder() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    capture_screen "28-created-folder-$attempt"
    xml_file="$evidence_dir/28-created-folder-$attempt.xml"

    if xml_has_text "$xml_file" "Actions for $target_name"; then
      cp "$xml_file" "$evidence_dir/28-created-folder.xml"
      if [[ -f "$evidence_dir/28-created-folder-$attempt.png" ]]; then
        cp "$evidence_dir/28-created-folder-$attempt.png" "$evidence_dir/28-created-folder.png"
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

  cancel_after_timeout "$xml_file" "28-created-folder-timeout"
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

confirm_restore() {
  tap_clickable_from_xml "$trash_xml" "Restore" exact
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

ensure_target_visible
open_target_actions
confirm_move_to_trash
wait_for_trash_follow_up
confirm_restore
wait_for_restore_completion
capture_text "99-logcat.txt" adb_device logcat -d -v time

printf 'Files %s trash/restore evidence captured in %s\n' "$target_kind" "$evidence_dir"
