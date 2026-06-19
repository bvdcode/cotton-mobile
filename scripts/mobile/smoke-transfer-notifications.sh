#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
destination_name="Mobile smoke folder"
upload_name="notification-smoke.txt"
upload_body="Cotton transfer notification smoke"
content_type="text/plain"
permission_state="preserve"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
launch_app=1
seed_upload=1
preflight_only=0
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive transfer-notification smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI            Cotton instance URI. Defaults to $instance_uri.
  --destination NAME        Destination folder name in cached root. Defaults to "$destination_name".
  --name FILE               Seed upload file name. Defaults to "$upload_name".
  --body TEXT               Seed upload file body.
  --content-type MIME       Seed upload MIME type. Defaults to "$content_type".
  --permission-state STATE  Set POST_NOTIFICATIONS before launch: preserve, allowed, denied, or fresh.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --skip-seed-upload        Do not seed transfer metadata; operator prepares queue manually.
  --preflight-only          Capture device/package/permission state and exit without manual prompts.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The script is intentionally manual after setup: open Account -> Transfers, tap
Run, wait for the upload to finish or fail, and review Android notification
dumpsys output. It preserves app data but may create a small smoke upload.
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
    --destination)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --destination.\n' >&2
        exit 64
      fi
      destination_name="$2"
      shift 2
      ;;
    --name)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --name.\n' >&2
        exit 64
      fi
      upload_name="$2"
      shift 2
      ;;
    --body)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --body.\n' >&2
        exit 64
      fi
      upload_body="$2"
      shift 2
      ;;
    --content-type)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --content-type.\n' >&2
        exit 64
      fi
      content_type="$2"
      shift 2
      ;;
    --permission-state)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --permission-state.\n' >&2
        exit 64
      fi
      permission_state="$2"
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
    --skip-seed-upload)
      seed_upload=0
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

case "$permission_state" in
  preserve|fresh|allowed|denied)
    ;;
  *)
    printf 'Invalid --permission-state: %s. Expected preserve, fresh, allowed, or denied.\n' \
      "$permission_state" >&2
    exit 64
    ;;
esac

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Run it from a shell attached to the Android device or emulator.\n' >&2
  printf 'Use --preflight-only for non-interactive package/permission evidence.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-transfer-notifications"
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
    printf 'instance_uri=%s\n' "$instance_uri"
    printf 'destination_name=%s\n' "$destination_name"
    printf 'upload_name=%s\n' "$upload_name"
    printf 'content_type=%s\n' "$content_type"
    printf 'permission_state=%s\n' "$permission_state"
    printf 'seed_upload=%s\n' "$seed_upload"
    printf 'preflight_only=%s\n' "$preflight_only"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'android_notification_permission_docs=https://developer.android.com/develop/ui/views/notifications/notification-permission\n'
    printf 'android_notification_channels_docs=https://developer.android.com/develop/ui/views/notifications/channels\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_dumpsys_docs=https://developer.android.com/tools/dumpsys\n'
    printf 'android_logcat_docs=https://developer.android.com/tools/logcat\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<EOF
# Transfer Notification Smoke

