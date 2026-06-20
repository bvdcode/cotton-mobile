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
grant_media=1
launch_app=1
choose_destination=1
preflight_only=0
queue_wait_seconds=10
media_name=""
files_root_xml=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a camera-backup media-scan smoke and captures UI, MediaStore, permission,
and transfer-queue evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --destination NAME        Folder to choose as the backup destination.
  --media-name NAME         Seeded image display name. Defaults to a timestamped PNG.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --no-grant-media          Do not grant Android media permissions with pm grant.
  --skip-destination        Use the existing camera-backup destination.
  --queue-wait SECONDS      Seconds to wait after tapping Queue now. Defaults to 10.
  --preflight-only          Capture package/permission state and exit.
  --no-launch               Do not launch the app before capture.
  --help, -h                Show this help.

The app must already have a signed-in session and a cached root listing for the
selected instance. The smoke validates that a real MediaStore image is visible
to the app, Camera Backup shows full media access, Queue now runs, and the
seeded image appears in the transfer queue as a Camera Backup source.
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
    --media-name)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --media-name.\n' >&2
        exit 64
      fi
      media_name="$2"
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
    --no-grant-media)
      grant_media=0
      shift
      ;;
    --skip-destination)
      choose_destination=0
      shift
      ;;
    --queue-wait)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --queue-wait.\n' >&2
        exit 64
      fi
      queue_wait_seconds="$2"
      shift 2
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

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"

if [[ -z "$media_name" ]]; then
  media_name="cotton-camera-backup-stage15-$timestamp.png"
fi

if [[ -z "${destination_name//[[:space:]]/}" ]]; then
  printf 'Destination name must not be blank.\n' >&2
  exit 64
fi

if [[ -z "${media_name//[[:space:]]/}" || "$media_name" == *"/"* ]]; then
  printf 'Media name must not be blank and must not contain a slash.\n' >&2
  exit 64
fi

