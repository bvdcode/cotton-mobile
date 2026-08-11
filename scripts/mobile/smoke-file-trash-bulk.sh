bulk_file_count() {
  local count=0

  if [[ "$target_kind" == "file" ]]; then
    count=$((count + 1))
  fi

  if [[ "$bulk_second_kind" == "file" ]]; then
    count=$((count + 1))
  fi

  printf '%s\n' "$count"
}
bulk_folder_count() {
  local count=0

  if [[ "$target_kind" == "folder" ]]; then
    count=$((count + 1))
  fi

  if [[ "$bulk_second_kind" == "folder" ]]; then
    count=$((count + 1))
  fi

  printf '%s\n' "$count"
}

format_bulk_selection_text() {
  local file_count
  local folder_count
  local parts=()

  file_count="$(bulk_file_count)"
  folder_count="$(bulk_folder_count)"
  if [[ "$file_count" -gt 0 ]]; then
    if [[ "$file_count" -eq 1 ]]; then
      parts+=("1 file")
    else
      parts+=("$file_count files")
    fi
  fi

  if [[ "$folder_count" -gt 0 ]]; then
    if [[ "$folder_count" -eq 1 ]]; then
      parts+=("1 folder")
    else
      parts+=("$folder_count folders")
    fi
  fi

  if [[ "${#parts[@]}" -eq 1 ]]; then
    printf '%s\n' "${parts[0]}"
  else
    printf '%s and %s\n' "${parts[0]}" "${parts[1]}"
  fi
}

ensure_bulk_targets_visible() {
  cotton_capture_screen "30-bulk-targets-visible"
  bulk_target_xml="$evidence_dir/30-bulk-targets-visible.xml"

  cotton_require_xml_text "$bulk_target_xml" "Actions for $target_name" \
    "Primary bulk target $target_kind row is not visible in Files."
  cotton_require_xml_text "$bulk_target_xml" "Actions for $bulk_second_name" \
    "Second bulk target $bulk_second_kind row is not visible in Files."
}

select_bulk_targets() {
  cotton_long_press_row_from_xml "$bulk_target_xml" "$target_name"
  sleep 2
  cotton_capture_screen "35-bulk-first-selected"
  cotton_require_xml_text "$evidence_dir/35-bulk-first-selected.xml" "1 selected" \
    "Long press did not start bulk file selection."

  cotton_tap_row_from_xml "$evidence_dir/35-bulk-first-selected.xml" "$bulk_second_name"
  sleep 1
  cotton_capture_screen "36-bulk-two-selected"
  cotton_require_xml_text "$evidence_dir/36-bulk-two-selected.xml" "2 selected" \
    "Second bulk target did not join the selection."

  local file_count
  local folder_count
  file_count="$(bulk_file_count)"
  folder_count="$(bulk_folder_count)"
  if [[ "$file_count" -gt 0 ]]; then
    cotton_require_xml_text "$evidence_dir/36-bulk-two-selected.xml" \
      "$file_count file" \
      "Bulk selection detail did not show the expected file count."
  fi

  if [[ "$folder_count" -gt 0 ]]; then
    cotton_require_xml_text "$evidence_dir/36-bulk-two-selected.xml" \
      "$folder_count folder" \
      "Bulk selection detail did not show the expected folder count."
  fi

  bulk_selected_xml="$evidence_dir/36-bulk-two-selected.xml"
}

open_bulk_selection_actions() {
  cotton_tap_clickable_from_xml "$bulk_selected_xml" "Actions" exact
  sleep 1
  cotton_capture_screen "40-file-actions"
  cotton_require_xml_text "$evidence_dir/40-file-actions.xml" "2 selected" \
    "Bulk selection action sheet did not open."
  cotton_require_xml_text "$evidence_dir/40-file-actions.xml" "Move to trash" \
    "Bulk selection action sheet did not expose Move to trash."
}

confirm_bulk_move_to_trash() {
  local selection_text

  selection_text="$(format_bulk_selection_text)"
  cotton_tap_clickable_from_xml "$evidence_dir/40-file-actions.xml" "Move to trash" exact
  sleep 1
  cotton_capture_screen "50-trash-confirm"
  cotton_require_xml_text "$evidence_dir/50-trash-confirm.xml" "Move selection to trash?" \
    "Bulk move-to-trash confirmation did not open."
  cotton_require_xml_text "$evidence_dir/50-trash-confirm.xml" \
    "$selection_text will be removed from this folder and can be restored from trash." \
    "Bulk move-to-trash confirmation did not describe the selected item kinds."
  cotton_tap_clickable_from_xml "$evidence_dir/50-trash-confirm.xml" "Move to trash" exact
}

wait_for_bulk_trash_completion() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    cotton_capture_screen "60-after-trash-$attempt"
    xml_file="$evidence_dir/60-after-trash-$attempt.xml"

    if cotton_xml_has_text "$xml_file" "2 items moved to trash."; then
      cp "$xml_file" "$evidence_dir/60-after-trash.xml"
      if [[ -f "$evidence_dir/60-after-trash-$attempt.png" ]]; then
        cp "$evidence_dir/60-after-trash-$attempt.png" "$evidence_dir/60-after-trash.png"
      fi
      trash_xml="$xml_file"
      return
    fi

    if cotton_xml_has_text "$xml_file" "Could not move selection to trash." \
      || cotton_xml_has_text "$xml_file" "Refresh this folder before moving selected files to trash." \
      || cotton_xml_has_text "$xml_file" "Offline. Move to trash needs internet." \
      || cotton_xml_has_text "$xml_file" "Move to trash is taking longer than expected. Refresh and try again." \
      || cotton_xml_has_text "$xml_file" "Move to trash failed after" \
      || cotton_xml_has_text "$xml_file" "Move to trash cancelled"; then
      printf 'Bulk move to trash did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "60-after-trash-timeout"
  printf 'Timed out waiting for bulk move-to-trash completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}
