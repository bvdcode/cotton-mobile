#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev/"
destination_name="Mobile smoke folder"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
wait_seconds=8
run_id=""
files_root_xml=""

queued_id="11111111-1111-1111-1111-111111111111"
failed_id="22222222-2222-2222-2222-222222222222"
completed_id="33333333-3333-3333-3333-333333333333"
queued_display_name=""
failed_display_name=""
completed_display_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a transfer-queue restart smoke:
  1. Seeds queued, failed, and completed transfers with staged files.
  2. Force-stops and launches the app.
  3. Captures transfer queue/staging state before and after startup restore.
  4. Opens Transfers UI and validates restored queue state.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --destination NAME        Destination folder used by seeded uploads.
  --run-id ID               Stable run id for seeded transfer display names.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before seeding.
  --wait-seconds N          Seconds to wait after launch. Defaults to 8.
  --no-launch               Seed and validate pre-launch state only.
  --help, -h                Show this help.

The app must already have a signed-in session and a cached root listing for the
selected instance.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--instance:instance_uri"
  "--destination:destination_name"
  "--run-id:run_id"
  "--evidence-dir:evidence_dir"
  "--wait-seconds:wait_seconds"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

if [[ -z "${destination_name//[[:space:]]/}" ]]; then
  printf 'Destination name must not be blank.\n' >&2
  exit 64
fi

