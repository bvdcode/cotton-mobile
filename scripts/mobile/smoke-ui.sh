#!/usr/bin/env bash

cotton_capture_screen() {
  local prefix="$1"
  local remote_xml="${2:-$COTTON_UI_DUMP_REMOTE_PATH}"

  cotton_capture_text_best_effort "$prefix-window.txt" cotton_adb shell dumpsys window
  if ! cotton_adb exec-out screencap -p \
    > "$evidence_dir/$prefix.png" \
    2> "$evidence_dir/$prefix-screencap.err"; then
    rm -f "$evidence_dir/$prefix.png"
  fi

  cotton_adb shell rm -f "$remote_xml" >/dev/null 2>&1 || true
  if cotton_adb shell uiautomator dump "$remote_xml" \
    > "$evidence_dir/$prefix-uiautomator.log" 2>&1; then
    if ! cotton_adb pull "$remote_xml" "$evidence_dir/$prefix.xml" \
      > "$evidence_dir/$prefix-pull-xml.log" 2>&1; then
      rm -f "$evidence_dir/$prefix.xml"
    fi
  else
    rm -f "$evidence_dir/$prefix.xml"
  fi
  cotton_adb shell rm -f "$remote_xml" >/dev/null 2>&1 || true
}

cotton_xml_has_text() {
  local xml_file="$1"
  local needle="$2"

  [[ -f "$xml_file" ]] \
    && "$SCRIPT_DIR/smoke-support.py" has-node "$xml_file" "$needle" --mode exact \
      >/dev/null 2>&1
}

cotton_require_xml_text() {
  local xml_file="$1"
  local needle="$2"
  local message="$3"

  if ! cotton_xml_has_text "$xml_file" "$needle"; then
    printf '%s\nMissing exact text: %s\nEvidence: %s\n' "$message" "$needle" "$xml_file" >&2
    exit "$COTTON_EXIT_EVIDENCE"
  fi
}

cotton_tap_clickable_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local point_file="$evidence_dir/tap-point.txt"
  local tap_x
  local tap_y

  "$SCRIPT_DIR/smoke-support.py" node-center "$xml_file" "$needle" \
    --mode exact \
    --clickable \
    > "$point_file"
  read -r tap_x tap_y < "$point_file"
  cotton_adb shell input tap "$tap_x" "$tap_y"
}

cotton_long_press_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local point_file="$evidence_dir/long-press-point.txt"
  local press_x
  local press_y

  "$SCRIPT_DIR/smoke-support.py" node-center "$xml_file" "$needle" \
    --mode exact \
    > "$point_file"
  read -r press_x press_y < "$point_file"
  cotton_adb shell input touchscreen swipe "$press_x" "$press_y" "$press_x" "$press_y" 1000
}
