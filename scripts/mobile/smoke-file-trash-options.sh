usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Runs a Files move-to-trash and restore smoke for an existing test file or folder.

Options:
  --package ID              Android package id to test. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --serial SERIAL           ADB serial to use. Defaults to COTTON_ADB_SERIAL.
  --evidence-dir DIR        Evidence directory. Defaults to a timestamped directory.
  --install-debug           Install the current debug APK with -r before launch.
  --expected-version-code N Require the installed package to have this Android versionCode.
  --expected-version-name V Require the installed package to have this versionName.
  --target-file NAME        Existing Cotton Files file row to move to trash and restore.
  --target-folder NAME      Existing Cotton Files folder row to move to trash and restore.
  --create-disposable-folder NAME
                            Create a root-visible disposable folder first, then trash/restore it.
  --bulk-second-file NAME   Move the target plus this visible file row to trash as one selection.
  --bulk-second-folder NAME Move the target plus this visible folder row to trash as one selection.
  --create-bulk-second-disposable-folder NAME
                            Create a second root-visible disposable folder for bulk selection.
  --restore-from-trash-page Open Account -> Trash after moving the item, then restore it there.
  --delete-forever-from-trash-page
                            Open Account -> Trash after moving a disposable folder, then delete forever.
  --restore-bulk-from-trash-page
                            After bulk move-to-trash proof, select both Trash rows and restore them together.
  --delete-bulk-forever-from-trash-page
                            After disposable bulk move-to-trash proof, select both Trash rows and delete them forever.
  --wait-seconds N          Seconds to wait for each server mutation. Defaults to $wait_seconds.
  --no-cancel-on-timeout    Leave the app in its current state when a mutation times out.
  --preflight-only          Capture device/package/root state and exit.
  --no-launch               Do not launch the app before capture.
  --help, -h                Show this help.

The app must already have a signed-in session. Use a disposable test file/folder
or a known smoke fixture because this script performs a real server
trash/restore cycle when the backend responds successfully. Bulk selection
proof moves both selected items to Trash and then verifies they are recoverable
from the Trash page. Add --restore-bulk-from-trash-page to clean up both
items through Trash page selection restore, or use disposable targets with
--delete-bulk-forever-from-trash-page to prove permanent bulk deletion.
EOF
}

COTTON_VALUE_OPTIONS=(
  "--package:package_id"
  "--serial:serial"
  "--evidence-dir:evidence_dir"
  "--expected-version-code:expected_version_code"
  "--expected-version-name:expected_version_name"
  "--target-file:target_file"
  "--target-folder:target_folder"
  "--create-disposable-folder:target_folder:create_disposable_folder:1"
  "--bulk-second-file:bulk_second_file"
  "--bulk-second-folder:bulk_second_folder"
  "--create-bulk-second-disposable-folder:bulk_second_folder:create_bulk_second_disposable_folder:1"
  "--wait-seconds:wait_seconds"
)
COTTON_FLAG_OPTIONS=(
  "--install-debug:install_debug:1"
  "--restore-from-trash-page:restore_from_trash_page:1"
  "--delete-forever-from-trash-page:delete_forever_from_trash_page:1"
  "--restore-bulk-from-trash-page:restore_bulk_from_trash_page:1"
  "--delete-bulk-forever-from-trash-page:delete_bulk_forever_from_trash_page:1"
  "--no-cancel-on-timeout:cancel_on_timeout:0"
  "--preflight-only:preflight_only:1"
  "--no-launch:launch_app:0"
)
cotton_parse_arguments "$@"

if [[ ! "$wait_seconds" =~ ^[0-9]+$ ]]; then
  printf 'Wait seconds must be a positive integer.\n' >&2
  exit 64
fi

if [[ "$wait_seconds" -le 0 ]]; then
  printf 'Wait seconds must be greater than zero.\n' >&2
  exit 64
fi

if [[ -n "${target_file//[[:space:]]/}" && -n "${target_folder//[[:space:]]/}" ]]; then
  printf 'Choose either --target-file or --target-folder, not both.\n' >&2
  exit 64
fi

if [[ "$create_disposable_folder" -eq 1 && "$preflight_only" -eq 1 ]]; then
  printf '%s\n' '--create-disposable-folder cannot be combined with --preflight-only.' >&2
  exit 64
fi

if [[ "$restore_from_trash_page" -eq 1 && "$delete_forever_from_trash_page" -eq 1 ]]; then
  printf '%s\n' '--restore-from-trash-page and --delete-forever-from-trash-page cannot be combined.' >&2
  exit 64
fi

if [[ "$preflight_only" -eq 1 \
  && ( "$restore_from_trash_page" -eq 1 \
    || "$delete_forever_from_trash_page" -eq 1 \
    || "$restore_bulk_from_trash_page" -eq 1 \
    || "$delete_bulk_forever_from_trash_page" -eq 1 ) ]]; then
  printf '%s\n' 'Trash page action options cannot be combined with --preflight-only.' >&2
  exit 64
