#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  printf 'Usage: %s <signed-release-apk> <android-api>\n' "$0" >&2
  exit 2
fi

readonly apk_path="$1"
readonly runtime_api="$2"
readonly package_name="dev.cottoncloud.app"
readonly authorization_complete_uri="cotton://authorization-complete"
readonly avd_name="cotton-release-runtime-$runtime_api"
readonly system_image="system-images;android-$runtime_api;google_apis;x86_64"

sdkmanager_bin="${ANDROID_HOME:-}/cmdline-tools/latest/bin/sdkmanager"
avdmanager_bin="${ANDROID_HOME:-}/cmdline-tools/latest/bin/avdmanager"
emulator_bin="${ANDROID_HOME:-}/emulator/emulator"
adb_bin="${ANDROID_HOME:-}/platform-tools/adb"
avd_home="${RUNNER_TEMP:-/tmp}/cotton-release-android-avd-$runtime_api"
emulator_log="${RUNNER_TEMP:-/tmp}/cotton-release-emulator-$runtime_api.log"
emulator_pid=""

export ANDROID_AVD_HOME="$avd_home"

cleanup() {
  if [[ -n "$emulator_pid" ]] && kill -0 "$emulator_pid" 2>/dev/null; then
    "$adb_bin" -s emulator-5554 emu kill >/dev/null 2>&1 || true

    local attempt
    for attempt in {1..10}; do
      if ! kill -0 "$emulator_pid" 2>/dev/null; then
        wait "$emulator_pid" 2>/dev/null || true
        emulator_pid=""
        break
      fi

      sleep 1
    done

    if [[ -n "$emulator_pid" ]]; then
      kill -KILL "$emulator_pid" 2>/dev/null || true
      wait "$emulator_pid" 2>/dev/null || true
    fi
  fi

  rm -rf "$avd_home"
}

wait_for_device() {
  local attempt
  for attempt in {1..120}; do
    if "$adb_bin" -s emulator-5554 get-state 2>/dev/null | grep -Fxq device; then
      return
    fi

    sleep 1
  done

  printf 'Android %s release emulator did not connect.\n' "$runtime_api" >&2
  tail -200 "$emulator_log" >&2 || true
  exit 1
}

wait_for_boot() {
  local attempt
  for attempt in {1..180}; do
    if [[ "$($adb_bin -s emulator-5554 shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" == "1" ]]; then
      return
    fi

    sleep 1
  done

  printf 'Android %s release emulator did not finish booting.\n' "$runtime_api" >&2
  exit 1
}

wait_for_process() {
  local attempt
  for attempt in {1..30}; do
    if [[ -n "$($adb_bin -s emulator-5554 shell pidof "$package_name" | tr -d '\r')" ]]; then
      return
    fi

    sleep 1
  done

  printf 'Signed Release application did not start on Android %s.\n' "$runtime_api" >&2
  "$adb_bin" -s emulator-5554 logcat -d >&2
  exit 1
}

trap cleanup EXIT
printf 'Testing signed Release runtime on Android API %s.\n' "$runtime_api"
timeout 300 "$sdkmanager_bin" --install emulator platform-tools "$system_image" >/dev/null
mkdir -p "$ANDROID_AVD_HOME"
printf 'no\n' | "$avdmanager_bin" create avd \
  --force \
  --name "$avd_name" \
  --package "$system_image" \
  --device pixel_6 >/dev/null

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
"$adb_bin" -s emulator-5554 install -r "$apk_path" >/dev/null
installed_api="$($adb_bin -s emulator-5554 shell getprop ro.build.version.sdk | tr -d '\r')"
if [[ "$installed_api" != "$runtime_api" ]]; then
  printf 'Expected Android API %s but emulator reports %s.\n' "$runtime_api" "$installed_api" >&2
  exit 1
fi

launcher_activity="$($adb_bin -s emulator-5554 shell cmd package resolve-activity --brief \
  -c android.intent.category.LAUNCHER "$package_name" | tr -d '\r' | tail -1)"
if [[ "$launcher_activity" != "$package_name/"* ]]; then
  printf 'Signed Release launcher activity could not be resolved: %s\n' "$launcher_activity" >&2
  exit 1
fi

deep_link_activity="$($adb_bin -s emulator-5554 shell cmd package resolve-activity --brief \
  -a android.intent.action.VIEW \
  -c android.intent.category.BROWSABLE \
  -d "$authorization_complete_uri" | tr -d '\r' | tail -1)"
if [[ "$deep_link_activity" != "$package_name/"* ]]; then
  printf 'Signed Release authorization callback is missing: %s\n' \
    "$deep_link_activity" >&2
  exit 1
fi

"$adb_bin" -s emulator-5554 logcat -c >/dev/null 2>&1 || true
"$adb_bin" -s emulator-5554 shell am start -W -n "$launcher_activity" >/dev/null
wait_for_process
sleep 3
if "$adb_bin" -s emulator-5554 logcat -d -s AndroidRuntime:E '*:S' | grep -Fq 'FATAL EXCEPTION'; then
  printf 'Signed Release crashed on Android %s.\n' "$runtime_api" >&2
  "$adb_bin" -s emulator-5554 logcat -d >&2
  exit 1
fi

"$adb_bin" -s emulator-5554 shell input keyevent 3
sleep 1
"$adb_bin" -s emulator-5554 shell am kill "$package_name"
sleep 1
"$adb_bin" -s emulator-5554 shell am start -W -n "$launcher_activity" >/dev/null
wait_for_process

printf 'Signed Release runtime passed on Android API %s.\n' "$runtime_api"
