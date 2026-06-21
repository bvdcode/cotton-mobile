#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
account_scope_key="user:sync-settings-smoke"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
leave_seed=0
run_id=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an Android Sync settings smoke:
  1. Backs up current app-private sync root metadata for the selected instance.
  2. Seeds one ready cloud-to-device root and one paused bidirectional root.
  3. Opens Files -> Sync and verifies the Sync settings page chrome/cards.
  4. Restores the previous sync root metadata unless --leave-seed is used.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Instance URI used for app-private metadata scope.
  --account-scope KEY       Account scope key to write into seeded roots.
  --run-id ID               Stable run id for seeded ids.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --no-launch               Do not launch automatically.
  --leave-seed              Leave seeded sync metadata in app data.
  --help, -h                Show this help.

The app must already have a signed-in session for the selected instance.
This seeded smoke requires a debuggable package because it uses adb run-as.
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
    --account-scope)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --account-scope.\n' >&2
        exit 64
      fi
      account_scope_key="$2"
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
    --leave-seed)
      leave_seed=1
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
if [[ -z "$run_id" ]]; then
  run_id="$timestamp"
fi

if [[ ! "$run_id" =~ ^[A-Za-z0-9._-]+$ ]]; then
  printf 'Run id must contain only letters, digits, dot, underscore, or hyphen.\n' >&2
  exit 64
fi

if [[ -z "${account_scope_key//[[:space:]]/}" ]]; then
  printf 'Account scope key is required.\n' >&2
  exit 64
fi

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-sync-settings"
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if ! command -v python3 >/dev/null 2>&1; then
  printf 'python3 was not found.\n' >&2
  exit 127
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
  local remote_xml="/sdcard/cotton-sync-settings-window.xml"

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

