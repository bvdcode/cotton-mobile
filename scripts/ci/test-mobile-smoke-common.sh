#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SCRIPT_DIR="$repo_root/scripts/mobile"

source "$SCRIPT_DIR/android-env.sh"
source "$SCRIPT_DIR/smoke-common.sh"

usage() {
  printf 'fixture usage\n'
}

value=""
flag=0
COTTON_VALUE_OPTIONS=("--value:value")
COTTON_FLAG_OPTIONS=("--flag:flag:1")
cotton_parse_arguments --value "fixture value" --flag

[[ "$value" == "fixture value" ]]
[[ "$flag" -eq 1 ]]
[[ "$(umask)" == "0077" ]]

if (cotton_parse_arguments --unsupported >/dev/null 2>&1); then
  printf 'Unknown arguments must fail.\n' >&2
  exit 1
fi

adb() {
  if [[ " $* " == *" install "* ]]; then
    return 23
  fi
  return 0
}

if cotton_install_android_apk "serial" "package" "fixture.apk"; then
  printf 'Failed adb install must not be masked by cleanup.\n' >&2
  exit 1
else
  install_status=$?
fi
[[ "$install_status" -eq 23 ]]

source "$SCRIPT_DIR/smoke-sync-settings-data.sh"
instance_uri="https://app.cottoncloud.dev"
account_scope_key="account-fixture"
run_id="fixture"
seed_dir="$(mktemp -d)"
trap 'rm -rf "$seed_dir"' EXIT
create_sync_seed "$seed_dir"
load_sync_seed "$seed_dir/seed-data.json"
[[ "$normalized_instance" == "https://app.cottoncloud.dev/" ]]
[[ "$(stat -c '%a' "$seed_dir/seed-data.json")" == "600" ]]

bash -n "$repo_root"/scripts/mobile/*.sh
bash "$repo_root/scripts/mobile/smoke-sync-settings.sh" --help >/dev/null
