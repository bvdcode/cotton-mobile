#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
configuration="$COTTON_ANDROID_CONFIGURATION"
config_file="$COTTON_REPO_ROOT/src/Cotton.Mobile/Platforms/Android/google-services.json"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
preflight_only=0
require_registered=1
capture_diagnostics_ui=0
wait_seconds=10
expected_version_code=""
expected_version_name=""
diagnostics_xml=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs the Android remote-push token registration smoke for a Firebase-configured
Cotton build. The script does not print or store the FCM token.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --configuration NAME      Android build configuration. Defaults to COTTON_ANDROID_CONFIGURATION.
  --config-file PATH        Firebase google-services.json path.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --wait-seconds N          Seconds to wait after launch for session restore and token registration.
  --diagnostics-ui          Open Diagnostics and validate the Remote push section after launch.
  --allow-unregistered      Capture evidence without failing when registration is not proven.
  --preflight-only          Validate package/config/device state and exit before launching.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

Preconditions for a passing registration smoke:
  - google-services.json contains a client for the tested package id.
  - Google Play services are available on the device.
  - The app has a restorable signed-in session.
  - The backend profile exposes device-token registration.
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
    --configuration)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --configuration.\n' >&2
        exit 64
      fi
      configuration="$2"
      shift 2
      ;;
    --config-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --config-file.\n' >&2
        exit 64
      fi
      config_file="$2"
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
    --wait-seconds)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --wait-seconds.\n' >&2
        exit 64
      fi
      wait_seconds="$2"
      shift 2
      ;;
    --allow-unregistered)
      require_registered=0
      shift
      ;;
    --diagnostics-ui)
      capture_diagnostics_ui=1
      shift
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

if [[ ! "$wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Invalid --wait-seconds: %s\n' "$wait_seconds" >&2
  exit 64
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-remote-push-token"
fi

mkdir -p "$evidence_dir"

adb_device() {
  adb -s "$serial" "$@"
}

capture_text() {
  local name="$1"
  shift
  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed while writing %s.\n' "$name" >&2
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
    printf 'configuration=%s\n' "$configuration"
    printf 'config_file=%s\n' "$config_file"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'launch_app=%s\n' "$launch_app"
    printf 'preflight_only=%s\n' "$preflight_only"
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

  capture_text "$prefix-window.txt" adb_device shell dumpsys window || true
  if ! adb_device exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  if adb_device shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    adb_device shell cat /sdcard/cotton-window.xml > "$evidence_dir/$prefix.xml" || true
    adb_device shell rm -f /sdcard/cotton-window.xml >/dev/null 2>&1 || true
  fi
}

xml_has_text() {
  local xml_file="$1"
  local needle="$2"

  [[ -f "$xml_file" ]] && grep -Fq "$needle" "$xml_file"
}

require_xml_text() {
  local xml_file="$1"
  local needle="$2"
  local message="$3"

  if ! xml_has_text "$xml_file" "$needle"; then
    printf '%s\n' "$message" >&2
    printf 'Missing text: %s\n' "$needle" >&2
    printf 'Evidence: %s\n' "$xml_file" >&2
    exit 66
  fi
}

tap_node_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local mode="${3:-exact}"
  local point_file="$evidence_dir/tap-point.txt"

  python3 - "$xml_file" "$needle" "$mode" > "$point_file" <<'PY'
import re
import sys
from xml.etree import ElementTree

xml_file, needle, mode = sys.argv[1:4]
root = ElementTree.parse(xml_file).getroot()

def center(bounds: str) -> tuple[int, int]:
    match = re.fullmatch(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", bounds)
    if match is None:
        raise ValueError(bounds)
    left, top, right, bottom = [int(value) for value in match.groups()]
    return ((left + right) // 2, (top + bottom) // 2)

matches = []
for node in root.iter("node"):
    values = [
        node.attrib.get("text", ""),
        node.attrib.get("content-desc", ""),
    ]
    if any((value == needle if mode == "exact" else needle in value) for value in values):
        try:
            matches.append(center(node.attrib["bounds"]))
        except (KeyError, ValueError):
            pass

if not matches:
    raise SystemExit(f"Could not find UI node: {needle}")

print(matches[0][0], matches[0][1])
PY

  read -r tap_x tap_y < "$point_file"
  adb_device shell input tap "$tap_x" "$tap_y"
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
      adb_device shell input keyevent KEYCODE_BACK
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

  tap_node_from_xml "$source_xml" "$account_entry" exact
  sleep 2
  capture_window "30-account-actions"
  require_xml_text "$evidence_dir/30-account-actions.xml" "Diagnostics" "Diagnostics action is not visible."
  tap_node_from_xml "$evidence_dir/30-account-actions.xml" "Diagnostics" exact

  local attempt
  local xml_file
  for attempt in 0 1 2 3 4 5 6 7 8; do
    sleep 2
    capture_window "40-diagnostics-$attempt"
    xml_file="$evidence_dir/40-diagnostics-$attempt.xml"
    if xml_has_text "$xml_file" "Remote push"; then
      diagnostics_xml="$xml_file"
      break
    fi

    if [[ "$attempt" -lt 8 ]]; then
      adb_device shell input swipe 540 2100 540 850 450
    fi
  done

  if [[ -z "$diagnostics_xml" ]]; then
    printf 'Diagnostics Remote push section was not visible.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir" >&2
    exit 66
  fi

  require_xml_text "$diagnostics_xml" "Firebase Cloud Messaging" "Remote push provider is not visible."
  require_xml_text "$diagnostics_xml" "Android" "Remote push platform is not visible."
  require_xml_text "$diagnostics_xml" "Token" "Remote push token row is not visible."
  require_xml_text "$diagnostics_xml" "Registration" "Remote push registration row is not visible."

  if [[ "$registration_status" == "registered" ]]; then
    require_xml_text "$diagnostics_xml" "Available" "Diagnostics did not show an available platform token."
    require_xml_text "$diagnostics_xml" "Registered" "Diagnostics did not show registered session state."
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
  } > "$evidence_dir/91-result.txt"
}

write_metadata
run_firebase_config_preflight

capture_text "01-adb-devices.txt" adb devices
if ! adb_device get-state > "$evidence_dir/03-device-state.txt" 2>&1; then
  printf 'ADB device is not available for serial %s. Evidence: %s\n' "$serial" "$evidence_dir" >&2
  exit 69
fi

device_state="$(tr -d '\r\n' < "$evidence_dir/03-device-state.txt")"
if [[ "$device_state" != "device" ]]; then
  printf 'ADB serial %s is in state %s, expected device. Evidence: %s\n' \
    "$serial" "$device_state" "$evidence_dir" >&2
  exit 69
fi

if [[ "$install_debug" -eq 1 ]]; then
  "$SCRIPT_DIR/install-android-debug.sh" --no-launch > "$evidence_dir/04-install-debug.txt" 2>&1
fi

if ! adb_device shell pm path "$package_id" > "$evidence_dir/05-package.txt" 2>&1; then
  printf 'Package %s is not installed on %s. Use --install-debug or install a Play build first. Evidence: %s\n' \
    "$package_id" "$serial" "$evidence_dir" >&2
  exit 69
fi

capture_text "06-package-dumpsys.txt" adb_device shell dumpsys package "$package_id"
installed_version_code="$(
  sed -n 's/.*versionCode=\([0-9][0-9]*\).*/\1/p' "$evidence_dir/06-package-dumpsys.txt" | head -1
)"
installed_version_name="$(
  sed -n 's/.*versionName=\([^[:space:]]*\).*/\1/p' "$evidence_dir/06-package-dumpsys.txt" | head -1
)"