if ! [[ "$queue_wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Queue wait must be a non-negative integer.\n' >&2
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

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-camera-backup-media-scan"
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

require_xml_any_text() {
  local xml_file="$1"
  local message="$2"
  shift 2

  if [[ ! -f "$xml_file" ]]; then
    printf '%s\n' "$message" >&2
    printf 'Missing XML: %s\n' "$xml_file" >&2
    exit 66
  fi

  local needle
  for needle in "$@"; do
    if grep -Fq "$needle" "$xml_file"; then
      return
    fi
  done

  printf '%s\n' "$message" >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 66
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

tap_destination_folder_open() {
  local xml_file="$1"
  local folder_name="$2"
  local point_file="$evidence_dir/destination-open-point.txt"

  python3 - "$xml_file" "$folder_name" > "$point_file" <<'PY'
import re
import sys
from xml.etree import ElementTree

xml_file, folder_name = sys.argv[1:3]
root = ElementTree.parse(xml_file).getroot()

def parse_bounds(bounds: str) -> tuple[int, int, int, int]:
    match = re.fullmatch(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", bounds)
    if match is None:
        raise ValueError(bounds)
    return tuple(int(value) for value in match.groups())

def center(bounds: tuple[int, int, int, int]) -> tuple[int, int]:
    left, top, right, bottom = bounds
    return ((left + right) // 2, (top + bottom) // 2)

folder_centers = []
open_buttons = []
for node in root.iter("node"):
    text = node.attrib.get("text", "")
    try:
        bounds = parse_bounds(node.attrib["bounds"])
    except (KeyError, ValueError):
        continue
    if text == folder_name:
        folder_centers.append(center(bounds))
    if text == "Open" and node.attrib.get("class") == "android.widget.Button":
        open_buttons.append(center(bounds))

if not folder_centers:
    raise SystemExit(f"Folder is not visible: {folder_name}")

folder_x, folder_y = folder_centers[0]
if open_buttons:
    open_x, open_y = min(open_buttons, key=lambda point: abs(point[1] - folder_y))
    if abs(open_y - folder_y) < 120:
        print(open_x, open_y)
        raise SystemExit(0)

print(folder_x, folder_y)
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

generate_media_file() {
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

seed_media_store() {
  local local_file="$1"
  local remote_dir="/sdcard/Pictures/CottonBackupSmoke"
  local remote_file="$remote_dir/$media_name"

  adb_device shell mkdir -p "$remote_dir"
  adb_device push "$local_file" "$remote_file" > "$evidence_dir/09-push-media.txt"
  adb_device shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file://$remote_file" > "$evidence_dir/10-media-scan-broadcast.txt" || true
  sleep 2

  adb_device shell content query \
    --uri content://media/external/images/media \
    --projection _id:_display_name:mime_type:_size:date_modified \
    > "$evidence_dir/11-mediastore-images.txt"

  if ! grep -F "$media_name" "$evidence_dir/11-mediastore-images.txt" > "$evidence_dir/12-mediastore-smoke-image.txt"; then
    printf 'Seeded media is not visible in MediaStore: %s\n' "$media_name" >&2
    exit 66
  fi
}

grant_media_permissions() {
  adb_device shell pm grant "$package_id" android.permission.READ_MEDIA_IMAGES
  adb_device shell pm grant "$package_id" android.permission.READ_MEDIA_VIDEO
  adb_device shell pm grant "$package_id" android.permission.READ_MEDIA_VISUAL_USER_SELECTED >/dev/null 2>&1 || true
}

wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5; do
    prefix="20-files-root-$attempt"
    capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if xml_has_text "$xml_file" "Open camera backup"; then
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

  printf 'Files root with Backup navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

choose_backup_destination() {
  tap_text "$evidence_dir/30-backup.xml" "Choose"
  sleep 3
  capture_screen "40-destination-root"
  require_xml_text "$evidence_dir/40-destination-root.xml" \
    "Choose Destination" \
    "Destination picker did not open."

  if ! xml_has_text "$evidence_dir/40-destination-root.xml" "Choose $destination_name"; then
    tap_destination_folder_open "$evidence_dir/40-destination-root.xml" "$destination_name"
    sleep 3
    capture_screen "41-destination-folder"
    require_xml_text "$evidence_dir/41-destination-folder.xml" \
      "Choose $destination_name" \
      "Destination folder did not open."
    tap_text "$evidence_dir/41-destination-folder.xml" "Choose $destination_name"
  else
    tap_text "$evidence_dir/40-destination-root.xml" "Choose $destination_name"
  fi

  sleep 4
  capture_screen "42-destination-saved"
  require_xml_text "$evidence_dir/42-destination-saved.xml" \
    "$destination_name" \
    "Camera Backup did not show the selected destination."
}

validate_queue_item() {
  local instance_key="$1"
  local queue_path="$evidence_dir/60-queue-after-queue-now.json"
  local item_path="$evidence_dir/61-queue-smoke-item.json"
  local staged_path="$evidence_dir/62-staged-files.txt"

  adb_device shell run-as "$package_id" cat "files/CottonTransfers/$instance_key/queue.json" \
    > "$queue_path"
  adb_device shell run-as "$package_id" find "files/CottonTransfers/$instance_key/Staged" \
    -maxdepth 2 -type f | sort > "$staged_path" || true

  python3 - "$queue_path" "$media_name" "$destination_name" "$item_path" <<'PY'
import json
import sys

queue_path, media_name, destination_name, item_path = sys.argv[1:5]
data = json.load(open(queue_path, encoding="utf-8"))
items = [item for item in data.get("items", []) if item.get("displayName") == media_name]
if not items:
    raise SystemExit(f"Missing queue item for {media_name}")

item = items[-1]
source = item.get("source") or {}
destination = item.get("destination") or {}

if source.get("kind") != 1:
    raise SystemExit(f"Queue item source kind is not Camera Backup: {source!r}")
if not str(source.get("sourceId", "")).startswith("content://media/external/images/media/"):
    raise SystemExit(f"Unexpected source id: {source.get('sourceId')!r}")
if destination.get("folderName") != destination_name:
    raise SystemExit(f"Unexpected destination: {destination!r}")
if not str(item.get("contentType", "")).startswith("image/"):
    raise SystemExit(f"Unexpected content type: {item.get('contentType')!r}")
if item.get("status") not in (0, 1, 2, 3):
    raise SystemExit(f"Unexpected transfer status: {item.get('status')!r}")

with open(item_path, "w", encoding="utf-8") as handle:
    json.dump(item, handle, indent=2)
    handle.write("\n")

print(json.dumps(
    {
        "displayName": item.get("displayName"),
        "contentType": item.get("contentType"),
        "status": item.get("status"),
        "transferredBytes": item.get("transferredBytes"),
        "totalBytes": item.get("totalBytes"),
        "sourceId": source.get("sourceId"),
        "destination": destination.get("path"),
    },
    indent=2,
))
PY
}

write_metadata() {
  {
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'destination=%s\n' "$destination_name"
    printf 'media=%s\n' "$media_name"
    printf 'android_media_permissions_docs=https://developer.android.com/about/versions/14/changes/partial-photo-video-access\n'
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

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/02-install.txt"
fi

if [[ "$grant_media" -eq 1 ]]; then
  grant_media_permissions
fi

capture_text "03-package.txt" adb_device shell dumpsys package "$package_id"
capture_text "04-appops.txt" adb_device shell cmd appops get --uid "$package_id"

local_seed_dir="$(mktemp -d "${TMPDIR:-/tmp}/cotton-camera-backup-media.XXXXXX")"
trap 'rm -rf "$local_seed_dir"' EXIT
local_media_file="$local_seed_dir/$media_name"
generate_media_file "$local_media_file"
seed_media_store "$local_media_file"

if [[ "$preflight_only" -eq 1 ]]; then
  printf 'Preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c >/dev/null 2>&1 || true
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/19-launch.txt"
  sleep 4
fi

wait_for_files_root
tap_text "$files_root_xml" "Backup"
sleep 4
capture_screen "30-backup"
require_xml_text "$evidence_dir/30-backup.xml" "Camera Backup" "Camera Backup page did not open."
require_xml_text "$evidence_dir/30-backup.xml" "Media Access" "Media Access state is not visible."
require_xml_text "$evidence_dir/30-backup.xml" "Allowed" "Camera Backup does not have full media access."

if [[ "$choose_destination" -eq 1 ]]; then
  choose_backup_destination
  backup_xml="$evidence_dir/42-destination-saved.xml"
else
  backup_xml="$evidence_dir/30-backup.xml"
fi

tap_text "$backup_xml" "Queue now"
sleep "$queue_wait_seconds"
capture_screen "50-queue-now"
require_xml_text "$evidence_dir/50-queue-now.xml" "Camera Backup" "Camera Backup page was lost after Queue now."
require_xml_any_text "$evidence_dir/50-queue-now.xml" \
  "Queue status was not visible after Queue now." \
  "camera backup upload" \
  "camera backup uploads"

validate_queue_item "$instance_key" | tee "$evidence_dir/63-queue-smoke-item-summary.json"

capture_text "90-logcat-raw.txt" adb_device logcat -d -v time
grep -E 'Cotton|WorkManager|SystemJobService|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true

{
  printf 'Camera backup media-scan smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Destination: %s\n' "$destination_name"
  printf 'Media: %s\n' "$media_name"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
