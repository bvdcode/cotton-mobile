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
skip_network_toggle=0
leave_network_disabled=0
network_disabled=0
preflight_only=0
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive offline folder-navigation smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --preflight-only          Capture device/package/version state and exit without manual prompts.
  --no-launch               Do not launch the app automatically.
  --skip-network-toggle     Do not disable Wi-Fi/mobile data; operator handles offline mode.
  --leave-network-disabled  Do not restore Wi-Fi/mobile data at the end.
  --help, -h                Show this help.

The script is intentionally manual: use the app while it captures screenshots,
UI XML, dumpsys window state, connectivity diagnostics, and logcat output.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--evidence-dir:evidence_dir"
  "--expected-version-code:expected_version_code"
  "--expected-version-name:expected_version_name"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--preflight-only:preflight_only:1"
  "--no-launch:launch_app:0"
  "--skip-network-toggle:skip_network_toggle:1"
  "--leave-network-disabled:leave_network_disabled:1"
)
cotton_parse_arguments "$@"

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Run it from a shell attached to the Android device or emulator.\n' >&2
  printf 'Use --preflight-only for non-interactive package/version evidence.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-offline-folder-navigation"
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
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'skip_network_toggle=%s\n' "$skip_network_toggle"
    printf 'leave_network_disabled=%s\n' "$leave_network_disabled"
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_logcat_docs=https://developer.android.com/tools/logcat\n'
    printf 'android_dumpsys_docs=https://developer.android.com/tools/dumpsys\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Offline Folder Navigation Smoke

Package: \`$package_id\`
Device: \`$serial\`

## Preconditions

- [ ] Signed-in session is restored without clearing app data.
- [ ] A target folder is visible while online.
- [ ] The target folder is kept offline and direct child files complete.

## Offline Pass

- [ ] Network is disabled or airplane-mode behavior is simulated.
- [ ] Cached root listing opens while offline.
- [ ] Cached folder listing opens while offline.
- [ ] Up navigation returns to the parent listing.
- [ ] Cached-listing age copy is visible and truthful.
- [ ] A kept-offline file opens from local bytes.
- [ ] Stale or missing offline bytes are labeled honestly if encountered.

## Evidence To Review

- \`00-device.txt\`
- \`04-package.txt\`
- \`05-package-version.txt\`
- \`10-online-ready.png\` and \`10-online-ready.xml\`
- \`20-online-kept-folder.png\` and \`20-online-kept-folder.xml\`
- \`30-network-disabled.txt\`
- \`40-offline-navigation.png\` and \`40-offline-navigation.xml\`
- \`90-logcat.txt\`
- \`91-connectivity.txt\`
EOF
}


capture_device_state() {
  local prefix="$1"

  cotton_capture_text_best_effort "$prefix-window.txt" cotton_adb shell dumpsys window
  cotton_capture_text_best_effort "$prefix-connectivity.txt" cotton_adb shell dumpsys connectivity
  cotton_capture_text_best_effort "$prefix-package.txt" cotton_adb shell pm path "$package_id"

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

prompt_continue() {
  local message="$1"
  printf '\n%s\n' "$message"
  printf 'Press Enter to continue... '
  read -r _
}

restore_network() {
  if [[ "$network_disabled" -eq 1 && "$leave_network_disabled" -eq 0 ]]; then
    printf '\nRestoring Wi-Fi and mobile data...\n'
    cotton_adb shell svc wifi enable >/dev/null 2>&1 || true
    cotton_adb shell svc data enable >/dev/null 2>&1 || true
    network_disabled=0
  fi
}

trap restore_network EXIT

write_metadata
write_checklist

cotton_prepare_installed_package

cotton_adb logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  cotton_capture_text_best_effort "05-launch.txt" cotton_adb shell monkey -p "$package_id" 1
  sleep 2
fi

if [[ "$preflight_only" -eq 1 ]]; then
  capture_device_state "10-preflight"
  printf '\nOffline folder-navigation preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

capture_device_state "10-online-ready"

prompt_continue "Online setup: sign in if needed, open Files, keep the target folder offline, and wait for the folder pack to finish."
capture_device_state "20-online-kept-folder"

if [[ "$skip_network_toggle" -eq 0 ]]; then
  cotton_capture_text_best_effort "29-network-before.txt" cotton_adb shell dumpsys connectivity
  cotton_adb shell svc wifi disable >/dev/null 2>&1 || true
  cotton_adb shell svc data disable >/dev/null 2>&1 || true
  network_disabled=1
  sleep 3
  cotton_capture_text_best_effort "30-network-disabled.txt" cotton_adb shell dumpsys connectivity
else
  printf 'Network toggle skipped by operator.\n' > "$evidence_dir/30-network-disabled.txt"
fi

prompt_continue "Offline pass: browse cached root/folder listings, enter/up the folder, verify cached-listing age copy, and open a kept-offline file."
capture_device_state "40-offline-navigation"

cotton_capture_text_best_effort "90-logcat.txt" cotton_adb logcat -d -v threadtime
cotton_capture_text_best_effort "91-connectivity.txt" cotton_adb shell dumpsys connectivity

restore_network
cotton_capture_text_best_effort "92-connectivity-after-restore.txt" cotton_adb shell dumpsys connectivity

printf '\nOffline folder-navigation smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md before marking the roadmap slice complete.\n'
