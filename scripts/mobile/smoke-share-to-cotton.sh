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
skip_source_app_file=0
expected_version_code=""
expected_version_name=""
share_text=""
share_file_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a share-to-Cotton smoke and captures Capture Inbox evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --share-text TEXT         Shell-safe text token for the automated ACTION_SEND text share.
  --share-file-name NAME    Seed file name for source-app file share. Defaults to a timestamped txt file.
  --preflight-only          Capture device/package/version state and exit.
  --seed-only               Seed the Android Downloads source-share file and exit.
  --skip-source-app-file    Skip the interactive source-app file-share capture.
  --no-launch               Do not launch the app before capture.
  --help, -h                Show this help.

The script uses adb to prove Cotton receives ACTION_SEND text shares, captures
known shell URI edge cases, and can optionally pause for a real source-app file
share so Android grants temporary read access like it does for users.
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
    --share-text)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --share-text.\n' >&2
        exit 64
      fi
      share_text="$2"
      shift 2
      ;;
    --share-file-name)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --share-file-name.\n' >&2
        exit 64
      fi
      share_file_name="$2"
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
    --skip-source-app-file)
      skip_source_app_file=1
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

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
if [[ -z "$share_text" ]]; then
  share_text="CottonShareSmoke-$timestamp"
fi

if [[ -z "$share_file_name" ]]; then
  share_file_name="cotton-share-source-$timestamp.txt"
fi

if [[ -z "${share_text//[[:space:]]/}" ]]; then
  printf 'Share text must not be blank.\n' >&2
  exit 64
fi

if [[ "$share_text" =~ [[:space:]] ]]; then
  printf 'Share text must not contain whitespace for deterministic adb automation.\n' >&2
  exit 64
fi

if [[ -z "${share_file_name//[[:space:]]/}" || "$share_file_name" == *"/"* ]]; then
  printf 'Share file name must not be blank and must not contain a slash.\n' >&2
  exit 64
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && "$seed_only" -eq 0 && "$skip_source_app_file" -eq 0 && ! -t 0 ]]; then
  printf 'The source-app file-share step requires an interactive terminal.\n' >&2
  printf 'Use --skip-source-app-file to capture automated share evidence only.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-share-to-cotton"
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
    printf 'skip_source_app_file=%s\n' "$skip_source_app_file"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'share_text=%s\n' "$share_text"
    printf 'share_file_name=%s\n' "$share_file_name"
    printf 'android_receive_share_docs=https://developer.android.com/training/sharing/receive\n'
    printf 'android_send_share_docs=https://developer.android.com/training/sharing/send\n'
    printf 'android_file_share_docs=https://developer.android.com/training/secure-file-sharing/share-file\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_uiautomator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Share To Cotton Smoke

