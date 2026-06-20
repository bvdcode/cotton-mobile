#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
run_id=""

protected_file_id="0f0f0f0f-0000-4000-8000-000000000001"
evictable_file_id="0f0f0f0f-0000-4000-8000-000000000002"
failed_transfer_id="0f0f0f0f-0000-4000-8000-000000000003"
completed_transfer_id="0f0f0f0f-0000-4000-8000-000000000004"
orphan_transfer_id="0f0f0f0f-0000-4000-8000-000000000005"

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an Android storage-cleanup smoke:
  1. Launches a signed-in app session.
  2. Seeds app-private thumbnails, folder listings, downloads, offline pins,
     and transfer staging for the selected instance scope.
  3. Opens Storage through the account action sheet.
  4. Runs Clear temp uploads and Free space.
  5. Verifies destructive cleanup keeps protected offline files and failed
     upload staging while removing evictable files.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --run-id ID               Stable run id for seeded file names.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --no-launch               Do not launch automatically before seeding.
  --help, -h                Show this help.

The app must already have a signed-in session for the selected instance.
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

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-storage-cleanup"
fi

mkdir -p "$evidence_dir"

protected_file_id_n="${protected_file_id//-/}"
evictable_file_id_n="${evictable_file_id//-/}"
failed_transfer_id_n="${failed_transfer_id//-/}"
completed_transfer_id_n="${completed_transfer_id//-/}"
orphan_transfer_id_n="${orphan_transfer_id//-/}"

thumbnail_name="storage-cleanup-smoke-$run_id.webp"
folder_listing_name="storage-cleanup-smoke-$run_id.json"
protected_file_name="protected-storage-cleanup-smoke-$run_id.txt"
evictable_file_name="evictable-storage-cleanup-smoke-$run_id.txt"
failed_upload_name="failed-storage-cleanup-smoke-$run_id.bin"
completed_upload_name="completed-storage-cleanup-smoke-$run_id.bin"
orphan_upload_name="orphan-storage-cleanup-smoke-$run_id.bin"

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
  local remote_xml="/sdcard/cotton-storage-cleanup-window.xml"

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
  local mode="${3:-contains}"
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

def matches(value: str) -> bool:
    return value == needle if mode == "exact" else needle in value

for node in root.iter("node"):
    values = (
        node.attrib.get("text", ""),
        node.attrib.get("content-desc", ""),
        node.attrib.get("hint", ""),
    )
    if any(matches(value) for value in values):
        print(*center(node.attrib["bounds"]))
        raise SystemExit(0)

raise SystemExit(f"Could not find UI node: {needle}")
PY

  read -r tap_x tap_y < "$point_file"
  adb_device shell input tap "$tap_x" "$tap_y"
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

wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="20-files-root-$attempt"
    capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if xml_has_text "$xml_file" "Files" && xml_has_text "$xml_file" "Account"; then
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

  printf 'Files root with Account navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
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

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'instance_key=%s\n' "$instance_key"
    printf 'run_id=%s\n' "$run_id"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'android_storage_docs=https://developer.android.com/training/data-storage/app-specific\n'
    printf 'maui_filesystem_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/file-system-helpers\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/00-metadata.txt"
}

pull_app_file_or_empty() {
  local app_path="$1"
  local local_path="$2"
  local empty_json="$3"

  if ! adb_device shell run-as "$package_id" cat "$app_path" > "$local_path" 2> "$local_path.err"; then
    printf '%s\n' "$empty_json" > "$local_path"
  fi
}

