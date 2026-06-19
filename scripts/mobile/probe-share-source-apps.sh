#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

adb_device() {
  adb -s "$COTTON_ADB_SERIAL" "$@"
}

print_section() {
  printf '\n== %s ==\n' "$1"
}

print_section "Device"
adb_device shell getprop ro.product.model | tr -d '\r'
adb_device shell getprop ro.build.version.release | tr -d '\r'
adb_device shell getprop ro.build.version.sdk | tr -d '\r'

print_section "Installed share-source packages"
adb_device shell pm list packages \
  | tr -d '\r' \
  | sort \
  | rg 'photos|gallery|documentsui|chrome|browser|docs|drive|pdf|camera|print' || true

print_section "Image viewer"
adb_device shell cmd package resolve-activity \
  -a android.intent.action.VIEW \
  -t image/png \
  | tr -d '\r'

print_section "PDF viewer"
adb_device shell cmd package resolve-activity \
  -a android.intent.action.VIEW \
  -t application/pdf \
  | tr -d '\r'

print_section "Browser"
adb_device shell cmd package resolve-activity \
  -a android.intent.action.VIEW \
  -d https://example.com \
  | tr -d '\r'

print_section "PDF-capable activities"
adb_device shell cmd package query-activities --components \
  -a android.intent.action.VIEW \
  -t application/pdf \
  | tr -d '\r'
