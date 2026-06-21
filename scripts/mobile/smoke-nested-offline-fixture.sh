#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
folder_name="Mobile smoke folder"
nested_folder_name="Nested offline smoke"
nested_file_name=""
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
auto_evidence_dir=""
install_debug=0
launch_app=1
preflight_only=0
seed_only=0
skip_seed=0
run_auto_proof=1
leave_network_disabled=0
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Prepares a nested cloud folder/file fixture through the Android app UI, then
optionally runs the automated offline-cache smoke against that prepared nested
folder.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Cotton instance URI. Defaults to $instance_uri.
  --folder NAME             Existing parent folder. Defaults to "$folder_name".
  --nested-folder NAME      Child folder to create/use. Defaults to "$nested_folder_name".
  --nested-file NAME        File seeded into Downloads and uploaded to the child folder.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --auto-evidence-dir DIR   Evidence directory for smoke-offline-cache-auto.sh.
  --install-debug           Install the current debug APK with -r before launch.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --preflight-only          Capture device/package/version state and exit without manual prompts.
  --seed-only               Seed the Android Downloads nested file and exit.
  --skip-seed-file          Do not seed the Android Downloads nested file.
  --skip-auto-proof         Stop after fixture setup instead of running the offline-cache smoke.
  --leave-network-disabled  Pass through to the offline-cache smoke.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The script is intentionally interactive for fixture creation: use Cotton Files
actions and Android DocumentsUI, while the script captures screenshots,
UIAutomator XML, package state, the seeded local file, and logcat output.
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
    --instance)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --instance.\n' >&2
        exit 64
      fi
      instance_uri="$2"
      shift 2
      ;;
    --folder)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --folder.\n' >&2
        exit 64
      fi
      folder_name="$2"
      shift 2
      ;;
    --nested-folder)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --nested-folder.\n' >&2
        exit 64
      fi
      nested_folder_name="$2"
      shift 2
      ;;
    --nested-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --nested-file.\n' >&2
        exit 64
      fi
      nested_file_name="$2"
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
    --auto-evidence-dir)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --auto-evidence-dir.\n' >&2
        exit 64
      fi
      auto_evidence_dir="$2"
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
    --seed-only)
      seed_only=1
      shift
      ;;
    --skip-seed-file)
      skip_seed=1
      shift
      ;;
    --skip-auto-proof)
      run_auto_proof=0
      shift
      ;;
    --leave-network-disabled)
      leave_network_disabled=1
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

if [[ -z "$nested_file_name" ]]; then
  nested_file_name="cotton-nested-offline-$(date -u +%Y%m%dT%H%M%SZ).txt"
fi

validate_plain_name() {
  local label="$1"
  local value="$2"

  if [[ -z "${value//[[:space:]]/}" || "$value" == *"/"* ]]; then
    printf '%s must not be blank and must not contain a slash.\n' "$label" >&2
    exit 64
  fi
}

validate_plain_name "Folder name" "$folder_name"
validate_plain_name "Nested folder name" "$nested_folder_name"
validate_plain_name "Nested file name" "$nested_file_name"

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && "$seed_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Use --preflight-only for package/version evidence or --seed-only to push the nested file.\n' >&2
  exit 65
fi

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-nested-offline-fixture"
fi

if [[ -z "$auto_evidence_dir" ]]; then
  auto_evidence_dir="$evidence_dir/auto-offline-cache"
fi

mkdir -p "$evidence_dir"