prepare_seed_files() {
  local seed_dir="$1"
  local now_utc="$2"
  local existing_queue="$seed_dir/existing-queue.json"
  local existing_offline="$seed_dir/existing-offline-files.json"

  printf 'thumbnail cleanup smoke %s\n' "$run_id" > "$seed_dir/$thumbnail_name"
  printf '{"schemaVersion":1,"folderId":"%s","entries":[]}\n' "$run_id" > "$seed_dir/$folder_listing_name"
  printf 'protected offline cleanup smoke %s\n' "$run_id" > "$seed_dir/$protected_file_name"
  printf 'evictable download cleanup smoke %s\n' "$run_id" > "$seed_dir/$evictable_file_name"
  printf 'failed upload staging cleanup smoke %s\n' "$run_id" > "$seed_dir/$failed_upload_name"
  printf 'completed upload staging cleanup smoke %s\n' "$run_id" > "$seed_dir/$completed_upload_name"
  printf 'orphan upload staging cleanup smoke %s\n' "$run_id" > "$seed_dir/$orphan_upload_name"

  protected_size="$(wc -c < "$seed_dir/$protected_file_name" | tr -d ' ')"
  evictable_size="$(wc -c < "$seed_dir/$evictable_file_name" | tr -d ' ')"
  failed_size="$(wc -c < "$seed_dir/$failed_upload_name" | tr -d ' ')"
  completed_size="$(wc -c < "$seed_dir/$completed_upload_name" | tr -d ' ')"

  pull_app_file_or_empty \
    "files/CottonTransfers/$instance_key/queue.json" \
    "$existing_queue" \
    '{"schemaVersion":1,"savedAtUtc":"2026-06-20T00:00:00Z","items":[]}'
  pull_app_file_or_empty \
    "files/CottonOfflineFiles/$instance_key/offline-files.json" \
    "$existing_offline" \
    '{"schemaVersion":1,"savedAtUtc":"2026-06-20T00:00:00Z","items":[]}'

  python3 - \
    "$existing_queue" \
    "$seed_dir/queue.json" \
    "$now_utc" \
    "$failed_transfer_id" \
    "$completed_transfer_id" \
    "$failed_upload_name" \
    "$completed_upload_name" \
    "$failed_size" \
    "$completed_size" <<'PY'
import json
import sys

(
    existing_queue_path,
    output_path,
    now_utc,
    failed_transfer_id,
    completed_transfer_id,
    failed_upload_name,
    completed_upload_name,
    failed_size,
    completed_size,
) = sys.argv[1:10]

try:
    data = json.load(open(existing_queue_path, encoding="utf-8"))
except json.JSONDecodeError:
    data = {}

smoke_ids = {failed_transfer_id, completed_transfer_id}
items = [
    item for item in data.get("items", [])
    if item.get("id") not in smoke_ids
]

items.extend([
    {
        "id": failed_transfer_id,
        "kind": 0,
        "displayName": failed_upload_name,
        "contentType": "application/octet-stream",
        "source": None,
        "destination": None,
        "status": 4,
        "transferredBytes": 0,
        "totalBytes": int(failed_size),
        "attemptCount": 1,
        "failureMessage": "Storage cleanup smoke keeps failed uploads.",
        "createdAtUtc": now_utc,
        "updatedAtUtc": now_utc,
    },
    {
        "id": completed_transfer_id,
        "kind": 0,
        "displayName": completed_upload_name,
        "contentType": "application/octet-stream",
        "source": None,
        "destination": None,
        "status": 3,
        "transferredBytes": int(completed_size),
        "totalBytes": int(completed_size),
        "attemptCount": 1,
        "failureMessage": None,
        "createdAtUtc": now_utc,
        "updatedAtUtc": now_utc,
    },
])

with open(output_path, "w", encoding="utf-8") as handle:
    json.dump(
        {
            "schemaVersion": 1,
            "savedAtUtc": now_utc,
            "items": items,
        },
        handle,
        indent=2,
    )
    handle.write("\n")
PY

  python3 - \
    "$existing_offline" \
    "$seed_dir/offline-files.json" \
    "$now_utc" \
    "$protected_file_id" \
    "$protected_file_name" \
    "$protected_size" <<'PY'
import json
import sys

(
    existing_offline_path,
    output_path,
    now_utc,
    protected_file_id,
    protected_file_name,
    protected_size,
) = sys.argv[1:7]

try:
    data = json.load(open(existing_offline_path, encoding="utf-8"))
except json.JSONDecodeError:
    data = {}

items = [
    item for item in data.get("items", [])
    if item.get("fileId") != protected_file_id
]
items.append(
    {
        "fileId": protected_file_id,
        "fileName": protected_file_name,
        "pinnedAtUtc": now_utc,
        "remoteUpdatedAtUtc": now_utc,
        "sizeBytes": int(protected_size),
        "contentType": "text/plain",
    }
)

with open(output_path, "w", encoding="utf-8") as handle:
    json.dump(
        {
            "schemaVersion": 1,
            "savedAtUtc": now_utc,
            "items": items,
        },
        handle,
        indent=2,
    )
    handle.write("\n")
PY
}

