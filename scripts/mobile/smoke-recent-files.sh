#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

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

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--instance:instance_uri"
  "--run-id:run_id"
  "--evidence-dir:evidence_dir"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--no-launch:launch_app:0"
  "--verify-clear:verify_clear:1"
  "--leave-seed:leave_seed:1"
)
cotton_parse_arguments "$@"

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








create_smoke_file_id() {
  python3 - "$run_id" <<'PY'
import sys
import uuid

print(uuid.uuid5(uuid.NAMESPACE_URL, f"cotton-recent-files-smoke:{sys.argv[1]}"))
PY
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

  if cotton_adb shell run-as "$package_id" cat "$recent_metadata_path" \
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

  cotton_adb shell rm -rf "$remote_seed_dir"
  cotton_adb shell mkdir -p "$remote_seed_dir"
  cotton_adb push "$seed_dir/$smoke_file_name" "$remote_seed_dir/$smoke_file_name" \
    > "$evidence_dir/10-push-smoke-file.txt"
  cotton_adb push "$seed_dir/recent-files.json" "$remote_seed_dir/recent-files.json" \
    > "$evidence_dir/11-push-recent-metadata.txt"

  cotton_adb shell run-as "$package_id" rm -rf "$download_directory"
  cotton_adb shell run-as "$package_id" mkdir -p "$recent_metadata_directory" "$download_directory"
  cotton_adb shell run-as "$package_id" cp \
    "$remote_seed_dir/$smoke_file_name" \
    "$download_directory/$smoke_file_name"
  cotton_adb shell run-as "$package_id" cp \
    "$remote_seed_dir/recent-files.json" \
    "$recent_metadata_path"
  cotton_adb shell rm -rf "$remote_seed_dir"
  seeded_recent_data=1
}

restore_recent_data() {
  if [[ "${seeded_recent_data:-0}" -ne 1 || "$leave_seed" -eq 1 ]]; then
    return
  fi

  if [[ "$recent_backup_exists" -eq 1 && -f "$recent_backup_path" ]]; then
    local remote_restore_dir="/data/local/tmp/cotton-recent-files-restore-$run_id"
    cotton_adb shell rm -rf "$remote_restore_dir" >/dev/null 2>&1 || true
    cotton_adb shell mkdir -p "$remote_restore_dir" >/dev/null 2>&1 || true
    cotton_adb push "$recent_backup_path" "$remote_restore_dir/recent-files.json" \
      > "$evidence_dir/98-restore-push.txt" 2>&1 || true
    cotton_adb shell run-as "$package_id" mkdir -p "$recent_metadata_directory" >/dev/null 2>&1 || true
    cotton_adb shell run-as "$package_id" cp \
      "$remote_restore_dir/recent-files.json" \
      "$recent_metadata_path" >/dev/null 2>&1 || true
    cotton_adb shell rm -rf "$remote_restore_dir" >/dev/null 2>&1 || true
  else
    cotton_adb shell run-as "$package_id" rm -f "$recent_metadata_path" >/dev/null 2>&1 || true
  fi

  cotton_adb shell run-as "$package_id" rm -rf "$download_directory" >/dev/null 2>&1 || true
}

open_recent_files() {
  cotton_tap_node_from_xml "$files_root_xml" "Account" exact
  sleep 2
  cotton_capture_screen "30-account-actions"
  cotton_require_xml_text "$evidence_dir/30-account-actions.xml" "Recent files" \
    "Account action sheet did not expose Recent files."
  cotton_tap_node_from_xml "$evidence_dir/30-account-actions.xml" "Recent files" exact
  sleep 2
  cotton_wait_for_text "40-recent-files" "Recent files"
  recent_files_xml="$waited_xml"
}

verify_recent_row() {
  cotton_require_xml_text "$recent_files_xml" "$smoke_file_name" \
    "Seeded Recent files row is not visible."
  cotton_require_xml_text "$recent_files_xml" "Downloaded" \
    "Seeded Recent files row did not show the seeded action."
}

verify_recent_open() {
  cotton_tap_node_from_xml "$recent_files_xml" "$smoke_file_name" exact
  sleep 2
  cotton_wait_for_text "50-text-viewer" "Recent files smoke $run_id"
  cotton_require_xml_text "$waited_xml" "$smoke_file_name" \
    "Text viewer did not show the seeded file name."
}

verify_clear_action() {
  cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
  sleep 2
  cotton_wait_for_text "60-recent-after-open" "$smoke_file_name"
  recent_files_xml="$waited_xml"
  cotton_require_xml_text "$recent_files_xml" "Opened" \
    "Recent files did not update the seeded row after opening."

  cotton_tap_node_from_xml "$recent_files_xml" "Clear" exact
  sleep 1
  cotton_capture_screen "70-clear-dialog"
  cotton_require_xml_text "$evidence_dir/70-clear-dialog.xml" "Clear recent files?" \
    "Clear confirmation dialog did not appear."
  cotton_tap_node_from_xml "$evidence_dir/70-clear-dialog.xml" "Clear" exact
  sleep 1
  cotton_wait_for_text "80-clear-result" "Recent files cleared."
  cotton_require_xml_text "$waited_xml" "No recent files yet" \
    "Recent files page did not show the empty state after clear."
}

capture_final_state() {
  cotton_capture_screen "90-final"
  cotton_capture_text_best_effort "91-logcat.txt" cotton_adb logcat -d -t 400
  if grep -Ei 'FATAL EXCEPTION|AndroidRuntime.*FATAL|SIGSEGV|libc.*Fatal signal|mono-rt.*SIG' \
      "$evidence_dir/91-logcat.txt" \
      > "$evidence_dir/92-fatal-logcat.txt"; then
    printf 'Fatal runtime marker found in logcat.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir/92-fatal-logcat.txt" >&2
    exit 66
  fi
}

instance_key="$(cotton_create_instance_key)"
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
cotton_capture_text_best_effort "01-adb-devices.txt" adb devices

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/02-install.txt"
fi

cotton_capture_text_best_effort "03-package.txt" cotton_adb shell dumpsys package "$package_id"

seed_dir="$evidence_dir/seed"
mkdir -p "$seed_dir"
prepare_seed_files "$seed_dir"
seed_recent_data "$seed_dir"

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c >/dev/null 2>&1 || true
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/04-launch.txt"
  sleep 4
fi

cotton_wait_for_files_root
open_recent_files
verify_recent_row
verify_recent_open

if [[ "$verify_clear" -eq 1 ]]; then
  verify_clear_action
fi

capture_final_state
printf 'Recent files smoke passed. Evidence: %s\n' "$evidence_dir"
