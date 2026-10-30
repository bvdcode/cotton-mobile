#!/usr/bin/env bash

cotton_adb() {
  adb -s "$serial" "$@"
}

cotton_capture_text_best_effort() {
  local name="$1"
  shift

  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed: %q\n' "$1" >> "$evidence_dir/$name"
  fi
}

cotton_require_device() {
  local state

  if ! state="$(cotton_adb get-state 2>/dev/null)" || [[ "$state" != "device" ]]; then
    printf 'ADB device %s is unavailable.\n' "$serial" >&2
    exit "$COTTON_EXIT_DEVICE_UNAVAILABLE"
  fi
}
