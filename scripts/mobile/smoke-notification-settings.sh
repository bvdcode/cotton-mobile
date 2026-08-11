#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
preflight_only=0
permission_state="preserve"
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive notification-settings smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --permission-state STATE  Set POST_NOTIFICATIONS before launch: preserve, fresh, allowed, or denied.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --preflight-only          Capture device/package/permission state and exit without manual prompts.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The script is intentionally manual: open Account -> Notifications in Cotton while
it captures Android permission state, notification channel diagnostics,
screenshots, UIAutomator XML, dumpsys window state, and logcat output.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--evidence-dir:evidence_dir"
  "--permission-state:permission_state"
  "--expected-version-code:expected_version_code"
  "--expected-version-name:expected_version_name"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--preflight-only:preflight_only:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

cotton_validate_notification_permission_state "$permission_state"

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Run it from a shell attached to the Android device or emulator.\n' >&2
  printf 'Use --preflight-only for non-interactive package/permission evidence.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-notification-settings"
fi

mkdir -p "$evidence_dir"

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'repo=%s\n' "$COTTON_REPO_ROOT"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'preflight_only=%s\n' "$preflight_only"
    printf 'permission_state=%s\n' "$permission_state"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'android_notification_permission_docs=https://developer.android.com/develop/ui/views/notifications/notification-permission\n'
    printf 'android_notification_channels_docs=https://developer.android.com/develop/ui/views/notifications/channels\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_dumpsys_docs=https://developer.android.com/tools/dumpsys\n'
    printf 'android_logcat_docs=https://developer.android.com/tools/logcat\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Notification Settings Smoke

