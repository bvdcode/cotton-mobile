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
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive security-settings smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --preflight-only          Capture device/package/security state and exit without manual prompts.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The script is intentionally manual: open Account -> Security in Cotton while
it captures screenshots, UIAutomator XML, package/appops state, device lock
diagnostics, dumpsys window state, and logcat output.
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

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Run it from a shell attached to the Android device or emulator.\n' >&2
  printf 'Use --preflight-only for non-interactive package/security evidence.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-security-settings"
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
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
    printf 'android_dumpsys_docs=https://developer.android.com/tools/dumpsys\n'
    printf 'android_logcat_docs=https://developer.android.com/tools/logcat\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Security Settings Smoke

Package: \`$package_id\`
Device: \`$serial\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Files root is visible and Account menu is reachable.
- [ ] The smoke is run on a device/emulator state chosen for the app-lock path: device lock configured or intentionally unavailable.

## Security Surface

- [ ] Account -> Security opens without requesting a new Android permission.
- [ ] Security page title and status render without clipping.
- [ ] App lock card shows truthful availability and enabled/disabled state.
- [ ] App lock toggle either enables through device-unlock verification or reports why device unlock is unavailable.
- [ ] Hide previews / secure-window state remains consistent with App lock settings.
- [ ] Clear cache on logout setting is visible and can be toggled without signing out.
- [ ] Permission ledger card is visible, compact, and does not trigger permission prompts on entry.
- [ ] Permission ledger rows include notifications, photos/videos, device lock, selected files/share intake, document scan, and network/offline access where applicable.
- [ ] Devices and sessions card loads the current account sessions or an honest unavailable/empty state.
- [ ] Current session row is clearly marked when sessions are available.
- [ ] Revoke current session opens a confirmation guard; cancel keeps the user signed in.

## App Lock Resume Pass

- [ ] If App lock can be enabled, enable it and background the app for longer than the configured timeout.
- [ ] Returning to Cotton shows the app-lock gate before file content is visible.
- [ ] Successful device unlock returns to the previous surface.
- [ ] Failed/cancelled unlock keeps protected content hidden.

## Sensitive Cache Policy Pass

- [ ] Open or share a sensitive file such as \`.env\`, \`private-key.pem\`, or \`service-account.p12\`.
- [ ] The unpinned sensitive file is not shown as \`On device\` after the transient action.
- [ ] Keeping the same sensitive file offline explicitly shows \`On device\`.
- [ ] Removing offline clears the marker and leaves the remote file intact.

## Evidence To Review

- \`00-device.txt\`
- \`04-package.txt\`
- \`05-package-version.txt\`
- \`06-package-permissions.txt\`
- \`07-appops.txt\`
- \`08-device-lock.txt\`
- \`10-preflight.png\` / \`10-preflight.xml\`
- \`20-account-menu.png\` / \`20-account-menu.xml\`
- \`30-security-top.png\` / \`30-security-top.xml\`
- \`40-security-ledger.png\` / \`40-security-ledger.xml\`
- \`50-security-sessions.png\` / \`50-security-sessions.xml\`
- \`60-revoke-confirmation.png\` / \`60-revoke-confirmation.xml\`
- \`70-app-lock-gate.png\` / \`70-app-lock-gate.xml\`
- \`80-sensitive-cache-policy.png\` / \`80-sensitive-cache-policy.xml\`
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

capture_security_state() {
  local prefix="$1"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window
  capture_text "$prefix-activity.txt" adb_device shell dumpsys activity top
  capture_text "$prefix-package-permissions.txt" adb_device shell dumpsys package "$package_id"
  capture_text "$prefix-appops.txt" adb_device shell appops get "$package_id"
  capture_text "$prefix-device-lock.txt" adb_device shell dumpsys trust
  capture_text "$prefix-keyguard.txt" adb_device shell dumpsys activity service KeyguardService

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
  capture_security_state "$prefix"
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

capture_text "06-package-permissions.txt" adb_device shell dumpsys package "$package_id"
capture_text "07-appops.txt" adb_device shell appops get "$package_id"
capture_text "08-device-lock.txt" adb_device shell dumpsys trust
capture_text "09-keyguard.txt" adb_device shell dumpsys activity service KeyguardService

adb_device logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  capture_text "11-launch.txt" adb_device shell monkey -p "$package_id" 1
  sleep 2
fi

capture_security_state "10-preflight"

if [[ "$preflight_only" -eq 1 ]]; then
  printf '\nSecurity settings preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

prompt_capture "Open the Account menu from Files. Leave the menu visible with the Security entry." \
  "20-account-menu"

prompt_capture "Open Security. Capture the top of the page with App lock and cache/logout controls visible." \
  "30-security-top"

prompt_capture "Scroll to the Permission ledger card. Verify it is compact and truthful, then capture it." \
  "40-security-ledger"

prompt_capture "Scroll to Devices and sessions. Verify current-session state or honest unavailable copy." \
  "50-security-sessions"

prompt_capture "Tap Revoke current session, leave the confirmation visible, then capture before cancelling." \
  "60-revoke-confirmation"

prompt_capture "If App lock is enabled, background and resume after timeout, then capture the lock gate." \
  "70-app-lock-gate"

prompt_capture "If running the sensitive-cache pass, capture the file row/action state that proves the policy." \
  "80-sensitive-cache-policy"

capture_text "90-logcat.txt" adb_device logcat -d -v threadtime

printf '\nSecurity settings smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md before marking Stage 13 security smoke complete.\n'
