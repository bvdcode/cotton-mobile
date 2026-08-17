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
account_scope_key="user:sync-settings-smoke"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
leave_seed=0
run_id=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an Android Sync settings smoke:
  1. Backs up current app-private sync root metadata for the selected instance.
  2. Seeds one reconnect-required upload root and one paused upload root.
  3. Opens the current Sync dashboard and verifies its toolbar and root cards.
  4. Taps Refresh and verifies the seeded roots reload from app-private metadata.
  5. Restores the previous sync root metadata unless --leave-seed is used.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --account-scope KEY       Account scope key to write into seeded roots.
  --run-id ID               Stable run id for seeded ids.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --no-launch               Do not launch automatically.
  --leave-seed              Leave seeded sync metadata in app data.
  --help, -h                Show this help.

The app must already have a signed-in session for the selected instance.
This seeded smoke requires a debuggable package because it uses adb run-as.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--instance:instance_uri"
  "--account-scope:account_scope_key"
  "--run-id:run_id"
  "--evidence-dir:evidence_dir"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--no-launch:launch_app:0"
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

if [[ -z "${account_scope_key//[[:space:]]/}" ]]; then
  printf 'Account scope key is required.\n' >&2
  exit 64
fi

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-sync-settings"
fi

cotton_require_command adb \
  "adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT."
cotton_require_command python3

mkdir -p "$evidence_dir"

# shellcheck source=smoke-sync-settings-data.sh
source "$SCRIPT_DIR/smoke-sync-settings-data.sh"
# shellcheck source=smoke-sync-settings-flow.sh
source "$SCRIPT_DIR/smoke-sync-settings-flow.sh"

seed_dir="$evidence_dir/seed"
mkdir -p "$seed_dir"
create_sync_seed "$seed_dir"
load_sync_seed "$seed_dir/seed-data.json"

sync_metadata_directory="files/CottonSyncRoots/$instance_key"
sync_roots_path="$sync_metadata_directory/sync-roots.json"
paused_roots_path="$sync_metadata_directory/paused-sync-roots.json"
automatic_status_path="$sync_metadata_directory/automatic-sync-status.json"
sync_roots_backup_path="$evidence_dir/09-existing-sync-roots.json"
paused_roots_backup_path="$evidence_dir/09-existing-paused-sync-roots.json"
automatic_status_backup_path="$evidence_dir/09-existing-automatic-sync-status.json"
sync_roots_backup_exists=0
paused_roots_backup_exists=0
automatic_status_backup_exists=0
seeded_sync_data=0

trap restore_sync_data EXIT

write_metadata
cotton_capture_text_best_effort "01-adb-devices.txt" adb devices
cotton_require_device

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/02-install.txt"
fi

cotton_capture_text_best_effort "03-package.txt" cotton_adb shell dumpsys package "$package_id"

backup_sync_data "$seed_dir"
seed_sync_data "$seed_dir"

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c >/dev/null 2>&1 || true
  cotton_adb shell am force-stop "$package_id" >/dev/null 2>&1 || true
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/04-launch.txt"
  sleep 4
fi

wait_for_sync_dashboard
verify_sync_dashboard
verify_refresh_action
capture_final_state

printf 'Sync settings smoke passed. Evidence: %s\n' "$evidence_dir"
