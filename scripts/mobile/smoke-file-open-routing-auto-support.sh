write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'instance_key=%s\n' "$instance_key"
    printf 'folder=%s\n' "$folder_name"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
  } > "$evidence_dir/00-metadata.txt"
}

pull_app_file() {
  local app_path="$1"
  local local_path="$2"

  cotton_adb shell run-as "$package_id" cat "$app_path" > "$local_path"
}

load_targets() {
  local root_cache="$evidence_dir/10-root-cache.json"
  local folder_cache="$evidence_dir/11-folder-cache.json"
  local downloads="$evidence_dir/12-download-files.txt"
  local folder_tsv="$evidence_dir/13-folder.tsv"

  pull_app_file "files/CottonFolderListings/$instance_key/root.json" "$root_cache"
  cotton_capture_text_best_effort "12-download-files.txt" \
    cotton_adb shell run-as "$package_id" find "files/CottonDownloads/$instance_key" -maxdepth 2 -type f

  python3 - "$root_cache" "$folder_name" > "$folder_tsv" <<'PY'
import json
import sys

root_path, folder_name = sys.argv[1:3]
root = json.load(open(root_path, encoding="utf-8"))
if root.get("schemaVersion") != 2:
    raise SystemExit(f"Root listing cache schema is {root.get('schemaVersion')}, expected 2.")

for entry in root.get("entries") or []:
    if entry.get("type") == 0 and entry.get("name") == folder_name:
        print(f"{entry['id']}\t{entry['name']}")
        break
else:
    raise SystemExit(f"Folder not found in cached root listing: {folder_name}")
PY

  IFS=$'\t' read -r folder_id folder_name < "$folder_tsv"
  folder_cache_name="${folder_id//-/}.json"
  pull_app_file "files/CottonFolderListings/$instance_key/$folder_cache_name" "$folder_cache"

  python3 - "$folder_cache" "$downloads" "$folder_id" "$folder_name" > "$evidence_dir/14-targets.tsv" <<'PY'
import json
import sys
from pathlib import Path

folder_cache_path, downloads_path, folder_id, folder_name = sys.argv[1:5]
folder = json.load(open(folder_cache_path, encoding="utf-8"))
downloads = [line.strip() for line in open(downloads_path, encoding="utf-8") if line.strip()]
downloads_by_id = {Path(path).parent.name: path for path in downloads}

if folder.get("schemaVersion") != 2:
    raise SystemExit(f"Folder cache schema is {folder.get('schemaVersion')}, expected 2.")
if folder.get("folderId") != folder_id or folder.get("folderName") != folder_name:
    raise SystemExit("Folder cache does not match selected folder.")

required = {
    "text": ("cotton-open-text.txt", "text"),
    "image": ("cotton-open-image.png", "image"),
    "pdf": ("cotton-open-doc.pdf", "pdf"),
    "audio": ("cotton-open-audio.wav", "audio"),
    "video": ("cotton-open-video-valid.mp4", "video"),
    "office": ("cotton-open-office.docx", "system"),
    "archive": ("cotton-open-archive.zip", "system"),
    "unknown": ("cotton-open-unknown.bin", "system"),
}

entries = {entry.get("name"): entry for entry in folder.get("entries") or []}
for key, (name, mode) in required.items():
    entry = entries.get(name)
    if entry is None:
        raise SystemExit(f"Required file missing from cached folder: {name}")
    path = downloads_by_id.get(entry.get("id"))
    if path is None:
        raise SystemExit(f"Required local download missing for {name}")
    print(
        "\t".join(
            [
                key,
                name,
                entry["id"],
                str(entry.get("sizeBytes") or ""),
                str(entry.get("kind") or ""),
                str(entry.get("contentType") or ""),
                mode,
                path,
            ]
        )
    )
PY
}

validate_target_bytes() {
  local expected_count
  local validated_count=0

  expected_count="$(wc -l < "$evidence_dir/14-targets.tsv" | tr -d '[:space:]')"
  : > "$evidence_dir/15-target-byte-validation.tsv"
  while IFS=$'\t' read -r -u 3 key name file_id size kind content_type mode path; do
    if [[ -z "$key" ]]; then
      continue
    fi
    local actual_size
    actual_size="$(cotton_adb shell run-as "$package_id" stat -c %s "$path" | tr -d '\r\n')"
    if [[ "$actual_size" != "$size" ]]; then
      printf 'Local size mismatch for %s: expected %s, got %s.\n' "$name" "$size" "$actual_size" >&2
      exit 66
    fi
    printf '%s\t%s\t%s\t%s\t%s\n' "$key" "$name" "$kind" "$content_type" "$actual_size" \
      >> "$evidence_dir/15-target-byte-validation.tsv"
    validated_count=$((validated_count + 1))
  done 3< "$evidence_dir/14-targets.tsv"

  if [[ "$validated_count" != "$expected_count" ]]; then
    printf 'Validated %s target files, expected %s.\n' "$validated_count" "$expected_count" >&2
    exit 66
  fi
}

