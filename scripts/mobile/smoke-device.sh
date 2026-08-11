#!/usr/bin/env bash

cotton_adb() {
  adb -s "$serial" "$@"
}

cotton_capture_text() {
  local name="$1"
  shift

  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed while writing %s.\n' "$name" >&2
    return 1
  fi
}

cotton_capture_text_best_effort() {
  local name="$1"
  shift

  if ! "$@" > "$evidence_dir/$name" 2>&1; then
    printf 'Command failed: %q\n' "$1" >> "$evidence_dir/$name"
  fi
}

cotton_verify_expected_version_file() {
  local version_file="$1"

  if [[ -n "$expected_version_code" ]] \
    && ! grep -Fq "versionCode=$expected_version_code" "$version_file"; then
    printf 'Installed versionCode does not match expected value %s.\n' "$expected_version_code" >&2
    exit "$COTTON_EXIT_VERSION_MISMATCH"
  fi
  if [[ -n "$expected_version_name" ]] \
    && ! grep -Fq "versionName=$expected_version_name" "$version_file"; then
    printf 'Installed versionName does not match expected value %s.\n' "$expected_version_name" >&2
    exit "$COTTON_EXIT_VERSION_MISMATCH"
  fi
}

cotton_write_installed_package_version() {
  local package_dump_file="$1"
  local version_file="$2"
  local installed_version_code
  local installed_version_name

  installed_version_code="$(
    sed -n 's/.*versionCode=\([0-9][0-9]*\).*/\1/p' "$package_dump_file" | head -1
  )"
  installed_version_name="$(
    sed -n 's/.*versionName=\([^[:space:]]*\).*/\1/p' "$package_dump_file" | head -1
  )"

  {
    printf 'installed_version_code=%s\n' "$installed_version_code"
    printf 'installed_version_name=%s\n' "$installed_version_name"
    printf 'expected_version_code=%s\n' "$expected_version_code"
    printf 'expected_version_name=%s\n' "$expected_version_name"
  } > "$version_file"

  if [[ -n "$expected_version_code" && "$installed_version_code" != "$expected_version_code" ]]; then
    printf 'Installed %s versionCode is %s, expected %s. Evidence: %s\n' \
      "$package_id" "$installed_version_code" "$expected_version_code" "$evidence_dir" >&2
    exit "$COTTON_EXIT_INSTALLED_VERSION_MISMATCH"
  fi
  if [[ -n "$expected_version_name" && "$installed_version_name" != "$expected_version_name" ]]; then
    printf 'Installed %s versionName is %s, expected %s. Evidence: %s\n' \
      "$package_id" "$installed_version_name" "$expected_version_name" "$evidence_dir" >&2
    exit "$COTTON_EXIT_INSTALLED_VERSION_MISMATCH"
  fi
}

cotton_prepare_installed_package() {
  cotton_capture_text_best_effort "00-device.txt" cotton_adb shell getprop
  cotton_capture_text_best_effort "01-adb-devices.txt" adb devices

  if ! cotton_adb get-state > "$evidence_dir/02-device-state.txt" 2>&1; then
    printf 'ADB device is not available for serial %s. See %s/01-adb-devices.txt.\n' \
      "$serial" "$evidence_dir" >&2
    exit "$COTTON_EXIT_DEVICE_UNAVAILABLE"
  fi

  local device_state
  device_state="$(tr -d '\r\n' < "$evidence_dir/02-device-state.txt")"
  if [[ "$device_state" != "device" ]]; then
    printf 'ADB serial %s is in state %s, expected device.\n' "$serial" "$device_state" >&2
    exit "$COTTON_EXIT_DEVICE_UNAVAILABLE"
  fi

  if [[ "$install_debug" -eq 1 ]]; then
    if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
      printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' \
        "$COTTON_ANDROID_APK" >&2
      exit "$COTTON_EXIT_EVIDENCE"
    fi
    cotton_capture_text_best_effort "03-install-debug.txt" \
      cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK"
  fi

  if ! cotton_adb shell pm path "$package_id" > "$evidence_dir/04-package.txt" 2>&1; then
    printf 'Package %s is not installed on %s. Use --install-debug or install a Play-delivered build first.\n' \
      "$package_id" "$serial" >&2
    exit "$COTTON_EXIT_DEVICE_UNAVAILABLE"
  fi
  if ! cotton_adb shell dumpsys package "$package_id" > "$evidence_dir/05-package-dumpsys.txt" 2>&1; then
    printf 'Could not inspect installed package %s. See %s/05-package-dumpsys.txt.\n' \
      "$package_id" "$evidence_dir" >&2
    exit "$COTTON_EXIT_DEVICE_UNAVAILABLE"
  fi

  cotton_write_installed_package_version \
    "$evidence_dir/05-package-dumpsys.txt" \
    "$evidence_dir/05-package-version.txt"
}

