restore_network() {
  if [[ "$network_disabled" -eq 1 && "$leave_network_disabled" -eq 0 ]]; then
    cotton_adb shell svc wifi enable >/dev/null 2>&1 || true
    cotton_adb shell svc data enable >/dev/null 2>&1 || true
    network_disabled=0
  fi
}

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'instance_key=%s\n' "$instance_key"
    printf 'folder=%s\n' "$folder_name"
    printf 'nested_folder=%s\n' "$nested_folder_name"
    printf 'offline_file=%s\n' "$offline_file_name"
    printf 'nested_file=%s\n' "$nested_file_name"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/00-metadata.txt"
}

pull_app_file() {
  local app_path="$1"
  local local_path="$2"

  cotton_adb shell run-as "$package_id" cat "$app_path" > "$local_path"
}

select_smoke_targets() {
  local selected_tsv="$evidence_dir/12-selected-targets.tsv"

  python3 - \
    "$evidence_dir/10-root-cache.json" \
    "$evidence_dir/11-offline-files.json" \
    "$evidence_dir/12-download-files.txt" \
    "$folder_name" \
    "$offline_file_name" \
    > "$selected_tsv" <<'PY'
import json
import sys

root_path, pins_path, downloads_path, folder_name, requested_file_name = sys.argv[1:6]
root = json.load(open(root_path, encoding="utf-8"))
pins = json.load(open(pins_path, encoding="utf-8"))
download_paths = [
    line.strip()
    for line in open(downloads_path, encoding="utf-8")
    if line.strip()
]

if root.get("schemaVersion") != 2:
    raise SystemExit(f"Root listing cache schema is {root.get('schemaVersion')}, expected 2.")
if pins.get("schemaVersion") != 1:
    raise SystemExit(f"Offline file metadata schema is {pins.get('schemaVersion')}, expected 1.")

entries = root.get("entries") or []
folder = next(
    (
        entry
        for entry in entries
        if entry.get("type") == 0 and entry.get("name") == folder_name
    ),
    None,
)
if folder is None:
    raise SystemExit(f"Folder not found in cached root listing: {folder_name}")

files_by_id = {
    entry.get("id"): entry
    for entry in entries
    if entry.get("type") == 1 and entry.get("id")
}
pins_by_id = {
    item.get("fileId"): item
    for item in pins.get("items") or []
    if item.get("fileId")
}
downloaded_ids = {
    path.split("/")[-2]
    for path in download_paths
    if "/" in path
}

def score(entry: dict) -> tuple[int, str]:
    content_type = str(entry.get("contentType") or "")
    name = str(entry.get("name") or "")
    is_pinned = entry.get("id") in pins_by_id
    if requested_file_name and name != requested_file_name:
        return (999, name)
    if any(character.isspace() for character in name) or "/" in name:
        return (30, name)
    if content_type.startswith("text/") or content_type == "application/json":
        return (0 if is_pinned else 10, name)
    if content_type.startswith("image/"):
        return (1 if is_pinned else 11, name)
    if content_type.startswith("video/") or content_type.startswith("audio/"):
        return (2 if is_pinned else 12, name)
    return (20 if is_pinned else 25, name)

candidates = [
    entry
    for file_id, entry in files_by_id.items()
    if file_id in downloaded_ids
]
if requested_file_name:
    candidates = [entry for entry in candidates if entry.get("name") == requested_file_name]
if not candidates:
    raise SystemExit("No on-device root file is available for offline-open smoke.")

selected = sorted(candidates, key=score)[0]
pin = pins_by_id.get(selected["id"])
if pin is not None and selected.get("sizeBytes") != pin.get("sizeBytes"):
    raise SystemExit("Selected pinned file size does not match root cache metadata.")

print(
    "\t".join(
        [
            folder["id"],
            folder["name"],
            selected["id"],
            selected["name"],
            str(selected.get("sizeBytes") or ""),
            str(selected.get("contentType") or ""),
            "true" if pin is not None else "false",
        ]
    )
)
PY

  IFS=$'\t' read -r \
    folder_id \
    folder_name \
    selected_file_id \
    selected_file_name \
    selected_file_size \
    selected_content_type \
    selected_file_is_pinned \
    < "$selected_tsv"
  offline_file_name="$selected_file_name"
}

