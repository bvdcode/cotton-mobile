#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SCRIPT_DIR="$repo_root/scripts/mobile"

# shellcheck source=../mobile/smoke-common.sh
source "$SCRIPT_DIR/smoke-common.sh"

usage() {
  printf 'fixture usage\n'
}

value=""
flag=0
mode="default"
COTTON_VALUE_OPTIONS=("--value:value:mode:explicit")
COTTON_FLAG_OPTIONS=("--flag:flag:1")
cotton_parse_arguments --value "fixture value" --flag

[[ "$value" == "fixture value" ]]
[[ "$flag" -eq 1 ]]
[[ "$mode" == "explicit" ]]

if (cotton_parse_arguments --unsupported >/dev/null 2>&1); then
  printf 'Unknown arguments must fail.\n' >&2
  exit 1
fi

bash -n "$repo_root"/scripts/mobile/*.sh

for smoke_script in "$repo_root"/scripts/mobile/smoke-*.sh; do
  if ! grep -Fq "set -euo pipefail" "$smoke_script"; then
    continue
  fi

  bash "$smoke_script" --help >/dev/null
done
