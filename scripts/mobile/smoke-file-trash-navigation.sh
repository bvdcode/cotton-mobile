trash_wait_for_files_root() {
  local attempt
  local attempt_limit
  local xml_file

  attempt_limit=$(( (wait_seconds + 2) / 3 ))
  for attempt in $(seq 0 "$((attempt_limit - 1))"); do
    cotton_capture_screen "20-files-root-$attempt"
    xml_file="$evidence_dir/20-files-root-$attempt.xml"
    if cotton_xml_has_text "$xml_file" "Files" \
      && cotton_xml_has_text "$xml_file" "Account" \
      && cotton_xml_has_text "$xml_file" "Add files"; then
      cp "$xml_file" "$evidence_dir/20-files-root-ready.xml"
      if [[ -f "$evidence_dir/20-files-root-$attempt.png" ]]; then
        cp "$evidence_dir/20-files-root-$attempt.png" "$evidence_dir/20-files-root-ready.png"
      fi
      files_root_xml="$xml_file"
      return
    fi

    sleep 3
  done

  printf 'Files root with signed-in chrome is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

create_disposable_target_folder() {
  create_disposable_folder_named \
    "$target_name" \
    "25-add-actions" \
    "26-new-folder-prompt" \
    "27-new-folder-filled" \
    "28-created-folder"
}

create_bulk_second_disposable_target_folder() {
  create_disposable_folder_named \
    "$bulk_second_name" \
    "29-bulk-second-add-actions" \
    "29-bulk-second-new-folder-prompt" \
    "29-bulk-second-new-folder-filled" \
    "29-created-bulk-second-folder"
}

create_disposable_folder_named() {
  local folder_name="$1"
  local add_actions_prefix="$2"
  local prompt_prefix="$3"
  local filled_prefix="$4"
  local created_prefix="$5"

  cotton_tap_clickable_from_xml "$files_root_xml" "Add files" exact
  sleep 1
  cotton_capture_screen "$add_actions_prefix"
  cotton_require_xml_text "$evidence_dir/$add_actions_prefix.xml" "New folder" \
    "Add action sheet did not expose New folder."

  cotton_tap_clickable_from_xml "$evidence_dir/$add_actions_prefix.xml" "New folder" exact
  sleep 1
  cotton_capture_screen "$prompt_prefix"
  cotton_require_xml_text "$evidence_dir/$prompt_prefix.xml" "New folder" "New-folder prompt did not open."
  cotton_require_xml_text "$evidence_dir/$prompt_prefix.xml" "Folder name" \
    "New-folder prompt did not show the folder-name field."

  cotton_tap_editable_from_xml "$evidence_dir/$prompt_prefix.xml"
  cotton_adb_input_text "$folder_name"
  sleep 1
  cotton_capture_screen "$filled_prefix"
  cotton_tap_clickable_from_xml "$evidence_dir/$filled_prefix.xml" "Create" exact
  wait_for_created_folder "$folder_name" "$created_prefix"
}

wait_for_created_folder() {
  local folder_name="$1"
  local created_prefix="$2"
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    cotton_capture_screen "$created_prefix-$attempt"
    xml_file="$evidence_dir/$created_prefix-$attempt.xml"

    if cotton_xml_has_text "$xml_file" "Actions for $folder_name"; then
      cp "$xml_file" "$evidence_dir/$created_prefix.xml"
      if [[ -f "$evidence_dir/$created_prefix-$attempt.png" ]]; then
        cp "$evidence_dir/$created_prefix-$attempt.png" "$evidence_dir/$created_prefix.png"
      fi
      files_root_xml="$xml_file"
      return
    fi

    if cotton_xml_has_text "$xml_file" "An item with that name already exists." \
      || cotton_xml_has_text "$xml_file" "Could not create folder." \
      || cotton_xml_has_text "$xml_file" "Offline. New folder needs internet." \
      || cotton_xml_has_text "$xml_file" "New folder cancelled."; then
      printf 'Disposable folder creation did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "$created_prefix-timeout"
  printf 'Timed out waiting for disposable folder creation.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

ensure_target_visible() {
  local xml_file="$files_root_xml"

  if ! cotton_xml_has_text "$xml_file" "$target_name"; then
    cotton_tap_clickable_from_xml "$xml_file" "Search files" exact
    sleep 1
    cotton_capture_screen "30-search-open"
    cotton_adb_input_text "$target_name"
    sleep 2
    cotton_capture_screen "30-target-visible"
    xml_file="$evidence_dir/30-target-visible.xml"
  else
    cotton_capture_screen "30-target-visible"
    xml_file="$evidence_dir/30-target-visible.xml"
  fi

  cotton_require_xml_text "$xml_file" "Actions for $target_name" "Target $target_kind row is not visible in Files."
  target_xml="$xml_file"
}
