#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"
# shellcheck source=smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

COTTON_NODE_MATCH_MODE_DEFAULT=exact

package_id="$COTTON_ANDROID_PACKAGE_ID"
serial="$COTTON_ADB_SERIAL"
evidence_root="${COTTON_MOBILE_EVIDENCE_ROOT:-${TMPDIR:-/tmp}/cotton-mobile-evidence}"
evidence_dir=""
install_debug=0
launch_app=1
preflight_only=0
cancel_on_timeout=1
wait_seconds=90
expected_version_code=""
expected_version_name=""
target_file=""
target_folder=""
create_disposable_folder=0
restore_from_trash_page=0
delete_forever_from_trash_page=0
restore_bulk_from_trash_page=0
delete_bulk_forever_from_trash_page=0
bulk_second_file=""
bulk_second_folder=""
create_bulk_second_disposable_folder=0
bulk_selection=0
bulk_second_kind=""
bulk_second_name=""
target_kind=""
target_name=""

# shellcheck source=smoke-file-trash-options.sh
source "$SCRIPT_DIR/smoke-file-trash-options.sh"

# shellcheck source=smoke-file-trash-support.sh
source "$SCRIPT_DIR/smoke-file-trash-support.sh"
# shellcheck source=smoke-file-trash-navigation.sh
source "$SCRIPT_DIR/smoke-file-trash-navigation.sh"
# shellcheck source=smoke-file-trash-bulk.sh
source "$SCRIPT_DIR/smoke-file-trash-bulk.sh"
# shellcheck source=smoke-file-trash-bulk-actions.sh
source "$SCRIPT_DIR/smoke-file-trash-bulk-actions.sh"
# shellcheck source=smoke-file-trash-single.sh
source "$SCRIPT_DIR/smoke-file-trash-single.sh"
trap capture_failure_evidence EXIT

write_metadata
write_checklist

cotton_capture_text_best_effort "00-device.txt" cotton_adb shell getprop ro.product.model
cotton_capture_text_best_effort "01-adb-devices.txt" adb devices
cotton_capture_text_best_effort "02-window.txt" cotton_adb shell dumpsys window

if [[ "$install_debug" -eq 1 ]]; then
  if [[ ! -f "$COTTON_ANDROID_APK" ]]; then
    printf 'APK not found: %s\nRun scripts/mobile/build-android-debug.sh first.\n' "$COTTON_ANDROID_APK" >&2
    exit 66
  fi

  cotton_install_android_apk "$serial" "$package_id" "$COTTON_ANDROID_APK" > "$evidence_dir/03-install.txt"
fi

cotton_capture_text_best_effort "04-package.txt" cotton_adb shell dumpsys package "$package_id"
cotton_capture_text_best_effort "05-package-version.txt" bash -lc \
  "adb -s '$serial' shell dumpsys package '$package_id' | grep -E 'versionCode|versionName|firstInstallTime|lastUpdateTime'"
cotton_verify_expected_version_file "$evidence_dir/05-package-version.txt"

if [[ "$launch_app" -eq 1 ]]; then
  cotton_adb logcat -c || true
  cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/06-launch.txt"
  sleep 3
fi

trash_wait_for_files_root

if [[ "$preflight_only" -eq 1 ]]; then
  cotton_capture_text_best_effort "99-logcat.txt" cotton_adb logcat -d -v time
  printf 'Files trash/restore preflight evidence captured in %s\n' "$evidence_dir"
  exit 0
fi

if [[ "$create_disposable_folder" -eq 1 ]]; then
  create_disposable_target_folder
fi

if [[ "$create_bulk_second_disposable_folder" -eq 1 ]]; then
  create_bulk_second_disposable_target_folder
fi

if [[ "$bulk_selection" -eq 1 ]]; then
  ensure_bulk_targets_visible
  select_bulk_targets
  open_bulk_selection_actions
  confirm_bulk_move_to_trash
  wait_for_bulk_trash_completion
  open_bulk_trash_page
  if [[ "$restore_bulk_from_trash_page" -eq 1 ]]; then
    restore_bulk_from_trash_page
  elif [[ "$delete_bulk_forever_from_trash_page" -eq 1 ]]; then
    delete_bulk_forever_from_trash_page
  fi
  cotton_capture_text_best_effort "99-logcat.txt" cotton_adb logcat -d -v time
  if [[ "$restore_bulk_from_trash_page" -eq 1 ]]; then
    printf 'Files bulk selection trash and Trash restore evidence captured in %s\n' "$evidence_dir"
  elif [[ "$delete_bulk_forever_from_trash_page" -eq 1 ]]; then
    printf 'Files bulk selection trash and Trash delete-forever evidence captured in %s\n' "$evidence_dir"
  else
    printf 'Files bulk selection trash evidence captured in %s\n' "$evidence_dir"
  fi
  exit 0
fi

ensure_target_visible
open_target_actions
confirm_move_to_trash
wait_for_trash_follow_up
if [[ "$delete_forever_from_trash_page" -eq 1 ]]; then
  open_trash_page
  confirm_trash_page_delete_forever
  wait_for_delete_forever_completion
elif [[ "$restore_from_trash_page" -eq 1 ]]; then
  open_trash_page
  confirm_trash_page_restore
  wait_for_restore_completion
else
  confirm_restore
  wait_for_restore_completion
fi
cotton_capture_text_best_effort "99-logcat.txt" cotton_adb logcat -d -v time

if [[ "$delete_forever_from_trash_page" -eq 1 ]]; then
  printf 'Files %s trash/delete-forever evidence captured in %s\n' "$target_kind" "$evidence_dir"
else
  printf 'Files %s trash/restore evidence captured in %s\n' "$target_kind" "$evidence_dir"
fi