create_sync_seed() {
  local seed_dir="$1"
  local output_roots="$seed_dir/sync-roots.json"
  local output_paused="$seed_dir/paused-sync-roots.json"

  python3 - \
    "$instance_uri" \
    "$account_scope_key" \
    "$run_id" \
    "$output_roots" \
    "$output_paused" \
    "$seed_dir/seed.env" <<'PY'
import hashlib
import json
import sys
import uuid
from datetime import datetime, timezone
from urllib.parse import quote, urlsplit, urlunsplit

instance_uri, account_scope, run_id, roots_path, paused_path, env_path = sys.argv[1:7]

def normalize_instance(value: str) -> str:
    parsed = urlsplit(value.strip())
    if parsed.scheme.lower() != "https" or parsed.hostname is None:
        raise SystemExit("Instance URI must be an absolute HTTPS URL.")
    if parsed.username or parsed.password or parsed.query or parsed.fragment:
        raise SystemExit("Instance URI must not include user info, query, or fragment.")

    scheme = parsed.scheme.lower()
    host = parsed.hostname.lower()
    port = parsed.port
    default_port = port in (None, 443)
    authority = host if default_port else f"{host}:{port}"
    path = parsed.path.rstrip("/") or "/"
    return urlunsplit((scheme, authority, path, "", ""))

def create_instance_key(normalized_uri: str) -> str:
    parsed = urlsplit(normalized_uri)
    authority = parsed.hostname or ""
    if parsed.port not in (None, 443):
        authority = f"{authority}:{parsed.port}"
    path = "" if parsed.path in ("", "/") else parsed.path.rstrip("/")
    return hashlib.sha256(f"{parsed.scheme}://{authority}{path}".encode("utf-8")).hexdigest()

def create_stable_key(
    normalized_uri: str,
    cloud_folder_id: uuid.UUID,
    storage_kind_name: str,
    local_root_key: str,
) -> str:
    source = "|".join(
        (
            normalized_uri,
            account_scope.strip(),
            cloud_folder_id.hex,
            storage_kind_name,
            local_root_key,
        )
    )
    return hashlib.sha256(source.encode("utf-8")).hexdigest()

def create_id(label: str) -> uuid.UUID:
    return uuid.uuid5(uuid.NAMESPACE_URL, f"cotton-sync-settings-smoke:{label}:{run_id}")

normalized = normalize_instance(instance_uri)
instance_key = create_instance_key(normalized)
now = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")

active_root_id = create_id("active-root")
active_folder_id = create_id("active-folder")
active_local_key = f"sync-settings-smoke-downloads-{run_id}"
active_stable_key = create_stable_key(
    normalized,
    active_folder_id,
    "AppPrivateDirectory",
    active_local_key,
)

paused_root_id = create_id("paused-root")
paused_folder_id = create_id("paused-folder")
paused_local_key = (
    "content://com.android.externalstorage.documents/tree/"
    + quote(f"primary:CottonSyncSmoke/{run_id}", safe="")
)
paused_stable_key = create_stable_key(
    normalized,
    paused_folder_id,
    "UserSelectedDocumentTree",
    paused_local_key,
)

roots = {
    "schemaVersion": 1,
    "savedAtUtc": now,
    "items": [
        {
            "id": str(active_root_id),
            "instanceUri": normalized,
            "accountScopeKey": account_scope.strip(),
            "cloudFolderId": str(active_folder_id),
            "cloudFolderName": "Smoke Downloads",
            "cloudFolderPath": "Files / Smoke Downloads",
            "localStorageKind": 0,
            "localRootKey": active_local_key,
            "localRootDisplayName": "On-device smoke root",
            "localPermissionStatus": 0,
            "direction": 0,
            "stableKey": active_stable_key,
        },
        {
            "id": str(paused_root_id),
            "instanceUri": normalized,
            "accountScopeKey": account_scope.strip(),
            "cloudFolderId": str(paused_folder_id),
            "cloudFolderName": "Smoke Paused",
            "cloudFolderPath": "Files / Smoke Paused",
            "localStorageKind": 1,
            "localRootKey": paused_local_key,
            "localRootDisplayName": "Selected smoke folder",
            "localPermissionStatus": 0,
            "direction": 2,
            "stableKey": paused_stable_key,
        },
    ],
}

paused = {
    "schemaVersion": 1,
    "savedAtUtc": now,
    "rootIds": [str(paused_root_id)],
}

with open(roots_path, "w", encoding="utf-8") as handle:
    json.dump(roots, handle, indent=2)
    handle.write("\n")

with open(paused_path, "w", encoding="utf-8") as handle:
    json.dump(paused, handle, indent=2)
    handle.write("\n")

with open(env_path, "w", encoding="utf-8") as handle:
    handle.write(f"normalized_instance={normalized}\n")
    handle.write(f"instance_key={instance_key}\n")
    handle.write(f"active_root_id={active_root_id}\n")
    handle.write(f"paused_root_id={paused_root_id}\n")
    handle.write(f"active_stable_key={active_stable_key}\n")
    handle.write(f"paused_stable_key={paused_stable_key}\n")
PY
}

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'normalized_instance=%s\n' "$normalized_instance"
    printf 'instance_key=%s\n' "$instance_key"
    printf 'account_scope_key=%s\n' "$account_scope_key"
    printf 'run_id=%s\n' "$run_id"
    printf 'active_root_id=%s\n' "$active_root_id"
    printf 'paused_root_id=%s\n' "$paused_root_id"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'leave_seed=%s\n' "$leave_seed"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_storage_docs=https://developer.android.com/training/data-storage/app-specific\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/00-metadata.txt"
}

backup_sync_data() {
  local seed_dir="$1"

  if adb_device shell run-as "$package_id" cat "$sync_roots_path" \
    > "$sync_roots_backup_path" 2> "$seed_dir/existing-sync-roots.err"; then
    sync_roots_backup_exists=1
  else
    sync_roots_backup_exists=0
  fi

  if adb_device shell run-as "$package_id" cat "$paused_roots_path" \
    > "$paused_roots_backup_path" 2> "$seed_dir/existing-paused-sync-roots.err"; then
    paused_roots_backup_exists=1
  else
    paused_roots_backup_exists=0
  fi
}