Package: \`$package_id\`
Device: \`$serial\`
Text payload: \`$share_text\`
Seeded file: \`$share_file_name\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Signed-in session is restored without clearing app data.
- [ ] \`06-seed-share-file.txt\` shows \`$share_file_name\` pushed to Android Downloads.

## Automated Text Share

- [ ] \`20-text-share-inbox.png\` / \`20-text-share-inbox.xml\` show \`Capture Inbox\`.
- [ ] The captured item shows \`$share_text\`.
- [ ] The captured item shows \`Text share captured\`, \`Ready\`, and \`Text\`.

## Shell URI Edge Cases

- [ ] \`30-shell-content-uri-edge.png\` / \`30-shell-content-uri-edge.xml\` show Cotton does not upload a shell content URI without a valid source-app grant.
- [ ] \`40-file-uri-edge.png\` / \`40-file-uri-edge.xml\` show Cotton reports unsupported file URI content without crashing.

## Source-App File Share

- [ ] Share \`$share_file_name\` from Android Files, Photos, Drive, or another real source app to Cotton.
- [ ] \`50-source-app-file-share.png\` / \`50-source-app-file-share.xml\` show \`$share_file_name\`.
- [ ] The captured file item shows \`Copied to this device\` and \`Choose folder\`.
- [ ] \`90-logcat.txt\` has no share-to-Cotton crashes.

## Evidence To Review

- \`00-device.txt\`
- \`04-package.txt\`
- \`05-package-version.txt\`
- \`06-seed-share-file.txt\`
- \`20-text-share-inbox.png\` / \`20-text-share-inbox.xml\`
- \`30-shell-content-uri-edge.png\` / \`30-shell-content-uri-edge.xml\`
- \`40-file-uri-edge.png\` / \`40-file-uri-edge.xml\`
- \`50-source-app-file-share.png\` / \`50-source-app-file-share.xml\`
- \`90-logcat.txt\`
EOF
}

seed_share_file() {
  local seed_dir="$evidence_dir/seed-files"
  local seed_file="$seed_dir/$share_file_name"
  mkdir -p "$seed_dir"

  {
    printf 'Cotton source-app share smoke file.\n'
    printf 'Created at UTC: %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    printf 'Package: %s\n' "$package_id"
  } > "$seed_file"

  : > "$evidence_dir/06-seed-share-file.txt"
  adb_device push "$seed_file" "/sdcard/Download/$share_file_name" \
    >> "$evidence_dir/06-seed-share-file.txt" 2>&1
  adb_device shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file:///sdcard/Download/$share_file_name" \
    >> "$evidence_dir/06-seed-share-file.txt" 2>&1 || true
  adb_device shell ls -la "/sdcard/Download/$share_file_name" \
    >> "$evidence_dir/06-seed-share-file.txt" 2>&1
  capture_text "07-share-file-mediastore.txt" adb_device shell content query \
    --uri content://media/external/file \
    --projection _id:_display_name:mime_type:size \
    --where "_display_name='$share_file_name'"
}

content_uri_for_seeded_file() {
  local media_id
  media_id="$(sed -n 's/.*_id=\([0-9][0-9]*\).*/\1/p' "$evidence_dir/07-share-file-mediastore.txt" | sed -n '1p')"
  if [[ -n "${media_id//[[:space:]]/}" ]]; then
    printf 'content://media/external/file/%s' "$media_id"
  fi
}

start_text_share() {
  adb_device shell am start \
    -a android.intent.action.SEND \
    -t text/plain \
    -p "$package_id" \
    --es android.intent.extra.TEXT "$share_text" \
    > "$evidence_dir/20-text-share-start.txt" 2>&1
}

start_content_uri_edge_share() {
  local content_uri="$1"

  if [[ -z "$content_uri" ]]; then
    printf 'No MediaStore URI was available for %s.\n' "$share_file_name" \
      > "$evidence_dir/30-shell-content-uri-edge-start.txt"
    return
  fi

  adb_device shell am start \
    -a android.intent.action.SEND \
    -t text/plain \
    -p "$package_id" \
    --eu android.intent.extra.STREAM "$content_uri" \
    --grant-read-uri-permission \
    > "$evidence_dir/30-shell-content-uri-edge-start.txt" 2>&1
}

start_file_uri_edge_share() {
  adb_device shell am start \
    -a android.intent.action.SEND \
    -t text/plain \
    -p "$package_id" \
    --eu android.intent.extra.STREAM "file:///sdcard/Download/$share_file_name" \
    > "$evidence_dir/40-file-uri-edge-start.txt" 2>&1
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

  adb_device install --no-incremental -r "$COTTON_ANDROID_APK" > "$evidence_dir/08-install.txt"
fi

capture_text "03-package-path.txt" adb_device shell pm path "$package_id"
capture_text "04-package.txt" adb_device shell dumpsys package "$package_id"
capture_text "05-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
verify_expected_version

seed_share_file

if [[ "$preflight_only" -eq 1 || "$seed_only" -eq 1 ]]; then
  printf 'Share-to-Cotton preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$launch_app" -eq 1 ]]; then
  adb_device logcat -c || true
  adb_device shell monkey -p "$package_id" 1 > "$evidence_dir/09-launch.txt"
  sleep 3
fi

capture_screen "10-launch"

start_text_share
sleep 3
capture_screen "20-text-share-inbox"
require_xml_text "$evidence_dir/20-text-share-inbox.xml" "Capture Inbox" "Capture Inbox did not open for text share."
require_xml_text "$evidence_dir/20-text-share-inbox.xml" "$share_text" "Text share payload is not visible in Capture Inbox."
require_xml_text "$evidence_dir/20-text-share-inbox.xml" "Text share captured" "Text share detail is not visible."
require_xml_text "$evidence_dir/20-text-share-inbox.xml" "Ready" "Text share is not marked ready."
require_xml_text "$evidence_dir/20-text-share-inbox.xml" "Text" "Text share kind is not visible."

seed_content_uri="$(content_uri_for_seeded_file)"
start_content_uri_edge_share "$seed_content_uri"
sleep 3
capture_screen "30-shell-content-uri-edge"
require_xml_text "$evidence_dir/30-shell-content-uri-edge.xml" "Capture Inbox" "Capture Inbox did not stay visible for shell content URI edge case."
require_xml_text "$evidence_dir/30-shell-content-uri-edge.xml" "Needs access" "Shell content URI edge case did not surface missing access."
require_xml_text "$evidence_dir/30-shell-content-uri-edge.xml" "Android revoked access to the shared content." "Missing-permission message is not visible."

start_file_uri_edge_share
sleep 3
capture_screen "40-file-uri-edge"
require_xml_text "$evidence_dir/40-file-uri-edge.xml" "Capture Inbox" "Capture Inbox did not stay visible for file URI edge case."
require_xml_text "$evidence_dir/40-file-uri-edge.xml" "$share_file_name" "File URI edge case did not show the source file name."
require_xml_text "$evidence_dir/40-file-uri-edge.xml" "Unsupported" "File URI edge case did not surface unsupported status."
require_xml_text "$evidence_dir/40-file-uri-edge.xml" "Android could not open the shared content." "Unsupported-content message is not visible."

if [[ "$skip_source_app_file" -eq 0 ]]; then
  wait_for_operator "Share $share_file_name from Android Files, Photos, Drive, or another source app to Cotton."
  capture_screen "50-source-app-file-share"
  require_xml_text "$evidence_dir/50-source-app-file-share.xml" "Capture Inbox" "Capture Inbox is not visible after source-app file share."
  require_xml_text "$evidence_dir/50-source-app-file-share.xml" "$share_file_name" "Source-app shared file name is not visible."
  require_xml_text "$evidence_dir/50-source-app-file-share.xml" "Copied to this device" "Source-app file was not copied to local staging."
  require_xml_text "$evidence_dir/50-source-app-file-share.xml" "Choose folder" "Source-app shared file is not waiting for destination selection."
fi

capture_text "90-logcat.txt" adb_device logcat -d -v time

printf 'Share-to-Cotton evidence captured in %s\n' "$evidence_dir"
