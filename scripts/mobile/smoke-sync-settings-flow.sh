cotton_wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="20-files-root-$attempt"
    cotton_capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if cotton_xml_has_text "$xml_file" "Files" \
      && cotton_xml_has_text "$xml_file" "Sync" \
      && cotton_xml_has_text "$xml_file" "More"; then
      files_root_xml="$xml_file"
      return
    fi

    if cotton_xml_has_text "$xml_file" "Navigate up"; then
      cotton_tap_node_from_xml "$xml_file" "Navigate up" exact
      sleep 2
      continue
    fi

    cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 1
    cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/20-relaunch-$attempt.txt" || true
    sleep 2
  done

  printf 'Files root with Sync navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

wait_for_sync_settings() {
  local prefix_root="${1:-30-sync-settings}"
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="$prefix_root-$attempt"
    cotton_capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if cotton_xml_has_text "$xml_file" "Folders" \
      && cotton_xml_has_text "$xml_file" "Smoke Downloads"; then
      sync_settings_xml="$xml_file"
      return
    fi

    sleep 2
  done

  printf 'Sync settings page did not show seeded sync roots.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

capture_scrolled_sync_settings() {
  local prefix="${1:-40-sync-settings-scrolled}"

  cotton_adb shell input swipe 500 1600 500 700 300 >/dev/null 2>&1 || true
  sleep 1
  cotton_capture_screen "$prefix"
  sync_settings_scrolled_xml="$evidence_dir/$prefix.xml"
}

verify_sync_settings() {
  local scrolled_prefix="${1:-40-sync-settings-scrolled}"

  cotton_require_xml_text "$sync_settings_xml" "Folders" \
    "Sync settings page header is not visible."
  cotton_require_xml_text "$sync_settings_xml" "2 folders set to sync" \
    "Seeded sync-root summary is not visible."
  cotton_require_xml_text "$sync_settings_xml" "Run all" \
    "Sync settings did not expose the Run all toolbar action."
  cotton_require_xml_text "$sync_settings_xml" "Refresh" \
    "Sync settings did not expose the Refresh toolbar action."
  cotton_require_xml_text "$sync_settings_xml" "Smoke Downloads" \
    "Ready seeded sync root is not visible."
  cotton_require_xml_text "$sync_settings_xml" "Files / Smoke Downloads" \
    "Ready seeded sync root path is not visible."
  cotton_require_xml_text "$sync_settings_xml" "Cloud to device" \
    "Ready seeded sync root direction is not visible."
  cotton_require_xml_text "$sync_settings_xml" "On-device smoke root" \
    "Ready seeded sync root local label is not visible."
  cotton_require_xml_text "$sync_settings_xml" "Sync root ready" \
    "Ready seeded sync root status is not visible."
  cotton_require_xml_text "$sync_settings_xml" "Run now" \
    "Sync settings did not expose Run now for the ready root."
  cotton_require_xml_text "$sync_settings_xml" "Pause" \
    "Sync settings did not expose Pause for the ready root."
  cotton_require_xml_text "$sync_settings_xml" "Stop syncing" \
    "Sync settings did not expose Stop syncing."

  if ! cotton_xml_has_text "$sync_settings_xml" "Smoke Paused"; then
    capture_scrolled_sync_settings "$scrolled_prefix"
  else
    sync_settings_scrolled_xml="$sync_settings_xml"
  fi

  cotton_require_xml_text "$sync_settings_scrolled_xml" "Smoke Paused" \
    "Paused seeded sync root is not visible."
  cotton_require_xml_text "$sync_settings_scrolled_xml" "Files / Smoke Paused" \
    "Paused seeded sync root path is not visible."
  cotton_require_xml_text "$sync_settings_scrolled_xml" "Bidirectional" \
    "Paused seeded sync root direction is not visible."
  cotton_require_xml_text "$sync_settings_scrolled_xml" "Selected smoke folder" \
    "Paused seeded sync root local label is not visible."
  cotton_require_xml_text "$sync_settings_scrolled_xml" "Paused" \
    "Paused seeded sync root status is not visible."
  cotton_require_xml_text "$sync_settings_scrolled_xml" "Resume" \
    "Sync settings did not expose Resume for the paused root."
}

verify_refresh_action() {
  cotton_tap_node_from_xml "$sync_settings_xml" "Refresh" exact
  sleep 2
  wait_for_sync_settings "50-sync-settings-after-refresh"
  verify_sync_settings "60-sync-settings-after-refresh-scrolled"
}

capture_final_state() {
  cotton_capture_screen "90-final"
  cotton_capture_text_best_effort "91-logcat.txt" cotton_adb logcat -d -t 400
  if grep -Ei 'FATAL EXCEPTION|AndroidRuntime.*FATAL|SIGSEGV|libc.*Fatal signal|mono-rt.*SIG' \
      "$evidence_dir/91-logcat.txt" \
      > "$evidence_dir/92-fatal-logcat.txt"; then
    printf 'Fatal runtime marker found in logcat.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir/92-fatal-logcat.txt" >&2
    exit 66
  fi
}
