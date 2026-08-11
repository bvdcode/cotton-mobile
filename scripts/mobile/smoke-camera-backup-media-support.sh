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

tap_text() {
  local xml_file="$1"
  local needle="$2"
  cotton_tap_node_from_xml "$xml_file" "$needle" exact
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
  cotton_adb shell input tap "$tap_x" "$tap_y"
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

  cotton_adb shell mkdir -p "$remote_dir"
  cotton_adb push "$local_file" "$remote_file" > "$evidence_dir/09-push-media.txt"
  cotton_adb shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file://$remote_file" > "$evidence_dir/10-media-scan-broadcast.txt" || true
  sleep 2

  cotton_adb shell content query \
    --uri content://media/external/images/media \
    --projection _id:_display_name:mime_type:_size:date_modified \
    > "$evidence_dir/11-mediastore-images.txt"

  if ! grep -F "$media_name" "$evidence_dir/11-mediastore-images.txt" > "$evidence_dir/12-mediastore-smoke-image.txt"; then
    printf 'Seeded media is not visible in MediaStore: %s\n' "$media_name" >&2
    exit 66
  fi
}

grant_media_permissions() {
  cotton_adb shell pm grant "$package_id" android.permission.READ_MEDIA_IMAGES
  cotton_adb shell pm grant "$package_id" android.permission.READ_MEDIA_VIDEO
  cotton_adb shell pm grant "$package_id" android.permission.READ_MEDIA_VISUAL_USER_SELECTED >/dev/null 2>&1 || true
}

cotton_wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5; do
    prefix="20-files-root-$attempt"
    cotton_capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if cotton_xml_has_text "$xml_file" "Open camera backup"; then
      files_root_xml="$xml_file"
      return
    fi

    if cotton_xml_has_text "$xml_file" "Navigate up"; then
      cotton_tap_node_from_xml "$xml_file" "Navigate up" exact
      sleep 2
      continue
    fi

    cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 1
    cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/20-relaunch-$attempt.txt" || true
    sleep 2
  done

  printf 'Files root with Backup navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

choose_backup_destination() {
  tap_text "$evidence_dir/30-backup.xml" "Choose"
  sleep 3
  cotton_capture_screen "40-destination-root"
  cotton_require_xml_text "$evidence_dir/40-destination-root.xml" \
    "Choose Destination" \
    "Destination picker did not open."

  if ! cotton_xml_has_text "$evidence_dir/40-destination-root.xml" "Choose $destination_name"; then
    tap_destination_folder_open "$evidence_dir/40-destination-root.xml" "$destination_name"
    sleep 3
    cotton_capture_screen "41-destination-folder"
    cotton_require_xml_text "$evidence_dir/41-destination-folder.xml" \
      "Choose $destination_name" \
      "Destination folder did not open."
    tap_text "$evidence_dir/41-destination-folder.xml" "Choose $destination_name"
  else
    tap_text "$evidence_dir/40-destination-root.xml" "Choose $destination_name"
  fi

  sleep 4
  cotton_capture_screen "42-destination-saved"
  cotton_require_xml_text "$evidence_dir/42-destination-saved.xml" \
    "$destination_name" \
    "Camera Backup did not show the selected destination."
}

validate_queue_item() {
  local instance_key="$1"
  local queue_path="$evidence_dir/60-queue-after-queue-now.json"
  local item_path="$evidence_dir/61-queue-smoke-item.json"
  local staged_path="$evidence_dir/62-staged-files.txt"

  cotton_adb shell run-as "$package_id" cat "files/CottonTransfers/$instance_key/queue.json" \
    > "$queue_path"
  cotton_adb shell run-as "$package_id" find "files/CottonTransfers/$instance_key/Staged" \
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
