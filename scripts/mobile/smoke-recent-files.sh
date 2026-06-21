#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
run_id=""
verify_clear=0
leave_seed=0

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an Android Recent files smoke:
  1. Seeds one app-private recent metadata entry and matching local text download.
  2. Opens Recent files from the account action sheet.
  3. Verifies the seeded recent row is visible.
  4. Taps the row and verifies the in-app text viewer opens the local copy.
  5. Optionally verifies Clear with --verify-clear.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --run-id ID               Stable run id for seeded file names.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --no-launch               Do not launch automatically.
  --verify-clear            Also verify the Recent files Clear toolbar action.
  --leave-seed              Leave seeded recent metadata and local download in app data.
  --help, -h                Show this help.

The app must already have a signed-in session for the selected instance.
By default, existing recent metadata is restored and the seeded download is removed.
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
    --instance)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --instance.\n' >&2
        exit 64
      fi
      instance_uri="$2"
      shift 2
      ;;
    --run-id)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --run-id.\n' >&2
        exit 64
      fi
      run_id="$2"
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
    --verify-clear)
      verify_clear=1
      shift
      ;;
    --leave-seed)
      leave_seed=1
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

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
if [[ -z "$run_id" ]]; then
  run_id="$timestamp"
fi

if [[ ! "$run_id" =~ ^[A-Za-z0-9._-]+$ ]]; then
  printf 'Run id must contain only letters, digits, dot, underscore, or hyphen.\n' >&2
  exit 64
fi

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-recent-files"
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if ! command -v python3 >/dev/null 2>&1; then
  printf 'python3 was not found.\n' >&2
  exit 127
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
  local remote_xml="/sdcard/cotton-recent-files-window.xml"

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

create_instance_key() {
  python3 - "$instance_uri" <<'PY'
import hashlib
import sys
from urllib.parse import urlparse

uri = urlparse(sys.argv[1])
if uri.scheme.lower() not in ("http", "https") or not uri.hostname:
    raise SystemExit("Instance URI must include http(s) scheme and host.")

scheme = uri.scheme.lower()
host = uri.hostname.lower()
default_port = (scheme == "http" and uri.port in (None, 80)) or (
    scheme == "https" and uri.port in (None, 443)
)
authority = host if default_port else f"{host}:{uri.port}"
path = "" if uri.path in ("", "/") else uri.path.rstrip("/")
scope = f"{scheme}://{authority}{path}"
print(hashlib.sha256(scope.encode("utf-8")).hexdigest())
PY
}

create_smoke_file_id() {
  python3 - "$run_id" <<'PY'
import sys
import uuid

print(uuid.uuid5(uuid.NAMESPACE_URL, f"cotton-recent-files-smoke:{sys.argv[1]}"))
PY
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

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'instance_key=%s\n' "$instance_key"
    printf 'run_id=%s\n' "$run_id"
    printf 'file_id=%s\n' "$smoke_file_id"
    printf 'file_name=%s\n' "$smoke_file_name"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'verify_clear=%s\n' "$verify_clear"
    printf 'leave_seed=%s\n' "$leave_seed"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_storage_docs=https://developer.android.com/training/data-storage/app-specific\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
    printf 'maui_tap_docs=https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/gestures/tap\n'
  } > "$evidence_dir/00-metadata.txt"
}

prepare_seed_files() {
  local seed_dir="$1"
  local content_file="$seed_dir/$smoke_file_name"
  local existing_metadata="$seed_dir/existing-recent-files.json"
  local output_metadata="$seed_dir/recent-files.json"
  local now_utc
  local size_bytes

  now_utc="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  printf 'Recent files smoke %s\n' "$run_id" > "$content_file"
  size_bytes="$(wc -c < "$content_file" | tr -d ' ')"

  if adb_device shell run-as "$package_id" cat "$recent_metadata_path" \
    > "$existing_metadata" 2> "$seed_dir/existing-recent-files.err"; then
    recent_backup_exists=1
    cp "$existing_metadata" "$recent_backup_path"
  else
    recent_backup_exists=0
    printf '{"schemaVersion":1,"savedAtUtc":"%s","items":[]}\n' "$now_utc" > "$existing_metadata"
  fi

  python3 - \
    "$existing_metadata" \
    "$output_metadata" \
    "$now_utc" \
    "$smoke_file_id" \
    "$smoke_file_name" \
    "$size_bytes" <<'PY'
import json
import sys

existing_path, output_path, now_utc, file_id, file_name, size_bytes = sys.argv[1:7]

try:
    data = json.load(open(existing_path, encoding="utf-8"))
except json.JSONDecodeError:
    data = {"items": []}

items = [
    item for item in data.get("items") or []
    if item.get("fileId") != file_id
]

items.append(
    {
        "fileId": file_id,
        "fileName": file_name,
        "kind": "Text",
        "badgeText": "TXT",
        "remoteUpdatedAtUtc": "2020-01-01T00:00:00Z",
        "sizeBytes": int(size_bytes),
        "contentType": "text/plain",
        "lastUsedAtUtc": now_utc,
        "lastAction": 1,
    }
)

with open(output_path, "w", encoding="utf-8") as handle:
    json.dump(
        {
            "schemaVersion": 1,
            "savedAtUtc": now_utc,
            "items": items,
        },
        handle,
        indent=2,
    )
    handle.write("\n")
PY
}

