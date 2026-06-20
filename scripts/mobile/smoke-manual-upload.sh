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
seed_only=0
skip_seed=0
expected_version_code=""
expected_version_name=""
upload_file_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive manual upload smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --upload-file-name NAME   Seed/upload file name. Defaults to a timestamped cotton-manual-upload-*.txt.
  --preflight-only          Capture device/package/version state and exit without manual prompts.
  --seed-only               Seed the Android Downloads upload file and exit.
  --skip-seed-file          Do not seed the Android Downloads upload file.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The script is intentionally manual: use the Files Add action and Android
DocumentsUI picker while it captures screenshots, UIAutomator XML, package
state, the seeded local file, and logcat output.
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
    --upload-file-name)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --upload-file-name.\n' >&2
        exit 64
      fi
      upload_file_name="$2"
      shift 2
      ;;
    --preflight-only)
      preflight_only=1
      shift
      ;;
    --seed-only)
      seed_only=1
      shift
      ;;
    --skip-seed-file)
      skip_seed=1
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

if [[ -z "$upload_file_name" ]]; then
  upload_file_name="cotton-manual-upload-$(date -u +%Y%m%dT%H%M%SZ).txt"
fi

if [[ -z "${upload_file_name//[[:space:]]/}" || "$upload_file_name" == *"/"* ]]; then
  printf 'Upload file name must not be blank and must not contain a slash.\n' >&2
  exit 64
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && "$seed_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Use --preflight-only for package/version evidence or --seed-only to push the upload file.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-manual-upload"
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
    printf 'seed_only=%s\n' "$seed_only"
    printf 'skip_seed_file=%s\n' "$skip_seed"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'upload_file_name=%s\n' "$upload_file_name"
    printf 'maui_file_picker_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/file-picker\n'
    printf 'android_documents_docs=https://developer.android.com/training/data-storage/shared/documents-files\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Manual Upload Smoke

