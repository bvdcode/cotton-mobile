capture_failure_evidence() {
  local exit_code=$?

  if [[ "$exit_code" -ne 0 && -d "$evidence_dir" ]]; then
    cotton_capture_screen "98-failure" || true
    cotton_capture_text_best_effort "99-logcat.txt" cotton_adb logcat -d -v time || true
  fi

  exit "$exit_code"
}

wait_for_trash_page_items() {
  local failure_message="$1"
  local timeout_message="$2"
  shift 2
  local attempt_limit=$((wait_seconds / 3))
  local attempt=0
  local item_name
  local xml_file

  if [[ "$attempt_limit" -lt 1 ]]; then
    attempt_limit=1
  fi

  while [[ "$attempt" -le "$attempt_limit" ]]; do
    cotton_capture_screen "66-trash-page-$attempt"
    xml_file="$evidence_dir/66-trash-page-$attempt.xml"

    local is_ready=1
    for item_name in "$@"; do
      if ! cotton_xml_has_text "$xml_file" "$item_name"; then
        is_ready=0
        break
      fi
    done
    if [[ "$is_ready" -eq 1 ]] \
      && cotton_xml_has_text "$xml_file" "Trash" \
      && cotton_xml_has_text "$xml_file" "Search trash" \
      && cotton_xml_has_text "$xml_file" "Sort trash" \
      && cotton_xml_has_text "$xml_file" "Change trash view" \
      && cotton_xml_has_text "$xml_file" "Restore" \
      && cotton_xml_has_text "$xml_file" "Delete forever"; then
      cp "$xml_file" "$evidence_dir/66-trash-page.xml"
      if [[ -f "$evidence_dir/66-trash-page-$attempt.png" ]]; then
        cp "$evidence_dir/66-trash-page-$attempt.png" "$evidence_dir/66-trash-page.png"
      fi
      trash_page_xml="$xml_file"
      return
    fi

    if cotton_xml_has_text "$xml_file" "Could not load trash." \
      || cotton_xml_has_text "$xml_file" "Offline. Trash needs internet." \
      || cotton_xml_has_text "$xml_file" "Trash refresh cancelled."; then
      printf '%s\n' "$failure_message" >&2
      printf 'Evidence: %s\n' "$xml_file" >&2
      exit 68
    fi

    cotton_adb shell input swipe 540 1700 540 650 350 >/dev/null 2>&1 || true
    sleep 3
    attempt=$((attempt + 1))
  done

  printf '%s\n' "$timeout_message" >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 68
}

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'repo=%s\n' "$COTTON_REPO_ROOT"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'preflight_only=%s\n' "$preflight_only"
    printf 'wait_seconds=%s\n' "$wait_seconds"
    printf 'cancel_on_timeout=%s\n' "$cancel_on_timeout"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'create_disposable_folder=%s\n' "$create_disposable_folder"
    printf 'restore_from_trash_page=%s\n' "$restore_from_trash_page"
    printf 'delete_forever_from_trash_page=%s\n' "$delete_forever_from_trash_page"
    printf 'restore_bulk_from_trash_page=%s\n' "$restore_bulk_from_trash_page"
    printf 'delete_bulk_forever_from_trash_page=%s\n' "$delete_bulk_forever_from_trash_page"
    printf 'bulk_selection=%s\n' "$bulk_selection"
    printf 'create_bulk_second_disposable_folder=%s\n' "$create_bulk_second_disposable_folder"
    printf 'bulk_second_kind=%s\n' "$bulk_second_kind"
    printf 'bulk_second_name=%s\n' "$bulk_second_name"
    printf 'bulk_second_file=%s\n' "$bulk_second_file"
    printf 'bulk_second_folder=%s\n' "$bulk_second_folder"
    printf 'target_kind=%s\n' "$target_kind"
    printf 'target_name=%s\n' "$target_name"
    printf 'target_file=%s\n' "$target_file"
    printf 'target_folder=%s\n' "$target_folder"
    printf 'maui_popups_docs=https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups\n'
    printf 'maui_toolbar_docs=https://learn.microsoft.com/en-us/dotnet/maui/user-interface/toolbaritem\n'
    printf 'maui_commanding_docs=https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/commanding\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  {
    if [[ "$bulk_selection" -eq 1 ]]; then
      cat <<EOF
# Files Bulk Trash Smoke

