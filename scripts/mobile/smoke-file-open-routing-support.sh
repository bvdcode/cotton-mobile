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
    printf 'maui_mediaelement_docs=https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/views/mediaelement\n'
    printf 'maui_open_file_request_docs=https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.applicationmodel.openfilerequest?view=net-maui-10.0\n'
    printf 'android_intent_docs=https://developer.android.com/reference/android/content/Intent\n'
    printf 'android_exoplayer_supported_formats_docs=https://developer.android.com/media/media3/exoplayer/supported-formats\n'
    printf 'android_pdf_renderer_docs=https://developer.android.com/reference/android/graphics/pdf/PdfRenderer\n'
  } > "$evidence_dir/metadata.env"
}

write_checklist() {
  cat > "$evidence_dir/checklist.md" <<'EOF'
# File Open Routing Smoke

## Preconditions

- [ ] Package/version in `05-package-version.txt` matches the build under test.
- [ ] `--install-debug` uses an APK built by `scripts/mobile/build-android-debug.sh`.
- [ ] Seeded files are visible in Android Downloads or uploaded to Cotton already.
- [ ] Signed-in Cotton session is restored without clearing app data.
- [ ] A dedicated smoke folder is open in Files.

## Seeded Files

- `cotton-open-text.txt`
- `cotton-open-vector.svg`
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
- [ ] SVG appears as `SVG` and opens in Cotton text viewer without using WebView.
- [ ] Image opens in Cotton image viewer.
- [ ] PDF opens in Cotton PDF viewer and exposes `Open` for external handoff.
- [ ] Audio opens in Cotton media viewer with playback controls.
- [ ] Video opens in Cotton media viewer with playback controls.
- [ ] Office document action label says `Open with system app` and launches/handles system document flow or honest no-app fallback.
- [ ] Archive action label says `Open with system app` and launches/handles system archive flow or honest no-app fallback.
- [ ] Unknown file action label says `Open with system app` and shows honest no-app fallback if no handler exists.

## Evidence To Review

- `00-device.txt`
- `05-package-version.txt`
- `10-preflight.png` / `10-preflight.xml`
- `11-launch-logcat.txt`
- `20-files-ready.png` / `20-files-ready.xml`
- `30-files-uploaded.png` / `30-files-uploaded.xml`
- `40-text-open.png` / `40-text-open.xml`
- `41-svg-open.png` / `41-svg-open.xml`
- `42-image-open.png` / `42-image-open.xml`
- `43-pdf-open.png` / `43-pdf-open.xml`
- `44-audio-open.png` / `44-audio-open.xml`
- `45-video-open.png` / `45-video-open.xml`
- `46-office-open.png` / `46-office-open.xml`
- `47-archive-open.png` / `47-archive-open.xml`
- `48-unknown-open.png` / `48-unknown-open.xml`
- `90-logcat.txt`
EOF
}


