#!/usr/bin/env bash

cotton_create_instance_key() {
  "$SCRIPT_DIR/smoke-support.py" instance-key "$instance_uri"
}

cotton_resolve_cached_destination() {
  local root_cache="$1"
  local destination_name="$2"
  local destination_tsv="$3"

  "$SCRIPT_DIR/smoke-support.py" cached-folder "$root_cache" "$destination_name" > "$destination_tsv"
  IFS=$'\t' read -r destination_id destination_folder_name < "$destination_tsv"
}

cotton_new_uuid() {
  "$SCRIPT_DIR/smoke-support.py" uuid
}

cotton_stage_queued_upload() {
  local adb_serial="$1"
  local android_package_id="$2"
  local remote_seed_dir="$3"
  local queue_json="$4"
  local upload_file="$5"
  local upload_name="$6"
  local instance_key="$7"
  local transfer_id_n="$8"
  local transfer_root="files/CottonTransfers/$instance_key"
  local staged_root="$transfer_root/Staged"

  adb -s "$adb_serial" shell am force-stop "$android_package_id" >/dev/null 2>&1 || true
  adb -s "$adb_serial" shell rm -rf "$remote_seed_dir"
  adb -s "$adb_serial" shell mkdir -p "$remote_seed_dir"
  adb -s "$adb_serial" push "$queue_json" "$remote_seed_dir/queue.json" >/dev/null
  adb -s "$adb_serial" push "$upload_file" "$remote_seed_dir/$upload_name" >/dev/null
  adb -s "$adb_serial" shell run-as "$android_package_id" rm -rf "$transfer_root"
  adb -s "$adb_serial" shell run-as "$android_package_id" mkdir -p "$staged_root/$transfer_id_n"
  adb -s "$adb_serial" shell run-as "$android_package_id" cp \
    "$remote_seed_dir/queue.json" \
    "$transfer_root/queue.json"
  adb -s "$adb_serial" shell run-as "$android_package_id" cp \
    "$remote_seed_dir/$upload_name" \
    "$staged_root/$transfer_id_n/$upload_name"
  adb -s "$adb_serial" shell rm -rf "$remote_seed_dir"
}