if ! [[ "$wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Wait seconds must be a non-negative integer.\n' >&2
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
if [[ -z "$run_id" ]]; then
  run_id="$timestamp"
fi

if [[ -z "${run_id//[[:space:]]/}" || "$run_id" == *"/"* ]]; then
  printf 'Run id must not be blank and must not contain a slash.\n' >&2
  exit 64
fi

queued_display_name="queued-restart-smoke-$run_id.jpg"
failed_display_name="failed-restart-smoke-$run_id.jpg"
completed_display_name="completed-restart-smoke-$run_id.jpg"

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-transfer-queue-restart"
fi

mkdir -p "$evidence_dir"

capture_transfer_state() {
  local prefix="$1"
  local instance_key="$2"
  local transfer_root="files/CottonTransfers/$instance_key"

  cotton_adb shell run-as "$package_id" cat "$transfer_root/queue.json" \
    > "$evidence_dir/$prefix-queue.json"
  cotton_adb shell run-as "$package_id" find "$transfer_root/Staged" \
    -maxdepth 2 -type f | sort > "$evidence_dir/$prefix-staged-files.txt" || true
}

cotton_wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5; do
    prefix="30-files-root-$attempt"
    cotton_capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if cotton_xml_has_text "$xml_file" "Open transfers"; then
      files_root_xml="$xml_file"
      return
    fi

    if cotton_xml_has_text "$xml_file" "Navigate up"; then
      cotton_tap_node_from_xml "$xml_file" "Navigate up"
      sleep 2
      continue
    fi

    cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 1
    cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/30-relaunch-$attempt.txt" || true
    sleep 2
  done

  printf 'Files root with Transfers navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

validate_transfer_state() {
  local before_queue="$evidence_dir/20-before-launch-queue.json"
  local before_staged="$evidence_dir/20-before-launch-staged-files.txt"
  local after_queue="$evidence_dir/50-after-launch-queue.json"
  local after_staged="$evidence_dir/50-after-launch-staged-files.txt"

  python3 - \
    "$before_queue" \
    "$before_staged" \
    "$after_queue" \
    "$after_staged" \
    "$queued_id" \
    "$failed_id" \
    "$completed_id" \
    "$queued_display_name" \
    "$failed_display_name" \
    "$completed_display_name" \
    > "$evidence_dir/60-validation-summary.json" <<'PY'
import json
import sys

(
    before_queue,
    before_staged,
    after_queue,
    after_staged,
    queued_id,
    failed_id,
    completed_id,
    queued_display_name,
    failed_display_name,
    completed_display_name,
) = sys.argv[1:11]

def load_queue(path: str) -> dict[str, dict]:
    data = json.load(open(path, encoding="utf-8"))
    return {item["id"]: item for item in data.get("items", [])}

def load_staged(path: str) -> str:
    return open(path, encoding="utf-8").read()

before = load_queue(before_queue)
after = load_queue(after_queue)
before_staged_text = load_staged(before_staged)
after_staged_text = load_staged(after_staged)

for transfer_id in (queued_id, failed_id, completed_id):
    if transfer_id not in before:
        raise SystemExit(f"Seeded transfer missing before launch: {transfer_id}")
    if transfer_id not in after:
        raise SystemExit(f"Transfer missing after restore: {transfer_id}")

expected_names = {
    queued_id: queued_display_name,
    failed_id: failed_display_name,
    completed_id: completed_display_name,
}
for transfer_id, expected_name in expected_names.items():
    if before[transfer_id].get("displayName") != expected_name:
        raise SystemExit(f"Unexpected seeded display name for {transfer_id}")

expected_before = {
    queued_id: 0,
    failed_id: 4,
    completed_id: 3,
}
for transfer_id, expected_status in expected_before.items():
    actual_status = before[transfer_id].get("status")
    if actual_status != expected_status:
        raise SystemExit(f"Unexpected before status for {transfer_id}: {actual_status}")

queued_n = queued_id.replace("-", "")
failed_n = failed_id.replace("-", "")
completed_n = completed_id.replace("-", "")
for transfer_n in (queued_n, failed_n, completed_n):
    if transfer_n not in before_staged_text:
        raise SystemExit(f"Seeded staged file missing before launch: {transfer_n}")

if after[failed_id].get("status") != 4:
    raise SystemExit(f"Failed transfer did not remain failed: {after[failed_id].get('status')}")
if after[completed_id].get("status") != 3:
    raise SystemExit(f"Completed transfer did not remain completed: {after[completed_id].get('status')}")
if completed_n in after_staged_text:
    raise SystemExit("Completed transfer staged file was not cleaned after restore.")
if failed_n not in after_staged_text:
    raise SystemExit("Failed transfer staged file should remain available for retry.")

queued_status = after[queued_id].get("status")
queued_failure = after[queued_id].get("failureMessage")
if queued_status not in (0, 1, 3, 4):
    raise SystemExit(f"Queued transfer has unexpected status after launch: {queued_status}")
if queued_status in (0, 1) and queued_n not in after_staged_text:
    raise SystemExit("Queued/running transfer lost its staged file after restore.")
if queued_status == 4 and queued_failure in (
    "Upload destination is missing.",
    "Upload file is no longer available on this device.",
):
    raise SystemExit(f"Queued transfer failed for a restore precondition: {queued_failure}")
if queued_status == 4 and queued_failure and "Object already exists" in queued_failure:
    raise SystemExit(f"Queued transfer hit a stale server-name conflict: {queued_failure}")

summary = {
    "queuedStatus": queued_status,
    "queuedFailure": queued_failure,
    "failedStatus": after[failed_id].get("status"),
    "completedStatus": after[completed_id].get("status"),
    "completedStagedFileCleaned": completed_n not in after_staged_text,
    "failedStagedFileKept": failed_n in after_staged_text,
}
print(json.dumps(summary, indent=2))
PY
}

write_metadata() {
  {
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'destination=%s\n' "$destination_name"
    printf 'run_id=%s\n' "$run_id"
    printf 'queued_name=%s\n' "$queued_display_name"
    printf 'failed_name=%s\n' "$failed_display_name"
    printf 'completed_name=%s\n' "$completed_display_name"
    printf 'queued=%s\n' "$queued_id"
    printf 'failed=%s\n' "$failed_id"
    printf 'completed=%s\n' "$completed_id"
  } > "$evidence_dir/00-metadata.txt"
}

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

"$SCRIPT_DIR/seed-transfer-restart-smoke.sh" \
  --instance "$instance_uri" \
  --destination "$destination_name" \
  --run-id "$run_id" \
  --no-launch \
  > "$evidence_dir/10-seed.txt"

capture_transfer_state "20-before-launch" "$instance_key"
cotton_capture_text_best_effort "21-package.txt" cotton_adb shell dumpsys package "$package_id"

if [[ "$launch_app" -eq 0 ]]; then
  validate_transfer_state
  printf 'Transfer queue restart seed evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

cotton_adb logcat -c >/dev/null 2>&1 || true
cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/29-launch.txt"
sleep "$wait_seconds"

capture_transfer_state "50-after-launch" "$instance_key"
cotton_capture_text_best_effort "51-jobscheduler.txt" cotton_adb shell dumpsys jobscheduler "$package_id"
validate_transfer_state

cotton_wait_for_files_root
cotton_tap_node_from_xml "$files_root_xml" "Transfers"
sleep 3
cotton_capture_screen "70-transfers"
cotton_require_xml_text "$evidence_dir/70-transfers.xml" "Transfers" "Transfers page did not open."
cotton_require_xml_text "$evidence_dir/70-transfers.xml" "$failed_display_name" "Failed transfer is not visible."
cotton_require_xml_text "$evidence_dir/70-transfers.xml" "$completed_display_name" "Completed transfer is not visible."

cotton_capture_text_best_effort "90-logcat-raw.txt" cotton_adb logcat -d -v time
grep -E 'Cotton|WorkManager|SystemJobService|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true

{
  printf 'Transfer queue restart smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