launch_folder() {
  cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
  cotton_adb shell am force-stop "$package_id" >/dev/null 2>&1 || true
  cotton_adb shell am start -n "$package_id/$main_activity" > "$evidence_dir/20-launch-$current_key.txt"
  sleep 5

  waited_xml=""
  cotton_wait_for_text "21-$current_key-files" "Files"
  local files_xml="$waited_xml"
  if cotton_xml_has_text "$files_xml" "Files / $folder_name"; then
    folder_xml="$files_xml"
    return
  fi

  cotton_require_xml_text "$files_xml" "$folder_name" "Files root did not show the smoke folder."
  cotton_tap_node_from_xml "$files_xml" "$folder_name"
  sleep 5

  waited_xml=""
  cotton_wait_for_text "22-$current_key-folder" "Files / $folder_name"
  folder_xml="$waited_xml"
}

search_target() {
  local query="$1"
  local name="$2"

  cotton_tap_node_from_xml "$folder_xml" "Search files"
  sleep 1
  cotton_adb shell input text "$query"
  cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
  sleep 2
  cotton_capture_screen "30-$current_key-search"
  search_xml="$evidence_dir/30-$current_key-search.xml"
  cotton_require_xml_text "$search_xml" "$name" "Search did not reveal $name."
}

is_external_window() {
  local window_file="$1"
  local focus_lines

  focus_lines="$(grep -E 'mCurrentFocus=|mFocusedApp=' "$window_file" || true)"
  if [[ -z "$focus_lines" ]]; then
    return 1
  fi

  ! grep -Fq "$package_id/" <<< "$focus_lines"
}

validate_open_result() {
  local key="$1"
  local name="$2"
  local mode="$3"
  local prefix="40-$key-open"
  local xml_file="$evidence_dir/$prefix.xml"
  local window_file="$evidence_dir/$prefix-window.txt"

  case "$mode" in
    text)
      cotton_require_xml_text "$xml_file" "$name" "Text file did not open in Cotton text viewer."
      cotton_require_xml_text "$xml_file" "Text" "Text viewer did not show text details."
      ;;
    image)
      cotton_require_xml_text "$xml_file" "$name" "Image file did not open in Cotton image viewer."
      cotton_require_xml_text "$xml_file" "Image" "Image viewer did not show image details."
      ;;
    pdf)
      cotton_require_xml_text "$xml_file" "$name" "PDF file did not open in Cotton PDF viewer."
      cotton_require_xml_text "$xml_file" "PDF" "PDF viewer did not show PDF details."
      cotton_require_xml_text "$xml_file" "Open" "PDF viewer did not expose external open action."
      ;;
    audio)
      cotton_require_xml_text "$xml_file" "$name" "Audio file did not open in Cotton media viewer."
      cotton_require_xml_text "$xml_file" "Audio" "Audio viewer did not show audio details."
      ;;
    video)
      cotton_require_xml_text "$xml_file" "$name" "Video file did not open in Cotton media viewer."
      cotton_require_xml_text "$xml_file" "Video" "Video viewer did not show video details."
      ;;
    system)
      if cotton_xml_has_text "$xml_file" "$name"; then
        return
      fi

      case "$key" in
        office)
          if cotton_xml_has_text "$xml_file" "No document app can open this file."; then
            return
          fi
          ;;
        archive)
          if cotton_xml_has_text "$xml_file" "No archive app can open this file."; then
            return
          fi
          ;;
        unknown)
          if cotton_xml_has_text "$xml_file" "No app can open this file type."; then
            return
          fi
          ;;
      esac

      if is_external_window "$window_file"; then
        return
      fi

      printf 'System-open result for %s was neither an external handler nor expected fallback copy.\n' "$name" >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 66
      ;;
    *)
      printf 'Unknown open mode: %s\n' "$mode" >&2
      exit 64
      ;;
  esac
}

open_target() {
  local key="$1"
  local name="$2"
  local mode="$3"
  local query="$4"

  current_key="$key"
  launch_folder
  search_target "$query" "$name"
  cotton_tap_node_from_xml "$search_xml" "$name"
  sleep 5
  cotton_capture_screen "40-$key-open"
  validate_open_result "$key" "$name" "$mode"

  {
    printf 'key=%s\n' "$key"
    printf 'name=%s\n' "$name"
    printf 'mode=%s\n' "$mode"
  } > "$evidence_dir/41-$key-result.env"
}

query_for_key() {
  case "$1" in
    text) printf 'open-text' ;;
    image) printf 'open-image' ;;
    pdf) printf 'open-doc' ;;
    audio) printf 'open-audio' ;;
    video) printf 'video-valid' ;;
    office) printf 'open-office' ;;
    archive) printf 'open-archive' ;;
    unknown) printf 'open-unknown' ;;
    *) printf '%s' "$1" ;;
  esac
}
