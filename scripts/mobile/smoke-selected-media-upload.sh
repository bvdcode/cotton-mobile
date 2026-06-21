#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev/"
kind="photo"
count=2
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
preflight_only=0
seed_only=0
wait_seconds=6
expected_version_code=""
expected_version_name=""
files_root_xml=""

declare -a media_names=()

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a selected-photo/video upload smoke:
  1. Seeds real Android shared-media items.
  2. Opens Cotton Files and the selected-media picker flow.
  3. Captures picker, Files, Transfers, MediaStore, queue, staging, and logcat evidence.
  4. Validates queued Transfers records use the SelectedMedia source kind.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --kind photo|video        Selected media kind to test. Defaults to photo.
  --count N                 Number of seeded items to select. Defaults to 2.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --expected-version-code N Require the installed package versionCode in dumpsys.
  --expected-version-name N Require the installed package versionName in dumpsys.
  --wait-seconds N          Seconds to wait after returning from the picker. Defaults to 6.
  --preflight-only          Capture package and seeded-media state, then exit.
  --seed-only               Seed shared-media items and exit after MediaStore validation.
  --no-launch               Do not launch the app before capture.
  --help, -h                Show this help.

The app must already have a signed-in session and a cached Files root for the
selected instance. Full mode is intentionally interactive because Android's
system photo/video picker requires user selection.
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
    --kind)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --kind.\n' >&2
        exit 64
      fi
      kind="$2"
      shift 2
      ;;
    --count)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --count.\n' >&2
        exit 64
      fi
      count="$2"
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
    --wait-seconds)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --wait-seconds.\n' >&2
        exit 64
      fi
      wait_seconds="$2"
      shift 2
      ;;
    --preflight-only)
      preflight_only=1
      shift
      ;;
    --seed-only)
      seed_only=1
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

if [[ "$kind" != "photo" && "$kind" != "video" ]]; then
  printf 'Kind must be either photo or video.\n' >&2
  exit 64
fi

if ! [[ "$count" =~ ^[0-9]+$ ]] || [[ "$count" -lt 1 ]]; then
  printf 'Count must be a positive integer.\n' >&2
  exit 64
fi

if [[ "$kind" == "photo" && "$count" -gt 20 ]]; then
  printf 'Photo count must not exceed 20.\n' >&2
  exit 64
fi

if [[ "$kind" == "video" && "$count" -gt 10 ]]; then
  printf 'Video count must not exceed 10.\n' >&2
  exit 64
fi