seed_sync_data() {
  local seed_dir="$1"
  local remote_seed_dir="/data/local/tmp/cotton-sync-settings-smoke-$run_id"

  adb_device shell rm -rf "$remote_seed_dir"
  adb_device shell mkdir -p "$remote_seed_dir"
  adb_device push "$seed_dir/sync-roots.json" "$remote_seed_dir/sync-roots.json" \
    > "$evidence_dir/10-push-sync-roots.txt"
  adb_device push "$seed_dir/paused-sync-roots.json" "$remote_seed_dir/paused-sync-roots.json" \
    > "$evidence_dir/11-push-paused-sync-roots.txt"

  seeded_sync_data=1
  adb_device shell run-as "$package_id" mkdir -p "$sync_metadata_directory"
  adb_device shell run-as "$package_id" cp \
    "$remote_seed_dir/sync-roots.json" \
    "$sync_roots_path"
  adb_device shell run-as "$package_id" cp \
    "$remote_seed_dir/paused-sync-roots.json" \
    "$paused_roots_path"
  adb_device shell rm -rf "$remote_seed_dir"
}

restore_one_metadata_file() {
  local backup_exists="$1"
  local backup_path="$2"
  local app_path="$3"
  local restore_name="$4"
  local remote_restore_dir="$5"

  if [[ "$backup_exists" -eq 1 && -f "$backup_path" ]]; then
    adb_device push "$backup_path" "$remote_restore_dir/$restore_name" \
      > "$evidence_dir/98-restore-$restore_name-push.txt" 2>&1 || true
    adb_device shell run-as "$package_id" mkdir -p "$sync_metadata_directory" >/dev/null 2>&1 || true
    adb_device shell run-as "$package_id" cp \
      "$remote_restore_dir/$restore_name" \
      "$app_path" >/dev/null 2>&1 || true
  else
    adb_device shell run-as "$package_id" rm -f "$app_path" >/dev/null 2>&1 || true
  fi
}

restore_sync_data() {
  if [[ "${seeded_sync_data:-0}" -ne 1 || "$leave_seed" -eq 1 ]]; then
    return
  fi

  local remote_restore_dir="/data/local/tmp/cotton-sync-settings-restore-$run_id"
  adb_device shell rm -rf "$remote_restore_dir" >/dev/null 2>&1 || true
  adb_device shell mkdir -p "$remote_restore_dir" >/dev/null 2>&1 || true

  restore_one_metadata_file \
    "$sync_roots_backup_exists" \
    "$sync_roots_backup_path" \
    "$sync_roots_path" \
    "sync-roots.json" \
    "$remote_restore_dir"
  restore_one_metadata_file \
    "$paused_roots_backup_exists" \
    "$paused_roots_backup_path" \
    "$paused_roots_path" \
    "paused-sync-roots.json" \
    "$remote_restore_dir"

  adb_device shell rm -rf "$remote_restore_dir" >/dev/null 2>&1 || true
}

wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="20-files-root-$attempt"
    capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if xml_has_text "$xml_file" "Files" \
      && xml_has_text "$xml_file" "Sync" \
      && xml_has_text "$xml_file" "More"; then
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

  printf 'Files root with Sync navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

wait_for_sync_settings() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="30-sync-settings-$attempt"
    capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if xml_has_text "$xml_file" "Folders" \
      && xml_has_text "$xml_file" "Smoke Downloads"; then
      sync_settings_xml="$xml_file"
      return
    fi

    sleep 2
  done

  printf 'Sync settings page did not show seeded sync roots.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

capture_scrolled_sync_settings() {
  adb_device shell input swipe 500 1600 500 700 300 >/dev/null 2>&1 || true
  sleep 1
  capture_screen "40-sync-settings-scrolled"
  sync_settings_scrolled_xml="$evidence_dir/40-sync-settings-scrolled.xml"
}

