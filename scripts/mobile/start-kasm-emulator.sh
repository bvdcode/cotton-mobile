#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

foreground=1
configure_wifi=1
boot_timeout_seconds="${COTTON_EMULATOR_BOOT_TIMEOUT_SECONDS:-120}"
wifi_timeout_seconds="${COTTON_EMULATOR_WIFI_TIMEOUT_SECONDS:-60}"
emulator_log="${COTTON_EMULATOR_LOG:-/tmp/cotton-mobile-emulator.log}"

usage() {
  cat <<EOF
Usage: $(basename "$0") [--detach] [--no-wifi]

Starts the KASM Android emulator for Cotton.Mobile.

Environment overrides:
  COTTON_AVD_NAME=$COTTON_AVD_NAME
  COTTON_AVD_HOME=$COTTON_AVD_HOME
  COTTON_ADB_SERIAL=$COTTON_ADB_SERIAL
  COTTON_EMULATOR_LOG=$emulator_log
EOF
}

for arg in "$@"; do
  case "$arg" in
    --detach)
      foreground=0
      ;;
    --no-wifi)
      configure_wifi=0
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      printf 'Unknown argument: %s\n' "$arg" >&2
      exit 64
      ;;
  esac
done

export HOME="${COTTON_EMULATOR_HOME:-/root}"
export USER="${COTTON_EMULATOR_USER:-root}"
export LOGNAME="${COTTON_EMULATOR_LOGNAME:-root}"
export ANDROID_AVD_HOME="$COTTON_AVD_HOME"

adb_state() {
  adb -s "$COTTON_ADB_SERIAL" get-state 2>/dev/null || true
}

wait_for_boot() {
  adb -s "$COTTON_ADB_SERIAL" wait-for-device

  local attempt
  for attempt in $(seq 1 "$boot_timeout_seconds"); do
    if [[ "$(adb -s "$COTTON_ADB_SERIAL" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" == "1" ]]; then
      return 0
    fi
    sleep 1
  done

  printf 'Emulator did not boot within %s seconds. Log: %s\n' "$boot_timeout_seconds" "$emulator_log" >&2
  exit 70
}

wait_for_wifi() {
  adb -s "$COTTON_ADB_SERIAL" shell settings put global http_proxy :0 >/dev/null
  adb -s "$COTTON_ADB_SERIAL" shell settings delete global global_http_proxy_host >/dev/null 2>&1 || true
  adb -s "$COTTON_ADB_SERIAL" shell settings delete global global_http_proxy_port >/dev/null 2>&1 || true
  adb -s "$COTTON_ADB_SERIAL" shell cmd wifi set-wifi-enabled enabled >/dev/null 2>&1 || true
  adb -s "$COTTON_ADB_SERIAL" shell cmd wifi start-scan >/dev/null 2>&1 || true
  sleep 2
  adb -s "$COTTON_ADB_SERIAL" shell cmd wifi connect-network AndroidWifi open >/dev/null 2>&1 || true

  local attempt
  for attempt in $(seq 1 "$wifi_timeout_seconds"); do
    if adb -s "$COTTON_ADB_SERIAL" shell ping -c 1 -W 3 app.cottoncloud.dev >/dev/null 2>&1; then
      adb -s "$COTTON_ADB_SERIAL" shell cmd wifi status | sed -n '1,8p'
      return 0
    fi
    sleep 1
  done

  printf 'Emulator Wi-Fi did not validate within %s seconds.\n' "$wifi_timeout_seconds" >&2
  adb -s "$COTTON_ADB_SERIAL" shell cmd wifi status >&2 || true
  exit 69
}

adb start-server >/dev/null

if [[ "$(adb_state)" != "device" ]]; then
  install -m 600 /dev/null "$emulator_log"
  nohup emulator \
    -avd "$COTTON_AVD_NAME" \
    -no-window \
    -no-audio \
    -no-boot-anim \
    -gpu swiftshader_indirect \
    -no-snapshot-load \
    -no-snapshot-save \
    -netfast \
    -dns-server 8.8.8.8,1.1.1.1 \
    -no-metrics \
    >"$emulator_log" 2>&1 &
  emulator_pid="$!"
  printf 'Started %s as pid %s. Log: %s\n' "$COTTON_AVD_NAME" "$emulator_pid" "$emulator_log"
else
  emulator_pid=""
  printf '%s is already connected.\n' "$COTTON_ADB_SERIAL"
fi

wait_for_boot

if [[ "$configure_wifi" -eq 1 ]]; then
  wait_for_wifi
fi

printf 'Emulator ready: %s\n' "$COTTON_ADB_SERIAL"

if [[ "$foreground" -eq 1 && -n "$emulator_pid" ]]; then
  wait "$emulator_pid"
fi