cotton_write_remote_push_metadata() {
  local config_source="none"
  if [[ -n "$config_source_file" ]]; then
    config_source="file"
  elif [[ -n "$config_source_env_name" ]]; then
    config_source="env"
  fi

  printf 'timestamp_utc=%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  printf 'repo=%s\n' "$COTTON_REPO_ROOT"
  printf 'git_head=%s\n' "$(git -C "$COTTON_REPO_ROOT" rev-parse --short HEAD 2>/dev/null || printf unknown)"
  printf 'package=%s\n' "$package_id"
  printf 'serial=%s\n' "$serial"
  printf 'configuration=%s\n' "$configuration"
  printf 'config_file=%s\n' "$config_file"
  printf 'config_source=%s\n' "$config_source"
  printf 'config_source_env_name=%s\n' "$config_source_env_name"
  printf 'install_debug=%s\n' "$install_debug"
  printf 'launch_app=%s\n' "$launch_app"
  printf 'preflight_only=%s\n' "$preflight_only"
}

cotton_apply_notification_permission_state() {
  local permission_state="$1"
  local android_package_id="$2"
  local output_file="$3"

  case "$permission_state" in
    preserve)
      printf 'Preserving existing Android notification permission state.\n' > "$output_file"
      ;;
    fresh)
      {
        cotton_adb shell pm revoke "$android_package_id" android.permission.POST_NOTIFICATIONS || true
        cotton_adb shell pm clear-permission-flags \
          "$android_package_id" android.permission.POST_NOTIFICATIONS user-set || true
        cotton_adb shell pm clear-permission-flags \
          "$android_package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$output_file" 2>&1
      ;;
    allowed)
      {
        cotton_adb shell pm grant "$android_package_id" android.permission.POST_NOTIFICATIONS || true
        cotton_adb shell pm set-permission-flags \
          "$android_package_id" android.permission.POST_NOTIFICATIONS user-set || true
        cotton_adb shell pm clear-permission-flags \
          "$android_package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$output_file" 2>&1
      ;;
    denied)
      {
        cotton_adb shell pm revoke "$android_package_id" android.permission.POST_NOTIFICATIONS || true
        cotton_adb shell pm set-permission-flags \
          "$android_package_id" android.permission.POST_NOTIFICATIONS user-set || true
        cotton_adb shell pm clear-permission-flags \
          "$android_package_id" android.permission.POST_NOTIFICATIONS user-fixed || true
      } > "$output_file" 2>&1
      ;;
    *)
      printf 'Unsupported notification permission state: %s.\n' "$permission_state" >&2
      exit "$COTTON_EXIT_USAGE"
      ;;
  esac
}

cotton_capture_standard_package_evidence() {
  cotton_capture_text_best_effort "00-device.txt" cotton_adb shell getprop ro.product.model
  cotton_capture_text_best_effort "01-adb-devices.txt" adb devices
  cotton_capture_text_best_effort "02-window.txt" cotton_adb shell dumpsys window
  cotton_capture_text_best_effort "03-package-path.txt" cotton_adb shell pm path "$package_id"
  cotton_capture_text_best_effort "04-package.txt" cotton_adb shell dumpsys package "$package_id"
  cotton_capture_text_best_effort "05-package-version.txt" bash -lc \
    "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
  cotton_verify_expected_version_file "$evidence_dir/05-package-version.txt"

  if [[ "$install_debug" -eq 1 ]]; then
    if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
      printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' \
        "$COTTON_ANDROID_APK" >&2
      exit "$COTTON_EXIT_EVIDENCE"
    fi

    cotton_install_android_apk \
      "$serial" \
      "$package_id" \
      "$COTTON_ANDROID_APK" \
      > "$evidence_dir/06-install.txt"
  fi
}
