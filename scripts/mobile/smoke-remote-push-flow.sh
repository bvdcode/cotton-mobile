require_signed_out_state() {
  local xml_file="$1"
  local state_name="$2"

  cotton_require_xml_text "$xml_file" "Cotton Cloud" "$state_name did not show the signed-out screen."
  cotton_require_xml_text "$xml_file" "Connect" "$state_name did not expose Connect."
  require_xml_without_text "$xml_file" "Server URL" "$state_name exposed the legacy server URL label."
}

build_token_smoke_args() {
  token_smoke_args=(
    --package "$package_id"
    --serial "$serial"
    --configuration "$configuration"
    --config-file "$config_file"
    --evidence-dir "$evidence_dir/10-token-registration"
    --wait-seconds "$token_wait_seconds"
  )

  if [[ -n "$config_source_file" ]]; then
    token_smoke_args+=(--config-source-file "$config_source_file")
  fi

  if [[ -n "$config_source_env_name" ]]; then
    token_smoke_args+=(--config-source-env "$config_source_env_name")
  fi

  if [[ "$install_debug" -eq 1 ]]; then
    token_smoke_args+=(--install-debug)
  fi

  if [[ "$capture_diagnostics_ui" -eq 1 ]]; then
    token_smoke_args+=(--diagnostics-ui)
  fi

  if [[ "$launch_app" -eq 0 ]]; then
    token_smoke_args+=(--no-launch)
  fi

  if [[ -n "$expected_version_code" ]]; then
    token_smoke_args+=(--expected-version-code "$expected_version_code")
  fi

  if [[ -n "$expected_version_name" ]]; then
    token_smoke_args+=(--expected-version-name "$expected_version_name")
  fi
}

run_token_preflight() {
  build_token_smoke_args
  "$SCRIPT_DIR/smoke-remote-push-token.sh" \
    "${token_smoke_args[@]}" \
    --preflight-only \
    > "$evidence_dir/09-token-preflight.txt" 2>&1
}

run_token_registration_smoke() {
  build_token_smoke_args
  "$SCRIPT_DIR/smoke-remote-push-token.sh" \
    "${token_smoke_args[@]}" \
    > "$evidence_dir/09-token-registration.txt" 2>&1
}

ensure_device_ready() {
  cotton_capture_text "01-adb-devices.txt" adb devices

  if ! cotton_adb get-state > "$evidence_dir/02-device-state.txt" 2>&1; then
    printf 'ADB device is not available for serial %s. Evidence: %s\n' "$serial" "$evidence_dir" >&2
    exit 69
  fi

  device_state="$(tr -d '\r\n' < "$evidence_dir/02-device-state.txt")"
  if [[ "$device_state" != "device" ]]; then
    printf 'ADB serial %s is in state %s, expected device. Evidence: %s\n' \
      "$serial" "$device_state" "$evidence_dir" >&2
    exit 69
  fi
}

capture_installed_package() {
  if ! cotton_adb shell pm path "$package_id" > "$evidence_dir/03-package.txt" 2>&1; then
    printf 'Package %s is not installed on %s. Use --install-debug or install a Play build first. Evidence: %s\n' \
      "$package_id" "$serial" "$evidence_dir" >&2
    exit 69
  fi

  cotton_capture_text "03-package-dumpsys.txt" cotton_adb shell dumpsys package "$package_id"
  cotton_write_installed_package_version "$evidence_dir/03-package-dumpsys.txt" "$evidence_dir/04-package-version.txt"
}

capture_remote_push_lifecycle_log() {
  cotton_adb logcat -d -v threadtime |
    awk '/Cotton mobile remote push|remote push token|Firebase Cloud Messaging|remote logout|Logout failed|FATAL EXCEPTION/' \
      > "$evidence_dir/90-remote-push-lifecycle-log.txt"
}

