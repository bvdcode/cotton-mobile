#!/usr/bin/env bash

create_sync_seed() {
  local seed_dir="$1"

  python3 - \
    "$instance_uri" \
    "$account_scope_key" \
    "$run_id" \
    "$seed_dir/sync-roots.json" \
    "$seed_dir/paused-sync-roots.json" \
    "$seed_dir/seed-data.json" <<'PY'
import hashlib
import json
import sys
import uuid
from datetime import datetime, timezone
from urllib.parse import quote, urlsplit, urlunsplit

instance_uri, account_scope, run_id, roots_path, paused_path, data_path = sys.argv[1:7]

def normalize_instance(value: str) -> str:
    parsed = urlsplit(value.strip())
    if parsed.scheme.lower() != "https" or parsed.hostname is None:
        raise SystemExit("Instance URI must be an absolute HTTPS URL.")
    if parsed.username or parsed.password or parsed.query or parsed.fragment:
        raise SystemExit("Instance URI must not include user info, query, or fragment.")

    authority = parsed.hostname.lower()
    if parsed.port not in (None, 443):
        authority = f"{authority}:{parsed.port}"
    return urlunsplit(("https", authority, parsed.path.rstrip("/") or "/", "", ""))

def create_instance_key(normalized_uri: str) -> str:
    parsed = urlsplit(normalized_uri)
    authority = parsed.hostname or ""
    if parsed.port not in (None, 443):
        authority = f"{authority}:{parsed.port}"
    path = "" if parsed.path in ("", "/") else parsed.path.rstrip("/")
    return hashlib.sha256(f"https://{authority}{path}".encode()).hexdigest()

def create_stable_key(
    normalized_uri: str,
    cloud_folder_id: uuid.UUID,
    storage_kind_name: str,
    local_root_key: str,
) -> str:
    source = "|".join((
        normalized_uri,
        account_scope.strip(),
        cloud_folder_id.hex,
        storage_kind_name,
        local_root_key,
    ))
    return hashlib.sha256(source.encode()).hexdigest()

def create_id(label: str) -> uuid.UUID:
    return uuid.uuid5(uuid.NAMESPACE_URL, f"cotton-sync-dashboard-smoke:{label}:{run_id}")

normalized = normalize_instance(instance_uri)
instance_key = create_instance_key(normalized)
now = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
active_root_id = create_id("active-root")
active_folder_id = create_id("active-folder")
active_local_key = f"sync-dashboard-smoke-downloads-{run_id}"
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
            "uploadOriginalRetention": 0,
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
            "uploadOriginalRetention": 0,
            "stableKey": paused_stable_key,
        },
    ],
}
paused = {
    "schemaVersion": 1,
    "savedAtUtc": now,
    "rootIds": [str(paused_root_id)],
}
seed_data = {
    "normalizedInstance": normalized,
    "instanceKey": instance_key,
    "activeRootId": str(active_root_id),
    "pausedRootId": str(paused_root_id),
}

for path, value in ((roots_path, roots), (paused_path, paused), (data_path, seed_data)):
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(value, handle, indent=2)
        handle.write("\n")
PY
}

load_sync_seed() {
  local seed_data_path="$1"
  local -a seed_values

  mapfile -d '' -t seed_values < <(python3 - "$seed_data_path" <<'PY'
import json
import sys

with open(sys.argv[1], encoding="utf-8") as handle:
    data = json.load(handle)

for key in ("normalizedInstance", "instanceKey", "activeRootId", "pausedRootId"):
    value = data[key]
    if not isinstance(value, str) or not value:
        raise SystemExit(f"Invalid seed value: {key}")
    sys.stdout.write(value)
    sys.stdout.write("\0")
PY
  )

  if [[ "${#seed_values[@]}" -ne 4 ]]; then
    printf 'Could not read generated sync seed metadata.\n' >&2
    exit "$COTTON_EXIT_EVIDENCE"
  fi

  normalized_instance="${seed_values[0]}"
  instance_key="${seed_values[1]}"
  active_root_id="${seed_values[2]}"
  paused_root_id="${seed_values[3]}"
}

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$normalized_instance"
    printf 'instance_key=%s\n' "$instance_key"
    printf 'run_id=%s\n' "$run_id"
    printf 'active_root_id=%s\n' "$active_root_id"
    printf 'paused_root_id=%s\n' "$paused_root_id"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
  } > "$evidence_dir/00-metadata.txt"
}

backup_sync_data() {
  local seed_dir="$1"

  if cotton_adb shell run-as "$package_id" cat "$sync_roots_path" \
    > "$sync_roots_backup_path" 2> "$seed_dir/existing-sync-roots.err"; then
    sync_roots_backup_exists=1
  fi
  if cotton_adb shell run-as "$package_id" cat "$paused_roots_path" \
    > "$paused_roots_backup_path" 2> "$seed_dir/existing-paused-sync-roots.err"; then
    paused_roots_backup_exists=1
  fi
}

seed_sync_data() {
  local seed_dir="$1"
  local remote_seed_dir="/data/local/tmp/cotton-sync-dashboard-smoke-$run_id"

  cotton_adb shell rm -rf "$remote_seed_dir"
  cotton_adb shell mkdir -p "$remote_seed_dir"
  cotton_adb push "$seed_dir/sync-roots.json" "$remote_seed_dir/sync-roots.json" \
    > "$evidence_dir/10-push-sync-roots.txt"
  cotton_adb push "$seed_dir/paused-sync-roots.json" "$remote_seed_dir/paused-sync-roots.json" \
    > "$evidence_dir/11-push-paused-sync-roots.txt"
  seeded_sync_data=1
  cotton_adb shell run-as "$package_id" mkdir -p "$sync_metadata_directory"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/sync-roots.json" "$sync_roots_path"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/paused-sync-roots.json" "$paused_roots_path"
  cotton_adb shell rm -rf "$remote_seed_dir"
}

restore_one_metadata_file() {
  local backup_exists="$1"
  local backup_path="$2"
  local app_path="$3"
  local restore_name="$4"
  local remote_restore_dir="$5"

  if [[ "$backup_exists" -eq 1 && -f "$backup_path" ]]; then
    cotton_adb push "$backup_path" "$remote_restore_dir/$restore_name" >/dev/null 2>&1 || true
    cotton_adb shell run-as "$package_id" mkdir -p "$sync_metadata_directory" >/dev/null 2>&1 || true
    cotton_adb shell run-as "$package_id" cp \
      "$remote_restore_dir/$restore_name" "$app_path" >/dev/null 2>&1 || true
  else
    cotton_adb shell run-as "$package_id" rm -f "$app_path" >/dev/null 2>&1 || true
  fi
}

restore_sync_data() {
  if [[ "${seeded_sync_data:-0}" -ne 1 || "$leave_seed" -eq 1 ]]; then
    return
  fi

  local remote_restore_dir="/data/local/tmp/cotton-sync-dashboard-restore-$run_id"
  cotton_adb shell rm -rf "$remote_restore_dir" >/dev/null 2>&1 || true
  cotton_adb shell mkdir -p "$remote_restore_dir" >/dev/null 2>&1 || true
  restore_one_metadata_file \
    "$sync_roots_backup_exists" "$sync_roots_backup_path" "$sync_roots_path" \
    "sync-roots.json" "$remote_restore_dir"
  restore_one_metadata_file \
    "$paused_roots_backup_exists" "$paused_roots_backup_path" "$paused_roots_path" \
    "paused-sync-roots.json" "$remote_restore_dir"
  cotton_adb shell rm -rf "$remote_restore_dir" >/dev/null 2>&1 || true
}
