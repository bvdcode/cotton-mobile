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

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive file-open routing smoke and captures evidence.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before launch, preserving app data.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --preflight-only          Capture device/package/version state and exit without manual prompts.
  --seed-only               Generate and push sample files to Android Downloads, then exit.
  --skip-seed-files         Do not generate or push sample files.
  --no-launch               Do not launch the app automatically.
  --help, -h                Show this help.

The script is intentionally manual: upload/open the seeded files in Cotton while
it captures screenshots, UIAutomator XML, package state, and logcat output.
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
    --preflight-only)
      preflight_only=1
      shift
      ;;
    --seed-only)
      seed_only=1
      shift
      ;;
    --skip-seed-files)
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
    printf 'skip_seed_files=%s\n' "$skip_seed"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
    printf 'maui_launcher_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/launcher?view=net-maui-10.0\n'
    printf 'maui_open_file_request_docs=https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.applicationmodel.openfilerequest?view=net-maui-10.0\n'
    printf 'android_intent_docs=https://developer.android.com/reference/android/content/Intent\n'
    printf 'android_pdf_renderer_docs=https://developer.android.com/reference/android/graphics/pdf/PdfRenderer\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<'EOF'
# File Open Routing Smoke

## Preconditions

- [ ] Package/version in `05-package-version.txt` matches the build under test.
- [ ] Seeded files are visible in Android Downloads or uploaded to Cotton already.
- [ ] Signed-in Cotton session is restored without clearing app data.
- [ ] A dedicated smoke folder is open in Files.

## Seeded Files

- `cotton-open-text.txt`
- `cotton-open-image.png`
- `cotton-open-doc.pdf`
- `cotton-open-audio.wav`
- `cotton-open-video.mp4`
- `cotton-open-office.docx`
- `cotton-open-archive.zip`
- `cotton-open-unknown.bin`

## Upload Pass

- [ ] Upload all seeded files through `+` -> `Upload file`.
- [ ] Verify all uploaded files appear in the current folder.
- [ ] Verify file rows show expected kinds/badges where visible.

## Open Pass

- [ ] Text opens in Cotton text viewer.
- [ ] Image opens in Cotton image viewer.
- [ ] PDF action label says `Open with system app` and launches/handles system PDF flow or honest no-app fallback.
- [ ] Audio action label says `Open with system app` and launches/handles system audio flow or honest no-app fallback.
- [ ] Video action label says `Open with system app` and launches/handles system video flow or honest no-app fallback.
- [ ] Office document action label says `Open with system app` and launches/handles system document flow or honest no-app fallback.
- [ ] Archive action label says `Open with system app` and launches/handles system archive flow or honest no-app fallback.
- [ ] Unknown file action label says `Open with system app` and shows honest no-app fallback if no handler exists.

## Evidence To Review

- `00-device.txt`
- `05-package-version.txt`
- `10-preflight.png` / `10-preflight.xml`
- `20-files-ready.png` / `20-files-ready.xml`
- `30-files-uploaded.png` / `30-files-uploaded.xml`
- `40-text-open.png` / `40-text-open.xml`
- `41-image-open.png` / `41-image-open.xml`
- `42-pdf-open.png` / `42-pdf-open.xml`
- `43-audio-open.png` / `43-audio-open.xml`
- `44-video-open.png` / `44-video-open.xml`
- `45-office-open.png` / `45-office-open.xml`
- `46-archive-open.png` / `46-archive-open.xml`
- `47-unknown-open.png` / `47-unknown-open.xml`
- `90-logcat.txt`
EOF
}

capture_text() {
  local name="$1"
  shift
  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed: %q\n' "$1" >> "$evidence_dir/$name"
  fi
}

