#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

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

while [[ $# -gt 0 ]]; do
  case "$1" in
    --package)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --package.\n' >&2
        exit 64
      fi
      package_id="$2"
      shift 2
      ;;
    --serial)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --serial.\n' >&2
        exit 64
      fi
      serial="$2"
      shift 2
      ;;
    --evidence-dir)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --evidence-dir.\n' >&2
        exit 64
      fi
      evidence_dir="$2"
      shift 2
      ;;
    --install-debug)
      install_debug=1
      shift
      ;;
    --permission-state)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --permission-state.\n' >&2
        exit 64
      fi
      permission_state="$2"
      shift 2
      ;;
    --expected-version-code)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --expected-version-code.\n' >&2
        exit 64
      fi
      expected_version_code="$2"
      shift 2
      ;;
    --expected-version-name)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --expected-version-name.\n' >&2
        exit 64
      fi
      expected_version_name="$2"
      shift 2
      ;;
    --preflight-only)
      preflight_only=1
      shift
      ;;
    --no-launch)
      launch_app=0
      shift
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      printf 'Unknown argument: %s\n' "$1" >&2
      exit 64
      ;;
  esac
done

case "$permission_state" in
  preserve|fresh|allowed|denied)
    ;;
  *)
    printf 'Invalid --permission-state: %s. Expected preserve, fresh, allowed, or denied.\n' \
      "$permission_state" >&2
    exit 64
    ;;
esac

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

adb_device() {
  adb -s "$serial" "$@"
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

capture_text() {
  local name="$1"
  shift
  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed: %q\n' "$1" >> "$evidence_dir/$name"
  fi
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

  capture_text "$prefix-notification-permission.txt" \
    adb_device shell dumpsys package "$package_id"
  capture_text "$prefix-notification-appops.txt" \
    adb_device shell appops get "$package_id" POST_NOTIFICATION
  capture_text "$prefix-notification-dumpsys.txt" \
    adb_device shell dumpsys notification --noredact
  summarize_notification_dumpsys \
    "$evidence_dir/$prefix-notification-dumpsys.txt" \
    "$evidence_dir/$prefix-notification-channels.txt"
}

capture_device_state() {
  local prefix="$1"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window
  capture_text "$prefix-package.txt" adb_device shell pm path "$package_id"
  capture_notification_state "$prefix"

  if ! adb_device exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  if adb_device shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    if ! adb_device pull /sdcard/cotton-window.xml "$evidence_dir/$prefix.xml" > "$evidence_dir/$prefix-pull-xml.log" 2>&1; then
      rm -f "$evidence_dir/$prefix.xml"
    fi
    adb_device shell rm -f /sdcard/cotton-window.xml >/dev/null 2>&1 || true
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

apply_permission_state() {
  case "$permission_state" in
    preserve)
      printf 'Preserving existing Android notification permission state.\n' \
        > "$evidence_dir/06-permission-setup.txt"
      ;;
    fresh)
      {
        adb_device shell pm revoke "$package_id" android.permission.POST_NOTIFICATIONS || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/06-permission-setup.txt" 2>&1
      ;;
    allowed)
      {
        adb_device shell pm grant "$package_id" android.permission.POST_NOTIFICATIONS || true
        adb_device shell pm set-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/06-permission-setup.txt" 2>&1
      ;;
    denied)
      {
        adb_device shell pm revoke "$package_id" android.permission.POST_NOTIFICATIONS || true
        adb_device shell pm set-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/06-permission-setup.txt" 2>&1
      ;;
  esac
}

write_metadata
write_checklist

capture_text "00-device.txt" adb_device shell getprop
capture_text "01-adb-devices.txt" adb devices

if ! adb_device get-state > "$evidence_dir/02-device-state.txt" 2>&1; then
  printf 'ADB device is not available for serial %s. See %s/01-adb-devices.txt.\n' "$serial" "$evidence_dir" >&2
  exit 69
fi

device_state="$(tr -d '\r\n' < "$evidence_dir/02-device-state.txt")"
if [[ "$device_state" != "device" ]]; then
  printf 'ADB serial %s is in state %s, expected device.\n' "$serial" "$device_state" >&2
  exit 69
fi

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  capture_text "03-install-debug.txt" cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK"
fi

if ! adb_device shell pm path "$package_id" > "$evidence_dir/04-package.txt" 2>&1; then
  printf 'Package %s is not installed on %s. Use --install-debug or install a Play-delivered build first.\n' "$package_id" "$serial" >&2
  exit 69
fi

if ! adb_device shell dumpsys package "$package_id" > "$evidence_dir/05-package-dumpsys.txt" 2>&1; then
  printf 'Could not inspect installed package %s. See %s/05-package-dumpsys.txt.\n' "$package_id" "$evidence_dir" >&2
  exit 69
fi

installed_version_code="$(
  sed -n 's/.*versionCode=\([0-9][0-9]*\).*/\1/p' "$evidence_dir/05-package-dumpsys.txt" | head -1
)"
installed_version_name="$(
  sed -n 's/.*versionName=\([^[:space:]]*\).*/\1/p' "$evidence_dir/05-package-dumpsys.txt" | head -1
)"

{
  printf 'installed_version_code=%s\n' "$installed_version_code"
  printf 'installed_version_name=%s\n' "$installed_version_name"
  printf 'expected_version_code=%s\n' "$expected_version_code"
  printf 'expected_version_name=%s\n' "$expected_version_name"
} > "$evidence_dir/05-package-version.txt"

if [[ -n "$expected_version_code" && "$installed_version_code" != "$expected_version_code" ]]; then
  printf 'Installed %s versionCode is %s, expected %s. Evidence: %s\n' \
    "$package_id" "$installed_version_code" "$expected_version_code" "$evidence_dir" >&2
  exit 70
fi

if [[ -n "$expected_version_name" && "$installed_version_name" != "$expected_version_name" ]]; then
  printf 'Installed %s versionName is %s, expected %s. Evidence: %s\n' \
    "$package_id" "$installed_version_name" "$expected_version_name" "$evidence_dir" >&2
  exit 70
fi

apply_permission_state

capture_text "06-notification-permission.txt" adb_device shell dumpsys package "$package_id"
capture_text "07-notification-appops.txt" adb_device shell appops get "$package_id" POST_NOTIFICATION
capture_text "08-notification-dumpsys.txt" adb_device shell dumpsys notification --noredact
summarize_notification_dumpsys \
  "$evidence_dir/08-notification-dumpsys.txt" \
  "$evidence_dir/08-notification-channels.txt"

adb_device logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  capture_text "11-launch.txt" adb_device shell monkey -p "$package_id" 1
  sleep 2
fi

capture_device_state "10-preflight"

if [[ "$preflight_only" -eq 1 ]]; then
  printf '\nNotification settings preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

prompt_capture "Open Account -> Notifications. Verify the page matches the requested permission path." \
  "20-notifications-page"

prompt_capture "If applicable, tap Allow or Settings and leave the Android dialog/settings destination visible." \
  "30-permission-action"

capture_text "90-logcat.txt" adb_device logcat -d -v threadtime

printf '\nNotification settings smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md before marking notification allowed/denied runtime proof complete.\n'