validate_local_file_bytes() {
  local download_dir="files/CottonDownloads/$instance_key/$selected_file_id"
  local download_path="$download_dir/$selected_file_name"

  cotton_capture_text_best_effort "13-selected-download-dir.txt" \
    cotton_adb shell run-as "$package_id" find "$download_dir" -maxdepth 1 -type f

  if ! cotton_adb shell run-as "$package_id" test -f "$download_path"; then
    printf 'Selected offline file is missing from app-private downloads: %s\n' "$download_path" >&2
    printf 'Evidence: %s/13-selected-download-dir.txt\n' "$evidence_dir" >&2
    exit 66
  fi

  local actual_size
  actual_size="$(cotton_adb shell run-as "$package_id" stat -c %s "$download_path" | tr -d '\r\n')"
  if [[ "$actual_size" != "$selected_file_size" ]]; then
    printf 'Selected offline file size mismatch: expected %s, got %s.\n' \
      "$selected_file_size" "$actual_size" >&2
    exit 66
  fi

  {
    printf 'file_id=%s\n' "$selected_file_id"
    printf 'file_name=%s\n' "$selected_file_name"
    printf 'content_type=%s\n' "$selected_content_type"
    printf 'is_pinned=%s\n' "$selected_file_is_pinned"
    printf 'expected_size=%s\n' "$selected_file_size"
    printf 'actual_size=%s\n' "$actual_size"
    printf 'download_path=%s\n' "$download_path"
  } > "$evidence_dir/14-selected-offline-file.env"
}

validate_folder_cache() {
  python3 - "$evidence_dir/30-folder-cache.json" "$folder_id" "$folder_name" <<'PY'
import json
import sys

cache_path, folder_id, folder_name = sys.argv[1:4]
cache = json.load(open(cache_path, encoding="utf-8"))

if cache.get("schemaVersion") != 2:
    raise SystemExit(f"Folder cache schema is {cache.get('schemaVersion')}, expected 2.")
if cache.get("folderId") != folder_id:
    raise SystemExit("Folder cache id does not match selected folder.")
if cache.get("folderName") != folder_name:
    raise SystemExit("Folder cache name does not match selected folder.")
if not isinstance(cache.get("entries"), list):
    raise SystemExit("Folder cache entries are missing.")

print(
    json.dumps(
        {
            "folderId": cache.get("folderId"),
            "folderName": cache.get("folderName"),
            "entryCount": len(cache.get("entries") or []),
            "cachedAtUtc": cache.get("cachedAtUtc"),
        },
        indent=2,
    )
)
PY
}

select_nested_folder_target() {
  local selected_tsv="$evidence_dir/32-selected-nested-folder.tsv"

  python3 - \
    "$evidence_dir/30-folder-cache.json" \
    "$nested_folder_name" \
    > "$selected_tsv" <<'PY'
import json
import sys

cache_path, nested_folder_name = sys.argv[1:3]
cache = json.load(open(cache_path, encoding="utf-8"))

for entry in cache.get("entries") or []:
    if entry.get("type") == 0 and entry.get("name") == nested_folder_name:
        print(f"{entry['id']}\t{entry['name']}")
        break
else:
    raise SystemExit(f"Nested folder not found in cached folder listing: {nested_folder_name}")
PY

  IFS=$'\t' read -r nested_folder_id nested_folder_name < "$selected_tsv"
}

validate_nested_folder_cache() {
  python3 - \
    "$evidence_dir/33-nested-folder-cache.json" \
    "$nested_folder_id" \
    "$nested_folder_name" \
    "$nested_file_name" <<'PY'
import json
import sys

cache_path, folder_id, folder_name, nested_file_name = sys.argv[1:5]
cache = json.load(open(cache_path, encoding="utf-8"))

if cache.get("schemaVersion") != 2:
    raise SystemExit(f"Nested folder cache schema is {cache.get('schemaVersion')}, expected 2.")
if cache.get("folderId") != folder_id:
    raise SystemExit("Nested folder cache id does not match selected child folder.")
if cache.get("folderName") != folder_name:
    raise SystemExit("Nested folder cache name does not match selected child folder.")
entries = cache.get("entries")
if not isinstance(entries, list):
    raise SystemExit("Nested folder cache entries are missing.")
if nested_file_name and not any(entry.get("name") == nested_file_name for entry in entries):
    raise SystemExit(f"Nested file not found in cached child folder listing: {nested_file_name}")

print(
    json.dumps(
        {
            "folderId": cache.get("folderId"),
            "folderName": cache.get("folderName"),
            "entryCount": len(entries),
            "cachedAtUtc": cache.get("cachedAtUtc"),
            "nestedFile": nested_file_name,
        },
        indent=2,
    )
)
PY
}
