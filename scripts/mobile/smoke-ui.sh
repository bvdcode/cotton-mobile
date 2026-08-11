#!/usr/bin/env bash

cotton_capture_screen() {
  local prefix="$1"
  local remote_xml="${2:-$COTTON_UI_DUMP_REMOTE_PATH}"

  cotton_capture_text_best_effort "$prefix-window.txt" cotton_adb shell dumpsys window
  if [[ "${COTTON_CAPTURE_ACTIVITY:-0}" -eq 1 ]]; then
    cotton_capture_text_best_effort "$prefix-activity.txt" cotton_adb shell dumpsys activity top
  fi
  if [[ "${COTTON_CAPTURE_CONNECTIVITY:-0}" -eq 1 ]]; then
    cotton_capture_text_best_effort "$prefix-connectivity.txt" cotton_adb shell dumpsys connectivity
  fi
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
    cotton_adb shell rm -f "$remote_xml" >/dev/null 2>&1 || true
  else
    rm -f "$evidence_dir/$prefix.xml"
  fi
}

cotton_xml_has_text() {
  local xml_file="$1"
  local needle="$2"

  [[ -f "$xml_file" ]] && grep -Fq "$needle" "$xml_file"
}

cotton_require_xml_text() {
  local xml_file="$1"
  local needle="$2"
  local message="$3"

  if ! cotton_xml_has_text "$xml_file" "$needle"; then
    printf '%s\n' "$message" >&2
    printf 'Missing text: %s\n' "$needle" >&2
    printf 'Evidence: %s\n' "$xml_file" >&2
    exit "$COTTON_EXIT_EVIDENCE"
  fi
}

cotton_tap_node_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local mode="${3:-${COTTON_NODE_MATCH_MODE_DEFAULT:-contains}}"
  local point_file="$evidence_dir/tap-point.txt"
  local tap_x
  local tap_y

  "$SCRIPT_DIR/smoke-support.py" node-center "$xml_file" "$needle" --mode "$mode" > "$point_file"
  read -r tap_x tap_y < "$point_file"
  cotton_adb shell input tap "$tap_x" "$tap_y"
}

cotton_tap_clickable_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local mode="${3:-${COTTON_NODE_MATCH_MODE_DEFAULT:-contains}}"
  local point_file="$evidence_dir/tap-point.txt"
  local tap_x
  local tap_y

  "$SCRIPT_DIR/smoke-support.py" node-center "$xml_file" "$needle" \
    --mode "$mode" \
    --clickable \
    > "$point_file"
  read -r tap_x tap_y < "$point_file"
  cotton_adb shell input tap "$tap_x" "$tap_y"
}

cotton_tap_row_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local point_file="$evidence_dir/row-point.txt"
  local tap_x
  local tap_y

  "$SCRIPT_DIR/smoke-support.py" row-point "$xml_file" "$needle" > "$point_file"
  read -r tap_x tap_y < "$point_file"
  cotton_adb shell input tap "$tap_x" "$tap_y"
}

cotton_long_press_row_from_xml() {
  local xml_file="$1"
  local needle="$2"
  local point_file="$evidence_dir/row-point.txt"
  local tap_x
  local tap_y

  "$SCRIPT_DIR/smoke-support.py" row-point "$xml_file" "$needle" > "$point_file"
  read -r tap_x tap_y < "$point_file"
  cotton_adb shell input touchscreen swipe "$tap_x" "$tap_y" "$tap_x" "$tap_y" 1800
}

cotton_tap_editable_from_xml() {
  local xml_file="$1"
  local point_file="$evidence_dir/edit-point.txt"
  local tap_x
  local tap_y

  "$SCRIPT_DIR/smoke-support.py" editable-point "$xml_file" > "$point_file"
  read -r tap_x tap_y < "$point_file"
  cotton_adb shell input tap "$tap_x" "$tap_y"
}

cotton_tap_row_action_from_xml() {
  local xml_file="$1"
  local item_name="$2"
  local action_text="$3"
  local point_file="$evidence_dir/row-action-point.txt"
  local tap_x
  local tap_y

  "$SCRIPT_DIR/smoke-support.py" row-action-point \
    "$xml_file" \
    "$item_name" \
    "$action_text" \
    > "$point_file"
  read -r tap_x tap_y < "$point_file"
  cotton_adb shell input tap "$tap_x" "$tap_y"
}

cotton_adb_input_text() {
  local value="$1"
  value="${value// /%s}"
  cotton_adb shell input text "$value"
}

cotton_wait_for_text() {
  local prefix="$1"
  local needle="$2"
  local attempt=0
  local attempts="${COTTON_WAIT_ATTEMPTS:-8}"
  local xml_file

  while [[ "$attempt" -lt "$attempts" ]]; do
    cotton_capture_screen "$prefix-$attempt"
    xml_file="$evidence_dir/$prefix-$attempt.xml"
    if cotton_xml_has_text "$xml_file" "$needle"; then
      waited_xml="$xml_file"
      return
    fi
    sleep 2
    attempt=$((attempt + 1))
  done

  printf 'Timed out waiting for UI text: %s\n' "$needle" >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit "$COTTON_EXIT_EVIDENCE"
}

cotton_wait_for_files_root() {
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5 6 7; do
    prefix="20-files-root-$attempt"
    cotton_capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if cotton_xml_has_text "$xml_file" "Files" && cotton_xml_has_text "$xml_file" "Account"; then
      files_root_xml="$xml_file"
      return
    fi
    if cotton_xml_has_text "$xml_file" "Navigate up"; then
      cotton_tap_node_from_xml "$xml_file" "Navigate up" exact
      sleep 2
      continue
    fi

    cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 1
    cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/20-relaunch-$attempt.txt" || true
    sleep 2
  done

  printf 'Files root with Account navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit "$COTTON_EXIT_EVIDENCE"
}

cotton_wait_for_operator() {
  local prompt="$1"

  printf '\n%s\n' "$prompt"
  printf 'Press Enter when ready to capture evidence... '
  read -r _
}
