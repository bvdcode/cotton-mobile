xml_has_selection_banner() {
  local xml_file="$1"

  [[ -f "$xml_file" ]] && grep -Eq 'text="[0-9]+ selected"' "$xml_file"
}

is_cotton_focused() {
  local window_file="$1"
  grep -Fq "$package_id/" "$window_file"
}

cotton_wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="20-files-root-$attempt"
    cotton_capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if cotton_xml_has_text "$xml_file" "Files" \
      && cotton_xml_has_text "$xml_file" "Account" \
      && ! xml_has_selection_banner "$xml_file"; then
      files_root_xml="$xml_file"
      return
    fi

    if xml_has_selection_banner "$xml_file" && cotton_xml_has_text "$xml_file" "Cancel"; then
      cotton_tap_node_from_xml "$xml_file" "Cancel" exact
      sleep 2
      continue
    fi

    cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 1
    cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/20-relaunch-$attempt.txt" || true
    sleep 2
  done

  printf 'Files root with Account navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'repo=%s\n' "$COTTON_REPO_ROOT"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'first_file=%s\n' "$first_file"
    printf 'second_file=%s\n' "$second_file"
    printf 'mixed_folder=%s\n' "$mixed_folder"
    printf 'maui_share_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/data/share\n'
    printf 'maui_action_sheet_docs=https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups\n'
    printf 'maui_share_request_docs=https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.applicationmodel.datatransfer.share.requestasync\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Selection Local Share Smoke

