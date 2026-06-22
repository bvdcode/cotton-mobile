#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
configuration="$COTTON_ANDROID_CONFIGURATION"
config_file="$COTTON_REPO_ROOT/src/Cotton.Mobile/Platforms/Android/google-services.json"
config_source_file=""
config_source_env_name=""
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
preflight_only=0
launch_app=1
require_logout_revoke=1
require_logout_refresh_cancel=1
capture_diagnostics_ui=0
reinstall_mode="update"
token_wait_seconds=10
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive Android remote-push lifecycle smoke for a Firebase-configured
Cotton build. The script never prints or stores the FCM token.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --configuration NAME      Android build configuration. Defaults to COTTON_ANDROID_CONFIGURATION.
  --config-file PATH        Firebase google-services.json path.
  --config-source-file PATH Restore google-services.json from this local source before preflight.
  --config-source-env NAME  Restore google-services.json from this environment variable before preflight.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before token proof.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --token-wait-seconds N    Seconds to wait for token registration after app launch.
  --diagnostics-ui          Validate the Diagnostics Remote push section during token proof.
  --reinstall-mode MODE     Post-logout reinstall check: none, update, or fresh. Defaults to update.
  --allow-missing-revoke    Capture logout evidence without failing on a missing revoke log.
  --allow-missing-refresh-cancel
                            Capture logout evidence without failing on a missing refresh-cancel log.
  --preflight-only          Validate package/config/device state and exit before manual prompts.
  --no-launch               Do not launch the app in the token registration step.
  --help, -h                Show this help.

Preconditions for a passing lifecycle smoke:
  - google-services.json contains a client for the tested package id.
  - Google Play services are available on the device.
  - The app has a restorable signed-in session.
  - The backend profile exposes push token registration, revocation, and preferences.
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
    --config-source-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --config-source-file.\n' >&2
        exit 64
      fi
      config_source_file="$2"
      shift 2
      ;;
    --config-source-env)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --config-source-env.\n' >&2
        exit 64
      fi
      config_source_env_name="$2"
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
    --token-wait-seconds)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --token-wait-seconds.\n' >&2
        exit 64
      fi
      token_wait_seconds="$2"
      shift 2
      ;;
    --reinstall-mode)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --reinstall-mode.\n' >&2
        exit 64
      fi
      reinstall_mode="$2"
      shift 2
      ;;
    --allow-missing-revoke)
      require_logout_revoke=0
      shift
      ;;
    --allow-missing-refresh-cancel)
      require_logout_refresh_cancel=0
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

case "$reinstall_mode" in
  none|update|fresh)
    ;;
  *)
    printf 'Invalid --reinstall-mode: %s. Expected none, update, or fresh.\n' "$reinstall_mode" >&2
    exit 64
    ;;
esac