seed_storage_data() {
  local seed_dir="$1"
  local remote_seed_dir="/data/local/tmp/cotton-storage-cleanup-smoke-$run_id"
  local transfer_root="files/CottonTransfers/$instance_key"
  local staged_root="$transfer_root/Staged"

  adb_device shell rm -rf "$remote_seed_dir"
  adb_device shell mkdir -p "$remote_seed_dir"
  adb_device push "$seed_dir/queue.json" "$remote_seed_dir/queue.json" > "$evidence_dir/10-push-queue.txt"
  adb_device push "$seed_dir/offline-files.json" "$remote_seed_dir/offline-files.json" > "$evidence_dir/11-push-offline.txt"
  adb_device push "$seed_dir/$thumbnail_name" "$remote_seed_dir/$thumbnail_name" > "$evidence_dir/12-push-thumbnail.txt"
  adb_device push "$seed_dir/$folder_listing_name" "$remote_seed_dir/$folder_listing_name" > "$evidence_dir/13-push-folder-listing.txt"
  adb_device push "$seed_dir/$protected_file_name" "$remote_seed_dir/$protected_file_name" > "$evidence_dir/14-push-protected.txt"
  adb_device push "$seed_dir/$evictable_file_name" "$remote_seed_dir/$evictable_file_name" > "$evidence_dir/15-push-evictable.txt"
  adb_device push "$seed_dir/$failed_upload_name" "$remote_seed_dir/$failed_upload_name" > "$evidence_dir/16-push-failed-upload.txt"
  adb_device push "$seed_dir/$completed_upload_name" "$remote_seed_dir/$completed_upload_name" > "$evidence_dir/17-push-completed-upload.txt"
  adb_device push "$seed_dir/$orphan_upload_name" "$remote_seed_dir/$orphan_upload_name" > "$evidence_dir/18-push-orphan-upload.txt"

  adb_device shell run-as "$package_id" rm -rf \
    "files/CottonDownloads/$instance_key/$protected_file_id" \
    "files/CottonDownloads/$instance_key/$evictable_file_id" \
    "$staged_root/$failed_transfer_id_n" \
    "$staged_root/$completed_transfer_id_n" \
    "$staged_root/$orphan_transfer_id_n"

  adb_device shell run-as "$package_id" mkdir -p \
    "files/ThumbnailCache" \
    "files/CottonFolderListings/$instance_key" \
    "files/CottonDownloads/$instance_key/$protected_file_id" \
    "files/CottonDownloads/$instance_key/$evictable_file_id" \
    "files/CottonOfflineFiles/$instance_key" \
    "$transfer_root" \
    "$staged_root/$failed_transfer_id_n" \
    "$staged_root/$completed_transfer_id_n" \
    "$staged_root/$orphan_transfer_id_n"

  adb_device shell run-as "$package_id" cp "$remote_seed_dir/queue.json" "$transfer_root/queue.json"
  adb_device shell run-as "$package_id" cp "$remote_seed_dir/offline-files.json" \
    "files/CottonOfflineFiles/$instance_key/offline-files.json"
  adb_device shell run-as "$package_id" cp "$remote_seed_dir/$thumbnail_name" "files/ThumbnailCache/$thumbnail_name"
  adb_device shell run-as "$package_id" cp "$remote_seed_dir/$folder_listing_name" \
    "files/CottonFolderListings/$instance_key/$folder_listing_name"
  adb_device shell run-as "$package_id" cp "$remote_seed_dir/$protected_file_name" \
    "files/CottonDownloads/$instance_key/$protected_file_id/$protected_file_name"
  adb_device shell run-as "$package_id" cp "$remote_seed_dir/$evictable_file_name" \
    "files/CottonDownloads/$instance_key/$evictable_file_id/$evictable_file_name"
  adb_device shell run-as "$package_id" cp "$remote_seed_dir/$failed_upload_name" \
    "$staged_root/$failed_transfer_id_n/$failed_upload_name"
  adb_device shell run-as "$package_id" cp "$remote_seed_dir/$completed_upload_name" \
    "$staged_root/$completed_transfer_id_n/$completed_upload_name"
  adb_device shell run-as "$package_id" cp "$remote_seed_dir/$orphan_upload_name" \
    "$staged_root/$orphan_transfer_id_n/$orphan_upload_name"
  adb_device shell rm -rf "$remote_seed_dir"
}