Package: \`$package_id\`
Device: \`$serial\`
Requested Android permission setup: \`$permission_state\`

## Preconditions

- [ ] Package/version in \`05-package-version.txt\` matches the build under test.
- [ ] Android notification permission/appops in \`06-notification-permission.txt\` and \`07-notification-appops.txt\` match the requested state.
- [ ] A signed-in session is restored without clearing app data.
- [ ] The cached root listing contains \`$destination_name\` if seed upload is enabled.
- [ ] A queued upload is visible on the Transfers page.

## Allowed Path

- [ ] Run with \`--permission-state allowed\`.
- [ ] Tap \`Run\` on Transfers and wait for upload completion or failure.
- [ ] \`30-after-run-notification-summary.txt\` contains a Cotton notification record on \`cotton.transfers\`, or a running UIDT notification if the job is still active.
- [ ] The notification text matches the transfer outcome.
- [ ] The Transfers page shows the same terminal state.

## Denied Path

- [ ] Run with \`--permission-state denied\`.
- [ ] Tap \`Run\` on Transfers and wait for upload completion or failure.
- [ ] Android permission/appops still show notifications blocked.
- [ ] \`30-after-run-notification-summary.txt\` does not contain a posted Cotton transfer notification.
- [ ] The Transfers page still shows the upload outcome in-app.

## Evidence Files

- \`00-device.txt\`
- \`05-package-version.txt\`
- \`06-notification-permission.txt\`
- \`07-notification-appops.txt\`
- \`08-notification-summary.txt\`
- \`09-seed-upload.txt\`
- \`10-preflight.png\` / \`10-preflight.xml\`
- \`20-transfers-ready.png\` / \`20-transfers-ready.xml\`
- \`30-after-run.png\` / \`30-after-run.xml\`
- \`30-after-run-notification-summary.txt\`
- \`40-notification-shade.png\` / \`40-notification-shade.xml\`
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

summarize_notification_dumpsys() {
  local source_file="$1"
  local summary_file="$2"

  awk -v package_id="$package_id" '
    $0 ~ package_id { print; next }
    $0 ~ /cotton\./ { print; next }
    $0 ~ /Upload complete/ { print; next }
    $0 ~ /Upload failed/ { print; next }
    $0 ~ /uploaded/ { print; next }
    $0 ~ /uploading/ { print; next }
  ' "$source_file" > "$summary_file"
}

capture_notification_state() {
  local prefix="$1"

  capture_text "$prefix-notification-permission.txt" \
    adb_device shell dumpsys package "$package_id"
  capture_text "$prefix-notification-appops.txt" \
    adb_device shell appops get "$package_id" POST_NOTIFICATION
  capture_text "$prefix-notification-dumpsys.txt" \
    adb_device shell dumpsys notification --noredact
  summarize_notification_dumpsys \
    "$evidence_dir/$prefix-notification-dumpsys.txt" \
    "$evidence_dir/$prefix-notification-summary.txt"
}

capture_device_state() {
  local prefix="$1"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window
  capture_notification_state "$prefix"

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
  capture_device_state "$prefix"
}

apply_permission_state() {
  case "$permission_state" in
    preserve)
      printf 'Preserving existing Android notification permission state.\n' \
        > "$evidence_dir/06-permission-setup.txt"
      ;;
    fresh)
      {
        adb_device shell pm revoke "$package_id" android.permission.POST_NOTIFICATIONS || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/06-permission-setup.txt" 2>&1
      ;;
    allowed)
      {
        adb_device shell pm grant "$package_id" android.permission.POST_NOTIFICATIONS || true
        adb_device shell pm set-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/06-permission-setup.txt" 2>&1
      ;;
    denied)
      {
        adb_device shell pm revoke "$package_id" android.permission.POST_NOTIFICATIONS || true
        adb_device shell pm set-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-set || true
        adb_device shell pm clear-permission-flags "$package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$evidence_dir/06-permission-setup.txt" 2>&1
      ;;
  esac
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

if ! adb_device shell pm path "$package_id" > "$evidence_dir/04-package.txt" 2>&1; then
  printf 'Package %s is not installed on %s.\n' "$package_id" "$serial" >&2
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

apply_permission_state

capture_text "06-notification-permission.txt" adb_device shell dumpsys package "$package_id"
capture_text "07-notification-appops.txt" adb_device shell appops get "$package_id" POST_NOTIFICATION
capture_text "08-notification-dumpsys.txt" adb_device shell dumpsys notification --noredact
summarize_notification_dumpsys \
  "$evidence_dir/08-notification-dumpsys.txt" \
  "$evidence_dir/08-notification-summary.txt"

adb_device logcat -c >/dev/null 2>&1 || true

if [[ "$preflight_only" -eq 1 ]]; then
  if [[ "$launch_app" -eq 1 ]]; then
    capture_text "11-launch.txt" adb_device shell monkey -p "$package_id" 1
    sleep 2
  fi

  capture_device_state "10-preflight"
  printf '\nTransfer notification preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$seed_upload" -eq 1 ]]; then
  COTTON_ANDROID_PACKAGE_ID="$package_id" \
  COTTON_ADB_SERIAL="$serial" \
    "$SCRIPT_DIR/seed-queued-upload-smoke.sh" \
      --instance "$instance_uri" \
      --destination "$destination_name" \
      --name "$upload_name" \
      --body "$upload_body" \
      --content-type "$content_type" \
      --no-launch > "$evidence_dir/09-seed-upload.txt" 2>&1
else
  printf 'Seed upload skipped by operator.\n' > "$evidence_dir/09-seed-upload.txt"
fi

if [[ "$launch_app" -eq 1 ]]; then
  capture_text "11-launch.txt" adb_device shell monkey -p "$package_id" 1
  sleep 2
fi

capture_device_state "10-preflight"

prompt_capture "Open Account -> Transfers and verify the seeded upload is visible." \
  "20-transfers-ready"

prompt_capture "Tap Run, wait for the upload to finish or fail, and leave Transfers visible." \
  "30-after-run"

prompt_capture "Optionally open the Android notification shade or leave the app visible for final capture." \
  "40-notification-shade"

capture_text "90-logcat.txt" adb_device logcat -d -v threadtime

printf '\nTransfer notification smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md before marking transfer notification delivery proof complete.\n'
