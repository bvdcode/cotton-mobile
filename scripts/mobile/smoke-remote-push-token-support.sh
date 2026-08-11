write_metadata() {
  {
    cotton_write_remote_push_metadata
    printf 'require_registered=%s\n' "$require_registered"
    printf 'capture_diagnostics_ui=%s\n' "$capture_diagnostics_ui"
    printf 'wait_seconds=%s\n' "$wait_seconds"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'firebase_android_setup_docs=https://firebase.google.com/docs/android/setup\n'
    printf 'firebase_messaging_android_docs=https://firebase.google.com/docs/cloud-messaging/android/get-started\n'
    printf 'firebase_token_management_docs=https://firebase.google.com/docs/cloud-messaging/manage-tokens\n'
    printf 'google_services_plugin_docs=https://developers.google.com/android/guides/google-services-plugin\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_logcat_docs=https://developer.android.com/tools/logcat\n'
  } > "$evidence_dir/00-metadata.txt"
}

restore_firebase_config_if_requested() {
  if [[ -z "$config_source_file" && -z "$config_source_env_name" ]]; then
    return
  fi

  local restore_args=(
    --configuration "$configuration"
    --package-id "$package_id"
    --config-file "$config_file"
  )

  if [[ -n "$config_source_file" ]]; then
    restore_args+=(--source-file "$config_source_file")
  fi

  if [[ -n "$config_source_env_name" ]]; then
    restore_args+=(--source-env "$config_source_env_name")
  fi

  "$SCRIPT_DIR/restore-android-firebase-config.sh" "${restore_args[@]}" \
    > "$evidence_dir/01-restore-firebase-config.txt" 2>&1
}

run_firebase_config_preflight() {
  local status

  set +e
  "$SCRIPT_DIR/check-android-firebase-config.py" \
    --configuration "$configuration" \
    --package-id "$package_id" \
    --config-file "$config_file" \
    > "$evidence_dir/02-firebase-config.txt" 2>&1
  status=$?
  set -e

  if [[ "$status" -ne 0 ]]; then
    cat "$evidence_dir/02-firebase-config.txt" >&2
    printf 'Remote-push token smoke stopped at Firebase config preflight. Evidence: %s\n' \
      "$evidence_dir" >&2
    exit "$status"
  fi
}

capture_window() {
  local prefix="$1"

  cotton_capture_text "$prefix-window.txt" cotton_adb shell dumpsys window || true
  if ! cotton_adb exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  if cotton_adb shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    cotton_adb shell cat /sdcard/cotton-window.xml > "$evidence_dir/$prefix.xml" || true
    cotton_adb shell rm -f /sdcard/cotton-window.xml >/dev/null 2>&1 || true
  fi
}

find_account_entry_text() {
  local xml_file="$1"

  python3 - "$xml_file" <<'PY'
import sys
from xml.etree import ElementTree

xml_file = sys.argv[1]
root = ElementTree.parse(xml_file).getroot()
candidates = ("Account", "More")

for candidate in candidates:
    for node in root.iter("node"):
        if node.attrib.get("clickable") != "true":
            continue

        values = (
            node.attrib.get("text", ""),
            node.attrib.get("content-desc", ""),
        )
        if candidate in values:
            print(candidate)
            raise SystemExit(0)

raise SystemExit(1)
PY
}

