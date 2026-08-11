open_target_actions() {
  cotton_tap_clickable_from_xml "$target_xml" "Actions for $target_name" exact
  sleep 1
  cotton_capture_screen "40-file-actions"
  cotton_require_xml_text "$evidence_dir/40-file-actions.xml" "$target_name" "Target action sheet did not open."
  cotton_require_xml_text "$evidence_dir/40-file-actions.xml" "Move to trash" "Target action sheet did not expose Move to trash."
}

confirm_move_to_trash() {
  local confirm_title="Move to trash?"
  local confirm_message="$target_name will be removed"

  if [[ "$target_kind" == "folder" ]]; then
    confirm_title="Move folder to trash?"
    confirm_message="$target_name and its contents will be removed"
  fi

  cotton_tap_clickable_from_xml "$evidence_dir/40-file-actions.xml" "Move to trash" exact
  sleep 1
  cotton_capture_screen "50-trash-confirm"
  cotton_require_xml_text "$evidence_dir/50-trash-confirm.xml" "$confirm_title" "Move-to-trash confirmation did not open."
  cotton_require_xml_text "$evidence_dir/50-trash-confirm.xml" "$confirm_message" "Move-to-trash confirmation did not name the target $target_kind."
  cotton_tap_clickable_from_xml "$evidence_dir/50-trash-confirm.xml" "Move to trash" exact
}

cancel_after_timeout() {
  local xml_file="$1"
  local prefix="$2"

  if [[ "$cancel_on_timeout" -eq 1 ]] && cotton_xml_has_text "$xml_file" "Cancel"; then
    cotton_tap_clickable_from_xml "$xml_file" "Cancel" exact
    sleep 1
    cotton_capture_screen "$prefix-cancelled"
  fi
}