seed_recent_data() {
  local seed_dir="$1"
  local remote_seed_dir="/data/local/tmp/cotton-recent-files-smoke-$run_id"

  adb_device shell rm -rf "$remote_seed_dir"
  adb_device shell mkdir -p "$remote_seed_dir"
  adb_device push "$seed_dir/$smoke_file_name" "$remote_seed_dir/$smoke_file_name" \
    > "$evidence_dir/10-push-smoke-file.txt"
  adb_device push "$seed_dir/recent-files.json" "$remote_seed_dir/recent-files.json" \
    > "$evidence_dir/11-push-recent-metadata.txt"

  adb_device shell run-as "$package_id" rm -rf "$download_directory"
  adb_device shell run-as "$package_id" mkdir -p "$recent_metadata_directory" "$download_directory"
  adb_device shell run-as "$package_id" cp \
    "$remote_seed_dir/$smoke_file_name" \
    "$download_directory/$smoke_file_name"
  adb_device shell run-as "$package_id" cp \
    "$remote_seed_dir/recent-files.json" \
    "$recent_metadata_path"
  adb_device shell rm -rf "$remote_seed_dir"
  seeded_recent_data=1
}

restore_recent_data() {
  if [[ "${seeded_recent_data:-0}" -ne 1 || "$leave_seed" -eq 1 ]]; then
    return
  fi

  if [[ "$recent_backup_exists" -eq 1 && -f "$recent_backup_path" ]]; then
    local remote_restore_dir="/data/local/tmp/cotton-recent-files-restore-$run_id"
    adb_device shell rm -rf "$remote_restore_dir" >/dev/null 2>&1 || true
    adb_device shell mkdir -p "$remote_restore_dir" >/dev/null 2>&1 || true
    adb_device push "$recent_backup_path" "$remote_restore_dir/recent-files.json" \
      > "$evidence_dir/98-restore-push.txt" 2>&1 || true
    adb_device shell run-as "$package_id" mkdir -p "$recent_metadata_directory" >/dev/null 2>&1 || true
    adb_device shell run-as "$package_id" cp \
      "$remote_restore_dir/recent-files.json" \
      "$recent_metadata_path" >/dev/null 2>&1 || true
    adb_device shell rm -rf "$remote_restore_dir" >/dev/null 2>&1 || true
  else
    adb_device shell run-as "$package_id" rm -f "$recent_metadata_path" >/dev/null 2>&1 || true
  fi

  adb_device shell run-as "$package_id" rm -rf "$download_directory" >/dev/null 2>&1 || true
}

open_recent_files() {
  tap_node_from_xml "$files_root_xml" "Account" exact
  sleep 2
  capture_screen "30-account-actions"
  require_xml_text "$evidence_dir/30-account-actions.xml" "Recent files" \
    "Account action sheet did not expose Recent files."
  tap_node_from_xml "$evidence_dir/30-account-actions.xml" "Recent files" exact
  sleep 2
  wait_for_text "40-recent-files" "Recent files"
  recent_files_xml="$waited_xml"
}

verify_recent_row() {
  require_xml_text "$recent_files_xml" "$smoke_file_name" \
    "Seeded Recent files row is not visible."
  require_xml_text "$recent_files_xml" "Downloaded" \
    "Seeded Recent files row did not show the seeded action."
}

verify_recent_open() {
  tap_node_from_xml "$recent_files_xml" "$smoke_file_name" exact
  sleep 2
  wait_for_text "50-text-viewer" "Recent files smoke $run_id"
  require_xml_text "$waited_xml" "$smoke_file_name" \
    "Text viewer did not show the seeded file name."
}

verify_clear_action() {
  adb_device shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
  sleep 2
  wait_for_text "60-recent-after-open" "$smoke_file_name"
  recent_files_xml="$waited_xml"
  require_xml_text "$recent_files_xml" "Opened" \
    "Recent files did not update the seeded row after opening."

  tap_node_from_xml "$recent_files_xml" "Clear" exact
  sleep 1
  capture_screen "70-clear-dialog"
  require_xml_text "$evidence_dir/70-clear-dialog.xml" "Clear recent files?" \
    "Clear confirmation dialog did not appear."
  tap_node_from_xml "$evidence_dir/70-clear-dialog.xml" "Clear" exact
  sleep 1
  wait_for_text "80-clear-result" "Recent files cleared."
  require_xml_text "$waited_xml" "No recent files yet" \
    "Recent files page did not show the empty state after clear."
}

capture_final_state() {
  capture_screen "90-final"
  capture_text "91-logcat.txt" adb_device logcat -d -t 400
  if grep -Ei 'FATAL EXCEPTION|AndroidRuntime.*FATAL|SIGSEGV|libc.*Fatal signal|mono-rt.*SIG' \
      "$evidence_dir/91-logcat.txt" \
      > "$evidence_dir/92-fatal-logcat.txt"; then
    printf 'Fatal runtime marker found in logcat.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir/92-fatal-logcat.txt" >&2
    exit 66
  fi
}

instance_key="$(create_instance_key)"
smoke_file_id="$(create_smoke_file_id)"
smoke_file_name="cotton-recent-files-smoke-$run_id.txt"
recent_metadata_directory="files/CottonRecentFiles/$instance_key"
recent_metadata_path="$recent_metadata_directory/recent-files.json"
download_directory="files/CottonDownloads/$instance_key/$smoke_file_id"
recent_backup_path="$evidence_dir/09-existing-recent-files.json"
recent_backup_exists=0
seeded_recent_data=0

trap restore_recent_data EXIT

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

seed_dir="$evidence_dir/seed"
mkdir -p "$seed_dir"
prepare_seed_files "$seed_dir"
seed_recent_data "$seed_dir"

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c >/dev/null 2>&1 || true
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/04-launch.txt"
  sleep 4
fi

wait_for_files_root
open_recent_files
verify_recent_row
verify_recent_open

if [[ "$verify_clear" -eq 1 ]]; then
  verify_clear_action
fi

capture_final_state
printf 'Recent files smoke passed. Evidence: %s\n' "$evidence_dir"