Package: \`$package_id\`
Device: \`$serial\`
Seeded file: \`$upload_file_name\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] \`06-seed-upload-file.txt\` shows \`$upload_file_name\` pushed to Android Downloads.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Files root or target folder is visible with the \`Add files\` action.

## Upload Pass

- [ ] \`20-files-ready.png\` / \`20-files-ready.xml\` show Files chrome and \`Add files\`.
- [ ] \`30-add-actions.png\` / \`30-add-actions.xml\` show the Add action sheet with \`Upload file\`.
- [ ] \`40-file-picker.png\` / \`40-file-picker.xml\` show Android DocumentsUI for file selection.
- [ ] Selecting \`$upload_file_name\` returns to Cotton Files.
- [ ] \`50-upload-return.png\` / \`50-upload-return.xml\` show the upload completion status or the uploaded row.
- [ ] \`60-upload-search.png\` / \`60-upload-search.xml\` show \`$upload_file_name\` as a Text file in Cotton search results.
- [ ] \`70-search-cleared.png\` / \`70-search-cleared.xml\` show search cleared and Files restored.
- [ ] \`90-logcat.txt\` has no manual upload crashes.

## Evidence To Review

- \`00-device.txt\`
- \`04-package.txt\`
- \`05-package-version.txt\`
- \`06-seed-upload-file.txt\`
- \`20-files-ready.png\` / \`20-files-ready.xml\`
- \`30-add-actions.png\` / \`30-add-actions.xml\`
- \`40-file-picker.png\` / \`40-file-picker.xml\`
- \`50-upload-return.png\` / \`50-upload-return.xml\`
- \`60-upload-search.png\` / \`60-upload-search.xml\`
- \`70-search-cleared.png\` / \`70-search-cleared.xml\`
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

capture_screen() {
  local prefix="$1"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window
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

require_xml_text() {
  local xml_file="$1"
  local needle="$2"
  local message="$3"

  if [[ ! -f "$xml_file" ]] || ! grep -Fq "$needle" "$xml_file"; then
    printf '%s\n' "$message" >&2
    printf 'Missing text: %s\n' "$needle" >&2
    printf 'Evidence: %s\n' "$xml_file" >&2
    exit 66
  fi
}

verify_expected_version() {
  if [[ -n "$expected_version_code" ]] \
    && ! grep -Fq "versionCode=$expected_version_code" "$evidence_dir/05-package-version.txt"; then
    printf 'Installed versionCode does not match expected value %s.\n' "$expected_version_code" >&2
    exit 67
  fi

  if [[ -n "$expected_version_name" ]] \
    && ! grep -Fq "versionName=$expected_version_name" "$evidence_dir/05-package-version.txt"; then
    printf 'Installed versionName does not match expected value %s.\n' "$expected_version_name" >&2
    exit 67
  fi
}

seed_upload_file() {
  local seed_dir="$evidence_dir/seed-files"
  local seed_file="$seed_dir/$upload_file_name"
  mkdir -p "$seed_dir"

  {
    printf 'Cotton manual upload smoke file.\n'
    printf 'Created at UTC: %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'Package: %s\n' "$package_id"
  } > "$seed_file"

  : > "$evidence_dir/06-seed-upload-file.txt"
  adb_device push "$seed_file" "/sdcard/Download/$upload_file_name" \
    >> "$evidence_dir/06-seed-upload-file.txt" 2>&1
  adb_device shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file:///sdcard/Download/$upload_file_name" \
    >> "$evidence_dir/06-seed-upload-file.txt" 2>&1 || true
  adb_device shell ls -la "/sdcard/Download/$upload_file_name" \
    >> "$evidence_dir/06-seed-upload-file.txt" 2>&1
}

wait_for_operator() {
  local prompt="$1"

  printf '\n%s\n' "$prompt"
  printf 'Press Enter when ready to capture evidence... '
  read -r _
}

write_metadata
write_checklist

capture_text "00-device.txt" adb_device shell getprop ro.product.model
capture_text "01-adb-devices.txt" adb devices
capture_text "02-window.txt" adb_device shell dumpsys window

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  adb_device install --no-incremental -r "$COTTON_ANDROID_APK" > "$evidence_dir/07-install.txt"
fi

capture_text "03-package-path.txt" adb_device shell pm path "$package_id"
capture_text "04-package.txt" adb_device shell dumpsys package "$package_id"
capture_text "05-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
verify_expected_version

if [[ "$skip_seed" -eq 0 ]]; then
  seed_upload_file
fi

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c || true
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/08-launch.txt"
  sleep 3
fi

capture_screen "10-launch"

if [[ "$preflight_only" -eq 1 || "$seed_only" -eq 1 ]]; then
  printf 'Manual upload preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

wait_for_operator "Confirm Cotton Files is visible with the Add files action."
capture_screen "20-files-ready"
require_xml_text "$evidence_dir/20-files-ready.xml" "Files" "Files screen is not visible."
require_xml_text "$evidence_dir/20-files-ready.xml" "Add files" "Files Add action is not visible."

wait_for_operator "Tap + so the Add action sheet is visible."
capture_screen "30-add-actions"
require_xml_text "$evidence_dir/30-add-actions.xml" "Upload file" "Add action sheet did not expose Upload file."

wait_for_operator "Tap Upload file and wait for Android DocumentsUI file picker."
capture_screen "40-file-picker"
require_xml_text "$evidence_dir/40-file-picker.xml" "com.google.android.documentsui" "Android file picker did not open."

wait_for_operator "Select $upload_file_name from Downloads, then wait for upload completion in Files."
capture_screen "50-upload-return"
require_xml_text "$evidence_dir/50-upload-return.xml" "$upload_file_name" "Uploaded file name is not visible after upload."

wait_for_operator "Search for $upload_file_name in Cotton Files."
capture_screen "60-upload-search"
require_xml_text "$evidence_dir/60-upload-search.xml" "$upload_file_name" "Uploaded file is not visible in search results."
require_xml_text "$evidence_dir/60-upload-search.xml" "Text" "Uploaded file kind is not shown as Text."

wait_for_operator "Clear search and return to the stable Files listing."
capture_screen "70-search-cleared"
require_xml_text "$evidence_dir/70-search-cleared.xml" "Files" "Files screen was lost after clearing search."
require_xml_text "$evidence_dir/70-search-cleared.xml" "Search files" "Search action did not return after clearing upload search."

capture_text "90-logcat.txt" adb_device logcat -d -v time

printf 'Manual upload evidence captured in %s\n' "$evidence_dir"