if ! [[ "$wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Wait seconds must be a non-negative integer.\n' >&2
  exit 64
fi

if [[ "$preflight_only" -eq 0 && "$seed_only" -eq 0 && ! -t 0 ]]; then
  printf 'Full selected-media smoke requires an interactive terminal.\n' >&2
  printf 'Use --preflight-only or --seed-only for non-interactive evidence.\n' >&2
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
run_id="cotton-selected-media-$kind-$timestamp"

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-selected-media-upload-$kind"
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
  local mode="${3:-exact}"
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

matches = []
for node in root.iter("node"):
    values = [
        node.attrib.get("text", ""),
        node.attrib.get("content-desc", ""),
    ]
    if any((value == needle if mode == "exact" else needle in value) for value in values):
        try:
            matches.append(center(node.attrib["bounds"]))
        except (KeyError, ValueError):
            pass

if not matches:
    raise SystemExit(f"Could not find UI node: {needle}")

print(matches[0][0], matches[0][1])
PY

  read -r tap_x tap_y < "$point_file"
  adb_device shell input tap "$tap_x" "$tap_y"
}

tap_text() {
  local xml_file="$1"
  local needle="$2"
  tap_node_from_xml "$xml_file" "$needle" exact
}

wait_for_operator() {
  local prompt="$1"

  printf '\n%s\n' "$prompt"
  printf 'Press Enter to continue after the device shows the expected state... '
  read -r _
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

generate_photo_file() {
  local output_path="$1"

  python3 - "$output_path" <<'PY'
import base64
import sys
from pathlib import Path

png = (
    "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAIAAAAlC+aJAAAAW0lEQVR4nO3PQQ0A"
    "IBDAMMC/5+ONAvZoFSzZnZnZ3S8D+A24DWgD2oA2oA1oA9qANqANaAPagDagDWgD"
    "2oA2oA1oA9qANqANaAPagDagDWgD2oA2oA1oA9qANqANaAPagHb2DgHrYcRrGgAA"
    "AABJRU5ErkJggg=="
)
Path(sys.argv[1]).write_bytes(base64.b64decode(png))
PY
}

seed_photo() {
  local name="$1"
  local local_file="$evidence_dir/$name"
  local remote_dir="/sdcard/Pictures/CottonSelectedMediaSmoke"
  local remote_file="$remote_dir/$name"

  generate_photo_file "$local_file"
  adb_device shell mkdir -p "$remote_dir"
  adb_device push "$local_file" "$remote_file" >> "$evidence_dir/10-push-media.txt"
  adb_device shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file://$remote_file" >> "$evidence_dir/11-media-scan-broadcast.txt" || true
}

seed_video() {
  local name="$1"
  local remote_dir="/sdcard/Movies/CottonSelectedMediaSmoke"
  local remote_file="$remote_dir/$name"

  adb_device shell mkdir -p "$remote_dir"
  adb_device shell rm -f "$remote_file" >/dev/null 2>&1 || true
  adb_device shell screenrecord --time-limit 1 "$remote_file" >> "$evidence_dir/10-screenrecord-video.txt" 2>&1
  adb_device shell test -s "$remote_file"
  adb_device shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file://$remote_file" >> "$evidence_dir/11-media-scan-broadcast.txt" || true
}

query_seeded_media_store() {
  local media_uri
  local projection
  local output_file

  if [[ "$kind" == "photo" ]]; then
    media_uri="content://media/external/images/media"
    projection="_id:_display_name:mime_type:_size:date_modified"
    output_file="$evidence_dir/12-mediastore-images.txt"
  else
    media_uri="content://media/external/video/media"
    projection="_id:_display_name:mime_type:_size:date_modified:duration"
    output_file="$evidence_dir/12-mediastore-videos.txt"
  fi

  sleep 2
  adb_device shell content query \
    --uri "$media_uri" \
    --projection "$projection" \
    > "$output_file"

  local name
  for name in "${media_names[@]}"; do
    if ! grep -F "$name" "$output_file" >> "$evidence_dir/13-mediastore-seeded-items.txt"; then
      printf 'Seeded media is not visible in MediaStore: %s\n' "$name" >&2
      printf 'Evidence: %s\n' "$output_file" >&2
      exit 66
    fi
  done

  local expected_transfer_items_file="$evidence_dir/14-expected-transfer-items.json"
  python3 - "$output_file" "$expected_transfer_items_file" "${media_names[@]}" <<'PY'
import json
import re
import sys
from pathlib import Path

source_path, output_path = sys.argv[1:3]
expected_names = sys.argv[3:]
source_text = Path(source_path).read_text(encoding="utf-8")
items = []

for name in expected_names:
    row_match = re.search(
        rf"_id=(?P<id>\d+), _display_name={re.escape(name)}, mime_type=(?P<mime>[^,]+),",
        source_text,
    )
    if row_match is None:
        raise SystemExit(f"Seeded MediaStore row was not found: {name}")

    extension = Path(name).suffix.lower()
    media_id = row_match.group("id")
    picker_name = f"{media_id}{extension}" if extension else media_id
    items.append(
        {
            "seededName": name,
            "pickerName": picker_name,
            "contentType": row_match.group("mime"),
        }
    )

Path(output_path).write_text(json.dumps(items, indent=2) + "\n", encoding="utf-8")
PY
}

seed_shared_media() {
  : > "$evidence_dir/10-push-media.txt"
  : > "$evidence_dir/10-screenrecord-video.txt"
  : > "$evidence_dir/11-media-scan-broadcast.txt"
  : > "$evidence_dir/13-mediastore-seeded-items.txt"

  local index
  local suffix
  local name
  for index in $(seq 1 "$count"); do
    suffix="$(printf '%02d' "$index")"
    if [[ "$kind" == "photo" ]]; then
      name="$run_id-$suffix.png"
      seed_photo "$name"
    else
      name="$run_id-$suffix.mp4"
      seed_video "$name"
    fi
    media_names+=("$name")
  done

  query_seeded_media_store
}

capture_transfer_state() {
  local prefix="$1"
  local instance_key="$2"
  local transfer_root="files/CottonTransfers/$instance_key"

  if ! adb_device shell run-as "$package_id" cat "$transfer_root/queue.json" \
      > "$evidence_dir/$prefix-queue.json" 2> "$evidence_dir/$prefix-queue.err"; then
    rm -f "$evidence_dir/$prefix-queue.json"
  fi

  adb_device shell run-as "$package_id" find "$transfer_root/Staged" \
    -maxdepth 2 -type f | sort > "$evidence_dir/$prefix-staged-files.txt" || true
}

wait_for_files_root() {
  local label="$1"
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5; do
    prefix="$label-$attempt"
    capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if xml_has_text "$xml_file" "Add files" \
        && { xml_has_text "$xml_file" "Open transfers" || xml_has_text "$xml_file" "Transfers"; }; then
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
    adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/$label-relaunch-$attempt.txt" || true
    sleep 2
  done

  printf 'Files root with Add files and Transfers navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

validate_package_version() {
  local package_file="$evidence_dir/03-package.txt"

  if [[ -n "$expected_version_code" ]] \
      && ! grep -E "versionCode=$expected_version_code( |$)" "$package_file" >/dev/null; then
    printf 'Installed versionCode does not match expected value: %s\n' "$expected_version_code" >&2
    printf 'Evidence: %s\n' "$package_file" >&2
    exit 66
  fi

  if [[ -n "$expected_version_name" ]] \
      && ! grep -F "versionName=$expected_version_name" "$package_file" >/dev/null; then
    printf 'Installed versionName does not match expected value: %s\n' "$expected_version_name" >&2
    printf 'Evidence: %s\n' "$package_file" >&2
    exit 66
  fi
}

validate_selected_media_queue() {
  local queue_path="$evidence_dir/60-after-picker-queue.json"
  local staged_path="$evidence_dir/60-after-picker-staged-files.txt"
  local item_path="$evidence_dir/61-selected-media-items.json"
  local expected_path="$evidence_dir/14-expected-transfer-items.json"

  if [[ ! -f "$queue_path" ]]; then
    printf 'Transfer queue metadata was not captured.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir/60-after-picker-queue.err" >&2
    exit 66
  fi

  if [[ ! -f "$expected_path" ]]; then
    printf 'Expected transfer item metadata was not captured.\n' >&2
    printf 'Evidence: %s\n' "$expected_path" >&2
    exit 66
  fi

  python3 - "$queue_path" "$staged_path" "$item_path" "$expected_path" "$kind" <<'PY'
import json
import sys

queue_path, staged_path, item_path, expected_path, kind = sys.argv[1:6]
expected_items = json.load(open(expected_path, encoding="utf-8"))
data = json.load(open(queue_path, encoding="utf-8"))
staged_text = open(staged_path, encoding="utf-8").read()
items = data.get("items", [])
matches = []

for expected in expected_items:
    seeded_name = expected["seededName"]
    picker_name = expected["pickerName"]
    aliases = {seeded_name, picker_name}
    item = next((candidate for candidate in reversed(items) if candidate.get("displayName") in aliases), None)
    if item is None:
        raise SystemExit(f"Missing selected-media transfer for {seeded_name} with aliases {sorted(aliases)!r}")

    source = item.get("source") or {}
    destination = item.get("destination") or {}
    content_type = str(item.get("contentType", ""))
    status = item.get("status")

    if source.get("kind") != 3:
        raise SystemExit(f"Transfer source kind is not SelectedMedia for {seeded_name}: {source!r}")
    if not source.get("sourceId"):
        raise SystemExit(f"Transfer source id is missing for {seeded_name}")
    if source.get("sourceId") in aliases:
        raise SystemExit(f"Transfer source id should not store the display name for {seeded_name}")
    if not destination:
        raise SystemExit(f"Transfer destination is missing for {seeded_name}")
    if kind == "photo" and not content_type.startswith("image/"):
        raise SystemExit(f"Unexpected photo content type for {seeded_name}: {content_type!r}")
    if kind == "video" and not content_type.startswith("video/"):
        raise SystemExit(f"Unexpected video content type for {seeded_name}: {content_type!r}")
    if status not in (0, 1, 2, 3, 4):
        raise SystemExit(f"Unexpected transfer status for {seeded_name}: {status!r}")
    if status != 3 and not any(alias in staged_text for alias in aliases):
        raise SystemExit(f"Waiting transfer staged file is missing for {seeded_name}")

    matches.append(item)

with open(item_path, "w", encoding="utf-8") as handle:
    json.dump(matches, handle, indent=2)
    handle.write("\n")

print(json.dumps(
    {
        "validated": len(matches),
        "kind": kind,
        "items": expected_items,
        "statuses": [item.get("status") for item in matches],
    },
    indent=2,
))
PY
}

open_transfers_page() {
  if xml_has_text "$files_root_xml" "Open transfers"; then
    tap_node_from_xml "$files_root_xml" "Open transfers" contains
  else
    tap_node_from_xml "$files_root_xml" "Transfers" exact
  fi
}

write_metadata() {
  {
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'kind=%s\n' "$kind"
    printf 'count=%s\n' "$count"
    printf 'run_id=%s\n' "$run_id"
    printf 'media_names=%s\n' "$(IFS=,; printf '%s' "${media_names[*]}")"
    printf 'maui_media_picker_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device-media/picker?view=net-maui-10.0\n'
    printf 'android_photo_picker_docs=https://developer.android.com/training/data-storage/shared/photo-picker\n'
    printf 'android_shared_media_docs=https://developer.android.com/training/data-storage/shared/media\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_ui_automator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/00-metadata.txt"
}

write_checklist() {
  {
    printf '# Selected Media Upload Smoke\n\n'
    printf '%s\n' '- [ ] Installed package/version matches the intended build.'
    printf -- '- [ ] Seeded %s item(s) are visible in Android MediaStore.\n' "$kind"
    printf '%s\n' '- [ ] Files root is visible with Add files and Transfers navigation.'
    printf '%s\n' '- [ ] Add files opens the upload action sheet.'
    printf '%s\n' '- [ ] Upload opens the media/source action sheet.'
    printf -- '- [ ] %s picker opens through the native selected-media flow.\n' "$kind"
    printf '%s\n' '- [ ] Operator selects all seeded items and confirms the picker.'
    printf '%s\n' '- [ ] Files returns without a fatal app crash.'
    printf '%s\n' '- [ ] Transfers queue contains SelectedMedia items for all seeded names.'
    printf '%s\n' '- [ ] Waiting selected-media transfers have staged files.'
    printf '%s\n' '- [ ] Transfers page opens after queueing.'
    printf '%s\n\n' '- [ ] Logcat contains no fatal runtime crash for this run.'
    printf 'Seeded items:\n'
    local name
    for name in "${media_names[@]}"; do
      printf -- '- %s\n' "$name"
    done
  } > "$evidence_dir/00-checklist.md"
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
capture_text "04-version.txt" adb_device shell getprop ro.build.version.sdk
validate_package_version

seed_shared_media
write_metadata
write_checklist

if [[ "$seed_only" -eq 1 ]]; then
  printf 'Selected-media seed evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$preflight_only" -eq 1 ]]; then
  printf 'Selected-media preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

instance_key="$(create_instance_key)"

adb_device logcat -c >/dev/null 2>&1 || true
if [[ "$launch_app" -eq 1 ]]; then
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/19-launch.txt"
  sleep 4
fi

wait_for_files_root "20-files-root"
require_xml_text "$files_root_xml" "Add files" "Files Add action is not visible."

tap_text "$files_root_xml" "Add files"
sleep 2
capture_screen "30-add-actions"
require_xml_text "$evidence_dir/30-add-actions.xml" "Upload..." "Upload action is not visible."
tap_text "$evidence_dir/30-add-actions.xml" "Upload..."
sleep 2
capture_screen "31-upload-actions"
if [[ "$kind" == "photo" ]]; then
  require_xml_text "$evidence_dir/31-upload-actions.xml" "Upload photo" "Upload photo action is not visible."
  tap_text "$evidence_dir/31-upload-actions.xml" "Upload photo"
else
  require_xml_text "$evidence_dir/31-upload-actions.xml" "Upload video" "Upload video action is not visible."
  tap_text "$evidence_dir/31-upload-actions.xml" "Upload video"
fi

sleep 3
capture_screen "40-selected-media-picker"

printf '\nSeeded %s items to select:\n' "$kind"
printf '  %s\n' "${media_names[@]}"
wait_for_operator "Select all seeded items in the Android picker, then tap the picker confirmation button."

sleep "$wait_seconds"
wait_for_files_root "50-files-after-picker"
capture_transfer_state "60-after-picker" "$instance_key"
validate_selected_media_queue > "$evidence_dir/62-validation-summary.json"

open_transfers_page
sleep 3
capture_screen "70-transfers"
require_xml_text "$evidence_dir/70-transfers.xml" "Transfers" "Transfers page did not open."

capture_text "90-logcat-raw.txt" adb_device logcat -d -v time
grep -E 'Cotton|WorkManager|SystemJobService|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true
if grep -E 'FATAL EXCEPTION|AndroidRuntime.*FATAL|mono-rt.*SIG' "$evidence_dir/90-logcat-raw.txt" \
    > "$evidence_dir/92-fatal-logcat.txt"; then
  printf 'Fatal runtime crash found in logcat.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir/92-fatal-logcat.txt" >&2
  exit 66
fi

{
  printf 'Selected-media upload smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Kind: %s\n' "$kind"
  printf 'Count: %s\n' "$count"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