capture_storage_state() {
  local prefix="$1"
  local transfer_root="files/CottonTransfers/$instance_key"

  {
    adb_device shell run-as "$package_id" find "files/ThumbnailCache" -maxdepth 1 -type f 2>/dev/null || true
    adb_device shell run-as "$package_id" find "files/CottonFolderListings/$instance_key" -maxdepth 2 -type f 2>/dev/null || true
    adb_device shell run-as "$package_id" find "files/CottonDownloads/$instance_key" -maxdepth 3 -type f 2>/dev/null || true
    adb_device shell run-as "$package_id" find "$transfer_root" -maxdepth 3 -type f 2>/dev/null || true
    adb_device shell run-as "$package_id" find "files/CottonOfflineFiles/$instance_key" -maxdepth 1 -type f 2>/dev/null || true
  } | sort > "$evidence_dir/$prefix-files.txt"

  adb_device shell run-as "$package_id" cat "$transfer_root/queue.json" \
    > "$evidence_dir/$prefix-queue.json" 2>/dev/null || true
  adb_device shell run-as "$package_id" cat "files/CottonOfflineFiles/$instance_key/offline-files.json" \
    > "$evidence_dir/$prefix-offline-files.json" 2>/dev/null || true
}

require_app_file() {
  local app_path="$1"
  local message="$2"

  if ! adb_device shell run-as "$package_id" test -f "$app_path"; then
    printf '%s\n' "$message" >&2
    printf 'Missing app file: %s\n' "$app_path" >&2
    printf 'Evidence: %s\n' "$evidence_dir" >&2
    exit 66
  fi
}

require_app_missing() {
  local app_path="$1"
  local message="$2"

  if adb_device shell run-as "$package_id" test -f "$app_path"; then
    printf '%s\n' "$message" >&2
    printf 'Unexpected app file: %s\n' "$app_path" >&2
    printf 'Evidence: %s\n' "$evidence_dir" >&2
    exit 66
  fi
}

validate_seeded_state() {
  require_app_file "files/ThumbnailCache/$thumbnail_name" "Seeded thumbnail cache file is missing."
  require_app_file "files/CottonFolderListings/$instance_key/$folder_listing_name" \
    "Seeded folder-listing cache file is missing."
  require_app_file "files/CottonDownloads/$instance_key/$protected_file_id/$protected_file_name" \
    "Seeded protected offline file is missing."
  require_app_file "files/CottonDownloads/$instance_key/$evictable_file_id/$evictable_file_name" \
    "Seeded evictable download file is missing."
  require_app_file "files/CottonOfflineFiles/$instance_key/offline-files.json" \
    "Seeded offline metadata is missing."
  require_app_file "files/CottonTransfers/$instance_key/Staged/$failed_transfer_id_n/$failed_upload_name" \
    "Seeded failed upload staging is missing."
  require_app_file "files/CottonTransfers/$instance_key/Staged/$completed_transfer_id_n/$completed_upload_name" \
    "Seeded completed upload staging is missing."
  require_app_file "files/CottonTransfers/$instance_key/Staged/$orphan_transfer_id_n/$orphan_upload_name" \
    "Seeded orphan upload staging is missing."
}

validate_temp_cleanup_state() {
  require_app_file "files/CottonTransfers/$instance_key/Staged/$failed_transfer_id_n/$failed_upload_name" \
    "Failed upload staging should survive Clear temp uploads."
  require_app_missing "files/CottonTransfers/$instance_key/Staged/$completed_transfer_id_n/$completed_upload_name" \
    "Completed upload staging should be removed by Clear temp uploads."
  require_app_missing "files/CottonTransfers/$instance_key/Staged/$orphan_transfer_id_n/$orphan_upload_name" \
    "Orphan upload staging should be removed by Clear temp uploads."
}