write_result() {
  local logout_revocation_status="no_signal"
  local logout_refresh_cancel_status="no_signal"
  local logout_refresh_cancel_log_count
  local fatal_count
  local sign_in_xml_count="0"

  if grep -q 'Revoked .* Cotton mobile remote push token(s) for the current session.' \
    "$evidence_dir/90-remote-push-lifecycle-log.txt"; then
    logout_revocation_status="revoked"
  elif grep -q 'Failed to revoke Cotton mobile remote push tokens for the current session.' \
    "$evidence_dir/90-remote-push-lifecycle-log.txt"; then
    logout_revocation_status="failed"
  fi

  if grep -q 'Cancelled .*remote push token refresh' \
    "$evidence_dir/90-remote-push-lifecycle-log.txt"; then
    logout_refresh_cancel_status="cancelled"
  elif grep -q 'Failed to cancel Cotton mobile remote push token refresh' \
    "$evidence_dir/90-remote-push-lifecycle-log.txt"; then
    logout_refresh_cancel_status="failed"
  fi

  logout_refresh_cancel_log_count="$(
    grep -c 'Cancelled .*remote push token refresh' "$evidence_dir/90-remote-push-lifecycle-log.txt" || true
  )"
  fatal_count="$(grep -c 'FATAL EXCEPTION' "$evidence_dir/90-remote-push-lifecycle-log.txt" || true)"
  if [[ -f "$evidence_dir/60-after-reinstall.xml" ]]; then
    sign_in_xml_count="$(
      grep -Eic 'Sign in|Signed out|Connect' "$evidence_dir/60-after-reinstall.xml" || true
    )"
  fi

  {
    printf 'token_registration_status=%s\n' "$(
      sed -n 's/^registration_status=//p' "$evidence_dir/10-token-registration/91-result.txt" 2>/dev/null | head -1
    )"
    printf 'logout_revocation_status=%s\n' "$logout_revocation_status"
    printf 'logout_refresh_cancel_status=%s\n' "$logout_refresh_cancel_status"
    printf 'logout_refresh_cancel_log_count=%s\n' "$logout_refresh_cancel_log_count"
    printf 'fatal_log_count=%s\n' "$fatal_count"
    printf 'reinstall_mode=%s\n' "$reinstall_mode"
    printf 'reinstall_signed_out_xml_match_count=%s\n' "$sign_in_xml_count"
  } > "$evidence_dir/91-result.txt"

  if [[ "$fatal_count" != "0" ]]; then
    printf 'Fatal runtime log entries were captured. Evidence: %s\n' "$evidence_dir" >&2
    exit 65
  fi

  if [[ "$require_logout_revoke" -eq 1 && "$logout_revocation_status" != "revoked" ]]; then
    printf 'Logout remote-push token revocation was not proven: %s. Evidence: %s\n' \
      "$logout_revocation_status" "$evidence_dir" >&2
    exit 65
  fi

  if [[ "$require_logout_refresh_cancel" -eq 1 && "$logout_refresh_cancel_status" != "cancelled" ]]; then
    printf 'Logout remote-push token refresh cancellation was not proven: %s. Evidence: %s\n' \
      "$logout_refresh_cancel_status" "$evidence_dir" >&2
    exit 65
  fi
}

run_reinstall_check() {
  case "$reinstall_mode" in
    none)
      return
      ;;
    update)
      if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
        printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first or use --reinstall-mode none.\n' \
          "$COTTON_ANDROID_APK" >&2
        exit 66
      fi

      cotton_capture_text "60-reinstall.txt" cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK"
      ;;
    fresh)
      if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
        printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first or use --reinstall-mode none.\n' \
          "$COTTON_ANDROID_APK" >&2
        exit 66
      fi

      cotton_capture_text "60-uninstall.txt" cotton_adb uninstall "$package_id"
      cotton_capture_text "60-reinstall.txt" cotton_adb install --no-incremental "$COTTON_ANDROID_APK"
      ;;
  esac

  cotton_capture_text "60-launch.txt" cotton_adb shell monkey -p "$package_id" 1
  sleep 3
  capture_window "60-after-reinstall"
  require_signed_out_state "$evidence_dir/60-after-reinstall.xml" "Post-reinstall"
}
