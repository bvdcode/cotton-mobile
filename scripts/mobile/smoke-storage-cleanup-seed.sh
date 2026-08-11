require_storage_quota_state() {
  local xml_file="$1"

  cotton_require_xml_text "$xml_file" "Account storage" "Storage page did not expose account storage."
  if grep -Eq '([0-9][^"]* used|No account quota reported\.|Account storage not checked\.|Storage limit unavailable\.)' "$xml_file"; then
    return
  fi

  printf 'Storage page did not show a recognized account storage state.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 66
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

  if ! cotton_adb shell run-as "$package_id" cat "$app_path" > "$local_path" 2> "$local_path.err"; then
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

  cotton_adb shell rm -rf "$remote_seed_dir"
  cotton_adb shell mkdir -p "$remote_seed_dir"
  cotton_adb push "$seed_dir/queue.json" "$remote_seed_dir/queue.json" > "$evidence_dir/10-push-queue.txt"
  cotton_adb push "$seed_dir/offline-files.json" "$remote_seed_dir/offline-files.json" > "$evidence_dir/11-push-offline.txt"
  cotton_adb push "$seed_dir/$thumbnail_name" "$remote_seed_dir/$thumbnail_name" > "$evidence_dir/12-push-thumbnail.txt"
  cotton_adb push "$seed_dir/$folder_listing_name" "$remote_seed_dir/$folder_listing_name" > "$evidence_dir/13-push-folder-listing.txt"
  cotton_adb push "$seed_dir/$protected_file_name" "$remote_seed_dir/$protected_file_name" > "$evidence_dir/14-push-protected.txt"
  cotton_adb push "$seed_dir/$evictable_file_name" "$remote_seed_dir/$evictable_file_name" > "$evidence_dir/15-push-evictable.txt"
  cotton_adb push "$seed_dir/$failed_upload_name" "$remote_seed_dir/$failed_upload_name" > "$evidence_dir/16-push-failed-upload.txt"
  cotton_adb push "$seed_dir/$completed_upload_name" "$remote_seed_dir/$completed_upload_name" > "$evidence_dir/17-push-completed-upload.txt"
  cotton_adb push "$seed_dir/$orphan_upload_name" "$remote_seed_dir/$orphan_upload_name" > "$evidence_dir/18-push-orphan-upload.txt"

  cotton_adb shell run-as "$package_id" rm -rf \
    "files/CottonDownloads/$instance_key/$protected_file_id" \
    "files/CottonDownloads/$instance_key/$evictable_file_id" \
    "$staged_root/$failed_transfer_id_n" \
    "$staged_root/$completed_transfer_id_n" \
    "$staged_root/$orphan_transfer_id_n"

  cotton_adb shell run-as "$package_id" mkdir -p \
    "files/ThumbnailCache" \
    "files/CottonFolderListings/$instance_key" \
    "files/CottonDownloads/$instance_key/$protected_file_id" \
    "files/CottonDownloads/$instance_key/$evictable_file_id" \
    "files/CottonOfflineFiles/$instance_key" \
    "$transfer_root" \
    "$staged_root/$failed_transfer_id_n" \
    "$staged_root/$completed_transfer_id_n" \
    "$staged_root/$orphan_transfer_id_n"

  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/queue.json" "$transfer_root/queue.json"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/offline-files.json" \
    "files/CottonOfflineFiles/$instance_key/offline-files.json"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/$thumbnail_name" "files/ThumbnailCache/$thumbnail_name"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/$folder_listing_name" \
    "files/CottonFolderListings/$instance_key/$folder_listing_name"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/$protected_file_name" \
    "files/CottonDownloads/$instance_key/$protected_file_id/$protected_file_name"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/$evictable_file_name" \
    "files/CottonDownloads/$instance_key/$evictable_file_id/$evictable_file_name"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/$failed_upload_name" \
    "$staged_root/$failed_transfer_id_n/$failed_upload_name"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/$completed_upload_name" \
    "$staged_root/$completed_transfer_id_n/$completed_upload_name"
  cotton_adb shell run-as "$package_id" cp "$remote_seed_dir/$orphan_upload_name" \
    "$staged_root/$orphan_transfer_id_n/$orphan_upload_name"
  cotton_adb shell rm -rf "$remote_seed_dir"
}