capture_device_state() {
  local prefix="$1"

  cotton_capture_text_best_effort "$prefix-window.txt" cotton_adb shell dumpsys window
  cotton_capture_text_best_effort "$prefix-package.txt" cotton_adb shell pm path "$package_id"

  if ! cotton_adb exec-out screencap -p > "$evidence_dir/$prefix.png" 2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  if cotton_adb shell uiautomator dump /sdcard/cotton-window.xml > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    if ! cotton_adb pull /sdcard/cotton-window.xml "$evidence_dir/$prefix.xml" > "$evidence_dir/$prefix-pull-xml.log" 2>&1; then
      rm -f "$evidence_dir/$prefix.xml"
    fi
    cotton_adb shell rm -f /sdcard/cotton-window.xml >/dev/null 2>&1 || true
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

(root / "cotton-open-vector.svg").write_text(
    '<svg xmlns="http://www.w3.org/2000/svg" width="120" height="80" viewBox="0 0 120 80">\n'
    '  <rect width="120" height="80" rx="12" fill="#132126"/>\n'
    '  <circle cx="40" cy="40" r="18" fill="#c6ff00"/>\n'
    '  <path d="M64 24 L94 40 L64 56 Z" fill="#ffffff"/>\n'
    "</svg>\n",
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

mp4 = (
    "AAAAIGZ0eXBpc29tAAACAGlzb21pc28yYXZjMW1wNDEAAAOIbW9vdgAAAGxtdmhkAAAAAAAAAAAA"
    "AAAAAAAD6AAAA+gAAQAAAQAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAABAAAAAAAAAAAAAAAA"
    "AABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAArJ0cmFrAAAAXHRraGQAAAADAAAA"
    "AAAAAAAAAAABAAAAAAAAA+gAAAAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAABAAAAAAAA"
    "AAAAAAAAAABAAAAAAEAAAABAAAAAAAAkZWR0cwAAABxlbHN0AAAAAAAAAAEAAAPoAAAAAAABAAAA"
    "AAIqbWRpYQAAACBtZGhkAAAAAAAAAAAAAAAAAAAyAAAAMgBVxAAAAAAALWhkbHIAAAAAAAAAAHZp"
    "ZGUAAAAAAAAAAAAAAABWaWRlb0hhbmRsZXIAAAAB1W1pbmYAAAAUdm1oZAAAAAEAAAAAAAAAAAAA"
    "ACRkaW5mAAAAHGRyZWYAAAAAAAAAAQAAAAx1cmwgAAAAAQAAAZVzdGJsAAAAuXN0c2QAAAAAAAAA"
    "AQAAAKlhdmMxAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAEAAQABIAAAASAAAAAAAAAABFUxhdmM2"
    "MC4zMS4xMDIgbGlieDI2NAAAAAAAAAAAAAAAGP//AAAAL2F2Y0MBQsAe/+EAF2dCwB7ZBCbARAAA"
    "AwAEAAADAMg8WLkgAQAFaMuDyyAAAAAQcGFzcAAAAAEAAAABAAAAFGJ0cnQAAAAAAAAcQAAAHEAA"
    "AAAYc3R0cwAAAAAAAAABAAAAGQAAAgAAAAAUc3RzcwAAAAAAAAABAAAAAQAAABxzdHNjAAAAAAAA"
    "AAEAAAABAAAAGQAAAAEAAAB4c3RzegAAAAAAAAAAAAAAGQAAApgAAAAKAAAACgAAAAoAAAAKAAAA"
    "CgAAAAoAAAAKAAAACgAAAAoAAAAKAAAACgAAAAoAAAAKAAAACgAAAAoAAAAKAAAACgAAAAoAAAAK"
    "AAAACgAAAAoAAAAKAAAACgAAAAoAAAAUc3RjbwAAAAAAAAABAAADuAAAAGJ1ZHRhAAAAWm1ldGEA"
    "AAAAAAAAIWhkbHIAAAAAAAAAAG1kaXJhcHBsAAAAAAAAAAAAAAAALWlsc3QAAAAlqXRvbwAAAB1k"
    "YXRhAAAAAQAAAABMYXZmNjAuMTYuMTAwAAAACGZyZWUAAAOQbWRhdAAAAnEGBf//bdxF6b3m2Ui3"
    "lizYINkj7u94MjY0IC0gY29yZSAxNjQgcjMxMDggMzFlMTlmOSAtIEguMjY0L01QRUctNCBBVkMg"
    "Y29kZWMgLSBDb3B5bGVmdCAyMDAzLTIwMjMgLSBodHRwOi8vd3d3LnZpZGVvbGFuLm9yZy94MjY0"
    "Lmh0bWwgLSBvcHRpb25zOiBjYWJhYz0wIHJlZj0zIGRlYmxvY2s9MTowOjAgYW5hbHlzZT0weDE6"
    "MHgxMTEgbWU9aGV4IHN1Ym1lPTcgcHN5PTEgcHN5X3JkPTEuMDA6MC4wMCBtaXhlZF9yZWY9MSBt"
    "ZV9yYW5nZT0xNiBjaHJvbWFfbWU9MSB0cmVsbGlzPTEgOHg4ZGN0PTAgY3FtPTAgZGVhZHpvbmU9"
    "MjEsMTEgZmFzdF9wc2tpcD0xIGNocm9tYV9xcF9vZmZzZXQ9LTIgdGhyZWFkcz0yIGxvb2thaGVh"
    "ZF90aHJlYWRzPTEgc2xpY2VkX3RocmVhZHM9MCBucj0wIGRlY2ltYXRlPTEgaW50ZXJsYWNlZD0w"
    "IGJsdXJheV9jb21wYXQ9MCBjb25zdHJhaW5lZF9pbnRyYT0wIGJmcmFtZXM9MCB3ZWlnaHRwPTAg"
    "a2V5aW50PTI1MCBrZXlpbnRfbWluPTI1IHNjZW5lY3V0PTQwIGludHJhX3JlZnJlc2g9MCByY19s"
    "b29rYWhlYWQ9NDAgcmM9Y3JmIG1idHJlZT0xIGNyZj0yMy4wIHFjb21wPTAuNjAgcXBtaW49MCBx"
    "cG1heD02OSBxcHN0ZXA9NCBpcF9yYXRpbz0xLjQwIGFxPTE6MS4wMACAAAAAH2WIhAzxGKAAIRMc"
    "AAR/o4AAiyycnJ1111111111114AAAAGQZo4GeEYAAAABkGaVAZ4RgAAAAZBmmAzwjAAAAAGQZqA"
    "M8IwAAAABkGaoDPCMAAAAAZBmsAzwjAAAAAGQZrgM8IwAAAABkGbADPCMAAAAAZBmyAzwjAAAAAG"
    "QZtAM8IwAAAABkGbYDPCMAAAAAZBm4AzwjAAAAAGQZugM8IwAAAABkGbwDPCMAAAAAZBm+AzwjAA"
    "AAAGQZoAM8IwAAAABkGaIDPCMAAAAAZBmkAzwjAAAAAGQZpgM8IwAAAABkGagDPCMAAAAAZBmqAz"
    "wjAAAAAGQZrAL8IwAAAABkGa4C/CMAAAAAZBmwArwjA="
)
(root / "cotton-open-video.mp4").write_bytes(base64.b64decode(mp4))

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
    cotton_adb push "$file" "/sdcard/Download/$name" >> "$evidence_dir/06-seeded-files.txt" 2>&1
    cotton_adb shell am broadcast \
      -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
      -d "file:///sdcard/Download/$name" >> "$evidence_dir/06-seeded-files.txt" 2>&1 || true
  done

  cotton_capture_text_best_effort "07-downloads-list.txt" cotton_adb shell ls -la /sdcard/Download
}
