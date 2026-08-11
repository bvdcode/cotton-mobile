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
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive bidirectional sync action smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --preflight-only          Capture device/package/version state and exit without manual prompts.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The script is intentionally manual: open a folder action sheet in Files, choose
Sync both ways, then cancel the Android folder picker while it captures
screenshots, UIAutomator XML, package state, and logcat output.
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
)
cotton_parse_arguments "$@"

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Use --preflight-only for package/version evidence.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-bidirectional-sync-action"
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
    printf 'android_saf_docs=https://developer.android.com/training/data-storage/shared/documents-files\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Bidirectional Sync Action Smoke

Package: \`$package_id\`
Device: \`$serial\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Files root is visible and a folder row is available.

## Action Sheet

- [ ] Folder action sheet opens without layout clipping.
- [ ] Folder action sheet includes \`Sync to this device\`.
- [ ] Folder action sheet includes \`Sync to folder\`.
- [ ] Folder action sheet includes \`Sync from folder\`.
- [ ] Folder action sheet includes \`Sync both ways\`.

## Picker And Cancel Pass

- [ ] Tapping \`Sync both ways\` opens the Android document-tree picker.
- [ ] The picker is shown by the platform DocumentsUI package.
- [ ] The picker uses a folder-selection affordance such as \`USE THIS FOLDER\`.
- [ ] Back/cancel returns to Cotton Files without binding a folder.
- [ ] Cotton shows \`Sync cancelled.\` after picker cancellation.

## Evidence To Review

- \`00-device.txt\`
- \`04-package.txt\`
- \`05-package-version.txt\`
- \`10-files-root.png\` / \`10-files-root.xml\`
- \`20-folder-actions.png\` / \`20-folder-actions.xml\`
- \`30-documents-picker.png\` / \`30-documents-picker.xml\`
- \`40-picker-cancel-return.png\` / \`40-picker-cancel-return.xml\`
- \`90-logcat.txt\`
EOF
}

write_metadata
write_checklist
cotton_capture_standard_package_evidence

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c || true
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/07-launch.txt"
  sleep 3
fi

cotton_capture_screen "10-files-root"

if [[ "$preflight_only" -eq 1 ]]; then
  printf 'Preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

cotton_wait_for_operator "Open a folder action sheet in Files so Sync both ways is visible."
cotton_capture_screen "20-folder-actions"
cotton_require_xml_text "$evidence_dir/20-folder-actions.xml" "Sync to this device" "Folder action sheet did not expose cloud-to-device sync."
cotton_require_xml_text "$evidence_dir/20-folder-actions.xml" "Sync to folder" "Folder action sheet did not expose selected-folder cloud-to-device sync."
cotton_require_xml_text "$evidence_dir/20-folder-actions.xml" "Sync from folder" "Folder action sheet did not expose device-to-cloud sync."
cotton_require_xml_text "$evidence_dir/20-folder-actions.xml" "Sync both ways" "Folder action sheet did not expose bidirectional sync."

cotton_wait_for_operator "Tap Sync both ways and wait for the Android folder picker."
cotton_capture_screen "30-documents-picker"
cotton_require_xml_text "$evidence_dir/30-documents-picker.xml" "com.google.android.documentsui" "Android document-tree picker did not open."
cotton_require_xml_text "$evidence_dir/30-documents-picker.xml" "USE THIS FOLDER" "Android document-tree picker did not show folder-selection action."

cotton_wait_for_operator "Cancel the picker with Back, returning to Cotton Files."
cotton_capture_screen "40-picker-cancel-return"
cotton_require_xml_text "$evidence_dir/40-picker-cancel-return.xml" "$package_id" "Cotton Files did not regain focus after picker cancellation."
cotton_require_xml_text "$evidence_dir/40-picker-cancel-return.xml" "Sync cancelled." "Cotton Files did not show the sync-cancelled status."

cotton_capture_text_best_effort "90-logcat.txt" cotton_adb logcat -d -v time

printf 'Bidirectional sync action evidence captured in %s\n' "$evidence_dir"
