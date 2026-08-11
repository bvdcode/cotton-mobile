#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
configuration="$COTTON_ANDROID_CONFIGURATION"
config_file="$COTTON_REPO_ROOT/src/Cotton.Mobile/Platforms/Android/google-services.json"
config_source_file=""
config_source_env_name=""
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
preflight_only=0
launch_app=1
require_logout_revoke=1
require_logout_refresh_cancel=1
capture_diagnostics_ui=0
reinstall_mode="update"
token_wait_seconds=10
expected_version_code=""
expected_version_name=""

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs an interactive Android remote-push lifecycle smoke for a Firebase-configured
Cotton build. The script never prints or stores the FCM token.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --configuration NAME      Android build configuration. Defaults to COTTON_ANDROID_CONFIGURATION.
  --config-file PATH        Firebase google-services.json path.
  --config-source-file PATH Restore google-services.json from this local source before preflight.
  --config-source-env NAME  Restore google-services.json from this environment variable before preflight.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory under $evidence_root.
  --install-debug           Install the current debug APK with -r before token proof.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --token-wait-seconds N    Seconds to wait for token registration after app launch.
  --diagnostics-ui          Validate the Diagnostics Remote push section during token proof.
  --reinstall-mode MODE     Post-logout reinstall check: none, update, or fresh. Defaults to update.
  --allow-missing-revoke    Capture logout evidence without failing on a missing revoke log.
  --allow-missing-refresh-cancel
                            Capture logout evidence without failing on a missing refresh-cancel log.
  --preflight-only          Validate package/config/device state and exit before manual prompts.
  --no-launch               Do not launch the app in the token registration step.
  --help, -h                Show this help.

Preconditions for a passing lifecycle smoke:
  - google-services.json contains a client for the tested package id.
  - Google Play services are available on the device.
  - The app has a restorable signed-in session.
  - The backend profile exposes push token registration, revocation, and preferences.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--configuration:configuration"
  "--config-file:config_file"
  "--config-source-file:config_source_file"
  "--config-source-env:config_source_env_name"
  "--evidence-dir:evidence_dir"
  "--expected-version-code:expected_version_code"
  "--expected-version-name:expected_version_name"
  "--token-wait-seconds:token_wait_seconds"
  "--reinstall-mode:reinstall_mode"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--allow-missing-revoke:require_logout_revoke:0"
  "--allow-missing-refresh-cancel:require_logout_refresh_cancel:0"
  "--diagnostics-ui:capture_diagnostics_ui:1"
  "--preflight-only:preflight_only:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

case "$reinstall_mode" in
  none|update|fresh)
    ;;
  *)
    printf 'Invalid --reinstall-mode: %s. Expected none, update, or fresh.\n' "$reinstall_mode" >&2
    exit 64
    ;;
esac

if [[ ! "$token_wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Invalid --token-wait-seconds: %s\n' "$token_wait_seconds" >&2
  exit 64
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if [[ "$preflight_only" -eq 0 && ! -t 0 ]]; then
  printf 'This smoke requires an interactive terminal because it waits for manual app navigation.\n' >&2
  printf 'Use --preflight-only for non-interactive package/config evidence.\n' >&2
  exit 65
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  evidence_dir="$evidence_root/$timestamp-remote-push-lifecycle"
fi

mkdir -p "$evidence_dir"

# shellcheck source=smoke-remote-push-presentation.sh
source "$SCRIPT_DIR/smoke-remote-push-presentation.sh"
# shellcheck source=smoke-remote-push-flow.sh
source "$SCRIPT_DIR/smoke-remote-push-flow.sh"

write_metadata
write_checklist
ensure_device_ready

if [[ "$install_debug" -eq 1 ]]; then
  "$SCRIPT_DIR/install-android-debug.sh" --no-launch > "$evidence_dir/03-install-debug.txt" 2>&1
fi

capture_installed_package

set +e
run_token_preflight
token_preflight_status=$?
set -e

if [[ "$token_preflight_status" -ne 0 ]]; then
  cat "$evidence_dir/09-token-preflight.txt" >&2
  printf 'Remote-push lifecycle stopped at token preflight. Evidence: %s\n' "$evidence_dir" >&2
  exit "$token_preflight_status"
fi

if [[ "$preflight_only" -eq 1 ]]; then
  printf '\nRemote-push lifecycle preflight evidence: %s\n' "$evidence_dir"
  exit 0
fi

run_token_registration_smoke

prompt_capture \
  "Open Account -> Notifications. If Android notifications are not allowed, tap Allow and grant the permission." \
  "20-notification-opt-in"

prompt_capture \
  "Turn supported server-push categories off and leave the Notification Settings page visible." \
  "30-server-push-opt-out"
require_server_push_switches "$evidence_dir/30-server-push-opt-out.xml" false "Server-push opt-out"

prompt_capture \
  "Turn supported server-push categories back on and leave the Notification Settings page visible." \
  "40-server-push-opt-in"
require_server_push_switches "$evidence_dir/40-server-push-opt-in.xml" true "Server-push opt-in"

cotton_adb logcat -c >/dev/null 2>&1 || true

prompt_capture \
  "Log out from the account menu and wait for the signed-out screen." \
  "50-after-logout"
require_signed_out_state "$evidence_dir/50-after-logout.xml" "After logout"

capture_remote_push_lifecycle_log
run_reinstall_check
write_result

printf '\nRemote-push lifecycle smoke evidence: %s\n' "$evidence_dir"
printf 'Review checklist.md and 91-result.txt before marking lifecycle proof complete.\n'
