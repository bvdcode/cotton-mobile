#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

if ! command -v ffmpeg >/dev/null 2>&1; then
  printf 'ffmpeg is required to generate the smoke PNG.\n' >&2
  exit 69
fi

adb_device() {
  adb -s "$COTTON_ADB_SERIAL" "$@"
}

work_dir="${1:-/tmp/cotton-mobile-share-source-smoke}"
mkdir -p "$work_dir"

image_file="$work_dir/cotton-share-source-image.png"
pdf_file="$work_dir/cotton-share-source-pdf.pdf"
html_file="$work_dir/cotton-share-source-download.html"
text_file="$work_dir/cotton-share-source-download.txt"

ffmpeg -hide_banner -loglevel error \
  -f lavfi \
  -i color=c=0x207A3B:s=320x240 \
  -frames:v 1 \
  -y "$image_file"

cat > "$pdf_file" <<'PDF'
%PDF-1.4
1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj
2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj
3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R >> endobj
4 0 obj << /Length 45 >> stream
BT /F1 12 Tf 20 100 Td (Cotton PDF viewer smoke) Tj ET
endstream endobj
xref
0 5
0000000000 65535 f 
trailer << /Root 1 0 R /Size 5 >>
startxref
0
%%EOF
PDF

cat > "$html_file" <<'HTML'
<!doctype html><title>Cotton download smoke</title><p>Cotton browser downloaded file smoke.</p>
HTML

printf 'Cotton browser downloaded source smoke\n' > "$text_file"

adb_device push "$image_file" /sdcard/Download/cotton-share-source-image.png
adb_device push "$pdf_file" /sdcard/Download/cotton-share-source-pdf.pdf
adb_device push "$html_file" /sdcard/Download/cotton-share-source-download.html
adb_device push "$text_file" /sdcard/Download/cotton-share-source-download.txt

for name in \
  cotton-share-source-image.png \
  cotton-share-source-pdf.pdf \
  cotton-share-source-download.html \
  cotton-share-source-download.txt; do
  adb_device shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file:///sdcard/Download/$name" >/dev/null || true
done

sleep 1

adb_device shell content query \
  --uri content://media/external/file \
  --projection _id:_display_name:mime_type \
  | tr -d '\r' \
  | rg 'cotton-share-source-'