Package: \`$package_id\`
Device: \`$serial\`
Files: \`$first_file\`, \`$second_file\`

## Preconditions

- [ ] Signed-in session is restored without clearing app data.
- [ ] Files root shows both selected file rows.

## Selection And Local State

- [ ] \`30-two-selected.xml\` shows \`2 selected\` and \`2 files\`.
- [ ] If the files were not local, \`40-actions-before-local.xml\` omits \`Share files\`.
- [ ] \`50-after-download.xml\` shows both files and \`On device\`.

## Share Files

- [ ] \`80-share-files-sheet.xml\` shows \`Share files\`.
- [ ] \`90-share-handoff-*.txt\` or \`90-share-handoff-*.xml\` shows Android system share UI handoff.
- [ ] \`99-logcat.txt\` has no ANR/FATAL markers.

## Optional Mixed Selection

- [ ] If \`mixed_folder\` is set, \`66-mixed-actions.xml\` shows \`Download file\`, \`Keep offline\`, \`Remove offline\`, and \`Share file\` for the selected file/folder pair.
EOF
}

select_two_files() {
  local prefix="$1"

  cotton_capture_screen "$prefix-root"
  cotton_require_xml_text "$evidence_dir/$prefix-root.xml" "$first_file" "First selected file is not visible in Files."
  cotton_require_xml_text "$evidence_dir/$prefix-root.xml" "$second_file" "Second selected file is not visible in Files."

  cotton_long_press_row_from_xml "$evidence_dir/$prefix-root.xml" "$first_file"
  sleep 2
  cotton_capture_screen "$prefix-first-selected"
  cotton_require_xml_text "$evidence_dir/$prefix-first-selected.xml" "1 selected" "Long press did not start file selection."

  cotton_tap_row_from_xml "$evidence_dir/$prefix-first-selected.xml" "$second_file"
  sleep 1
  cotton_capture_screen "$prefix-two-selected"
  cotton_require_xml_text "$evidence_dir/$prefix-two-selected.xml" "2 selected" "Second file did not join the selection."
  cotton_require_xml_text "$evidence_dir/$prefix-two-selected.xml" "2 files" "Selection detail did not show two files."

  selected_xml="$evidence_dir/$prefix-two-selected.xml"
}

open_selection_actions() {
  local prefix="$1"
  local download_label="${2:-Download files}"

  cotton_tap_node_from_xml "$selected_xml" "Actions" exact
  sleep 1
  cotton_capture_screen "$prefix"
  cotton_require_xml_text "$evidence_dir/$prefix.xml" "2 selected" "Selection action sheet did not open."
  cotton_require_xml_text "$evidence_dir/$prefix.xml" "$download_label" "Selection action sheet did not expose Download."
  cotton_require_xml_text "$evidence_dir/$prefix.xml" "Keep offline" "Selection action sheet did not expose Keep offline."
  cotton_require_xml_text "$evidence_dir/$prefix.xml" "Move to trash" "Selection action sheet did not expose Move to trash."
  actions_xml="$evidence_dir/$prefix.xml"
}

wait_for_local_files() {
  local attempt
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7 8 9; do
    cotton_capture_screen "50-after-download-$attempt"
    xml_file="$evidence_dir/50-after-download-$attempt.xml"

    if cotton_xml_has_text "$xml_file" "$first_file" \
      && cotton_xml_has_text "$xml_file" "$second_file" \
      && cotton_xml_has_text "$xml_file" "On device"; then
      cp "$xml_file" "$evidence_dir/50-after-download.xml"
      if [[ -f "$evidence_dir/50-after-download-$attempt.png" ]]; then
        cp "$evidence_dir/50-after-download-$attempt.png" "$evidence_dir/50-after-download.png"
      fi
      return
    fi

    cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 2
  done

  printf 'Selected files did not return to Files with an On device marker.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

ensure_selected_files_local() {
  select_two_files "30"
  open_selection_actions "40-actions-before-local"

  if cotton_xml_has_text "$actions_xml" "Share files"; then
    cotton_tap_node_from_xml "$actions_xml" "Cancel" exact
    sleep 1
    cotton_wait_for_files_root
    return
  fi

  cotton_tap_node_from_xml "$actions_xml" "Download files" exact
  sleep 4
  wait_for_local_files
  cotton_wait_for_files_root
}

validate_mixed_selection_actions() {
  if [[ -z "${mixed_folder//[[:space:]]/}" ]]; then
    return
  fi

  cotton_capture_screen "65-mixed-root"
  cotton_require_xml_text "$evidence_dir/65-mixed-root.xml" "$first_file" "Mixed-selection file is not visible in Files."
  cotton_require_xml_text "$evidence_dir/65-mixed-root.xml" "$mixed_folder" "Mixed-selection folder is not visible in Files."

  cotton_long_press_row_from_xml "$evidence_dir/65-mixed-root.xml" "$first_file"
  sleep 2
  cotton_capture_screen "65-mixed-first-selected"
  cotton_require_xml_text "$evidence_dir/65-mixed-first-selected.xml" "1 selected" \
    "Long press did not start mixed file selection."

  cotton_tap_row_from_xml "$evidence_dir/65-mixed-first-selected.xml" "$mixed_folder"
  sleep 1
  cotton_capture_screen "65-mixed-two-selected"
  cotton_require_xml_text "$evidence_dir/65-mixed-two-selected.xml" "2 selected" \
    "Folder did not join the mixed selection."
  cotton_require_xml_text "$evidence_dir/65-mixed-two-selected.xml" "1 file" \
    "Mixed selection detail did not show one file."
  cotton_require_xml_text "$evidence_dir/65-mixed-two-selected.xml" "1 folder" \
    "Mixed selection detail did not show one folder."

  selected_xml="$evidence_dir/65-mixed-two-selected.xml"
  open_selection_actions "66-mixed-actions" "Download file"
  cotton_require_xml_text "$actions_xml" "Copy links" "Mixed selection action sheet did not expose Copy links."
  cotton_require_xml_text "$actions_xml" "Share links" "Mixed selection action sheet did not expose Share links."
  cotton_require_xml_text "$actions_xml" "Download file" "Mixed selection action sheet did not expose file-scoped Download file."
  cotton_require_xml_text "$actions_xml" "Keep offline" "Mixed selection action sheet did not expose Keep offline."
  cotton_require_xml_text "$actions_xml" "Remove offline" "Mixed selection action sheet did not expose file-scoped Remove offline."
  cotton_require_xml_text "$actions_xml" "Share file" "Mixed selection action sheet did not expose file-scoped Share file."
  cotton_require_xml_text "$actions_xml" "Move to trash" "Mixed selection action sheet did not expose Move to trash."

  cotton_tap_node_from_xml "$actions_xml" "Cancel" exact
  sleep 1
  cotton_wait_for_files_root
}

wait_for_share_handoff() {
  local attempt
  local prefix
  local xml_file
  local window_file

  for attempt in 0 1 2 3 4 5; do
    prefix="90-share-handoff-$attempt"
    cotton_capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"
    window_file="$evidence_dir/$prefix-window.txt"

    if [[ -f "$window_file" ]] && ! is_cotton_focused "$window_file"; then
      share_handoff="external-window"
      return
    fi

    if cotton_xml_has_text "$xml_file" "Share" \
      || cotton_xml_has_text "$xml_file" "Nearby" \
      || cotton_xml_has_text "$xml_file" "Complete action"; then
      share_handoff="system-share-ui"
      return
    fi

    sleep 2
  done

  printf 'Android share UI handoff was not observed after tapping Share files.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}
