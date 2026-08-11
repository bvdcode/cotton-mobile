write_metadata() {
  {
    cotton_write_remote_push_metadata
    printf 'require_logout_revoke=%s\n' "$require_logout_revoke"
    printf 'require_logout_refresh_cancel=%s\n' "$require_logout_refresh_cancel"
    printf 'capture_diagnostics_ui=%s\n' "$capture_diagnostics_ui"
    printf 'reinstall_mode=%s\n' "$reinstall_mode"
    printf 'token_wait_seconds=%s\n' "$token_wait_seconds"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'android_notification_permission_docs=https://developer.android.com/develop/ui/views/notifications/notification-permission\n'
    printf 'firebase_messaging_android_docs=https://firebase.google.com/docs/cloud-messaging/android/get-started\n'
    printf 'android_workmanager_manage_work_docs=https://developer.android.com/develop/background-work/background-tasks/persistent/how-to/manage-work\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_logcat_docs=https://developer.android.com/tools/logcat\n'
  } > "$evidence_dir/00-metadata.txt"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Remote Push Lifecycle Smoke

Package: \`$package_id\`
Device: \`$serial\`
Configuration: \`$configuration\`
Post-logout reinstall mode: \`$reinstall_mode\`

## Preconditions

- [ ] \`10-token-registration/91-result.txt\` shows \`registration_status=registered\`.
- [ ] Google Play services are present in \`10-token-registration/08-play-services.txt\`.
- [ ] Package/version in evidence matches the build under test.
- [ ] The signed-in session restores without clearing app data.

## Opt-In

- [ ] Open Account -> Notifications.
- [ ] If Android notifications are not allowed, tap \`Allow\` and grant the Android permission.
- [ ] \`20-notification-opt-in.png\` and \`20-notification-opt-in.xml\` show the allowed state or the Android permission decision path.
- [ ] The UI remains aligned, compact, and unclipped.

## Server Preference Opt-Out / Opt-In

- [ ] Turn supported server-push categories off.
- [ ] \`30-server-push-opt-out.png\` and \`30-server-push-opt-out.xml\` show disabled shared-file and security/session categories.
- [ ] Turn supported server-push categories back on.
- [ ] \`40-server-push-opt-in.png\` and \`40-server-push-opt-in.xml\` show enabled shared-file and security/session categories.
- [ ] Unsupported access-request and comment/mention categories are not visible.

## Logout Revocation

- [ ] Log out from the account menu after token registration has been proven.
- [ ] \`50-after-logout.png\` and \`50-after-logout.xml\` show the signed-out state.
- [ ] \`90-remote-push-lifecycle-log.txt\` shows current-session token revocation, periodic refresh cancellation, and no fatal runtime crash.

## Reinstall / Update

- [ ] \`60-after-reinstall.png\` and \`60-after-reinstall.xml\` show the expected signed-out state after the selected reinstall mode.
- [ ] Fresh reinstall was used only when intentional, because it clears the app package data.

## Evidence Files

- \`00-metadata.txt\`
- \`01-adb-devices.txt\`
- \`02-device-state.txt\`
- \`03-package.txt\`
- \`04-package-version.txt\`
- \`10-token-registration/\`
- \`20-notification-opt-in.png\` / \`20-notification-opt-in.xml\`
- \`30-server-push-opt-out.png\` / \`30-server-push-opt-out.xml\`
- \`40-server-push-opt-in.png\` / \`40-server-push-opt-in.xml\`
- \`50-after-logout.png\` / \`50-after-logout.xml\`
- \`60-after-reinstall.png\` / \`60-after-reinstall.xml\`
- \`90-remote-push-lifecycle-log.txt\`
- \`91-result.txt\`
EOF
}

capture_window() {
  local prefix="$1"

  cotton_capture_text "$prefix-window.txt" cotton_adb shell dumpsys window || true
  cotton_capture_text "$prefix-activity.txt" cotton_adb shell dumpsys activity top || true

  if ! cotton_adb exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  if cotton_adb shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    cotton_adb shell cat /sdcard/cotton-window.xml > "$evidence_dir/$prefix.xml" || true
    cotton_adb shell rm -f /sdcard/cotton-window.xml >/dev/null 2>&1 || true
  fi
}

prompt_capture() {
  local message="$1"
  local prefix="$2"

  printf '\n%s\n' "$message"
  printf 'Press Enter to capture %s... ' "$prefix"
  read -r _
  capture_window "$prefix"
}



require_xml_without_text() {
  local xml_file="$1"
  local needle="$2"
  local message="$3"

  if cotton_xml_has_text "$xml_file" "$needle"; then
    printf '%s\n' "$message" >&2
    printf 'Unexpected text: %s\n' "$needle" >&2
    printf 'Evidence: %s\n' "$xml_file" >&2
    exit 66
  fi
}

require_xml_any_text() {
  local xml_file="$1"
  local message="$2"
  shift 2

  local needle
  for needle in "$@"; do
    if cotton_xml_has_text "$xml_file" "$needle"; then
      return
    fi
  done

  printf '%s\n' "$message" >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 66
}

require_server_push_switches() {
  local xml_file="$1"
  local expected_checked="$2"
  local state_name="$3"

  cotton_require_xml_text "$xml_file" "Notifications" "$state_name page title is missing."
  cotton_require_xml_text "$xml_file" "Server push" "$state_name did not show Server push."
  cotton_require_xml_text "$xml_file" "Shared-file activity" "$state_name did not show shared-file alerts."
  cotton_require_xml_text "$xml_file" "Security and sessions" "$state_name did not show security/session alerts."
  require_xml_without_text "$xml_file" "Access requests" "$state_name exposed unsupported access requests."
  require_xml_without_text "$xml_file" "Comments and mentions" "$state_name exposed unsupported comments/mentions."

  python3 - "$xml_file" "$expected_checked" "$state_name" <<'PY'
import sys
import xml.etree.ElementTree as ET

xml_file = sys.argv[1]
expected_checked = sys.argv[2].lower()
state_name = sys.argv[3]

try:
    root = ET.parse(xml_file).getroot()
except ET.ParseError as error:
    raise SystemExit(f"{state_name} XML could not be parsed: {error}")

switches = [
    node
    for node in root.iter()
    if "switch" in node.attrib.get("class", "").lower()
]

if len(switches) != 2:
    raise SystemExit(f"{state_name} expected 2 server-push switches, found {len(switches)}.")

for index, node in enumerate(switches, start=1):
    actual_checked = node.attrib.get("checked", "").lower()
    if actual_checked != expected_checked:
        raise SystemExit(
            f"{state_name} switch {index} checked={actual_checked}, expected {expected_checked}."
        )
PY
}
