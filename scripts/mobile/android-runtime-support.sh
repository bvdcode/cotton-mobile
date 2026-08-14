cleanup() {
  if [[ -x "$adb_bin" ]]; then
    if [[ -n "$remote_media_uri" ]]; then
      "$adb_bin" shell content delete --uri "$remote_media_uri" >/dev/null 2>&1 || true
    fi

    "$adb_bin" emu kill >/dev/null 2>&1 || true
  fi

  if [[ -n "$emulator_pid" ]]; then
    wait "$emulator_pid" >/dev/null 2>&1 || true
  fi
}

wait_for_boot() {
  local attempt
  for attempt in {1..120}; do
    if [[ "$("$adb_bin" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" == "1" ]]; then
      "$adb_bin" shell input keyevent 82 >/dev/null 2>&1 || true
      return
    fi

    sleep 2
  done

  printf 'Android emulator did not finish booting.\n' >&2
  tail -100 "$emulator_log" >&2 || true
  exit 1
}

wait_for_device() {
  if timeout 120 "$adb_bin" wait-for-device; then
    return
  fi

  printf 'Android emulator did not register with ADB.\n' >&2
  tail -100 "$emulator_log" >&2 || true
  exit 1
}

wait_for_disconnect() {
  local attempt
  for attempt in {1..60}; do
    if ! "$adb_bin" get-state >/dev/null 2>&1; then
      return
    fi

    sleep 1
  done

  printf 'Android emulator did not disconnect for reboot.\n' >&2
  exit 1
}

launch_application() {
  "$adb_bin" shell monkey \
    -p "$package_name" \
    -c android.intent.category.LAUNCHER \
    1 >/dev/null
  "$adb_bin" shell input keyevent 3 >/dev/null
}

wait_for_jobs() {
  local phase="$1"
  local maximum_attempts="${2:-60}"
  local attempt
  for ((attempt = 1; attempt <= maximum_attempts; attempt++)); do
    if has_scheduled_job; then
      return
    fi

    sleep 1
  done

  printf 'WorkManager jobs were not registered for %s %s.\n' "$package_name" "$phase" >&2
  "$adb_bin" shell dumpsys jobscheduler >&2
  exit 1
}

has_scheduled_job() {
  local line
  while IFS= read -r line; do
    if [[ "$line" == *"JOB "* && "$line" == *"$package_name/"* ]]; then
      return 0
    fi
  done < <("$adb_bin" shell dumpsys jobscheduler | tr -d '\r')

  return 1
}

find_media_id() {
  local collection_uri="$1"
  local attempt
  local media_id
  local query_output

  for attempt in {1..30}; do
    query_output="$("$adb_bin" shell content query \
      --uri "$collection_uri" \
      --projection _id:_display_name | tr -d '\r')"
    media_id="$(printf '%s\n' "$query_output" \
      | sed -n '/_display_name=cotton-runtime\.png/s/.*_id=\([0-9][0-9]*\).*/\1/p' \
      | head -1)"
    if [[ -n "$media_id" ]]; then
      printf '%s\n' "$media_id"
      return
    fi

    sleep 1
  done

  printf 'Could not locate Android MediaStore fixture: %s\n' "$query_output" >&2
  exit 1
}

create_scoped_media_fixture() {
  local media_id

  "$adb_bin" push "$media_file" "$remote_media_path" >/dev/null
  "$adb_bin" shell content insert \
    --uri "$media_collection_uri" \
    --bind _display_name:s:cotton-runtime.png \
    --bind mime_type:s:image/png \
    --bind relative_path:s:Pictures/CottonRuntime/ \
    --bind is_pending:i:1
  media_id="$(find_media_id "$pending_media_collection_uri")"
  remote_media_uri="$media_collection_uri/$media_id"
  "$adb_bin" shell "content write --uri '$remote_media_uri' < '$remote_media_path'"
  "$adb_bin" shell content update \
    --uri "$remote_media_uri" \
    --bind is_pending:i:0 >/dev/null
}

create_legacy_media_fixture() {
  local media_id

  "$adb_bin" shell mkdir -p "$legacy_media_directory"
  "$adb_bin" push "$media_file" "$legacy_media_path" >/dev/null
  "$adb_bin" shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file://$legacy_media_path" >/dev/null
  media_id="$(find_media_id "$legacy_media_collection_uri")"
  remote_media_uri="$legacy_media_collection_uri/$media_id"
}

create_media_fixture() {
  if [[ "$runtime_api" -lt 30 ]]; then
    create_legacy_media_fixture
    return
  fi

  create_scoped_media_fixture
}

update_scoped_media_fixture() {
  local modified_at

  modified_at="$(($(date +%s) + 60))"
  "$adb_bin" shell "printf x >> '$remote_media_path'"
  "$adb_bin" shell "content write --uri '$remote_media_uri' < '$remote_media_path'"
  "$adb_bin" shell content update \
    --uri "$remote_media_uri" \
    --bind date_modified:l:"$modified_at" >/dev/null
}

update_legacy_media_fixture() {
  "$adb_bin" shell "printf x >> '$legacy_media_path'"
  "$adb_bin" shell am broadcast \
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE \
    -d "file://$legacy_media_path" >/dev/null
  sleep 2
}

update_media_fixture() {
  if [[ "$runtime_api" -lt 30 ]]; then
    update_legacy_media_fixture
    return
  fi

  update_scoped_media_fixture
}
