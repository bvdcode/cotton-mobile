#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  printf 'Usage: %s <debug-apk> <media-file>\n' "$0" >&2
  exit 2
fi

readonly apk_path="$1"
readonly media_file="$2"
readonly package_name="dev.cottoncloud.app.debug"
readonly diagnostics_action="dev.cottoncloud.app.debug.SYNC_DIAGNOSTICS"
readonly diagnostics_tag="CottonSyncDiagnostics"
readonly runtime_api="${COTTON_ANDROID_RUNTIME_API:-35}"
readonly avd_name="cotton-runtime-$runtime_api"
readonly system_image="system-images;android-$runtime_api;google_apis;x86_64"
readonly media_collection_uri="content://media/external_primary/images/media"
readonly remote_media_path="/data/local/tmp/cotton-runtime.png"

sdkmanager_bin="${ANDROID_HOME:-}/cmdline-tools/latest/bin/sdkmanager"
avdmanager_bin="${ANDROID_HOME:-}/cmdline-tools/latest/bin/avdmanager"
emulator_bin="${ANDROID_HOME:-}/emulator/emulator"
adb_bin="${ANDROID_HOME:-}/platform-tools/adb"
emulator_log="${RUNNER_TEMP:-/tmp}/cotton-android-emulator.log"
avd_home="${RUNNER_TEMP:-/tmp}/cotton-android-avd"
emulator_pid=""
remote_media_uri=""

export ANDROID_AVD_HOME="$avd_home"

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

trap cleanup EXIT

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

wait_for_jobs() {
  local attempt
  for attempt in {1..60}; do
    if "$adb_bin" shell dumpsys jobscheduler | grep -Fq "$package_name"; then
      return
    fi

    sleep 1
  done

  printf 'WorkManager jobs were not registered for %s.\n' "$package_name" >&2
  "$adb_bin" shell dumpsys jobscheduler >&2
  exit 1
}

run_diagnostic() {
  local operation="$1"
  local request_id="$2"
  local attempt
  local output

  "$adb_bin" logcat -c
  "$adb_bin" shell am broadcast \
    --include-stopped-packages \
    -a "$diagnostics_action" \
    -p "$package_name" \
    --es operation "$operation" \
    --es request-id "$request_id" >/dev/null

  for attempt in {1..30}; do
    output="$("$adb_bin" logcat -d -s "$diagnostics_tag:I" '*:S' | tr -d '\r' | grep -F "$request_id:" || true)"
    if [[ -n "$output" ]]; then
      printf '%s\n' "$output" | tail -1
      return
    fi

    sleep 1
  done

  printf 'Android diagnostic %s did not complete.\n' "$operation" >&2
  "$adb_bin" logcat -d >&2
  exit 1
}

read_metric() {
  local output="$1"
  local metric="$2"
  local value

  value="$(printf '%s\n' "$output" | sed -n "s/.*${metric}=\([0-9][0-9]*\).*/\1/p")"
  if [[ -z "$value" ]]; then
    printf 'Metric %s is missing from: %s\n' "$metric" "$output" >&2
    exit 1
  fi

  printf '%s\n' "$value"
}

create_media_fixture() {
  local media_id
  local query_output

  "$adb_bin" push "$media_file" "$remote_media_path" >/dev/null
  "$adb_bin" shell content insert \
    --uri "$media_collection_uri" \
    --bind _display_name:s:cotton-runtime.png \
    --bind mime_type:s:image/png \
    --bind relative_path:s:Pictures/CottonRuntime/ \
    --bind is_pending:i:1
  query_output="$("$adb_bin" shell content query \
    --uri "$media_collection_uri" \
    --projection _id:_display_name | tr -d '\r')"
  media_id="$(printf '%s\n' "$query_output" \
    | sed -n '/_display_name=cotton-runtime\.png/s/.*_id=\([0-9][0-9]*\).*/\1/p' \
    | head -1)"
  if [[ -z "$media_id" ]]; then
    printf 'Could not locate Android MediaStore fixture: %s\n' "$query_output" >&2
    exit 1
  fi

  remote_media_uri="$media_collection_uri/$media_id"
  "$adb_bin" shell "content write --uri '$remote_media_uri' < '$remote_media_path'"
  "$adb_bin" shell content update \
    --uri "$remote_media_uri" \
    --bind is_pending:i:0 >/dev/null
}

update_media_fixture() {
  local modified_at

  modified_at="$(($(date +%s) + 60))"
  "$adb_bin" shell "printf x >> '$remote_media_path'"
  "$adb_bin" shell "content write --uri '$remote_media_uri' < '$remote_media_path'"
  "$adb_bin" shell content update \
    --uri "$remote_media_uri" \
    --bind date_modified:l:"$modified_at" >/dev/null
}

timeout 300 "$sdkmanager_bin" --install emulator platform-tools "$system_image" >/dev/null
mkdir -p "$ANDROID_AVD_HOME"
printf 'no\n' | "$avdmanager_bin" create avd \
  --force \
  --name "$avd_name" \
  --package "$system_image" \
  --device pixel_6 >/dev/null