wait_for_trash_follow_up() {
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

    if cotton_xml_has_text "$xml_file" "$target_name moved to trash." \
      && cotton_xml_has_text "$xml_file" "Restore"; then
      cp "$xml_file" "$evidence_dir/60-after-trash.xml"
      if [[ -f "$evidence_dir/60-after-trash-$attempt.png" ]]; then
        cp "$evidence_dir/60-after-trash-$attempt.png" "$evidence_dir/60-after-trash.png"
      fi
      trash_xml="$xml_file"
      return
    fi

    if cotton_xml_has_text "$xml_file" "Could not move file to trash." \
      || cotton_xml_has_text "$xml_file" "Could not move folder to trash." \
      || cotton_xml_has_text "$xml_file" "Offline. Move to trash needs internet." \
      || cotton_xml_has_text "$xml_file" "Move to trash is taking longer than expected. Refresh and try again." \
      || cotton_xml_has_text "$xml_file" "Move to trash cancelled."; then
      printf 'Move to trash did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "60-after-trash-timeout"
  printf 'Timed out waiting for move-to-trash completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

open_trash_page() {
  cotton_tap_clickable_from_xml "$trash_xml" "Account" exact
  sleep 1
  cotton_capture_screen "65-account-actions"
  cotton_require_xml_text "$evidence_dir/65-account-actions.xml" "Trash" \
    "Account action sheet did not expose Trash."
  cotton_tap_clickable_from_xml "$evidence_dir/65-account-actions.xml" "Trash" exact
  sleep 2
  wait_for_trash_page_item
  verify_empty_trash_overflow_action
}

wait_for_trash_page_item() {
  wait_for_trash_page_items \
    "Trash page did not load successfully." \
    "Timed out waiting for the target row on Trash page." \
    "$target_name"
}

confirm_restore() {
  cotton_tap_clickable_from_xml "$trash_xml" "Restore" exact
  sleep 1
  cotton_capture_screen "70-restore-confirm"
  cotton_require_xml_text "$evidence_dir/70-restore-confirm.xml" "Restore item?" "Restore confirmation did not open."
  cotton_require_xml_text "$evidence_dir/70-restore-confirm.xml" "Restore $target_name" "Restore confirmation did not name the target $target_kind."
  cotton_tap_clickable_from_xml "$evidence_dir/70-restore-confirm.xml" "Restore" exact
}

confirm_trash_page_restore() {
  cotton_tap_row_action_from_xml "$trash_page_xml" "$target_name" "Restore"
  sleep 1
  cotton_capture_screen "70-restore-confirm"
  cotton_require_xml_text "$evidence_dir/70-restore-confirm.xml" "Restore item?" "Restore confirmation did not open."
  cotton_require_xml_text "$evidence_dir/70-restore-confirm.xml" "Restore $target_name" "Restore confirmation did not name the target $target_kind."
  cotton_tap_clickable_from_xml "$evidence_dir/70-restore-confirm.xml" "Restore" exact
}

wait_for_restore_completion() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    cotton_capture_screen "80-after-restore-$attempt"
    xml_file="$evidence_dir/80-after-restore-$attempt.xml"

    if cotton_xml_has_text "$xml_file" "$target_name restored." \
      || cotton_xml_has_text "$xml_file" "Actions for $target_name"; then
      cp "$xml_file" "$evidence_dir/80-after-restore.xml"
      if [[ -f "$evidence_dir/80-after-restore-$attempt.png" ]]; then
        cp "$evidence_dir/80-after-restore-$attempt.png" "$evidence_dir/80-after-restore.png"
      fi
      return
    fi

    if cotton_xml_has_text "$xml_file" "Could not restore item." \
      || cotton_xml_has_text "$xml_file" "Offline. Restore needs internet." \
      || cotton_xml_has_text "$xml_file" "Restore is taking longer than expected. Refresh and try again." \
      || cotton_xml_has_text "$xml_file" "Restore cancelled."; then
      printf 'Restore did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "80-after-restore-timeout"
  printf 'Timed out waiting for restore completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

confirm_trash_page_delete_forever() {
  cotton_tap_row_action_from_xml "$trash_page_xml" "$target_name" "Delete forever"
  sleep 1
  cotton_capture_screen "70-delete-forever-confirm"
  cotton_require_xml_text "$evidence_dir/70-delete-forever-confirm.xml" "Delete forever?" \
    "Delete-forever confirmation did not open."
  cotton_require_xml_text "$evidence_dir/70-delete-forever-confirm.xml" "Permanently delete $target_name" \
    "Delete-forever confirmation did not name the target $target_kind."
  cotton_require_xml_text "$evidence_dir/70-delete-forever-confirm.xml" "This cannot be undone." \
    "Delete-forever confirmation did not explain the permanent action."
  cotton_tap_clickable_from_xml "$evidence_dir/70-delete-forever-confirm.xml" "Delete forever" exact
}

wait_for_delete_forever_completion() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    cotton_capture_screen "80-after-delete-forever-$attempt"
    xml_file="$evidence_dir/80-after-delete-forever-$attempt.xml"

    if cotton_xml_has_text "$xml_file" "$target_name permanently deleted."; then
      cp "$xml_file" "$evidence_dir/80-after-delete-forever.xml"
      if [[ -f "$evidence_dir/80-after-delete-forever-$attempt.png" ]]; then
        cp "$evidence_dir/80-after-delete-forever-$attempt.png" "$evidence_dir/80-after-delete-forever.png"
      fi
      return
    fi

    if cotton_xml_has_text "$xml_file" "Could not permanently delete item." \
      || cotton_xml_has_text "$xml_file" "Offline. Delete forever needs internet." \
      || cotton_xml_has_text "$xml_file" "Refresh trash before permanently deleting this file." \
      || cotton_xml_has_text "$xml_file" "Delete forever cancelled."; then
      printf 'Delete forever did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  cancel_after_timeout "$xml_file" "80-after-delete-forever-timeout"
  printf 'Timed out waiting for delete-forever completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}