validate_free_space_state() {
  require_app_missing "files/ThumbnailCache/$thumbnail_name" \
    "Thumbnail cache file should be removed by Free space."
  require_app_missing "files/CottonFolderListings/$instance_key/$folder_listing_name" \
    "Folder-listing cache file should be removed by Free space."
  require_app_missing "files/CottonDownloads/$instance_key/$evictable_file_id/$evictable_file_name" \
    "Evictable download should be removed by Free space."
  require_app_file "files/CottonDownloads/$instance_key/$protected_file_id/$protected_file_name" \
    "Protected offline file should survive Free space."
  require_app_file "files/CottonOfflineFiles/$instance_key/offline-files.json" \
    "Offline metadata should survive Free space."
  require_app_file "files/CottonTransfers/$instance_key/Staged/$failed_transfer_id_n/$failed_upload_name" \
    "Failed upload staging should survive Free space."

  if ! adb_device shell run-as "$package_id" cat "files/CottonOfflineFiles/$instance_key/offline-files.json" \
      | grep -Fq "$protected_file_id"; then
    printf 'Offline metadata no longer contains the protected file pin.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir" >&2
    exit 66
  fi
}

open_storage_page() {
  tap_node_from_xml "$files_root_xml" "Account" exact
  sleep 2
  capture_screen "30-account-actions"
  require_xml_text "$evidence_dir/30-account-actions.xml" "Storage" \
    "Account action sheet did not expose Storage."
  tap_node_from_xml "$evidence_dir/30-account-actions.xml" "Storage" exact
  sleep 4
  wait_for_text "40-storage" "Cleanup"
  storage_xml="$waited_xml"
  require_xml_text "$storage_xml" "Free space" "Storage page did not expose Free space."
  require_xml_text "$storage_xml" "Clear temp uploads" "Storage page did not expose Clear temp uploads."
}

run_clear_temp_uploads() {
  tap_node_from_xml "$storage_xml" "Clear temp uploads" exact
  sleep 2
  capture_screen "50-clear-temp-confirm"
  require_xml_text "$evidence_dir/50-clear-temp-confirm.xml" "Clear temporary upload files" \
    "Clear temp uploads confirmation did not appear."
  tap_node_from_xml "$evidence_dir/50-clear-temp-confirm.xml" "Clear temp uploads" exact
  sleep 4
  wait_for_text "60-clear-temp-result" "temporary upload"
  temp_result_xml="$waited_xml"
  require_xml_text "$temp_result_xml" "cleared" "Clear temp uploads did not report a cleared result."
}

run_free_space() {
  tap_node_from_xml "$temp_result_xml" "Free space" exact
  sleep 2
  capture_screen "70-free-space-confirm"
  require_xml_text "$evidence_dir/70-free-space-confirm.xml" "Free device space" \
    "Free space confirmation did not appear."
  tap_node_from_xml "$evidence_dir/70-free-space-confirm.xml" "Free space" exact
  sleep 4
  wait_for_text "80-free-space-result" "Freed"
  free_space_result_xml="$waited_xml"
  require_xml_text "$free_space_result_xml" "Cotton file" "Free space did not report freed Cotton files."
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

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c >/dev/null 2>&1 || true
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/04-launch.txt"
  sleep 4
fi

wait_for_files_root

local_seed_dir="$(mktemp -d "${TMPDIR:-/tmp}/cotton-storage-cleanup.XXXXXX")"
trap 'rm -rf "$local_seed_dir"' EXIT
prepare_seed_files "$local_seed_dir" "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
seed_storage_data "$local_seed_dir"
capture_storage_state "25-after-seed"
validate_seeded_state

open_storage_page
capture_storage_state "45-storage-opened"

run_clear_temp_uploads
capture_storage_state "65-after-clear-temp"
validate_temp_cleanup_state

run_free_space
capture_storage_state "85-after-free-space"
validate_free_space_state

capture_text "90-logcat-raw.txt" adb_device logcat -d -v time
grep -E 'Cotton|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true
if grep -E 'FATAL EXCEPTION|mono-rt' "$evidence_dir/91-logcat-cotton.txt" > "$evidence_dir/92-fatal-markers.txt"; then
  printf 'Fatal log markers were found during storage cleanup smoke.\n' >&2
  printf 'Evidence: %s/92-fatal-markers.txt\n' "$evidence_dir" >&2
  exit 66
fi

{
  printf 'Storage cleanup smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Instance key: %s\n' "$instance_key"
  printf 'Protected file: %s\n' "$protected_file_id"
  printf 'Evictable file: %s\n' "$evictable_file_id"
  printf 'Failed transfer: %s\n' "$failed_transfer_id"
  printf 'Completed transfer: %s\n' "$completed_transfer_id"
  printf 'Orphan transfer: %s\n' "$orphan_transfer_id"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
