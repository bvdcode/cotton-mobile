#!/usr/bin/env bash

wait_for_sync_dashboard() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="20-sync-dashboard-$attempt"
    cotton_capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if cotton_xml_has_text "$xml_file" "Sync" \
      && cotton_xml_has_text "$xml_file" "2026 → Pictures / 2026"; then
      sync_dashboard_xml="$xml_file"
      return
    fi

    if cotton_xml_has_text "$xml_file" "Sync"; then
      cotton_tap_clickable_from_xml "$xml_file" "Sync" || true
    fi
    sleep 2
  done

  printf 'Sync dashboard did not show seeded roots.\nEvidence: %s\n' "$evidence_dir" >&2
  exit "$COTTON_EXIT_EVIDENCE"
}

capture_scrolled_sync_dashboard() {
  local prefix="${1:-40-sync-dashboard-scrolled}"

  cotton_adb shell input swipe 500 1600 500 700 300 >/dev/null 2>&1 || true
  sleep 1
  cotton_capture_screen "$prefix"
  sync_dashboard_scrolled_xml="$evidence_dir/$prefix.xml"
}

verify_sync_dashboard() {
  local scrolled_prefix="${1:-40-sync-dashboard-scrolled}"

  cotton_require_xml_text "$sync_dashboard_xml" "Sync" \
    "Sync dashboard title is not visible."
  cotton_require_xml_text "$sync_dashboard_xml" "Refresh sync folders" \
    "Refresh action is not exposed."
  cotton_require_xml_text "$sync_dashboard_xml" "2026 → Pictures / 2026" \
    "Seeded sync-root path is not compact."
  cotton_require_xml_text "$sync_dashboard_xml" "Reconnect" \
    "Seeded root does not expose its local-access status."
  cotton_require_xml_text "$sync_dashboard_xml" "Pause" \
    "Seeded root does not expose pause."
  cotton_require_xml_text "$sync_dashboard_xml" "Delete sync" \
    "Seeded root does not expose delete."

  if ! cotton_xml_has_text "$sync_dashboard_xml" "Archive → Pictures / Archive"; then
    capture_scrolled_sync_dashboard "$scrolled_prefix"
  else
    sync_dashboard_scrolled_xml="$sync_dashboard_xml"
  fi

  cotton_require_xml_text "$sync_dashboard_scrolled_xml" "Archive → Pictures / Archive" \
    "Paused seeded sync root is not visible."
  cotton_require_xml_text "$sync_dashboard_scrolled_xml" "Paused" \
    "Paused seeded sync-root status is not visible."
  cotton_require_xml_text "$sync_dashboard_scrolled_xml" "Resume" \
    "Paused root does not expose resume."
}

verify_pause_resume_actions() {
  cotton_tap_clickable_from_xml "$sync_dashboard_xml" "Pause"
  sleep 1
  wait_for_sync_dashboard
  cotton_require_xml_text "$sync_dashboard_xml" "Paused syncing 2026." \
    "Pause action did not update the selected sync root."

  cotton_tap_clickable_from_xml "$sync_dashboard_xml" "Resume"
  sleep 1
  wait_for_sync_dashboard
  cotton_require_xml_text "$sync_dashboard_xml" "Resumed syncing 2026." \
    "Resume action did not update the selected sync root."
  cotton_require_xml_text "$sync_dashboard_xml" "Pause" \
    "Resumed sync root did not restore the pause action."
}

verify_delete_action() {
  local confirmation_xml="$evidence_dir/50-sync-dashboard-delete-confirmation.xml"

  cotton_tap_clickable_from_xml "$sync_dashboard_xml" "Delete sync"
  sleep 1
  cotton_capture_screen "50-sync-dashboard-delete-confirmation"
  cotton_require_xml_text "$confirmation_xml" "Delete sync for 2026?" \
    "Delete action did not ask for confirmation."
  cotton_require_xml_text "$confirmation_xml" \
    "This deletes the sync setup. Files on this device and in Cotton Cloud are not deleted." \
    "Delete confirmation does not explain its scope."
  cotton_tap_clickable_from_xml "$confirmation_xml" "Cancel"
  sleep 1
}

verify_refresh_action() {
  cotton_tap_clickable_from_xml "$sync_dashboard_xml" "Refresh sync folders"
  sleep 2
  wait_for_sync_dashboard
  verify_sync_dashboard "60-sync-dashboard-after-refresh-scrolled"
}

capture_final_state() {
  cotton_capture_screen "90-final"
  cotton_capture_text_best_effort "91-logcat.txt" cotton_adb logcat -d -t 400
  if grep -Ei 'FATAL EXCEPTION|AndroidRuntime.*FATAL|SIGSEGV|libc.*Fatal signal|mono-rt.*SIG' \
      "$evidence_dir/91-logcat.txt" \
      > "$evidence_dir/92-fatal-logcat.txt"; then
    printf 'Fatal runtime marker found in logcat.\nEvidence: %s\n' \
      "$evidence_dir/92-fatal-logcat.txt" >&2
    exit "$COTTON_EXIT_EVIDENCE"
  fi
}