capture_device_state() {
  local prefix="$1"

  capture_text "$prefix-window.txt" adb_device shell dumpsys window
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

prompt_capture() {
  local message="$1"
  local prefix="$2"
  printf '\n%s\n' "$message"
  printf 'Press Enter to capture %s... ' "$prefix"
  read -r _
  capture_device_state "$prefix"
}

generate_sample_files() {
  local sample_dir="$1"
  mkdir -p "$sample_dir"
  python3 - "$sample_dir" <<'PY'
import base64
import math
import struct
import sys
import wave
import zipfile
from pathlib import Path

root = Path(sys.argv[1])
root.mkdir(parents=True, exist_ok=True)

(root / "cotton-open-text.txt").write_text(
    "Cotton text open routing smoke.\nThis file should open inside Cotton.\n",
    encoding="utf-8",
)

png = (
    "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAIAAAAlC+aJAAAAW0lEQVR4nO3PQQ0A"
    "IBDAMMC/5+ONAvZoFSzZnZnZ3S8D+A24DWgD2oA2oA1oA9qANqANaAPagDagDWgD"
    "2oA2oA1oA9qANqANaAPagDagDWgD2oA2oA1oA9qANqANaAPagHb2DgHrYcRrGgAA"
    "AABJRU5ErkJggg=="
)
(root / "cotton-open-image.png").write_bytes(base64.b64decode(png))

(root / "cotton-open-doc.pdf").write_bytes(
    b"%PDF-1.4\n"
    b"1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n"
    b"2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n"
    b"3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 240 240] /Contents 4 0 R >> endobj\n"
    b"4 0 obj << /Length 57 >> stream\n"
    b"BT /F1 12 Tf 24 120 Td (Cotton PDF open routing smoke) Tj ET\n"
    b"endstream endobj\n"
    b"xref\n0 5\n0000000000 65535 f \n"
    b"trailer << /Root 1 0 R /Size 5 >>\nstartxref\n0\n%%EOF\n"
)

with wave.open(str(root / "cotton-open-audio.wav"), "wb") as wav:
    sample_rate = 8000
    wav.setnchannels(1)
    wav.setsampwidth(2)
    wav.setframerate(sample_rate)
    frames = bytearray()
    for i in range(sample_rate // 2):
        sample = int(12000 * math.sin(2 * math.pi * 440 * i / sample_rate))
        frames.extend(struct.pack("<h", sample))
    wav.writeframes(bytes(frames))

mp4_hex = (
    "00000018667479706d7034320000000069736f6d6d703432"
    "00000008667265650000001d6d646174000000000000000000000000000000000000000000"
)
(root / "cotton-open-video.mp4").write_bytes(bytes.fromhex(mp4_hex))

with zipfile.ZipFile(root / "cotton-open-office.docx", "w", zipfile.ZIP_DEFLATED) as docx:
    docx.writestr(
        "[Content_Types].xml",
        '<?xml version="1.0" encoding="UTF-8"?>'
        '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
        '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
        '<Default Extension="xml" ContentType="application/xml"/>'
        '<Override PartName="/word/document.xml" '
        'ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>'
        "</Types>",
    )
    docx.writestr(
        "_rels/.rels",
        '<?xml version="1.0" encoding="UTF-8"?>'
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
        '<Relationship Id="rId1" '
        'Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" '
        'Target="word/document.xml"/>'
        "</Relationships>",
    )
    docx.writestr(
        "word/document.xml",
        '<?xml version="1.0" encoding="UTF-8"?>'
        '<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">'
        "<w:body><w:p><w:r><w:t>Cotton Office open routing smoke</w:t></w:r></w:p></w:body>"
        "</w:document>",
    )

with zipfile.ZipFile(root / "cotton-open-archive.zip", "w", zipfile.ZIP_DEFLATED) as archive:
    archive.writestr("cotton-open-archive-readme.txt", "Cotton archive open routing smoke\n")

(root / "cotton-open-unknown.bin").write_bytes(b"\x00Cotton unknown file routing smoke\xff\n")
PY
}

seed_sample_files() {
  local sample_dir="$evidence_dir/seed-files"
  generate_sample_files "$sample_dir"
  : > "$evidence_dir/06-seeded-files.txt"
  for file in "$sample_dir"/cotton-open-*; do
    local name
    name="$(basename "$file")"
    adb_device push "$file" "/sdcard/Download/$name" >> "$evidence_dir/06-seeded-files.txt" 2>&1
    adb_device shell am broadcast \
      -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
      -d "file:///sdcard/Download/$name" >> "$evidence_dir/06-seeded-files.txt" 2>&1 || true
  done

  capture_text "07-downloads-list.txt" adb_device shell ls -la /sdcard/Download
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

if [[ "$skip_seed" -eq 0 ]]; then
  seed_sample_files
fi

adb_device logcat -c >/dev/null 2>&1 || true

if [[ "$launch_app" -eq 1 ]]; then
  capture_text "08-launch.txt" adb_device shell monkey -p "$package_id" 1
  sleep 2
fi

capture_device_state "10-preflight"

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
prompt_capture "Return to Files, open cotton-open-image.png, and verify Cotton image viewer." "41-image-open"
prompt_capture "Return to Files, open cotton-open-doc.pdf, and verify system PDF flow or honest no-app fallback." "42-pdf-open"
prompt_capture "Return to Files, open cotton-open-audio.wav, and verify system audio flow or honest no-app fallback." "43-audio-open"
prompt_capture "Return to Files, open cotton-open-video.mp4, and verify system video flow or honest no-app fallback." "44-video-open"
prompt_capture "Return to Files, open cotton-open-office.docx, and verify system Office flow or honest no-app fallback." "45-office-open"
prompt_capture "Return to Files, open cotton-open-archive.zip, and verify system archive flow or honest no-app fallback." "46-archive-open"
prompt_capture "Return to Files, open cotton-open-unknown.bin, and verify honest no-app fallback if no handler exists." "47-unknown-open"

capture_text "90-logcat.txt" adb_device logcat -d -v threadtime

printf '\nFile-open routing smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md before marking the roadmap runtime smoke complete.\n'
