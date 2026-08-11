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

protected_file_id="0f0f0f0f-0000-4000-8000-000000000001"
evictable_file_id="0f0f0f0f-0000-4000-8000-000000000002"
failed_transfer_id="0f0f0f0f-0000-4000-8000-000000000003"
completed_transfer_id="0f0f0f0f-0000-4000-8000-000000000004"
orphan_transfer_id="0f0f0f0f-0000-4000-8000-000000000005"

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an Android storage-cleanup smoke:
  1. Launches a signed-in app session.
  2. Seeds app-private thumbnails, folder listings, downloads, offline pins,
     and transfer staging for the selected instance scope.
  3. Opens Storage through the account action sheet.
  4. Runs Clear temp uploads and Free space.
  5. Verifies destructive cleanup keeps protected offline files and failed
     upload staging while removing evictable files.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --run-id ID               Stable run id for seeded file names.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --no-launch               Do not launch automatically before seeding.
  --help, -h                Show this help.

The app must already have a signed-in session for the selected instance.
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

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
if [[ -z "$run_id" ]]; then
  run_id="$timestamp"
fi

if [[ -z "${run_id//[[:space:]]/}" || "$run_id" == *"/"* ]]; then
  printf 'Run id must not be blank and must not contain a slash.\n' >&2
  exit 64
fi

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-storage-cleanup"
fi

mkdir -p "$evidence_dir"

protected_file_id_n="${protected_file_id//-/}"
evictable_file_id_n="${evictable_file_id//-/}"
failed_transfer_id_n="${failed_transfer_id//-/}"
completed_transfer_id_n="${completed_transfer_id//-/}"
orphan_transfer_id_n="${orphan_transfer_id//-/}"

thumbnail_name="storage-cleanup-smoke-$run_id.webp"
folder_listing_name="storage-cleanup-smoke-$run_id.json"
protected_file_name="protected-storage-cleanup-smoke-$run_id.txt"
evictable_file_name="evictable-storage-cleanup-smoke-$run_id.txt"
failed_upload_name="failed-storage-cleanup-smoke-$run_id.bin"
completed_upload_name="completed-storage-cleanup-smoke-$run_id.bin"
orphan_upload_name="orphan-storage-cleanup-smoke-$run_id.bin"

# shellcheck source=smoke-storage-cleanup-seed.sh
source "$SCRIPT_DIR/smoke-storage-cleanup-seed.sh"
# shellcheck source=smoke-storage-cleanup-flow.sh
source "$SCRIPT_DIR/smoke-storage-cleanup-flow.sh"

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

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c >/dev/null 2>&1 || true
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/04-launch.txt"
  sleep 4
fi

cotton_wait_for_files_root

local_seed_dir="$(mktemp -d "${TMPDIR:-/tmp}/cotton-storage-cleanup.XXXXXX")"
trap 'rm -rf "$local_seed_dir"' EXIT
prepare_seed_files "$local_seed_dir" "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
seed_storage_data "$local_seed_dir"
capture_storage_state "25-after-seed"
validate_seeded_state

open_storage_page
capture_storage_state "45-storage-opened"

run_clear_temp_uploads
capture_storage_state "65-after-clear-temp"
validate_temp_cleanup_state

run_free_space
capture_storage_state "85-after-free-space"
validate_free_space_state

cotton_capture_text_best_effort "90-logcat-raw.txt" cotton_adb logcat -d -v time
grep -E 'Cotton|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true
if grep -E 'FATAL EXCEPTION|mono-rt' "$evidence_dir/91-logcat-cotton.txt" > "$evidence_dir/92-fatal-markers.txt"; then
  printf 'Fatal log markers were found during storage cleanup smoke.\n' >&2
  printf 'Evidence: %s/92-fatal-markers.txt\n' "$evidence_dir" >&2
  exit 66
fi

{
  printf 'Storage cleanup smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Protected file: %s\n' "$protected_file_id"
  printf 'Evictable file: %s\n' "$evictable_file_id"
  printf 'Failed transfer: %s\n' "$failed_transfer_id"
  printf 'Completed transfer: %s\n' "$completed_transfer_id"
  printf 'Orphan transfer: %s\n' "$orphan_transfer_id"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
