#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
instance_uri="https://app.cottoncloud.dev"
folder_name="Mobile smoke folder"
main_activity="crc647f4f3c52a3509f5a.MainActivity"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a non-interactive file-open routing smoke against existing cotton-open-*
files in a cached Cotton folder. It validates app-private local bytes, opens
text/image/PDF/audio/video in Cotton viewers, and verifies document/archive/
unknown files either launch a system handler or show the expected no-app copy.

Options:
  --package ID        Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL     ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --instance URI      Cotton instance URI. Defaults to $instance_uri.
  --folder NAME       Folder containing cotton-open-* files. Defaults to "$folder_name".
  --evidence-dir DIR  Evidence directory. Defaults to a timestamped directory.
  --install-debug     Install the current debug APK with -r before launch.
  --help, -h          Show this help.

The app must already have a signed-in session, cached folder listing, and local
downloads for the selected smoke files.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--instance:instance_uri"
  "--folder:folder_name"
  "--evidence-dir:evidence_dir"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
)
cotton_parse_arguments "$@"

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if ! command -v python3 >/dev/null 2>&1; then
  printf 'python3 was not found.\n' >&2
  exit 127
fi

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
if [[ -z "$evidence_dir" ]]; then
  evidence_dir="$evidence_root/$timestamp-file-open-routing-auto"
fi

mkdir -p "$evidence_dir"

# shellcheck source=smoke-file-open-routing-auto-support.sh
source "$SCRIPT_DIR/smoke-file-open-routing-auto-support.sh"

instance_key="$(cotton_create_instance_key)"
write_metadata

cotton_capture_text_best_effort "01-adb-devices.txt" adb devices

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/02-install.txt"
fi

cotton_capture_text_best_effort "03-package.txt" cotton_adb shell dumpsys package "$package_id"
load_targets
validate_target_bytes

cotton_adb logcat -c >/dev/null 2>&1 || true

target_count="$(wc -l < "$evidence_dir/14-targets.tsv" | tr -d '[:space:]')"
opened_count=0
while IFS=$'\t' read -r -u 3 key name file_id size kind content_type mode path; do
  if [[ -z "$key" ]]; then
    continue
  fi
  open_target "$key" "$name" "$mode" "$(query_for_key "$key")"
  opened_count=$((opened_count + 1))
done 3< "$evidence_dir/14-targets.tsv"

if [[ "$opened_count" != "$target_count" ]]; then
  printf 'Opened %s target files, expected %s.\n' "$opened_count" "$target_count" >&2
  exit 66
fi

cotton_capture_text_best_effort "90-logcat-raw.txt" cotton_adb logcat -d -v time
grep -E 'Cotton|AndroidRuntime|FATAL EXCEPTION|mono-rt' \
  "$evidence_dir/90-logcat-raw.txt" \
  > "$evidence_dir/91-logcat-cotton.txt" || true

{
  printf 'File-open routing auto smoke passed.\n'
  printf 'Package: %s\n' "$package_id"
  printf 'Folder: %s\n' "$folder_name"
  printf 'Evidence: %s\n' "$evidence_dir"
} | tee "$evidence_dir/99-summary.txt"