fi

if [[ "$restore_bulk_from_trash_page" -eq 1 && "$delete_bulk_forever_from_trash_page" -eq 1 ]]; then
  printf '%s\n' '--restore-bulk-from-trash-page and --delete-bulk-forever-from-trash-page cannot be combined.' >&2
  exit 64
fi

if [[ -n "$bulk_second_file" ]]; then
  bulk_selection=1
  bulk_second_kind="file"
  bulk_second_name="$bulk_second_file"
elif [[ -n "$bulk_second_folder" ]]; then
  bulk_selection=1
  bulk_second_kind="folder"
  bulk_second_name="$bulk_second_folder"
fi

if [[ "$bulk_selection" -eq 1 && "$preflight_only" -eq 1 ]]; then
  printf '%s\n' 'Bulk selection options cannot be combined with --preflight-only.' >&2
  exit 64
fi

if [[ "$bulk_selection" -eq 1 \
  && ( "$restore_from_trash_page" -eq 1 || "$delete_forever_from_trash_page" -eq 1 ) ]]; then
  printf '%s\n' 'Bulk selection smoke verifies Trash page recoverability and cannot be combined with single-item Trash page actions.' >&2
  exit 64
fi

if [[ "$restore_bulk_from_trash_page" -eq 1 && "$bulk_selection" -ne 1 ]]; then
  printf '%s\n' '--restore-bulk-from-trash-page requires a bulk selection target.' >&2
  exit 64
fi

if [[ "$delete_bulk_forever_from_trash_page" -eq 1 && "$bulk_selection" -ne 1 ]]; then
  printf '%s\n' '--delete-bulk-forever-from-trash-page requires a bulk selection target.' >&2
  exit 64
fi

if [[ "$create_bulk_second_disposable_folder" -eq 1 && "$create_disposable_folder" -ne 1 ]]; then
  printf '%s\n' '--create-bulk-second-disposable-folder requires --create-disposable-folder.' >&2
  exit 64
fi

if [[ "$delete_bulk_forever_from_trash_page" -eq 1 \
  && ( "$create_disposable_folder" -ne 1 || "$create_bulk_second_disposable_folder" -ne 1 ) ]]; then
  printf '%s\n' '--delete-bulk-forever-from-trash-page requires both disposable folder creation options.' >&2
  exit 64
fi

if [[ "$delete_forever_from_trash_page" -eq 1 && "$create_disposable_folder" -ne 1 ]]; then
  printf '%s\n' '--delete-forever-from-trash-page requires --create-disposable-folder.' >&2
  exit 64
fi

if [[ -n "${target_file//[[:space:]]/}" ]]; then
  target_kind="file"
  target_name="$target_file"
elif [[ -n "${target_folder//[[:space:]]/}" ]]; then
  target_kind="folder"
  target_name="$target_folder"
fi

if [[ -z "$target_kind" && "$preflight_only" -eq 0 ]]; then
  printf 'Target file or folder is required unless --preflight-only is used.\n' >&2
  exit 64
fi

if [[ -n "$target_kind" ]]; then
  if [[ -z "${target_name//[[:space:]]/}" || "$target_name" == *"/"* ]]; then
    printf 'Target %s name must not be blank and must not contain a slash.\n' "$target_kind" >&2
    exit 64
  fi
fi

if [[ "$bulk_selection" -eq 1 ]]; then
  if [[ -z "${bulk_second_name//[[:space:]]/}" || "$bulk_second_name" == *"/"* ]]; then
    printf 'Bulk second %s name must not be blank and must not contain a slash.\n' "$bulk_second_kind" >&2
    exit 64
  fi

  if [[ "$bulk_second_name" == "$target_name" ]]; then
    printf 'Bulk second target name must be different from the primary target name.\n' >&2
    exit 64
  fi
fi

if ! command -v adb >/dev/null 2>&1; then
  printf 'adb was not found. Install Android SDK Platform-Tools or set ANDROID_HOME/COTTON_ANDROID_SDK_ROOT.\n' >&2
  exit 127
fi

if ! command -v python3 >/dev/null 2>&1; then
  printf 'python3 was not found.\n' >&2
  exit 127
fi

if [[ -z "$evidence_dir" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  if [[ -n "$target_kind" ]]; then
    if [[ "$bulk_selection" -eq 1 ]]; then
      evidence_dir="$evidence_root/$timestamp-selection-trash"
    elif [[ "$delete_forever_from_trash_page" -eq 1 ]]; then
      evidence_dir="$evidence_root/$timestamp-$target_kind-trash-delete-forever"
    elif [[ "$restore_from_trash_page" -eq 1 ]]; then
      evidence_dir="$evidence_root/$timestamp-$target_kind-trash-page-restore"
    else
      evidence_dir="$evidence_root/$timestamp-$target_kind-trash-restore"
    fi
  else
    evidence_dir="$evidence_root/$timestamp-trash-restore-preflight"
  fi
fi

mkdir -p "$evidence_dir"
