#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

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
    --destination)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --destination.\n' >&2
        exit 64
      fi
      destination_name="$2"
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
    --wait-seconds)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --wait-seconds.\n' >&2
        exit 64
      fi
      wait_seconds="$2"
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

  capture_text "$prefix-window.txt" adb_device shell dumpsys window
  if ! adb_device exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  if adb_device shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    if ! adb_device pull /sdcard/cotton-window.xml "$evidence_dir/$prefix.xml" > "$evidence_dir/$prefix-pull-xml.log" 2>&1; then
      rm -f "$evidence_dir/$prefix.xml"
    fi
    adb_device shell rm -f /sdcard/cotton-window.xml >/dev/null 2>&1 || true
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
  local point_file="$evidence_dir/tap-point.txt"

  python3 - "$xml_file" "$needle" > "$point_file" <<'PY'
import re
import sys
from xml.etree import ElementTree

xml_file, needle = sys.argv[1:3]
root = ElementTree.parse(xml_file).getroot()

def center(bounds: str) -> tuple[int, int]:
    match = re.fullmatch(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", bounds)
    if match is None:
        raise ValueError(bounds)
    left, top, right, bottom = [int(value) for value in match.groups()]
    return ((left + right) // 2, (top + bottom) // 2)

for node in root.iter("node"):
    if needle in (node.attrib.get("text", ""), node.attrib.get("content-desc", "")):
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

capture_transfer_state() {
  local prefix="$1"
  local instance_key="$2"
  local transfer_root="files/CottonTransfers/$instance_key"

  adb_device shell run-as "$package_id" cat "$transfer_root/queue.json" \
    > "$evidence_dir/$prefix-queue.json"
  adb_device shell run-as "$package_id" find "$transfer_root/Staged" \
    -maxdepth 2 -type f | sort > "$evidence_dir/$prefix-staged-files.txt" || true
}

wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5; do
    prefix="30-files-root-$attempt"
    capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if xml_has_text "$xml_file" "Open transfers"; then
      files_root_xml="$xml_file"
      return
    fi

    if xml_has_text "$xml_file" "Navigate up"; then
      tap_node_from_xml "$xml_file" "Navigate up"
      sleep 2
      continue
    fi

    adb_device shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 1
    adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/30-relaunch-$attempt.txt" || true
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

instance_key="$(create_instance_key)"
write_metadata
capture_text "01-adb-devices.txt" adb devices

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  adb_device install --no-incremental -r "$COTTON_ANDROID_APK" > "$evidence_dir/02-install.txt"
fi

"$SCRIPT_DIR/seed-transfer-restart-smoke.sh" \
  --instance "$instance_uri" \
  --destination "$destination_name" \
  --run-id "$run_id" \
  --no-launch \
  > "$evidence_dir/10-seed.txt"

capture_transfer_state "20-before-launch" "$instance_key"
capture_text "21-package.txt" adb_device shell dumpsys package "$package_id"

if [[ "$launch_app" -eq 0 ]]; then
  validate_transfer_state
  printf 'Transfer queue restart seed evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

adb_device logcat -c >/dev/null 2>&1 || true
adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/29-launch.txt"
sleep "$wait_seconds"

capture_transfer_state "50-after-launch" "$instance_key"
capture_text "51-jobscheduler.txt" adb_device shell dumpsys jobscheduler "$package_id"
validate_transfer_state

wait_for_files_root
tap_node_from_xml "$files_root_xml" "Transfers"
sleep 3
capture_screen "70-transfers"
require_xml_text "$evidence_dir/70-transfers.xml" "Transfers" "Transfers page did not open."
require_xml_text "$evidence_dir/70-transfers.xml" "$failed_display_name" "Failed transfer is not visible."
require_xml_text "$evidence_dir/70-transfers.xml" "$completed_display_name" "Completed transfer is not visible."

capture_text "90-logcat-raw.txt" adb_device logcat -d -v time
grep -E 'Cotton|WorkManager|SystemJobService|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true

{
  printf 'Transfer queue restart smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
