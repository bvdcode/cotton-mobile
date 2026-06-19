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
skip_network_toggle=0
leave_network_disabled=0
network_disabled=0

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive offline folder-navigation smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --no-launch               Do not launch the app automatically.
  --skip-network-toggle     Do not disable Wi-Fi/mobile data; operator handles offline mode.
  --leave-network-disabled  Do not restore Wi-Fi/mobile data at the end.
  --help, -h                Show this help.

The script is intentionally manual: use the app while it captures screenshots,
UI XML, dumpsys window state, connectivity diagnostics, and logcat output.
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
    --no-launch)
      launch_app=0
      shift
      ;;
    --skip-network-toggle)
      skip_network_toggle=1
      shift
      ;;
    --leave-network-disabled)
      leave_network_disabled=1
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

if [[ ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Run it from a shell attached to the Android device or emulator.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-offline-folder-navigation"
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
- \`10-online-ready.png\` and \`10-online-ready.xml\`
- \`20-online-kept-folder.png\` and \`20-online-kept-folder.xml\`
- \`30-network-disabled.txt\`
- \`40-offline-navigation.png\` and \`40-offline-navigation.xml\`
- \`90-logcat.txt\`
- \`91-connectivity.txt\`
EOF
}

capture_text() {
  local name="$1"
  shift
  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed: %q' "$1" >> "$evidence_dir/$name"
    printf '\n' >> "$evidence_dir/$name"
  fi
}

capture_device_state() {
  local prefix="$1"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window
  capture_text "$prefix-connectivity.txt" adb_device shell dumpsys connectivity
  capture_text "$prefix-package.txt" adb_device shell pm path "$package_id"

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

prompt_continue() {
  local message="$1"
  printf '\n%s\n' "$message"
  printf 'Press Enter to continue... '
  read -r _
}

restore_network() {
  if [[ "$network_disabled" -eq 1 && "$leave_network_disabled" -eq 0 ]]; then
    printf '\nRestoring Wi-Fi and mobile data...\n'
    adb_device shell svc wifi enable >/dev/null 2>&1 || true
    adb_device shell svc data enable >/dev/null 2>&1 || true
    network_disabled=0
  fi
}

trap restore_network EXIT

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

  capture_text "03-install-debug.txt" adb_device install --no-incremental -r "$COTTON_ANDROID_APK"
fi

if ! adb_device shell pm path "$package_id" > "$evidence_dir/04-package.txt" 2>&1; then
  printf 'Package %s is not installed on %s. Use --install-debug or install a Play-delivered build first.\n' "$package_id" "$serial" >&2
  exit 69
fi

adb_device logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  capture_text "05-launch.txt" adb_device shell monkey -p "$package_id" 1
  sleep 2
fi

capture_device_state "10-online-ready"

prompt_continue "Online setup: sign in if needed, open Files, keep the target folder offline, and wait for the folder pack to finish."
capture_device_state "20-online-kept-folder"

if [[ "$skip_network_toggle" -eq 0 ]]; then
  capture_text "29-network-before.txt" adb_device shell dumpsys connectivity
  adb_device shell svc wifi disable >/dev/null 2>&1 || true
  adb_device shell svc data disable >/dev/null 2>&1 || true
  network_disabled=1
  sleep 3
  capture_text "30-network-disabled.txt" adb_device shell dumpsys connectivity
else
  printf 'Network toggle skipped by operator.\n' > "$evidence_dir/30-network-disabled.txt"
fi

prompt_continue "Offline pass: browse cached root/folder listings, enter/up the folder, verify cached-listing age copy, and open a kept-offline file."
capture_device_state "40-offline-navigation"

capture_text "90-logcat.txt" adb_device logcat -d -v threadtime
capture_text "91-connectivity.txt" adb_device shell dumpsys connectivity

restore_network
capture_text "92-connectivity-after-restore.txt" adb_device shell dumpsys connectivity

printf '\nOffline folder-navigation smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md before marking the roadmap slice complete.\n'
