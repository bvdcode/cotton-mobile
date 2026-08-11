tap_text() {
  local xml_file="$1"
  local needle="$2"
  cotton_tap_node_from_xml "$xml_file" "$needle" exact
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
  cotton_adb shell mkdir -p "$remote_dir"
  cotton_adb push "$local_file" "$remote_file" >> "$evidence_dir/10-push-media.txt"
  cotton_adb shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file://$remote_file" >> "$evidence_dir/11-media-scan-broadcast.txt" || true
}

seed_video() {
  local name="$1"
  local remote_dir="/sdcard/Movies/CottonSelectedMediaSmoke"
  local remote_file="$remote_dir/$name"

  cotton_adb shell mkdir -p "$remote_dir"
  cotton_adb shell rm -f "$remote_file" >/dev/null 2>&1 || true
  cotton_adb shell screenrecord --time-limit 1 "$remote_file" >> "$evidence_dir/10-screenrecord-video.txt" 2>&1
  cotton_adb shell test -s "$remote_file"
  cotton_adb shell am broadcast \
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
  cotton_adb shell content query \
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
