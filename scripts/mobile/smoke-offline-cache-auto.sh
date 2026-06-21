#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
folder_name="Mobile smoke folder"
offline_file_name=""
nested_folder_name=""
nested_file_name=""
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
leave_network_disabled=0
network_disabled=0
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a non-interactive offline file/folder cache smoke for the current Android
build. It refreshes a known folder online, validates app-private cached listings
and local offline bytes, disables network, verifies cached root/folder UI, and
opens an on-device file while offline.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Cotton instance URI. Defaults to $instance_uri.
  --folder NAME             Cached folder to navigate. Defaults to "$folder_name".
  --nested-folder NAME      Optional cached child folder to navigate inside --folder.
  --offline-file NAME       On-device file to open offline. Defaults to a pinned root file.
  --nested-file NAME        Optional file expected inside --nested-folder cache.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --leave-network-disabled  Do not restore Wi-Fi/mobile data at the end.
  --help, -h                Show this help.

The app must already have a signed-in session and cached root listing for the
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
    --folder)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --folder.\n' >&2
        exit 64
      fi
      folder_name="$2"
      shift 2
      ;;
    --nested-folder)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --nested-folder.\n' >&2
        exit 64
      fi
      nested_folder_name="$2"
      shift 2
      ;;
    --offline-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --offline-file.\n' >&2
        exit 64
      fi
      offline_file_name="$2"
      shift 2
      ;;
    --nested-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --nested-file.\n' >&2
        exit 64
      fi
      nested_file_name="$2"
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
    --leave-network-disabled)
      leave_network_disabled=1
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

validate_plain_name() {
  local label="$1"
  local value="$2"
  local is_required="$3"

  if [[ -z "${value//[[:space:]]/}" ]]; then
    if [[ "$is_required" -eq 1 ]]; then
      printf '%s must not be blank.\n' "$label" >&2
      exit 64
    fi

    return
  fi

  if [[ "$value" == *"/"* ]]; then
    printf '%s must not contain a slash.\n' "$label" >&2
    exit 64
  fi
}

validate_plain_name "Folder name" "$folder_name" 1
validate_plain_name "Nested folder name" "$nested_folder_name" 0
validate_plain_name "Offline file name" "$offline_file_name" 0
validate_plain_name "Nested file name" "$nested_file_name" 0

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
  evidence_dir="$evidence_root/$timestamp-offline-cache-auto"
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
  capture_text "$prefix-connectivity.txt" adb_device shell dumpsys connectivity

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

verify_expected_version() {
  if [[ -n "$expected_version_code" ]] \
    && ! grep -Fq "versionCode=$expected_version_code" "$evidence_dir/03-package-version.txt"; then
    printf 'Installed versionCode does not match expected value %s.\n' "$expected_version_code" >&2
    exit 67
  fi

  if [[ -n "$expected_version_name" ]] \
    && ! grep -Fq "versionName=$expected_version_name" "$evidence_dir/03-package-version.txt"; then
    printf 'Installed versionName does not match expected value %s.\n' "$expected_version_name" >&2
    exit 67
  fi
}

