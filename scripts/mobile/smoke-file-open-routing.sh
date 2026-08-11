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
seed_only=0
skip_seed=0
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive file-open routing smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current embedded debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --preflight-only          Capture device/package/version state and exit without manual prompts.
  --seed-only               Generate and push sample files to Android Downloads, then exit.
  --skip-seed-files         Do not generate or push sample files.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The script is intentionally manual: upload/open the seeded files in Cotton while
it captures screenshots, UIAutomator XML, package state, and logcat output.
Build the debug APK with scripts/mobile/build-android-debug.sh before using
--install-debug so assemblies are embedded in the APK.
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
  "--seed-only:seed_only:1"
  "--skip-seed-files:skip_seed:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if ! command -v python3 >/dev/null 2>&1; then
  printf 'python3 was not found. It is required to generate smoke files.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && "$seed_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Use --preflight-only for package/version evidence or --seed-only to push sample files.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-file-open-routing"
fi

mkdir -p "$evidence_dir"

# shellcheck source=smoke-file-open-routing-support.sh
source "$SCRIPT_DIR/smoke-file-open-routing-support.sh"

write_metadata
write_checklist

cotton_prepare_installed_package

if [[ "$skip_seed" -eq 0 ]]; then
  seed_sample_files
fi

cotton_adb logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  cotton_capture_text_best_effort "08-launch.txt" cotton_adb shell monkey -p "$package_id" 1
  sleep 2
fi

capture_device_state "10-preflight"
cotton_capture_text_best_effort "11-launch-logcat.txt" cotton_adb logcat -d -v threadtime

if [[ "$preflight_only" -eq 1 || "$seed_only" -eq 1 ]]; then
  if [[ "$seed_only" -eq 1 ]]; then
    printf '\nFile-open routing seed evidence: %s\n' "$evidence_dir"
  else
    printf '\nFile-open routing preflight evidence: %s\n' "$evidence_dir"
  fi
  exit 0
fi

prompt_capture "Open Cotton Files. Sign in if needed, then navigate to a dedicated smoke folder." "20-files-ready"
prompt_capture "Upload all seeded cotton-open-* files from Android Downloads and verify they appear in Cotton." "30-files-uploaded"
prompt_capture "Open cotton-open-text.txt and verify Cotton text viewer." "40-text-open"
prompt_capture "Return to Files, open cotton-open-vector.svg, and verify Cotton text viewer with SVG details." "41-svg-open"
prompt_capture "Return to Files, open cotton-open-image.png, and verify Cotton image viewer." "42-image-open"
prompt_capture "Return to Files, open cotton-open-doc.pdf, and verify Cotton PDF viewer with external Open action." "43-pdf-open"
prompt_capture "Return to Files, open cotton-open-audio.wav, and verify Cotton media viewer with playback controls." "44-audio-open"
prompt_capture "Return to Files, open cotton-open-video.mp4, and verify Cotton media viewer with playback controls." "45-video-open"
prompt_capture "Return to Files, open cotton-open-office.docx, and verify system Office flow or honest no-app fallback." "46-office-open"
prompt_capture "Return to Files, open cotton-open-archive.zip, and verify system archive flow or honest no-app fallback." "47-archive-open"
prompt_capture "Return to Files, open cotton-open-unknown.bin, and verify honest no-app fallback if no handler exists." "48-unknown-open"

cotton_capture_text_best_effort "90-logcat.txt" cotton_adb logcat -d -v threadtime

printf '\nFile-open routing smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md before marking the roadmap runtime smoke complete.\n'