verify_sync_settings() {
  require_xml_text "$sync_settings_xml" "Folders" \
    "Sync settings page header is not visible."
  require_xml_text "$sync_settings_xml" "2 folders set to sync" \
    "Seeded sync-root summary is not visible."
  require_xml_text "$sync_settings_xml" "Smoke Downloads" \
    "Ready seeded sync root is not visible."
  require_xml_text "$sync_settings_xml" "Files / Smoke Downloads" \
    "Ready seeded sync root path is not visible."
  require_xml_text "$sync_settings_xml" "Cloud to device" \
    "Ready seeded sync root direction is not visible."
  require_xml_text "$sync_settings_xml" "On-device smoke root" \
    "Ready seeded sync root local label is not visible."
  require_xml_text "$sync_settings_xml" "Sync root ready" \
    "Ready seeded sync root status is not visible."
  require_xml_text "$sync_settings_xml" "Run now" \
    "Sync settings did not expose Run now for the ready root."
  require_xml_text "$sync_settings_xml" "Pause" \
    "Sync settings did not expose Pause for the ready root."
  require_xml_text "$sync_settings_xml" "Stop syncing" \
    "Sync settings did not expose Stop syncing."

  if ! xml_has_text "$sync_settings_xml" "Smoke Paused"; then
    capture_scrolled_sync_settings
  else
    sync_settings_scrolled_xml="$sync_settings_xml"
  fi

  require_xml_text "$sync_settings_scrolled_xml" "Smoke Paused" \
    "Paused seeded sync root is not visible."
  require_xml_text "$sync_settings_scrolled_xml" "Files / Smoke Paused" \
    "Paused seeded sync root path is not visible."
  require_xml_text "$sync_settings_scrolled_xml" "Bidirectional" \
    "Paused seeded sync root direction is not visible."
  require_xml_text "$sync_settings_scrolled_xml" "Selected smoke folder" \
    "Paused seeded sync root local label is not visible."
  require_xml_text "$sync_settings_scrolled_xml" "Paused" \
    "Paused seeded sync root status is not visible."
  require_xml_text "$sync_settings_scrolled_xml" "Resume" \
    "Sync settings did not expose Resume for the paused root."
}

capture_final_state() {
  capture_screen "90-final"
  capture_text "91-logcat.txt" adb_device logcat -d -t 400
  if grep -Ei 'FATAL EXCEPTION|AndroidRuntime.*FATAL|SIGSEGV|libc.*Fatal signal|mono-rt.*SIG' \
      "$evidence_dir/91-logcat.txt" \
      > "$evidence_dir/92-fatal-logcat.txt"; then
    printf 'Fatal runtime marker found in logcat.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir/92-fatal-logcat.txt" >&2
    exit 66
  fi
}

seed_dir="$evidence_dir/seed"
mkdir -p "$seed_dir"
create_sync_seed "$seed_dir"
# shellcheck disable=SC1091
source "$seed_dir/seed.env"

sync_metadata_directory="files/CottonSyncRoots/$instance_key"
sync_roots_path="$sync_metadata_directory/sync-roots.json"
paused_roots_path="$sync_metadata_directory/paused-sync-roots.json"
sync_roots_backup_path="$evidence_dir/09-existing-sync-roots.json"
paused_roots_backup_path="$evidence_dir/09-existing-paused-sync-roots.json"
sync_roots_backup_exists=0
paused_roots_backup_exists=0
seeded_sync_data=0

trap restore_sync_data EXIT

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

backup_sync_data "$seed_dir"
seed_sync_data "$seed_dir"

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c >/dev/null 2>&1 || true
  adb_device shell am force-stop "$package_id" >/dev/null 2>&1 || true
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/04-launch.txt"
  sleep 4
fi

wait_for_files_root
tap_node_from_xml "$files_root_xml" "Sync" exact
sleep 2
wait_for_sync_settings
verify_sync_settings
capture_final_state

printf 'Sync settings smoke passed. Evidence: %s\n' "$evidence_dir"
