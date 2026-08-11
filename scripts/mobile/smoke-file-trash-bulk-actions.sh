open_bulk_trash_page() {
  cotton_tap_clickable_from_xml "$trash_xml" "Account" exact
  sleep 1
  cotton_capture_screen "65-account-actions"
  cotton_require_xml_text "$evidence_dir/65-account-actions.xml" "Trash" \
    "Account action sheet did not expose Trash."
  cotton_tap_clickable_from_xml "$evidence_dir/65-account-actions.xml" "Trash" exact
  sleep 2
  wait_for_bulk_trash_page_items
  verify_empty_trash_overflow_action
}

wait_for_bulk_trash_page_items() {
  wait_for_trash_page_items \
    "Trash page did not load successfully after bulk move to trash." \
    "Timed out waiting for both bulk target rows on Trash page." \
    "$target_name" \
    "$bulk_second_name"
}

verify_empty_trash_overflow_action() {
  cotton_require_xml_text "$evidence_dir/66-trash-page.xml" "More" \
    "Trash page did not expose the toolbar overflow for Empty."
  cotton_tap_clickable_from_xml "$evidence_dir/66-trash-page.xml" "More" contains
  sleep 1
  cotton_capture_screen "66-trash-overflow"
  cotton_require_xml_text "$evidence_dir/66-trash-overflow.xml" "Empty" \
    "Trash page overflow did not expose Empty."
  cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
  sleep 1
}

select_bulk_trash_items() {
  cotton_tap_clickable_from_xml "$evidence_dir/66-trash-page.xml" "Select" exact
  sleep 1
  cotton_capture_screen "67-trash-bulk-select-mode"
  cotton_require_xml_text "$evidence_dir/67-trash-bulk-select-mode.xml" "Select trash items" \
    "Trash page selection mode did not open."
  cotton_require_xml_text "$evidence_dir/67-trash-bulk-select-mode.xml" "Tap items to select them." \
    "Trash page selection mode did not explain item selection."

  cotton_tap_row_from_xml "$evidence_dir/67-trash-bulk-select-mode.xml" "$target_name"
  sleep 1
  cotton_capture_screen "68-trash-bulk-first-selected"
  cotton_require_xml_text "$evidence_dir/68-trash-bulk-first-selected.xml" "1 selected" \
    "Primary Trash row did not become selected."

  cotton_tap_row_from_xml "$evidence_dir/68-trash-bulk-first-selected.xml" "$bulk_second_name"
  sleep 1
  cotton_capture_screen "69-trash-bulk-two-selected"
  cotton_require_xml_text "$evidence_dir/69-trash-bulk-two-selected.xml" "2 selected" \
    "Second Trash row did not join the selection."
  cotton_require_xml_text "$evidence_dir/69-trash-bulk-two-selected.xml" "Restore" \
    "Trash selection bar did not expose Restore."
  cotton_require_xml_text "$evidence_dir/69-trash-bulk-two-selected.xml" "Delete forever" \
    "Trash selection bar did not expose Delete forever."
}

restore_bulk_from_trash_page() {
  local selection_text

  select_bulk_trash_items

  cotton_tap_clickable_from_xml "$evidence_dir/69-trash-bulk-two-selected.xml" "Restore" exact
  sleep 1
  cotton_capture_screen "70-trash-bulk-restore-confirm"
  selection_text="$(format_bulk_selection_text)"
  cotton_require_xml_text "$evidence_dir/70-trash-bulk-restore-confirm.xml" "Restore selected items?" \
    "Trash bulk restore confirmation did not open."
  cotton_require_xml_text "$evidence_dir/70-trash-bulk-restore-confirm.xml" \
    "Restore 2 selected items to their original locations?" \
    "Trash bulk restore confirmation did not describe the selected item count."
  cotton_tap_clickable_from_xml "$evidence_dir/70-trash-bulk-restore-confirm.xml" "Restore" exact

  wait_for_bulk_trash_restore_completion "$selection_text"
}

wait_for_bulk_trash_restore_completion() {
  local selection_text="$1"
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    cotton_capture_screen "80-after-trash-bulk-restore-$attempt"
    xml_file="$evidence_dir/80-after-trash-bulk-restore-$attempt.xml"

    if cotton_xml_has_text "$xml_file" "2 selected items restored."; then
      cp "$xml_file" "$evidence_dir/80-after-trash-bulk-restore.xml"
      if [[ -f "$evidence_dir/80-after-trash-bulk-restore-$attempt.png" ]]; then
        cp "$evidence_dir/80-after-trash-bulk-restore-$attempt.png" \
          "$evidence_dir/80-after-trash-bulk-restore.png"
      fi
      return
    fi

    if cotton_xml_has_text "$xml_file" "Could not restore selected items." \
      || cotton_xml_has_text "$xml_file" "Offline. Restore needs internet." \
      || cotton_xml_has_text "$xml_file" "Selection action cancelled." \
      || cotton_xml_has_text "$xml_file" "0 of 2 selected items restored."; then
      printf 'Trash page bulk restore did not complete successfully for %s.\n' "$selection_text" >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  printf 'Timed out waiting for Trash page bulk restore completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

delete_bulk_forever_from_trash_page() {
  select_bulk_trash_items

  cotton_tap_clickable_from_xml "$evidence_dir/69-trash-bulk-two-selected.xml" "Delete forever" exact
  sleep 1
  cotton_capture_screen "70-trash-bulk-delete-forever-confirm"
  cotton_require_xml_text "$evidence_dir/70-trash-bulk-delete-forever-confirm.xml" "Delete selected forever?" \
    "Trash bulk delete-forever confirmation did not open."
  cotton_require_xml_text "$evidence_dir/70-trash-bulk-delete-forever-confirm.xml" \
    "Permanently delete 2 selected items? This cannot be undone." \
    "Trash bulk delete-forever confirmation did not describe the selected item count."
  cotton_tap_clickable_from_xml "$evidence_dir/70-trash-bulk-delete-forever-confirm.xml" "Delete forever" exact

  wait_for_bulk_trash_delete_forever_completion
}

wait_for_bulk_trash_delete_forever_completion() {
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    sleep 3
    cotton_capture_screen "80-after-trash-bulk-delete-forever-$attempt"
    xml_file="$evidence_dir/80-after-trash-bulk-delete-forever-$attempt.xml"

    if cotton_xml_has_text "$xml_file" "2 selected items deleted forever."; then
      cp "$xml_file" "$evidence_dir/80-after-trash-bulk-delete-forever.xml"
      if [[ -f "$evidence_dir/80-after-trash-bulk-delete-forever-$attempt.png" ]]; then
        cp "$evidence_dir/80-after-trash-bulk-delete-forever-$attempt.png" \
          "$evidence_dir/80-after-trash-bulk-delete-forever.png"
      fi
      return
    fi

    if cotton_xml_has_text "$xml_file" "Could not delete selected items." \
      || cotton_xml_has_text "$xml_file" "Offline. Delete forever needs internet." \
      || cotton_xml_has_text "$xml_file" "Selection action cancelled." \
      || cotton_xml_has_text "$xml_file" "0 of 2 selected items deleted forever."; then
      printf 'Trash page bulk delete-forever did not complete successfully.\n' >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    attempt=$((attempt + 1))
  done

  printf 'Timed out waiting for Trash page bulk delete-forever completion.\n' >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}