if ! "$emulator_bin" -list-avds | grep -Fxq "$avd_name"; then
  printf 'Android virtual device %s was not created in %s.\n' "$avd_name" "$ANDROID_AVD_HOME" >&2
  exit 1
fi

"$emulator_bin" \
  -avd "$avd_name" \
  -no-window \
  -no-audio \
  -no-boot-anim \
  -no-snapshot \
  -wipe-data \
  -gpu swiftshader_indirect \
  -camera-back none \
  -camera-front none >"$emulator_log" 2>&1 &
emulator_pid="$!"

wait_for_device
wait_for_boot
"$adb_bin" install -r "$apk_path" >/dev/null
"$adb_bin" shell pm clear "$package_name" >/dev/null
create_media_fixture

denied_output="$(run_diagnostic scan-media denied)"
if [[ "$denied_output" != *":failed:"* ]]; then
  denied_files="$(read_metric "$denied_output" files)"
  if [[ "$denied_files" -ne 0 ]]; then
    printf 'MediaStore content was visible before permission was granted: %s\n' "$denied_output" >&2
    exit 1
  fi
fi

device_api="$("$adb_bin" shell getprop ro.build.version.sdk | tr -d '\r')"
if [[ "$device_api" -ge 33 ]]; then
  "$adb_bin" shell pm grant "$package_name" android.permission.READ_MEDIA_IMAGES
  "$adb_bin" shell pm grant "$package_name" android.permission.READ_MEDIA_VIDEO
else
  "$adb_bin" shell pm grant "$package_name" android.permission.READ_EXTERNAL_STORAGE
fi

first_output="$(run_diagnostic scan-media first)"
first_files="$(read_metric "$first_output" files)"
first_hashed="$(read_metric "$first_output" hashed)"
if [[ "$first_output" == *":failed:"* || "$first_files" -lt 1 || "$first_hashed" -lt 1 ]]; then
  printf 'Initial MediaStore scan did not hash the test media: %s\n' "$first_output" >&2
  exit 1
fi

second_output="$(run_diagnostic scan-media second)"
second_hashed="$(read_metric "$second_output" hashed)"
second_reused="$(read_metric "$second_output" reused)"
if [[ "$device_api" -ge 30 ]]; then
  if [[ "$second_output" == *":failed:"* || "$second_hashed" -ne 0 || "$second_reused" -lt 1 ]]; then
    printf 'Warm MediaStore scan did not reuse the revision index: %s\n' "$second_output" >&2
    exit 1
  fi
elif [[ "$second_output" == *":failed:"* || "$second_hashed" -lt 1 || "$second_reused" -ne 0 ]]; then
  printf 'Legacy MediaStore scan did not preserve full hashing: %s\n' "$second_output" >&2
  exit 1
fi

update_media_fixture

changed_output="$(run_diagnostic scan-media changed)"
changed_hashed="$(read_metric "$changed_output" hashed)"
if [[ "$changed_output" == *":failed:"* || "$changed_hashed" -lt 1 ]]; then
  printf 'Changed MediaStore content did not invalidate the revision index: %s\n' "$changed_output" >&2
  exit 1
fi

if [[ "$device_api" -ge 30 ]]; then
  changed_warm_output="$(run_diagnostic scan-media changed-warm)"
  changed_warm_hashed="$(read_metric "$changed_warm_output" hashed)"
  changed_warm_reused="$(read_metric "$changed_warm_output" reused)"
  if [[ "$changed_warm_output" == *":failed:"* \
    || "$changed_warm_hashed" -ne 0 \
    || "$changed_warm_reused" -lt 1 ]]; then
    printf 'Changed MediaStore revision was not persisted: %s\n' "$changed_warm_output" >&2
    exit 1
  fi
fi

if [[ "$device_api" -ge 33 ]]; then
  "$adb_bin" shell pm revoke "$package_name" android.permission.READ_MEDIA_IMAGES
  "$adb_bin" shell pm revoke "$package_name" android.permission.READ_MEDIA_VIDEO
else
  "$adb_bin" shell pm revoke "$package_name" android.permission.READ_EXTERNAL_STORAGE
fi

revoked_output="$(run_diagnostic scan-media revoked)"
if [[ "$revoked_output" != *":failed:"* ]]; then
  revoked_files="$(read_metric "$revoked_output" files)"
  if [[ "$revoked_files" -ne 0 ]]; then
    printf 'MediaStore content remained visible after permission revocation: %s\n' "$revoked_output" >&2
    exit 1
  fi
fi

schedule_output="$(run_diagnostic schedule-work schedule)"
if [[ "$schedule_output" == *":failed:"* ]]; then
  printf 'WorkManager scheduling failed: %s\n' "$schedule_output" >&2
  exit 1
fi

wait_for_jobs
"$adb_bin" shell am kill "$package_name"
wait_for_jobs
"$adb_bin" reboot
wait_for_device
wait_for_boot
wait_for_jobs

printf 'Android runtime smoke passed.\n'