restore_network() {
  if [[ "$network_disabled" -eq 1 && "$leave_network_disabled" -eq 0 ]]; then
    adb_device shell svc wifi enable >/dev/null 2>&1 || true
    adb_device shell svc data enable >/dev/null 2>&1 || true
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

  adb_device shell run-as "$package_id" cat "$app_path" > "$local_path"
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

  capture_text "13-selected-download-dir.txt" \
    adb_device shell run-as "$package_id" find "$download_dir" -maxdepth 1 -type f

  if ! adb_device shell run-as "$package_id" test -f "$download_path"; then
    printf 'Selected offline file is missing from app-private downloads: %s\n' "$download_path" >&2
    printf 'Evidence: %s/13-selected-download-dir.txt\n' "$evidence_dir" >&2
    exit 66
  fi

  local actual_size
  actual_size="$(adb_device shell run-as "$package_id" stat -c %s "$download_path" | tr -d '\r\n')"
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

trap restore_network EXIT

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
capture_text "03-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
verify_expected_version

adb_device shell svc wifi enable >/dev/null 2>&1 || true
adb_device shell svc data enable >/dev/null 2>&1 || true
sleep 3
capture_text "04-connectivity-online.txt" adb_device shell dumpsys connectivity

adb_device logcat -c >/dev/null 2>&1 || true
adb_device shell am force-stop "$package_id" >/dev/null 2>&1 || true
adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/05-launch-online.txt"
sleep 5

waited_xml=""
wait_for_text "20-online-root" "Files"
online_root_xml="$waited_xml"
require_xml_text "$online_root_xml" "$folder_name" "Selected folder is not visible in online root Files."

pull_app_file "files/CottonFolderListings/$instance_key/root.json" "$evidence_dir/10-root-cache.json"
pull_app_file "files/CottonOfflineFiles/$instance_key/offline-files.json" "$evidence_dir/11-offline-files.json"
capture_text "12-download-files.txt" \
  adb_device shell run-as "$package_id" find "files/CottonDownloads/$instance_key" -maxdepth 2 -type f
select_smoke_targets
write_metadata
validate_local_file_bytes
require_xml_text "$online_root_xml" "$selected_file_name" "Selected offline file is not visible in online root Files."
require_xml_text "$online_root_xml" "On device" "Online root does not show any On device marker."

tap_node_from_xml "$online_root_xml" "$folder_name"
sleep 5
capture_screen "25-online-folder"
require_xml_text "$evidence_dir/25-online-folder.xml" "Files / $folder_name" \
  "Online folder navigation did not open the selected folder."
online_folder_xml="$evidence_dir/25-online-folder.xml"

folder_cache_name="${folder_id//-/}.json"
pull_app_file "files/CottonFolderListings/$instance_key/$folder_cache_name" "$evidence_dir/30-folder-cache.json"
validate_folder_cache > "$evidence_dir/31-folder-cache-summary.json"

if [[ -n "${nested_folder_name//[[:space:]]/}" ]]; then
  select_nested_folder_target
  write_metadata
  require_xml_text "$online_folder_xml" "$nested_folder_name" \
    "Selected nested folder is not visible in the online parent folder."

  tap_node_from_xml "$online_folder_xml" "$nested_folder_name"
  sleep 5
  capture_screen "32-online-nested-folder"
  require_xml_text "$evidence_dir/32-online-nested-folder.xml" "Files /" \
    "Online nested folder navigation did not show a Files breadcrumb."
  require_xml_text "$evidence_dir/32-online-nested-folder.xml" "$nested_folder_name" \
    "Online nested folder navigation did not open the selected child folder."

  nested_folder_cache_name="${nested_folder_id//-/}.json"
  pull_app_file \
    "files/CottonFolderListings/$instance_key/$nested_folder_cache_name" \
    "$evidence_dir/33-nested-folder-cache.json"
  validate_nested_folder_cache > "$evidence_dir/34-nested-folder-cache-summary.json"

  tap_node_from_xml "$evidence_dir/32-online-nested-folder.xml" "Up"
  wait_for_text "34-online-parent-return" "$folder_name"
  online_folder_xml="$waited_xml"
  require_xml_text "$online_folder_xml" "$nested_folder_name" \
    "Online up navigation did not return to the parent folder."
fi

tap_node_from_xml "$online_folder_xml" "Up"
wait_for_text "35-online-root-return" "$selected_file_name"
online_root_return_xml="$waited_xml"
require_xml_text "$online_root_return_xml" "$folder_name" "Online up navigation did not return to root."

capture_text "39-network-before-offline.txt" adb_device shell dumpsys connectivity
adb_device shell svc wifi disable >/dev/null 2>&1 || true
adb_device shell svc data disable >/dev/null 2>&1 || true
network_disabled=1
sleep 4
capture_text "40-network-disabled.txt" adb_device shell dumpsys connectivity

adb_device shell am force-stop "$package_id" >/dev/null 2>&1 || true
adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/41-launch-offline.txt"
sleep 6

wait_for_text "45-offline-root" "Offline"
offline_root_xml="$waited_xml"
require_xml_text "$offline_root_xml" "Files" "Offline root did not show Files."
require_xml_text "$offline_root_xml" "$folder_name" "Offline root did not show the selected cached folder."
require_xml_text "$offline_root_xml" "$selected_file_name" "Offline root did not show the selected on-device file."
require_xml_text "$offline_root_xml" "On device" "Offline root did not show an On device marker."

tap_node_from_xml "$offline_root_xml" "$folder_name"
sleep 4
capture_screen "50-offline-folder"
require_xml_text "$evidence_dir/50-offline-folder.xml" "Files / $folder_name" \
  "Offline folder navigation did not open cached folder."
require_xml_text "$evidence_dir/50-offline-folder.xml" "Saved folder list cached" "Offline folder did not show cached-listing notice."
require_xml_text "$evidence_dir/50-offline-folder.xml" "Files marked On device can still open" \
  "Offline folder did not show on-device-open guidance."
offline_folder_xml="$evidence_dir/50-offline-folder.xml"

if [[ -n "${nested_folder_name//[[:space:]]/}" ]]; then
  require_xml_text "$offline_folder_xml" "$nested_folder_name" \
    "Selected nested folder is not visible in the offline parent folder."
  tap_node_from_xml "$offline_folder_xml" "$nested_folder_name"
  sleep 4
  capture_screen "52-offline-nested-folder"
  require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "Files /" \
    "Offline nested folder navigation did not show a Files breadcrumb."
  require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "$nested_folder_name" \
    "Offline nested folder navigation did not open the cached child folder."
  require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "Saved folder list cached" \
    "Offline nested folder did not show cached-listing notice."
  require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "Files marked On device can still open" \
    "Offline nested folder did not show on-device-open guidance."
  if [[ -n "${nested_file_name//[[:space:]]/}" ]]; then
    require_xml_text "$evidence_dir/52-offline-nested-folder.xml" "$nested_file_name" \
      "Expected nested file is not visible in the offline child folder."
  fi

  tap_node_from_xml "$evidence_dir/52-offline-nested-folder.xml" "Up"
  wait_for_text "54-offline-parent-return" "$folder_name"
  offline_folder_xml="$waited_xml"
  require_xml_text "$offline_folder_xml" "$nested_folder_name" \
    "Offline up navigation did not return to the parent cached folder."
fi

tap_node_from_xml "$offline_folder_xml" "Up"
wait_for_text "55-offline-root-return" "$selected_file_name"
offline_root_return_xml="$waited_xml"
require_xml_text "$offline_root_return_xml" "$folder_name" \
  "Offline up navigation did not return to the root file list."

tap_node_from_xml "$offline_root_return_xml" "$selected_file_name"
sleep 5
capture_screen "60-offline-file-open"
require_xml_text "$evidence_dir/60-offline-file-open.xml" "$selected_file_name" \
  "Offline local file did not open in the app viewer."
require_xml_text "$evidence_dir/60-offline-file-open.xml" "Open" \
  "Offline local file viewer did not expose the external open action."

capture_text "90-logcat-raw.txt" adb_device logcat -d -v time
grep -E 'Cotton|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true

restore_network
sleep 5
capture_text "92-connectivity-after-restore.txt" adb_device shell dumpsys connectivity

{
  printf 'Offline cache smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Folder: %s\n' "$folder_name"
  if [[ -n "${nested_folder_name//[[:space:]]/}" ]]; then
    printf 'Nested folder: %s\n' "$nested_folder_name"
  fi
  printf 'Offline file: %s\n' "$selected_file_name"
  if [[ -n "${nested_file_name//[[:space:]]/}" ]]; then
    printf 'Nested file: %s\n' "$nested_file_name"
  fi
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