capture_files_screen_for_diagnostics() {
  local current_xml="$evidence_dir/20-after-launch.xml"
  local attempt
  local entry_text

  for attempt in 0 1 2 3; do
    if [[ -f "$current_xml" ]] && entry_text="$(find_account_entry_text "$current_xml")"; then
      printf 'files_xml=%s\naccount_entry=%s\n' "$current_xml" "$entry_text" \
        > "$evidence_dir/21-files-navigation-result.txt"
      return 0
    fi

    if [[ "$attempt" -lt 3 ]]; then
      cotton_adb shell input keyevent KEYCODE_BACK
      sleep 2
      capture_window "21-files-navigation-$attempt"
      current_xml="$evidence_dir/21-files-navigation-$attempt.xml"
    fi
  done

  printf 'Files screen account action is not visible before Diagnostics validation.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

capture_and_validate_diagnostics() {
  local source_xml="$evidence_dir/20-after-launch.xml"
  local account_entry
  if [[ ! -f "$source_xml" ]]; then
    printf 'Launch UI XML was not captured before Diagnostics validation.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir" >&2
    exit 66
  fi

  capture_files_screen_for_diagnostics
  source_xml="$(sed -n 's/^files_xml=//p' "$evidence_dir/21-files-navigation-result.txt" | head -1)"
  account_entry="$(sed -n 's/^account_entry=//p' "$evidence_dir/21-files-navigation-result.txt" | head -1)"

  cotton_tap_node_from_xml "$source_xml" "$account_entry" exact
  sleep 2
  capture_window "30-account-actions"
  cotton_require_xml_text "$evidence_dir/30-account-actions.xml" "Diagnostics" "Diagnostics action is not visible."
  cotton_tap_node_from_xml "$evidence_dir/30-account-actions.xml" "Diagnostics" exact

  local attempt
  local xml_file
  for attempt in 0 1 2 3 4 5 6 7 8; do
    sleep 2
    capture_window "40-diagnostics-$attempt"
    xml_file="$evidence_dir/40-diagnostics-$attempt.xml"
    if cotton_xml_has_text "$xml_file" "Remote push"; then
      diagnostics_xml="$xml_file"
      break
    fi

    if [[ "$attempt" -lt 8 ]]; then
      cotton_adb shell input swipe 540 2100 540 850 450
    fi
  done

  if [[ -z "$diagnostics_xml" ]]; then
    printf 'Diagnostics Remote push section was not visible.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir" >&2
    exit 66
  fi

  cotton_require_xml_text "$diagnostics_xml" "Firebase Cloud Messaging" "Remote push provider is not visible."
  cotton_require_xml_text "$diagnostics_xml" "Android" "Remote push platform is not visible."
  cotton_require_xml_text "$diagnostics_xml" "Token" "Remote push token row is not visible."
  cotton_require_xml_text "$diagnostics_xml" "Registration" "Remote push registration row is not visible."

  if [[ "$registration_status" == "registered" ]]; then
    cotton_require_xml_text "$diagnostics_xml" "Available" "Diagnostics did not show an available platform token."
    cotton_require_xml_text "$diagnostics_xml" "Registered" "Diagnostics did not show registered session state."
  fi

  if python3 - "$diagnostics_xml" "$evidence_dir/42-diagnostics-token-leak-check.txt" <<'PY'
import re
import sys
from xml.etree import ElementTree

xml_file, result_file = sys.argv[1:3]
pattern = re.compile(r"[A-Za-z0-9_-]{80,}")
root = ElementTree.parse(xml_file).getroot()
matches = []

for node in root.iter("node"):
    for attribute in ("text", "content-desc"):
        value = node.attrib.get(attribute, "")
        if pattern.search(value):
            matches.append(f"{attribute}={value}")

if matches:
    with open(result_file, "w", encoding="utf-8") as output:
        output.write("\n".join(matches))
        output.write("\n")
    raise SystemExit(1)

with open(result_file, "w", encoding="utf-8") as output:
    output.write("No long token-like visible text found.\n")
PY
  then
    :
  else
    printf 'Diagnostics XML contains an unexpected long token-like value.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir/42-diagnostics-token-leak-check.txt" >&2
    exit 66
  fi

  printf 'diagnostics_xml=%s\n' "$diagnostics_xml" > "$evidence_dir/41-diagnostics-result.txt"
}

write_result() {
  local status="$1"

  {
    printf 'registration_status=%s\n' "$status"
    printf 'registered_log_count=%s\n' "$(grep -c 'Registered the Cotton mobile remote push token for the current session.' "$evidence_dir/90-remote-push-log.txt" || true)"
    printf 'not_configured_log_count=%s\n' "$(grep -c 'not configured' "$evidence_dir/90-remote-push-log.txt" || true)"
    printf 'unavailable_log_count=%s\n' "$(grep -c 'unavailable' "$evidence_dir/90-remote-push-log.txt" || true)"
    printf 'periodic_refresh_schedule_log_count=%s\n' "$(grep -c 'remote push token refresh' "$evidence_dir/90-remote-push-log.txt" || true)"
  } > "$evidence_dir/91-result.txt"
}