Package: \`$package_id\`
Device: \`$serial\`
Requested Android permission setup: \`$permission_state\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] \`06-notification-permission.txt\` shows the expected Android \`POST_NOTIFICATIONS\` grant/flags.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Account menu is reachable from Files.

## Fresh / Not Requested Path

- [ ] Run with \`--permission-state fresh\` on an install where Cotton has not requested notifications yet.
- [ ] Opening Account -> Notifications does not show the Android permission dialog automatically.
- [ ] The page shows \`Not requested\`.
- [ ] The page shows an \`Allow\` action.
- [ ] Tapping \`Allow\` shows the Android notification permission dialog.

## Denied Path

- [ ] Run with \`--permission-state denied\`, or deny through the in-app \`Allow\` flow first.
- [ ] The Android permission state is denied in \`06-notification-permission.txt\`.
- [ ] The page shows \`Denied\` when Cotton knows the request was already attempted.
- [ ] The page shows a \`Settings\` action instead of re-promising background notifications.
- [ ] Tapping \`Settings\` opens Android App info/settings for \`$package_id\`.

## Allowed Path

- [ ] Run with \`--permission-state allowed\`.
- [ ] The Android permission state is granted in \`06-notification-permission.txt\`.
- [ ] The page shows \`Allowed\`.
- [ ] The page does not show the permission action button.
- [ ] Enabled category copy stays compact and truthful.

## Server Push Preferences

- [ ] The page shows the \`Server push\` section.
- [ ] If preferences load, the page shows \`Shared-file activity\` and \`Security and sessions\`.
- [ ] If preferences cannot load, the page shows \`Server alerts unavailable.\` and \`Retry\`.
- [ ] The server-push state matches the backend profile being tested.

## Channels And Evidence

- [ ] Notification dumpsys diagnostics include Transfers, Backup, Shares, and Security after channels are provisioned.
- [ ] \`20-notifications-page.png\` / \`20-notifications-page.xml\` show aligned, unclipped notification settings UI.
- [ ] \`30-permission-action.png\` / \`30-permission-action.xml\` capture the Android dialog or settings destination when applicable.
- [ ] \`90-logcat.txt\` has no notification permission/channel crashes.

## Evidence Files

- \`00-device.txt\`
- \`04-package.txt\`
- \`05-package-version.txt\`
- \`06-notification-permission.txt\`
- \`07-notification-appops.txt\`
- \`08-notification-dumpsys.txt\`
- \`08-notification-channels.txt\`
- \`10-preflight.png\` / \`10-preflight.xml\`
- \`20-notifications-page.png\` / \`20-notifications-page.xml\`
- \`30-permission-action.png\` / \`30-permission-action.xml\`
- \`90-logcat.txt\`
EOF
}

summarize_notification_dumpsys() {
  local source_file="$1"
  local summary_file="$2"

  awk -v package_id="$package_id" '
    $0 ~ package_id { print; next }
    $0 ~ /cotton\./ { print; next }
  ' "$source_file" > "$summary_file"
}

capture_notification_state() {
  local prefix="$1"

  cotton_capture_text_best_effort "$prefix-notification-permission.txt" \
    cotton_adb shell dumpsys package "$package_id"
  cotton_capture_text_best_effort "$prefix-notification-appops.txt" \
    cotton_adb shell appops get "$package_id" POST_NOTIFICATION
  cotton_capture_text_best_effort "$prefix-notification-dumpsys.txt" \
    cotton_adb shell dumpsys notification --noredact
  summarize_notification_dumpsys \
    "$evidence_dir/$prefix-notification-dumpsys.txt" \
    "$evidence_dir/$prefix-notification-channels.txt"
}

capture_device_state() {
  local prefix="$1"

  cotton_capture_text_best_effort "$prefix-window.txt" cotton_adb shell dumpsys window
  cotton_capture_text_best_effort "$prefix-package.txt" cotton_adb shell pm path "$package_id"
  capture_notification_state "$prefix"

  if ! cotton_adb exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  if cotton_adb shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    if ! cotton_adb pull /sdcard/cotton-window.xml "$evidence_dir/$prefix.xml" > "$evidence_dir/$prefix-pull-xml.log" 2>&1; then
      rm -f "$evidence_dir/$prefix.xml"
    fi
    cotton_adb shell rm -f /sdcard/cotton-window.xml >/dev/null 2>&1 || true
  fi
}

prompt_capture() {
  local message="$1"
  local prefix="$2"
  printf '\n%s\n' "$message"
  printf 'Press Enter to capture %s... ' "$prefix"
  read -r _
  capture_device_state "$prefix"
}

require_notification_page_state() {
  local xml_file="$1"
  local state_xml="$xml_file"
  local attempt

  cotton_require_xml_text "$xml_file" "Notifications" "Notifications page title is missing."
  cotton_require_xml_text "$xml_file" "Refresh" "Notifications page did not expose Refresh."

  for attempt in 0 1 2; do
    if cotton_xml_has_text "$state_xml" "Server push"; then
      if cotton_xml_has_text "$state_xml" "Shared-file activity" \
        || cotton_xml_has_text "$state_xml" "Security and sessions"; then
        cotton_require_xml_text "$state_xml" "Shared-file activity" \
          "Server push preferences did not show shared-file activity."
        cotton_require_xml_text "$state_xml" "Security and sessions" \
          "Server push preferences did not show security/session alerts."
        return
      fi

      if cotton_xml_has_text "$state_xml" "Server alerts unavailable."; then
        cotton_require_xml_text "$state_xml" "Retry" \
          "Server push unavailable state did not expose Retry."
        return
      fi
    fi

    cotton_adb shell input swipe 540 1700 540 650 350 >/dev/null 2>&1 || true
    sleep 1
    capture_device_state "21-notifications-server-push-$attempt"
    state_xml="$evidence_dir/21-notifications-server-push-$attempt.xml"
  done

  printf 'Notifications page did not show loaded or unavailable server-push preferences.\n' >&2
  printf 'Evidence: %s\n' "$state_xml" >&2
  exit 66
}

write_metadata
write_checklist

cotton_prepare_installed_package

cotton_apply_notification_permission_state \
  "$permission_state" \
  "$package_id" \
  "$evidence_dir/06-permission-setup.txt"

cotton_capture_text_best_effort "06-notification-permission.txt" cotton_adb shell dumpsys package "$package_id"
cotton_capture_text_best_effort "07-notification-appops.txt" cotton_adb shell appops get "$package_id" POST_NOTIFICATION
cotton_capture_text_best_effort "08-notification-dumpsys.txt" cotton_adb shell dumpsys notification --noredact
summarize_notification_dumpsys \
  "$evidence_dir/08-notification-dumpsys.txt" \
  "$evidence_dir/08-notification-channels.txt"

cotton_adb logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  cotton_capture_text_best_effort "11-launch.txt" cotton_adb shell monkey -p "$package_id" 1
  sleep 2
fi

capture_device_state "10-preflight"

if [[ "$preflight_only" -eq 1 ]]; then
  printf '\nNotification settings preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

prompt_capture "Open Account -> Notifications. Verify the page matches the requested permission path." \
  "20-notifications-page"
require_notification_page_state "$evidence_dir/20-notifications-page.xml"

prompt_capture "If applicable, tap Allow or Settings and leave the Android dialog/settings destination visible." \
  "30-permission-action"

cotton_capture_text_best_effort "90-logcat.txt" cotton_adb logcat -d -v threadtime

printf '\nNotification settings smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md before marking notification allowed/denied runtime proof complete.\n'
