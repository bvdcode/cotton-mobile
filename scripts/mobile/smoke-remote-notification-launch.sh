#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
component_activity="crc647f4f3c52a3509f5a.MainActivity"
notification_id="11111111-2222-3333-4444-555555555555"
event_category="SecuritySession"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a remote-notification launch smoke by sending the same Android Activity
intent extras that Cotton local notifications place in their PendingIntent.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --notification-id UUID    Notification id extra to send.
  --event-category NAME     Event category extra to send. Defaults to SecuritySession.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --help, -h                Show this help.

The smoke expects an installed app with a restorable signed-in session. It fails
if the simulated notification tap does not land on Cotton's Notifications page.
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
    --notification-id)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --notification-id.\n' >&2
        exit 64
      fi
      notification_id="$2"
      shift 2
      ;;
    --event-category)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --event-category.\n' >&2
        exit 64
      fi
      event_category="$2"
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

case "$event_category" in
  SharedFile|AccessRequest|CommentMention|SecuritySession)
    ;;
  *)
    printf 'Invalid --event-category: %s.\n' "$event_category" >&2
    exit 64
    ;;
esac

if [[ ! "$notification_id" =~ ^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$ ]]; then
  printf 'Invalid --notification-id: %s.\n' "$notification_id" >&2
  exit 64
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-remote-notification-launch"
fi

mkdir -p "$evidence_dir"

adb_device() {
  adb -s "$serial" "$@"
}

capture_text() {
  local name="$1"
  shift
  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed while writing %s. See %s/%s.\n' "$name" "$evidence_dir" "$name" >&2
    return 1
  fi
}

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'repo=%s\n' "$COTTON_REPO_ROOT"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'notification_id=%s\n' "$notification_id"
    printf 'event_category=%s\n' "$event_category"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_notification_navigation_docs=https://developer.android.com/develop/ui/views/notifications/navigation\n'
    printf 'android_dumpsys_docs=https://developer.android.com/tools/dumpsys\n'
  } > "$evidence_dir/00-metadata.txt"
}

capture_window() {
  local prefix="$1"
  capture_text "$prefix-window.txt" adb_device shell dumpsys window
  capture_text "$prefix-activity.txt" adb_device shell dumpsys activity top

  if ! adb_device exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    printf 'Could not capture screenshot. See %s/%s-screencap.err.\n' "$evidence_dir" "$prefix" >&2
  fi

  if adb_device shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    adb_device shell cat /sdcard/cotton-window.xml > "$evidence_dir/$prefix-window.xml"
  else
    printf 'Could not capture UIAutomator XML. See %s/%s-uiautomator.log.\n' "$evidence_dir" "$prefix" >&2
  fi
}

if [[ "$install_debug" -eq 1 ]]; then
  "$SCRIPT_DIR/install-android-debug.sh" --no-launch > "$evidence_dir/01-install-debug.txt" 2>&1
fi

write_metadata

capture_text "02-adb-devices.txt" adb devices
capture_text "03-package-dumpsys.txt" adb_device shell dumpsys package "$package_id"

version_code="$(
  sed -n 's/.*versionCode=\([0-9][0-9]*\).*/\1/p' "$evidence_dir/03-package-dumpsys.txt" | head -1
)"
version_name="$(
  sed -n 's/.*versionName=\([^[:space:]]*\).*/\1/p' "$evidence_dir/03-package-dumpsys.txt" | head -1
)"

if [[ -n "$expected_version_code" && "$version_code" != "$expected_version_code" ]]; then
  printf 'Installed %s versionCode is %s, expected %s. Evidence: %s\n' \
    "$package_id" "${version_code:-unknown}" "$expected_version_code" "$evidence_dir" >&2
  exit 65
fi

if [[ -n "$expected_version_name" && "$version_name" != "$expected_version_name" ]]; then
  printf 'Installed %s versionName is %s, expected %s. Evidence: %s\n' \
    "$package_id" "${version_name:-unknown}" "$expected_version_name" "$evidence_dir" >&2
  exit 65
fi

adb_device logcat -c >/dev/null 2>&1 || true

capture_text "04-notification-launch-intent.txt" \
  adb_device shell am start \
    -n "$package_id/$component_activity" \
    --ez dev.cottoncloud.app.extra.NOTIFICATION_LAUNCH true \
    --es dev.cottoncloud.app.extra.NOTIFICATION_ID "$notification_id" \
    --es dev.cottoncloud.app.extra.NOTIFICATION_EVENT_CATEGORY "$event_category"

sleep 5

capture_window "05-after-launch"
capture_text "06-logcat.txt" adb_device logcat -d -v threadtime

xml_path="$evidence_dir/05-after-launch-window.xml"
if [[ ! -s "$xml_path" ]]; then
  printf 'UIAutomator XML was not captured. Evidence: %s\n' "$evidence_dir" >&2
  exit 66
fi

if ! grep -q 'Notifications' "$xml_path"; then
  printf 'Notifications page title was not found after notification launch. Evidence: %s\n' "$evidence_dir" >&2
  exit 67
fi

if ! grep -Eq 'Server push|Push preferences|Android permission|Allowed|Denied|Not requested' "$xml_path"; then
  printf 'Notification settings content was not found after notification launch. Evidence: %s\n' "$evidence_dir" >&2
  exit 67
fi

printf '\nRemote notification launch smoke evidence: %s\n' "$evidence_dir"