{
  printf 'installed_version_code=%s\n' "$installed_version_code"
  printf 'installed_version_name=%s\n' "$installed_version_name"
  printf 'expected_version_code=%s\n' "$expected_version_code"
  printf 'expected_version_name=%s\n' "$expected_version_name"
} > "$evidence_dir/07-package-version.txt"

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

capture_text "08-play-services.txt" adb_device shell dumpsys package com.google.android.gms

if [[ "$preflight_only" -eq 1 ]]; then
  printf '\nRemote-push token preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

adb_device logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  capture_text "10-launch.txt" adb_device shell monkey -p "$package_id" 1
fi

sleep "$wait_seconds"
capture_window "20-after-launch"

adb_device logcat -d -v threadtime |
  awk '/Cotton mobile remote push|remote push token registration|Firebase Cloud Messaging|Google Play services/' \
    > "$evidence_dir/90-remote-push-log.txt"

registration_status="no_signal"
if grep -q 'Registered the Cotton mobile remote push token for the current session.' \
  "$evidence_dir/90-remote-push-log.txt"; then
  registration_status="registered"
elif grep -q 'not configured' "$evidence_dir/90-remote-push-log.txt"; then
  registration_status="not_configured"
elif grep -q 'unavailable' "$evidence_dir/90-remote-push-log.txt"; then
  registration_status="unavailable"
fi

write_result "$registration_status"

if [[ "$capture_diagnostics_ui" -eq 1 ]]; then
  capture_and_validate_diagnostics
fi

if [[ "$require_registered" -eq 1 && "$registration_status" != "registered" ]]; then
  printf 'Remote-push token registration was not proven: %s. Evidence: %s\n' \
    "$registration_status" "$evidence_dir" >&2
  exit 65
fi

printf '\nRemote-push token smoke evidence: %s\n' "$evidence_dir"
printf 'Registration status: %s\n' "$registration_status"
