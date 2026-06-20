#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

launch_app=1

for arg in "$@"; do
  case "$arg" in
    --no-launch)
      launch_app=0
      ;;
    --help|-h)
      printf 'Usage: %s [--no-launch]\n' "$(basename "$0")"
      exit 0
      ;;
    *)
      printf 'Unknown argument: %s\n' "$arg" >&2
      exit 64
      ;;
  esac
done

if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
  printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
  exit 66
fi

cotton_install_android_apk "$COTTON_ADB_SERIAL" "$COTTON_ANDROID_PACKAGE_ID" "$COTTON_ANDROID_APK"

if [[ "$launch_app" -eq 1 ]]; then
  adb -s "$COTTON_ADB_SERIAL" shell monkey -p "$COTTON_ANDROID_PACKAGE_ID" 1
fi