if [[ ! "$token_wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Invalid --token-wait-seconds: %s\n' "$token_wait_seconds" >&2
  exit 64
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Use --preflight-only for non-interactive package/config evidence.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-remote-push-lifecycle"
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
  local config_source="none"
  if [[ -n "$config_source_file" ]]; then
    config_source="file"
  elif [[ -n "$config_source_env_name" ]]; then
    config_source="env"
  fi

  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'repo=%s\n' "$COTTON_REPO_ROOT"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'configuration=%s\n' "$configuration"
    printf 'config_file=%s\n' "$config_file"
    printf 'config_source=%s\n' "$config_source"
    printf 'config_source_env_name=%s\n' "$config_source_env_name"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'launch_app=%s\n' "$launch_app"
    printf 'preflight_only=%s\n' "$preflight_only"
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

  capture_text "$prefix-window.txt" adb_device shell dumpsys window || true
  capture_text "$prefix-activity.txt" adb_device shell dumpsys activity top || true

  if ! adb_device exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  if adb_device shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    adb_device shell cat /sdcard/cotton-window.xml > "$evidence_dir/$prefix.xml" || true
    adb_device shell rm -f /sdcard/cotton-window.xml >/dev/null 2>&1 || true
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

require_xml_without_text() {
  local xml_file="$1"
  local needle="$2"
  local message="$3"

  if xml_has_text "$xml_file" "$needle"; then
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
    if xml_has_text "$xml_file" "$needle"; then
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

  require_xml_text "$xml_file" "Notifications" "$state_name page title is missing."
  require_xml_text "$xml_file" "Server push" "$state_name did not show Server push."
  require_xml_text "$xml_file" "Shared-file activity" "$state_name did not show shared-file alerts."
  require_xml_text "$xml_file" "Security and sessions" "$state_name did not show security/session alerts."
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

require_signed_out_state() {
  local xml_file="$1"
  local state_name="$2"

  require_xml_any_text "$xml_file" "$state_name did not show the signed-out screen." \
    "Sign in to your Cotton Cloud" \
    "Server URL"
  require_xml_text "$xml_file" "Connect" "$state_name did not expose Connect."
}

build_token_smoke_args() {
  token_smoke_args=(
    --package "$package_id"
    --serial "$serial"
    --configuration "$configuration"
    --config-file "$config_file"
    --evidence-dir "$evidence_dir/10-token-registration"
    --wait-seconds "$token_wait_seconds"
  )

  if [[ -n "$config_source_file" ]]; then
    token_smoke_args+=(--config-source-file "$config_source_file")
  fi

  if [[ -n "$config_source_env_name" ]]; then
    token_smoke_args+=(--config-source-env "$config_source_env_name")
  fi

  if [[ "$install_debug" -eq 1 ]]; then
    token_smoke_args+=(--install-debug)
  fi

  if [[ "$capture_diagnostics_ui" -eq 1 ]]; then
    token_smoke_args+=(--diagnostics-ui)
  fi

  if [[ "$launch_app" -eq 0 ]]; then
    token_smoke_args+=(--no-launch)
  fi

  if [[ -n "$expected_version_code" ]]; then
    token_smoke_args+=(--expected-version-code "$expected_version_code")
  fi

  if [[ -n "$expected_version_name" ]]; then
    token_smoke_args+=(--expected-version-name "$expected_version_name")
  fi
}

run_token_preflight() {
  build_token_smoke_args
  "$SCRIPT_DIR/smoke-remote-push-token.sh" \
    "${token_smoke_args[@]}" \
    --preflight-only \
    > "$evidence_dir/09-token-preflight.txt" 2>&1
}

run_token_registration_smoke() {
  build_token_smoke_args
  "$SCRIPT_DIR/smoke-remote-push-token.sh" \
    "${token_smoke_args[@]}" \
    > "$evidence_dir/09-token-registration.txt" 2>&1
}

ensure_device_ready() {
  capture_text "01-adb-devices.txt" adb devices

  if ! adb_device get-state > "$evidence_dir/02-device-state.txt" 2>&1; then
    printf 'ADB device is not available for serial %s. Evidence: %s\n' "$serial" "$evidence_dir" >&2
    exit 69
  fi

  device_state="$(tr -d '\r\n' < "$evidence_dir/02-device-state.txt")"
  if [[ "$device_state" != "device" ]]; then
    printf 'ADB serial %s is in state %s, expected device. Evidence: %s\n' \
      "$serial" "$device_state" "$evidence_dir" >&2
    exit 69
  fi
}

capture_installed_package() {
  if ! adb_device shell pm path "$package_id" > "$evidence_dir/03-package.txt" 2>&1; then
    printf 'Package %s is not installed on %s. Use --install-debug or install a Play build first. Evidence: %s\n' \
      "$package_id" "$serial" "$evidence_dir" >&2
    exit 69
  fi

  capture_text "03-package-dumpsys.txt" adb_device shell dumpsys package "$package_id"
  installed_version_code="$(
    sed -n 's/.*versionCode=\([0-9][0-9]*\).*/\1/p' "$evidence_dir/03-package-dumpsys.txt" | head -1
  )"
  installed_version_name="$(
    sed -n 's/.*versionName=\([^[:space:]]*\).*/\1/p' "$evidence_dir/03-package-dumpsys.txt" | head -1
  )"

  {
    printf 'installed_version_code=%s\n' "$installed_version_code"
    printf 'installed_version_name=%s\n' "$installed_version_name"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
  } > "$evidence_dir/04-package-version.txt"

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
}

capture_remote_push_lifecycle_log() {
  adb_device logcat -d -v threadtime |
    awk '/Cotton mobile remote push|remote push token|Firebase Cloud Messaging|remote logout|Logout failed|FATAL EXCEPTION/' \
      > "$evidence_dir/90-remote-push-lifecycle-log.txt"
}

write_result() {
  local logout_revocation_status="no_signal"
  local logout_refresh_cancel_status="no_signal"
  local logout_refresh_cancel_log_count
  local fatal_count
  local sign_in_xml_count="0"

  if grep -q 'Revoked .* Cotton mobile remote push token(s) for the current session.' \
    "$evidence_dir/90-remote-push-lifecycle-log.txt"; then
    logout_revocation_status="revoked"
  elif grep -q 'Failed to revoke Cotton mobile remote push tokens for the current session.' \
    "$evidence_dir/90-remote-push-lifecycle-log.txt"; then
    logout_revocation_status="failed"
  fi

  if grep -q 'Cancelled .*remote push token refresh' \
    "$evidence_dir/90-remote-push-lifecycle-log.txt"; then
    logout_refresh_cancel_status="cancelled"
  elif grep -q 'Failed to cancel Cotton mobile remote push token refresh' \
    "$evidence_dir/90-remote-push-lifecycle-log.txt"; then
    logout_refresh_cancel_status="failed"
  fi

  logout_refresh_cancel_log_count="$(
    grep -c 'Cancelled .*remote push token refresh' "$evidence_dir/90-remote-push-lifecycle-log.txt" || true
  )"
  fatal_count="$(grep -c 'FATAL EXCEPTION' "$evidence_dir/90-remote-push-lifecycle-log.txt" || true)"
  if [[ -f "$evidence_dir/60-after-reinstall.xml" ]]; then
    sign_in_xml_count="$(
      grep -Eic 'Sign in|Signed out|Connect' "$evidence_dir/60-after-reinstall.xml" || true
    )"
  fi

  {
    printf 'token_registration_status=%s\n' "$(
      sed -n 's/^registration_status=//p' "$evidence_dir/10-token-registration/91-result.txt" 2>/dev/null | head -1
    )"
    printf 'logout_revocation_status=%s\n' "$logout_revocation_status"
    printf 'logout_refresh_cancel_status=%s\n' "$logout_refresh_cancel_status"
    printf 'logout_refresh_cancel_log_count=%s\n' "$logout_refresh_cancel_log_count"
    printf 'fatal_log_count=%s\n' "$fatal_count"
    printf 'reinstall_mode=%s\n' "$reinstall_mode"
    printf 'reinstall_signed_out_xml_match_count=%s\n' "$sign_in_xml_count"
  } > "$evidence_dir/91-result.txt"

  if [[ "$fatal_count" != "0" ]]; then
    printf 'Fatal runtime log entries were captured. Evidence: %s\n' "$evidence_dir" >&2
    exit 65
  fi

  if [[ "$require_logout_revoke" -eq 1 && "$logout_revocation_status" != "revoked" ]]; then
    printf 'Logout remote-push token revocation was not proven: %s. Evidence: %s\n' \
      "$logout_revocation_status" "$evidence_dir" >&2
    exit 65
  fi

  if [[ "$require_logout_refresh_cancel" -eq 1 && "$logout_refresh_cancel_status" != "cancelled" ]]; then
    printf 'Logout remote-push token refresh cancellation was not proven: %s. Evidence: %s\n' \
      "$logout_refresh_cancel_status" "$evidence_dir" >&2
    exit 65
  fi
}

