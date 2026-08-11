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
search_query="jpg"

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive Files chrome smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --search-query TEXT       Query to type in Files search. Defaults to "$search_query".
  --preflight-only          Capture device/package/version state and exit without manual prompts.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The script is intentionally manual: operate the Files chrome while it captures
screenshots, UIAutomator XML, package state, and logcat output for view mode,
sorting, search, and pull-to-refresh behavior.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--evidence-dir:evidence_dir"
  "--expected-version-code:expected_version_code"
  "--expected-version-name:expected_version_name"
  "--search-query:search_query"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--preflight-only:preflight_only:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

if [[ -z "${search_query//[[:space:]]/}" ]]; then
  printf 'Search query must not be blank.\n' >&2
  exit 64
fi

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
  evidence_dir="$evidence_root/$timestamp-files-chrome"
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
    printf 'search_query=%s\n' "$search_query"
    printf 'maui_refreshview_docs=https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/refreshview\n'
    printf 'maui_searchbar_docs=https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/searchbar\n'
    printf 'maui_popups_docs=https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Files Chrome Smoke

Package: \`$package_id\`
Device: \`$serial\`
Search query: \`$search_query\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Files root is visible and contains at least one file or folder row.

## View Mode

- [ ] \`20-files-root-ready.png\` / \`20-files-root-ready.xml\` show Files chrome without clipping.
- [ ] \`30-view-actions.png\` / \`30-view-actions.xml\` show \`View files as\`.
- [ ] The view action sheet includes \`List\`.
- [ ] The view action sheet includes \`Tiles\`.
- [ ] \`40-view-applied.png\` / \`40-view-applied.xml\` show the selected mode applied without losing the root listing.

## Sort

- [ ] \`50-sort-actions.png\` / \`50-sort-actions.xml\` show \`Sort files by\`.
- [ ] The sort action sheet includes \`Name\`, \`Updated\`, \`Type\`, and \`Size\`.
- [ ] \`60-sort-updated.png\` / \`60-sort-updated.xml\` show the list after selecting \`Updated\`.
- [ ] The status line reflects the selected sort mode.

## Search

- [ ] \`70-search-query.png\` / \`70-search-query.xml\` show the active search field and query.
- [ ] Search results show either a match count or the empty-search copy.
- [ ] \`80-search-cleared.png\` / \`80-search-cleared.xml\` show search cleared and the root listing restored.

## Refresh

- [ ] Pull-to-refresh can be triggered from Files root.
- [ ] \`90-refresh-return.png\` / \`90-refresh-return.xml\` show Files returned to a stable root listing.
- [ ] \`90-logcat.txt\` has no Files chrome crashes during view, sort, search, or refresh.

## Evidence To Review

- \`00-device.txt\`
- \`04-package.txt\`
- \`05-package-version.txt\`
- \`20-files-root-ready.png\` / \`20-files-root-ready.xml\`
- \`30-view-actions.png\` / \`30-view-actions.xml\`
- \`40-view-applied.png\` / \`40-view-applied.xml\`
- \`50-sort-actions.png\` / \`50-sort-actions.xml\`
- \`60-sort-updated.png\` / \`60-sort-updated.xml\`
- \`70-search-query.png\` / \`70-search-query.xml\`
- \`80-search-cleared.png\` / \`80-search-cleared.xml\`
- \`90-refresh-return.png\` / \`90-refresh-return.xml\`
- \`90-logcat.txt\`
EOF
}




require_xml_any_text() {
  local xml_file="$1"
  local message="$2"
  shift 2

  if [[ ! -f "$xml_file" ]]; then
    printf '%s\n' "$message" >&2
    printf 'Missing XML: %s\n' "$xml_file" >&2
    exit 66
  fi

  local needle
  for needle in "$@"; do
    if grep -Fq "$needle" "$xml_file"; then
      return
    fi
  done

  printf '%s\n' "$message" >&2
  printf 'Evidence: %s\n' "$xml_file" >&2
  exit 66
}



write_metadata
write_checklist

cotton_capture_text_best_effort "00-device.txt" cotton_adb shell getprop ro.product.model
cotton_capture_text_best_effort "01-adb-devices.txt" adb devices
cotton_capture_text_best_effort "02-window.txt" cotton_adb shell dumpsys window
cotton_capture_text_best_effort "03-package-path.txt" cotton_adb shell pm path "$package_id"
cotton_capture_text_best_effort "04-package.txt" cotton_adb shell dumpsys package "$package_id"
cotton_capture_text_best_effort "05-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
cotton_verify_expected_version_file "$evidence_dir/05-package-version.txt"

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/06-install.txt"
fi

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c || true
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/07-launch.txt"
  sleep 3
fi

cotton_capture_screen "10-launch"

if [[ "$preflight_only" -eq 1 ]]; then
  printf 'Preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

cotton_wait_for_operator "Confirm Files root is visible with the search, sort, and view buttons."
cotton_capture_screen "20-files-root-ready"
cotton_require_xml_text "$evidence_dir/20-files-root-ready.xml" "Files" "Files root is not visible."
cotton_require_xml_text "$evidence_dir/20-files-root-ready.xml" "Search files" "Files search action is not exposed."
cotton_require_xml_text "$evidence_dir/20-files-root-ready.xml" "Sort files" "Files sort action is not exposed."
cotton_require_xml_text "$evidence_dir/20-files-root-ready.xml" "Change file view" "Files view action is not exposed."

cotton_wait_for_operator "Tap the file-view button so the View files as action sheet is visible."
cotton_capture_screen "30-view-actions"
cotton_require_xml_text "$evidence_dir/30-view-actions.xml" "View files as" "View action sheet did not open."
cotton_require_xml_text "$evidence_dir/30-view-actions.xml" "List" "View action sheet did not include List."
cotton_require_xml_text "$evidence_dir/30-view-actions.xml" "Tiles" "View action sheet did not include Tiles."

cotton_wait_for_operator "Choose the opposite view mode, then wait for Files to settle."
cotton_capture_screen "40-view-applied"
cotton_require_xml_text "$evidence_dir/40-view-applied.xml" "Files" "Files root was lost after view-mode change."
cotton_require_xml_text "$evidence_dir/40-view-applied.xml" "Change file view" "Files view action was lost after view-mode change."

cotton_wait_for_operator "Tap the sort button so the Sort files by action sheet is visible."
cotton_capture_screen "50-sort-actions"
cotton_require_xml_text "$evidence_dir/50-sort-actions.xml" "Sort files by" "Sort action sheet did not open."
cotton_require_xml_text "$evidence_dir/50-sort-actions.xml" "Name" "Sort action sheet did not include Name."
cotton_require_xml_text "$evidence_dir/50-sort-actions.xml" "Updated" "Sort action sheet did not include Updated."
cotton_require_xml_text "$evidence_dir/50-sort-actions.xml" "Type" "Sort action sheet did not include Type."
cotton_require_xml_text "$evidence_dir/50-sort-actions.xml" "Size" "Sort action sheet did not include Size."

cotton_wait_for_operator "Choose Updated sort, then wait for Files to settle."
cotton_capture_screen "60-sort-updated"
cotton_require_xml_text "$evidence_dir/60-sort-updated.xml" "Files" "Files root was lost after sort change."
require_xml_any_text \
  "$evidence_dir/60-sort-updated.xml" \
  "Updated sort status is not visible after sort change." \
  "Updated" \
  "Newest"

cotton_wait_for_operator "Tap search, type \"$search_query\", and wait for filtering to settle."
cotton_capture_screen "70-search-query"
cotton_require_xml_text "$evidence_dir/70-search-query.xml" "$search_query" "Search query is not visible."
require_xml_any_text \
  "$evidence_dir/70-search-query.xml" \
  "Search result state is not visible." \
  "match" \
  "matches" \
  "No matching files"

cotton_wait_for_operator "Clear search with the search action or SearchBar clear affordance."
cotton_capture_screen "80-search-cleared"
cotton_require_xml_text "$evidence_dir/80-search-cleared.xml" "Files" "Files root was lost after clearing search."
cotton_require_xml_text "$evidence_dir/80-search-cleared.xml" "Search files" "Search action did not return after clearing search."

cotton_wait_for_operator "Pull down from the Files root to refresh, then wait for the list to settle."
cotton_capture_screen "90-refresh-return"
cotton_require_xml_text "$evidence_dir/90-refresh-return.xml" "Files" "Files root was lost after refresh."
cotton_require_xml_text "$evidence_dir/90-refresh-return.xml" "Sort files" "Files sort action was lost after refresh."
cotton_require_xml_text "$evidence_dir/90-refresh-return.xml" "Change file view" "Files view action was lost after refresh."

cotton_capture_text_best_effort "90-logcat.txt" cotton_adb logcat -d -v time

printf 'Files chrome evidence captured in %s\n' "$evidence_dir"
