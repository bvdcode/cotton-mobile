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
verify_return=0
require_upload=0
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive Android document-scan smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --verify-return           Capture the return to Cotton after scanner completion or cancellation.
  --require-upload          Require a completed scanned PDF upload after returning to Cotton.
  --preflight-only          Capture device/package/version state and exit without manual prompts.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The default pass proves that Cotton exposes Scan document and launches the native
Android document scanner. Use --require-upload only when a tester can complete
the scanner flow with a real document or gallery import and allow the upload.
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
    --verify-return)
      verify_return=1
      shift
      ;;
    --require-upload)
      verify_return=1
      require_upload=1
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

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Use --preflight-only for non-interactive package/version evidence.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-document-scan"
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
    printf 'launch_app=%s\n' "$launch_app"
    printf 'preflight_only=%s\n' "$preflight_only"
    printf 'verify_return=%s\n' "$verify_return"
    printf 'require_upload=%s\n' "$require_upload"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'mlkit_document_scanner_docs=https://developers.google.com/ml-kit/vision/doc-scanner/android\n'
    printf 'mlkit_document_scanner_blog=https://android-developers.googleblog.com/2024/02/ml-kit-document-scanner-api.html\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Document Scan Smoke

Package: \`$package_id\`
Device: \`$serial\`
Verify return: \`$verify_return\`
Require upload: \`$require_upload\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Files root or target folder is visible with the \`Add files\` action.
- [ ] Google Play services document scanner is available on the device.
- [ ] A real document or gallery image is available if \`require_upload=1\`.

## Scanner Launch

- [ ] \`20-files-ready.png\` / \`20-files-ready.xml\` show Files chrome and \`Add files\`.
- [ ] \`30-add-actions.png\` / \`30-add-actions.xml\` show \`New folder\`, \`Upload...\`, and \`Scan document\`.
- [ ] Tapping \`Scan document\` opens the native Android document scanner, permission prompt, or Google Play services scanner surface.
- [ ] \`40-scanner-launched.png\` / \`40-scanner-launched.xml\` capture that native scanner surface.

## Optional Return / Upload

- [ ] If \`verify_return=1\`, \`50-scan-return.png\` / \`50-scan-return.xml\` show Cotton Files after scanner completion or cancellation.
- [ ] If \`require_upload=1\`, \`50-scan-return.xml\` shows a scanned PDF upload result or visible \`Scanned document ...pdf\` row.
- [ ] \`90-logcat.txt\` has no document-scan crashes.

## Evidence Files

- \`00-device.txt\`
- \`01-adb-devices.txt\`
- \`04-package.txt\`
- \`05-package-version.txt\`
- \`10-preflight.png\` / \`10-preflight.xml\`
- \`20-files-ready.png\` / \`20-files-ready.xml\`
- \`30-add-actions.png\` / \`30-add-actions.xml\`
- \`40-scanner-launched.png\` / \`40-scanner-launched.xml\`
- \`50-scan-return.png\` / \`50-scan-return.xml\` when return verification is enabled
- \`90-logcat.txt\`
EOF
}

capture_text() {
  local name="$1"
  shift
  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed while writing %s.\n' "$name" >&2
    return 1
  fi
}

capture_screen() {
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
  capture_screen "$prefix"
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
  if ! adb_device shell pm path "$package_id" > "$evidence_dir/04-package.txt" 2>&1; then
    printf 'Package %s is not installed on %s. Use --install-debug or install a Play build first. Evidence: %s\n' \
      "$package_id" "$serial" "$evidence_dir" >&2
    exit 69
  fi

  capture_text "05-package-dumpsys.txt" adb_device shell dumpsys package "$package_id"
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
}

assert_no_fatal_logcat() {
  local fatal_count

  fatal_count="$(grep -Ec 'FATAL EXCEPTION|ANR in|AndroidRuntime.*FATAL|SIGSEGV|libc.*Fatal signal' \
    "$evidence_dir/90-logcat.txt" || true)"
  if [[ "$fatal_count" != "0" ]]; then
    printf 'Fatal runtime log entries were captured. Evidence: %s\n' "$evidence_dir" >&2
    exit 65
  fi
}

write_metadata
write_checklist
capture_text "00-device.txt" adb_device shell getprop || true
ensure_device_ready

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  capture_text "03-install-debug.txt" cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK"
fi

capture_installed_package
capture_text "08-play-services.txt" adb_device shell dumpsys package com.google.android.gms || true

adb_device logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  capture_text "11-launch.txt" adb_device shell monkey -p "$package_id" 1
  sleep 2
fi

capture_screen "10-preflight"

if [[ "$preflight_only" -eq 1 ]]; then
  printf '\nDocument-scan preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

prompt_capture "Open Cotton Files in the folder where the scanned PDF should upload. Leave Add files visible." \
  "20-files-ready"
require_xml_text "$evidence_dir/20-files-ready.xml" "Files" "Files screen title is missing."
require_xml_text "$evidence_dir/20-files-ready.xml" "Add files" "Files screen did not expose Add files."

prompt_capture "Tap Add files and leave the native Add action sheet visible." \
  "30-add-actions"
require_xml_text "$evidence_dir/30-add-actions.xml" "New folder" "Add sheet did not show New folder."
require_xml_text "$evidence_dir/30-add-actions.xml" "Upload..." "Add sheet did not show Upload..."
require_xml_text "$evidence_dir/30-add-actions.xml" "Scan document" "Add sheet did not show Scan document."

prompt_capture "Tap Scan document. Leave the native scanner, permission prompt, or scanner error surface visible." \
  "40-scanner-launched"
require_xml_any_text "$evidence_dir/40-scanner-launched.xml" \
  "Document scanner launch surface was not recognized." \
  "Document" \
  "Scan" \
  "Camera" \
  "Allow" \
  "Import" \
  "Google" \
  "Lens"

if [[ "$verify_return" -eq 1 ]]; then
  prompt_capture "Complete or cancel the scanner flow and return to Cotton Files." \
    "50-scan-return"
  require_xml_text "$evidence_dir/50-scan-return.xml" "Files" "Cotton Files was not visible after scanner return."

  if [[ "$require_upload" -eq 1 ]]; then
    require_xml_any_text "$evidence_dir/50-scan-return.xml" \
      "Scanned PDF upload was not visible after scanner return." \
      "Uploaded Scanned document" \
      "Scanned document" \
      ".pdf" \
      "PDF"
  fi
fi

capture_text "90-logcat.txt" adb_device logcat -d -v threadtime || true
assert_no_fatal_logcat

printf '\nDocument-scan smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md before marking scanner launch or completed PDF upload proof done.\n'
