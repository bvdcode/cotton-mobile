wait_for_text_capture() {
  local prefix="$1"
  local needle="$2"
  local message="$3"
  local attempt
  local attempt_prefix
  local attempt_xml

  for attempt in 0 1 2 3 4 5 6 7 8 9; do
    attempt_prefix="$prefix-$attempt"
    cotton_capture_screen "$attempt_prefix"
    attempt_xml="$evidence_dir/$attempt_prefix.xml"
    if [[ -f "$attempt_xml" ]] && grep -Fq "$needle" "$attempt_xml"; then
      cp "$attempt_xml" "$evidence_dir/$prefix.xml"
      if [[ -f "$evidence_dir/$attempt_prefix.png" ]]; then
        cp "$evidence_dir/$attempt_prefix.png" "$evidence_dir/$prefix.png"
      fi
      if [[ -f "$evidence_dir/$attempt_prefix-window.txt" ]]; then
        cp "$evidence_dir/$attempt_prefix-window.txt" "$evidence_dir/$prefix-window.txt"
      fi
      return
    fi

    sleep 2
  done

  printf '%s\n' "$message" >&2
  printf 'Timed out waiting for text: %s\n' "$needle" >&2
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
    printf 'preflight_only=%s\n' "$preflight_only"
    printf 'seed_only=%s\n' "$seed_only"
    printf 'skip_source_app_file=%s\n' "$skip_source_app_file"
    printf 'queue_text_share=%s\n' "$queue_text_share"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'share_text=%s\n' "$share_text"
    printf 'share_file_name=%s\n' "$share_file_name"
    printf 'android_receive_share_docs=https://developer.android.com/training/sharing/receive\n'
    printf 'android_send_share_docs=https://developer.android.com/training/sharing/send\n'
    printf 'android_file_share_docs=https://developer.android.com/training/secure-file-sharing/share-file\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Share To Cotton Smoke

Package: \`$package_id\`
Device: \`$serial\`
Text payload: \`$share_text\`
Seeded file: \`$share_file_name\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] \`06-seed-share-file.txt\` shows \`$share_file_name\` pushed to Android Downloads.

## Automated Text Share

- [ ] \`20-text-share-inbox.png\` / \`20-text-share-inbox.xml\` show \`Capture Inbox\`.
- [ ] The captured item shows \`$share_text\`.
- [ ] The captured item shows \`Text share captured\`, \`Choose folder\`, \`No destination selected\`, and \`Text\`.

## Automated Text Queue

- [ ] If \`--queue-text-share\` was used, \`21-text-share-destination.xml\` shows \`Choose Destination\`.
- [ ] If \`--queue-text-share\` was used, \`22-text-share-destination-saved.xml\` shows \`Ready\` and \`Destination:\`.
- [ ] If \`--queue-text-share\` was used, \`23-text-share-queued.xml\` shows queued upload status.

## Shell URI Edge Cases

- [ ] \`30-shell-content-uri-edge.png\` / \`30-shell-content-uri-edge.xml\` show Cotton does not upload a shell content URI without a valid source-app grant.
- [ ] \`40-file-uri-edge.png\` / \`40-file-uri-edge.xml\` show Cotton reports unsupported file URI content without crashing.

## Source-App File Share

- [ ] Share \`$share_file_name\` from Android Files, Photos, Drive, or another real source app to Cotton.
- [ ] \`50-source-app-file-share.png\` / \`50-source-app-file-share.xml\` show \`$share_file_name\`.
- [ ] The captured file item shows \`Copied to this device\` and \`Choose folder\`.
- [ ] \`90-logcat.txt\` has no share-to-Cotton crashes.

## Evidence To Review

- \`00-device.txt\`
- \`04-package.txt\`
- \`05-package-version.txt\`
- \`06-seed-share-file.txt\`
- \`20-text-share-inbox.png\` / \`20-text-share-inbox.xml\`
- \`21-text-share-destination.png\` / \`21-text-share-destination.xml\`
- \`22-text-share-destination-saved.png\` / \`22-text-share-destination-saved.xml\`
- \`23-text-share-queued.png\` / \`23-text-share-queued.xml\`
- \`30-shell-content-uri-edge.png\` / \`30-shell-content-uri-edge.xml\`
- \`40-file-uri-edge.png\` / \`40-file-uri-edge.xml\`
- \`50-source-app-file-share.png\` / \`50-source-app-file-share.xml\`
- \`90-logcat.txt\`
EOF
}

seed_share_file() {
  local seed_dir="$evidence_dir/seed-files"
  local seed_file="$seed_dir/$share_file_name"
  mkdir -p "$seed_dir"

  {
    printf 'Cotton source-app share smoke file.\n'
    printf 'Created at UTC: %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'Package: %s\n' "$package_id"
  } > "$seed_file"

  : > "$evidence_dir/06-seed-share-file.txt"
  cotton_adb push "$seed_file" "/sdcard/Download/$share_file_name" \
    >> "$evidence_dir/06-seed-share-file.txt" 2>&1
  cotton_adb shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file:///sdcard/Download/$share_file_name" \
    >> "$evidence_dir/06-seed-share-file.txt" 2>&1 || true
  cotton_adb shell ls -la "/sdcard/Download/$share_file_name" \
    >> "$evidence_dir/06-seed-share-file.txt" 2>&1
  cotton_capture_text_best_effort "07-share-file-mediastore.txt" cotton_adb shell content query \
    --uri content://media/external/file \
    --projection _id:_display_name:mime_type:size \
    --where "_display_name='$share_file_name'"
}

content_uri_for_seeded_file() {
  local media_id
  media_id="$(sed -n 's/.*_id=\([0-9][0-9]*\).*/\1/p' "$evidence_dir/07-share-file-mediastore.txt" | sed -n '1p')"
  if [[ -n "${media_id//[[:space:]]/}" ]]; then
    printf 'content://media/external/file/%s' "$media_id"
  fi
}

start_text_share() {
  cotton_adb shell am start \
    -a android.intent.action.SEND \
    -t text/plain \
    -p "$package_id" \
    --es android.intent.extra.TEXT "$share_text" \
    > "$evidence_dir/20-text-share-start.txt" 2>&1
}

start_content_uri_edge_share() {
  local content_uri="$1"

  if [[ -z "$content_uri" ]]; then
    printf 'No MediaStore URI was available for %s.\n' "$share_file_name" \
      > "$evidence_dir/30-shell-content-uri-edge-start.txt"
    return
  fi

  cotton_adb shell am start \
    -a android.intent.action.SEND \
    -t text/plain \
    -p "$package_id" \
    --eu android.intent.extra.STREAM "$content_uri" \
    --grant-read-uri-permission \
    > "$evidence_dir/30-shell-content-uri-edge-start.txt" 2>&1
}

start_file_uri_edge_share() {
  cotton_adb shell am start \
    -a android.intent.action.SEND \
    -t text/plain \
    -p "$package_id" \
    --eu android.intent.extra.STREAM "file:///sdcard/Download/$share_file_name" \
    > "$evidence_dir/40-file-uri-edge-start.txt" 2>&1
}


write_metadata
write_checklist
