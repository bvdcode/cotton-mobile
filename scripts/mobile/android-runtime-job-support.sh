wait_for_media_sync_job() {
  local phase="$1"
  local maximum_attempts="${2:-60}"
  local attempt
  for ((attempt = 1; attempt <= maximum_attempts; attempt++)); do
    if has_media_sync_job; then
      return
    fi

    sleep 1
  done

  printf 'MediaStore sync job was not registered for %s %s.\n' "$package_name" "$phase" >&2
  "$adb_bin" shell dumpsys jobscheduler >&2
  "$adb_bin" logcat -d \
    | grep -E "$media_sync_log_tag|$media_sync_job_service|BOOT_COMPLETED" \
    | tail -300 >&2 \
    || true
  exit 1
}

wait_for_media_sync_start() {
  local attempt
  local output
  for attempt in {1..60}; do
    output="$("$adb_bin" logcat -d -s "$media_sync_log_tag:I" '*:S' | tr -d '\r')"
    if [[ "$output" == *"started"* ]]; then
      return
    fi

    sleep 1
  done

  printf 'MediaStore sync job did not start after media changed with the app closed.\n' >&2
  "$adb_bin" logcat -d >&2
  exit 1
}

has_media_sync_job() {
  local jobs
  local line
  jobs="$("$adb_bin" shell dumpsys jobscheduler | tr -d '\r')"

  while IFS= read -r line; do
    if [[ "$line" == *"JOB #"* \
      && "$line" == *"/$media_sync_job_id:"* \
      && "$line" == *"$package_name/$media_sync_job_service"* ]]; then
      return 0
    fi
  done <<<"$jobs"

  return 1
}
