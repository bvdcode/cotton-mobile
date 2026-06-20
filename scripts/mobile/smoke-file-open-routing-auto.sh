#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
folder_name="Mobile smoke folder"
main_activity="crc647f4f3c52a3509f5a.MainActivity"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a non-interactive file-open routing smoke against existing cotton-open-*
files in a cached Cotton folder. It validates app-private local bytes, opens
text/image/audio/video in Cotton viewers, and verifies PDF/document/archive/
unknown files either launch a system handler or show the expected no-app copy.

Options:
  --package ID        Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL     ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI      Cotton instance URI. Defaults to $instance_uri.
  --folder NAME       Folder containing cotton-open-* files. Defaults to "$folder_name".
  --evidence-dir DIR  Evidence directory. Defaults to a timestamped directory.
  --install-debug     Install the current debug APK with -r before launch.
  --help, -h          Show this help.

The app must already have a signed-in session, cached folder listing, and local
downloads for the selected smoke files.
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
    --folder)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --folder.\n' >&2
        exit 64
      fi
      folder_name="$2"
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

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if ! command -v python3 >/dev/null 2>&1; then
  printf 'python3 was not found.\n' >&2
  exit 127
fi

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-file-open-routing-auto"
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
  local remote_xml="/sdcard/cotton-window.xml"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window

  if ! adb_device exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  adb_device shell rm -f "$remote_xml" >/dev/null 2>&1 || true
  if adb_device shell uiautomator dump "$remote_xml" > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    if ! adb_device pull "$remote_xml" "$evidence_dir/$prefix.xml" > "$evidence_dir/$prefix-pull-xml.log" 2>&1; then
      rm -f "$evidence_dir/$prefix.xml"
    fi
    adb_device shell rm -f "$remote_xml" >/dev/null 2>&1 || true
  else
    rm -f "$evidence_dir/$prefix.xml"
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
    values = (
        node.attrib.get("text", ""),
        node.attrib.get("content-desc", ""),
        node.attrib.get("hint", ""),
    )
    if any(needle in value for value in values):
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

wait_for_text() {
  local prefix="$1"
  local needle="$2"
  local attempt
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    capture_screen "$prefix-$attempt"
    xml_file="$evidence_dir/$prefix-$attempt.xml"
    if xml_has_text "$xml_file" "$needle"; then
      waited_xml="$xml_file"
      return
    fi
    sleep 2
  done

  printf 'Timed out waiting for UI text: %s\n' "$needle" >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

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

  adb_device shell run-as "$package_id" cat "$app_path" > "$local_path"
}

load_targets() {
  local root_cache="$evidence_dir/10-root-cache.json"
  local folder_cache="$evidence_dir/11-folder-cache.json"
  local downloads="$evidence_dir/12-download-files.txt"
  local folder_tsv="$evidence_dir/13-folder.tsv"

  pull_app_file "files/CottonFolderListings/$instance_key/root.json" "$root_cache"
  capture_text "12-download-files.txt" \
    adb_device shell run-as "$package_id" find "files/CottonDownloads/$instance_key" -maxdepth 2 -type f

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
    "pdf": ("cotton-open-doc.pdf", "system"),
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
    actual_size="$(adb_device shell run-as "$package_id" stat -c %s "$path" | tr -d '\r\n')"
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
  adb_device shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
  adb_device shell am force-stop "$package_id" >/dev/null 2>&1 || true
  adb_device shell am start -n "$package_id/$main_activity" > "$evidence_dir/20-launch-$current_key.txt"
  sleep 5

  waited_xml=""
  wait_for_text "21-$current_key-files" "Files"
  local files_xml="$waited_xml"
  if xml_has_text "$files_xml" "Files / $folder_name"; then
    folder_xml="$files_xml"
    return
  fi

  require_xml_text "$files_xml" "$folder_name" "Files root did not show the smoke folder."
  tap_node_from_xml "$files_xml" "$folder_name"
  sleep 5

  waited_xml=""
  wait_for_text "22-$current_key-folder" "Files / $folder_name"
  folder_xml="$waited_xml"
}

search_target() {
  local query="$1"
  local name="$2"

  tap_node_from_xml "$folder_xml" "Search files"
  sleep 1
  adb_device shell input text "$query"
  adb_device shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
  sleep 2
  capture_screen "30-$current_key-search"
  search_xml="$evidence_dir/30-$current_key-search.xml"
  require_xml_text "$search_xml" "$name" "Search did not reveal $name."
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
      require_xml_text "$xml_file" "$name" "Text file did not open in Cotton text viewer."
      require_xml_text "$xml_file" "Text" "Text viewer did not show text details."
      ;;
    image)
      require_xml_text "$xml_file" "$name" "Image file did not open in Cotton image viewer."
      require_xml_text "$xml_file" "Image" "Image viewer did not show image details."
      ;;
    audio)
      require_xml_text "$xml_file" "$name" "Audio file did not open in Cotton media viewer."
      require_xml_text "$xml_file" "Audio" "Audio viewer did not show audio details."
      ;;
    video)
      require_xml_text "$xml_file" "$name" "Video file did not open in Cotton media viewer."
      require_xml_text "$xml_file" "Video" "Video viewer did not show video details."
      ;;
    system)
      if xml_has_text "$xml_file" "$name"; then
        return
      fi

      case "$key" in
        pdf)
          if xml_has_text "$xml_file" "No PDF app can open this file."; then
            return
          fi
          ;;
        office)
          if xml_has_text "$xml_file" "No document app can open this file."; then
            return
          fi
          ;;
        archive)
          if xml_has_text "$xml_file" "No archive app can open this file."; then
            return
          fi
          ;;
        unknown)
          if xml_has_text "$xml_file" "No app can open this file type."; then
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
  tap_node_from_xml "$search_xml" "$name"
  sleep 5
  capture_screen "40-$key-open"
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

capture_text "03-package.txt" adb_device shell dumpsys package "$package_id"
load_targets
validate_target_bytes

adb_device logcat -c >/dev/null 2>&1 || true

target_count="$(wc -l < "$evidence_dir/14-targets.tsv" | tr -d '[:space:]')"
opened_count=0
while IFS=$'\t' read -r -u 3 key name file_id size kind content_type mode path; do
  if [[ -z "$key" ]]; then
    continue
  fi
  open_target "$key" "$name" "$mode" "$(query_for_key "$key")"
  opened_count=$((opened_count + 1))
done 3< "$evidence_dir/14-targets.tsv"

if [[ "$opened_count" != "$target_count" ]]; then
  printf 'Opened %s target files, expected %s.\n' "$opened_count" "$target_count" >&2
  exit 66
fi

capture_text "90-logcat-raw.txt" adb_device logcat -d -v time
grep -E 'Cotton|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true

{
  printf 'File-open routing auto smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Folder: %s\n' "$folder_name"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
