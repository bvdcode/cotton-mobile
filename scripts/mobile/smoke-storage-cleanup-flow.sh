capture_storage_state() {
  local prefix="$1"
  local transfer_root="files/CottonTransfers/$instance_key"

  {
    cotton_adb shell run-as "$package_id" find "files/ThumbnailCache" -maxdepth 1 -type f 2>/dev/null || true
    cotton_adb shell run-as "$package_id" find "files/CottonFolderListings/$instance_key" -maxdepth 2 -type f 2>/dev/null || true
    cotton_adb shell run-as "$package_id" find "files/CottonDownloads/$instance_key" -maxdepth 3 -type f 2>/dev/null || true
    cotton_adb shell run-as "$package_id" find "$transfer_root" -maxdepth 3 -type f 2>/dev/null || true
    cotton_adb shell run-as "$package_id" find "files/CottonOfflineFiles/$instance_key" -maxdepth 1 -type f 2>/dev/null || true
  } | sort > "$evidence_dir/$prefix-files.txt"

  cotton_adb shell run-as "$package_id" cat "$transfer_root/queue.json" \
    > "$evidence_dir/$prefix-queue.json" 2>/dev/null || true
  cotton_adb shell run-as "$package_id" cat "files/CottonOfflineFiles/$instance_key/offline-files.json" \
    > "$evidence_dir/$prefix-offline-files.json" 2>/dev/null || true
}

require_app_file() {
  local app_path="$1"
  local message="$2"

  if ! cotton_adb shell run-as "$package_id" test -f "$app_path"; then
    printf '%s\n' "$message" >&2
    printf 'Missing app file: %s\n' "$app_path" >&2
    printf 'Evidence: %s\n' "$evidence_dir" >&2
    exit 66
  fi
}

require_app_missing() {
  local app_path="$1"
  local message="$2"

  if cotton_adb shell run-as "$package_id" test -f "$app_path"; then
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

  if ! cotton_adb shell run-as "$package_id" cat "files/CottonOfflineFiles/$instance_key/offline-files.json" \
      | grep -Fq "$protected_file_id"; then
    printf 'Offline metadata no longer contains the protected file pin.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir" >&2
    exit 66
  fi
}

open_storage_page() {
  cotton_tap_node_from_xml "$files_root_xml" "Account" exact
  sleep 2
  cotton_capture_screen "30-account-actions"
  cotton_require_xml_text "$evidence_dir/30-account-actions.xml" "Storage" \
    "Account action sheet did not expose Storage."
  cotton_tap_node_from_xml "$evidence_dir/30-account-actions.xml" "Storage" exact
  sleep 4
  cotton_wait_for_text "40-storage" "Cleanup"
  storage_xml="$waited_xml"
  require_storage_quota_state "$storage_xml"
  cotton_require_xml_text "$storage_xml" "Free space" "Storage page did not expose Free space."
  cotton_require_xml_text "$storage_xml" "Clear temp uploads" "Storage page did not expose Clear temp uploads."
}

run_clear_temp_uploads() {
  cotton_tap_node_from_xml "$storage_xml" "Clear temp uploads" exact
  sleep 2
  cotton_capture_screen "50-clear-temp-confirm"
  cotton_require_xml_text "$evidence_dir/50-clear-temp-confirm.xml" "Clear temporary upload files" \
    "Clear temp uploads confirmation did not appear."
  cotton_tap_node_from_xml "$evidence_dir/50-clear-temp-confirm.xml" "Clear temp uploads" exact
  sleep 4
  cotton_wait_for_text "60-clear-temp-result" "temporary upload"
  temp_result_xml="$waited_xml"
  cotton_require_xml_text "$temp_result_xml" "cleared" "Clear temp uploads did not report a cleared result."
}

run_free_space() {
  cotton_tap_node_from_xml "$temp_result_xml" "Free space" exact
  sleep 2
  cotton_capture_screen "70-free-space-confirm"
  cotton_require_xml_text "$evidence_dir/70-free-space-confirm.xml" "Free device space" \
    "Free space confirmation did not appear."
  cotton_tap_node_from_xml "$evidence_dir/70-free-space-confirm.xml" "Free space" exact
  sleep 4
  cotton_wait_for_text "80-free-space-result" "Freed"
  free_space_result_xml="$waited_xml"
  cotton_require_xml_text "$free_space_result_xml" "Cotton file" "Free space did not report freed Cotton files."
}