run_reinstall_check() {
  case "$reinstall_mode" in
    none)
      return
      ;;
    update)
      if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
        printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first or use --reinstall-mode none.\n' \
          "$COTTON_ANDROID_APK" >&2
        exit 66
      fi

      capture_text "60-reinstall.txt" cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK"
      ;;
    fresh)
      if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
        printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first or use --reinstall-mode none.\n' \
          "$COTTON_ANDROID_APK" >&2
        exit 66
      fi

      capture_text "60-uninstall.txt" adb_device uninstall "$package_id"
      capture_text "60-reinstall.txt" adb_device install --no-incremental "$COTTON_ANDROID_APK"
      ;;
  esac

  capture_text "60-launch.txt" adb_device shell monkey -p "$package_id" 1
  sleep 3
  capture_window "60-after-reinstall"
  require_signed_out_state "$evidence_dir/60-after-reinstall.xml" "Post-reinstall"
}

write_metadata
write_checklist
ensure_device_ready

if [[ "$install_debug" -eq 1 ]]; then
  "$SCRIPT_DIR/install-android-debug.sh" --no-launch > "$evidence_dir/03-install-debug.txt" 2>&1
fi

capture_installed_package

set +e
run_token_preflight
token_preflight_status=$?
set -e

if [[ "$token_preflight_status" -ne 0 ]]; then
  cat "$evidence_dir/09-token-preflight.txt" >&2
  printf 'Remote-push lifecycle stopped at token preflight. Evidence: %s\n' "$evidence_dir" >&2
  exit "$token_preflight_status"
fi

if [[ "$preflight_only" -eq 1 ]]; then
  printf '\nRemote-push lifecycle preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

run_token_registration_smoke

prompt_capture \
  "Open Account -> Notifications. If Android notifications are not allowed, tap Allow and grant the permission." \
  "20-notification-opt-in"

prompt_capture \
  "Turn supported server-push categories off and leave the Notification Settings page visible." \
  "30-server-push-opt-out"
require_server_push_switches "$evidence_dir/30-server-push-opt-out.xml" false "Server-push opt-out"

prompt_capture \
  "Turn supported server-push categories back on and leave the Notification Settings page visible." \
  "40-server-push-opt-in"
require_server_push_switches "$evidence_dir/40-server-push-opt-in.xml" true "Server-push opt-in"

adb_device logcat -c >/dev/null 2>&1 || true

prompt_capture \
  "Log out from the account menu and wait for the signed-out screen." \
  "50-after-logout"
require_signed_out_state "$evidence_dir/50-after-logout.xml" "After logout"

capture_remote_push_lifecycle_log
run_reinstall_check
write_result

printf '\nRemote-push lifecycle smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md and 91-result.txt before marking lifecycle proof complete.\n'
