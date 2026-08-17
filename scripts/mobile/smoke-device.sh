#!/usr/bin/env bash

cotton_adb() {
  local command_name="${1:-}"
  if command -v cygpath >/dev/null 2>&1 \
    && [[ "$command_name" == "push" || "$command_name" == "pull" ]] \
    && [[ $# -eq 3 ]]; then
    local source_path="$2"
    local target_path="$3"
    if [[ "$command_name" == "push" ]]; then
      source_path="$(cygpath -w "$source_path")"
    else
      target_path="$(cygpath -w "$target_path")"
    fi

    MSYS_NO_PATHCONV=1 adb -s "$serial" "$command_name" "$source_path" "$target_path"
    return
  fi

  MSYS_NO_PATHCONV=1 adb -s "$serial" "$@"
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

  if ! state="$(cotton_adb get-state 2>/dev/null)"; then
    printf 'ADB device %s is unavailable.\n' "$serial" >&2
    exit "$COTTON_EXIT_DEVICE_UNAVAILABLE"
  fi

  state="${state//$'\r'/}"
  if [[ "$state" != "device" ]]; then
    printf 'ADB device %s is unavailable.\n' "$serial" >&2
    exit "$COTTON_EXIT_DEVICE_UNAVAILABLE"
  fi
}