adb_device() {
  adb -s "$serial" "$@"
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
  local remote_xml="/sdcard/cotton-window.xml"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window
  capture_text "$prefix-connectivity.txt" adb_device shell dumpsys connectivity

  if ! adb_device exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  adb_device shell rm -f "$remote_xml" >/dev/null 2>&1 || true
  if adb_device shell uiautomator dump "$remote_xml" > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    if ! adb_device pull "$remote_xml" "$evidence_dir/$prefix.xml" > "$evidence_dir/$prefix-pull-xml.log" 2>&1; then
      rm -f "$evidence_dir/$prefix.xml"
    fi
    adb_device shell rm -f "$remote_xml" >/dev/null 2>&1 || true
  else
    rm -f "$evidence_dir/$prefix.xml"
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

write_metadata() {
  {
    printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'repo=%s\n' "$COTTON_REPO_ROOT"
    printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'folder=%s\n' "$folder_name"
    printf 'nested_folder=%s\n' "$nested_folder_name"
    printf 'nested_file=%s\n' "$nested_file_name"
    printf 'install_debug=%s\n' "$install_debug"
    printf 'preflight_only=%s\n' "$preflight_only"
    printf 'seed_only=%s\n' "$seed_only"
    printf 'skip_seed_file=%s\n' "$skip_seed"
    printf 'run_auto_proof=%s\n' "$run_auto_proof"
    printf 'auto_evidence_dir=%s\n' "$auto_evidence_dir"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'maui_file_picker_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/file-picker\n'
    printf 'android_documents_docs=https://developer.android.com/training/data-storage/shared/documents-files\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/00-metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Nested Offline Fixture Smoke

Package: \`$package_id\`
Device: \`$serial\`
Parent folder: \`$folder_name\`
Nested folder: \`$nested_folder_name\`
Nested file: \`$nested_file_name\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] Files root shows \`$folder_name\`.
- [ ] At least one root file is already on device for the automated offline-open check.

## Fixture Setup

- [ ] \`20-root-ready.png\` / \`20-root-ready.xml\` show Files root with \`$folder_name\`.
- [ ] \`30-parent-with-nested.png\` / \`30-parent-with-nested.xml\` show \`$nested_folder_name\` inside \`$folder_name\`.
- [ ] \`40-nested-open.png\` / \`40-nested-open.xml\` show the nested folder open.
- [ ] \`50-nested-file-uploaded.png\` / \`50-nested-file-uploaded.xml\` show \`$nested_file_name\` uploaded in the nested folder.
- [ ] \`60-root-ready-for-auto.png\` / \`60-root-ready-for-auto.xml\` show the app returned to Files root.
- [ ] \`70-logcat-setup.txt\` has no fixture setup crashes.

## Automated Proof

- [ ] \`auto-offline-cache/99-summary.txt\` exists when auto proof is enabled.
- [ ] Parent and nested cached folder listings are captured.
- [ ] Offline parent and nested folder screens show cached-listing guidance.
- [ ] Offline local file open still works after network is disabled.
EOF
}

seed_nested_file() {
  local seed_dir="$evidence_dir/seed-files"
  local seed_file="$seed_dir/$nested_file_name"
  mkdir -p "$seed_dir"

  {
    printf 'Cotton nested offline fixture file.\n'
    printf 'Created at UTC: %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'Parent folder: %s\n' "$folder_name"
    printf 'Nested folder: %s\n' "$nested_folder_name"
  } > "$seed_file"

  : > "$evidence_dir/06-seed-nested-file.txt"
  adb_device push "$seed_file" "/sdcard/Download/$nested_file_name" \
    >> "$evidence_dir/06-seed-nested-file.txt" 2>&1
  adb_device shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file:///sdcard/Download/$nested_file_name" \
    >> "$evidence_dir/06-seed-nested-file.txt" 2>&1 || true
  adb_device shell ls -la "/sdcard/Download/$nested_file_name" \
    >> "$evidence_dir/06-seed-nested-file.txt" 2>&1
}

wait_for_operator() {
  local prompt="$1"

  printf '\n%s\n' "$prompt"
  printf 'Press Enter when ready to capture evidence... '
  read -r _
}

run_auto_offline_proof() {
  local args=(
    --package "$package_id"
    --serial "$serial"
    --instance "$instance_uri"
    --folder "$folder_name"
    --nested-folder "$nested_folder_name"
    --nested-file "$nested_file_name"
    --evidence-dir "$auto_evidence_dir"
  )

  if [[ "$install_debug" -eq 1 ]]; then
    args+=(--install-debug)
  fi

  if [[ -n "$expected_version_code" ]]; then
    args+=(--expected-version-code "$expected_version_code")
  fi

  if [[ -n "$expected_version_name" ]]; then
    args+=(--expected-version-name "$expected_version_name")
  fi

  if [[ "$leave_network_disabled" -eq 1 ]]; then
    args+=(--leave-network-disabled)
  fi

  {
    printf '%q' "$SCRIPT_DIR/smoke-offline-cache-auto.sh"
    local arg
    for arg in "${args[@]}"; do
      printf ' %q' "$arg"
    done
    printf '\n'
  } > "$evidence_dir/80-auto-offline-command.txt"

  "$SCRIPT_DIR/smoke-offline-cache-auto.sh" "${args[@]}" \
    | tee "$evidence_dir/81-auto-offline-cache.log"
}

write_metadata
write_checklist

capture_text "01-adb-devices.txt" adb devices
capture_text "02-device.txt" adb_device shell getprop ro.product.model

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/03-install.txt"
fi

capture_text "04-package.txt" adb_device shell dumpsys package "$package_id"
capture_text "05-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
verify_expected_version

if [[ "$preflight_only" -eq 1 ]]; then
  printf 'Nested offline fixture preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$skip_seed" -eq 0 ]]; then
  seed_nested_file
fi

if [[ "$seed_only" -eq 1 ]]; then
  printf 'Nested offline fixture seed evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c || true
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/10-launch.txt"
  sleep 4
fi

capture_screen "10-launch"

wait_for_operator "Confirm Cotton Files root is visible and contains $folder_name."
capture_screen "20-root-ready"
require_xml_text "$evidence_dir/20-root-ready.xml" "Files" "Files root is not visible."
require_xml_text "$evidence_dir/20-root-ready.xml" "$folder_name" "Parent folder is not visible in Files root."

wait_for_operator "Open $folder_name. If $nested_folder_name is missing, create it with Add files -> New folder, then leave the parent folder open."
capture_screen "30-parent-with-nested"
require_xml_text "$evidence_dir/30-parent-with-nested.xml" "Files / $folder_name" "Parent folder is not open."
require_xml_text "$evidence_dir/30-parent-with-nested.xml" "Add files" "Parent folder Add files action is not visible."
require_xml_text "$evidence_dir/30-parent-with-nested.xml" "$nested_folder_name" "Nested folder is not visible in the parent folder."

wait_for_operator "Open $nested_folder_name and leave the nested folder visible."
capture_screen "40-nested-open"
require_xml_text "$evidence_dir/40-nested-open.xml" "Files /" "Nested folder breadcrumb is not visible."
require_xml_text "$evidence_dir/40-nested-open.xml" "$nested_folder_name" "Nested folder is not open."
require_xml_text "$evidence_dir/40-nested-open.xml" "Add files" "Nested folder Add files action is not visible."

wait_for_operator "Upload $nested_file_name from Android Downloads through Add files -> Upload file, then wait until it appears in the nested folder."
capture_screen "50-nested-file-uploaded"
require_xml_text "$evidence_dir/50-nested-file-uploaded.xml" "$nested_file_name" "Nested file is not visible after upload."

wait_for_operator "Return to Files root so $folder_name is visible for the automated offline-cache proof."
capture_screen "60-root-ready-for-auto"
require_xml_text "$evidence_dir/60-root-ready-for-auto.xml" "Files" "Files root is not visible before auto proof."
require_xml_text "$evidence_dir/60-root-ready-for-auto.xml" "$folder_name" "Parent folder is not visible before auto proof."

capture_text "70-logcat-setup.txt" adb_device logcat -d -v time

if [[ "$run_auto_proof" -eq 1 ]]; then
  run_auto_offline_proof
fi

printf 'Nested offline fixture evidence captured in %s\n' "$evidence_dir"