Package: \`$package_id\`
Device: \`$serial\`
Primary target kind: \`$target_kind\`
Primary target name: \`$target_name\`
Second target kind: \`$bulk_second_kind\`
Second target name: \`$bulk_second_name\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Both target rows are disposable or safe to leave in Trash after this run.
- [ ] Both target rows are visible together in Files root before the selection begins.
- [ ] If \`create_disposable_folder=1\`, \`28-created-folder.xml\` shows the primary disposable folder.
- [ ] If \`create_bulk_second_disposable_folder=1\`, \`29-created-bulk-second-folder.xml\` shows the second disposable folder.

## Bulk Move To Trash

- [ ] \`20-files-root-ready.xml\` shows Files root chrome.
- [ ] \`30-bulk-targets-visible.xml\` shows both target item rows.
- [ ] \`35-bulk-first-selected.xml\` shows \`1 selected\`.
- [ ] \`36-bulk-two-selected.xml\` shows \`2 selected\`.
- [ ] \`40-file-actions.xml\` shows the selection action sheet and \`Move to trash\`.
- [ ] \`50-trash-confirm.xml\` shows \`Move selection to trash?\` and names the selected item kinds.
- [ ] \`60-after-trash.xml\` shows \`2 items moved to trash.\`.

## Trash Page Recoverability

- [ ] \`65-account-actions.xml\` shows the \`Trash\` account action.
- [ ] \`66-trash-page.xml\` shows the Trash page chrome, both target items, \`Restore\`, and \`Delete forever\`.
- [ ] \`66-trash-overflow.xml\` shows the \`Empty\` toolbar overflow action without executing it.
- [ ] If \`restore_bulk_from_trash_page=1\`, \`69-trash-bulk-two-selected.xml\` shows \`2 selected\`, \`70-trash-bulk-restore-confirm.xml\` shows the restore confirmation, and \`80-after-trash-bulk-restore.xml\` shows \`2 selected items restored.\`.
- [ ] If \`delete_bulk_forever_from_trash_page=1\`, \`69-trash-bulk-two-selected.xml\` shows \`2 selected\`, \`70-trash-bulk-delete-forever-confirm.xml\` shows the permanent-delete confirmation, and \`80-after-trash-bulk-delete-forever.xml\` shows \`2 selected items deleted forever.\`.
- [ ] \`99-logcat.txt\` has no ANR/FATAL markers.
EOF
      return
    fi

    cat <<EOF
# Files Trash Smoke

Package: \`$package_id\`
Device: \`$serial\`
Target kind: \`$target_kind\`
Target name: \`$target_name\`
Restore from Trash page: \`$restore_from_trash_page\`
Delete forever from Trash page: \`$delete_forever_from_trash_page\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Target item is disposable or safe to restore after a trash cycle.
- [ ] Permanent delete is only run with \`create_disposable_folder=1\`.
- [ ] If \`create_disposable_folder=1\`, \`28-created-folder.xml\` shows the new folder before trash.

## Trash

- [ ] \`20-files-root-ready.xml\` shows Files root chrome.
- [ ] \`30-target-visible.xml\` shows the target item row.
- [ ] \`40-file-actions.xml\` shows the target action sheet and \`Move to trash\`.
- [ ] \`50-trash-confirm.xml\` shows the move-to-trash confirmation.
- [ ] \`60-after-trash.xml\` shows the moved-to-trash status and \`Restore\`.

EOF

    if [[ "$restore_from_trash_page" -eq 1 || "$delete_forever_from_trash_page" -eq 1 ]]; then
      cat <<EOF
## Trash Page

- [ ] \`65-account-actions.xml\` shows the \`Trash\` account action.
- [ ] \`66-trash-page.xml\` shows the Trash page chrome, target item, \`Restore\`, and \`Delete forever\`.
- [ ] \`66-trash-overflow.xml\` shows the \`Empty\` toolbar overflow action without executing it.

EOF
    fi

    if [[ "$delete_forever_from_trash_page" -eq 1 ]]; then
      cat <<EOF
## Delete Forever

- [ ] \`70-delete-forever-confirm.xml\` shows \`Delete forever?\`.
- [ ] \`80-after-delete-forever.xml\` shows the permanent delete result.
- [ ] \`99-logcat.txt\` has no ANR/FATAL markers.
EOF
    else
      cat <<EOF
## Restore

- [ ] \`70-restore-confirm.xml\` shows \`Restore item?\`.
- [ ] \`80-after-restore.xml\` shows the restored status or the target row restored in Files.
- [ ] \`99-logcat.txt\` has no ANR/FATAL markers.
EOF
    fi
  } > "$evidence_dir/checklist.md"
}

trap capture_failure_evidence EXIT
