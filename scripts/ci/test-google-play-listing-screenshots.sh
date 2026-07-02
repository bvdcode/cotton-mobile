#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
screenshots_dir="$repo_root/store/google-play/default-listing/graphics/phone-screenshots"

expected_screenshots=(
  "01-sign-in.png"
  "02-sign-in-light.png"
)

if [[ ! -d "$screenshots_dir" ]]; then
  printf 'Google Play phone screenshot directory was not found: %s\n' "$screenshots_dir" >&2
  exit 1
fi

mapfile -t actual_screenshots < <(
  find "$screenshots_dir" -maxdepth 1 -type f -name '*.png' -printf '%f\n' | sort
)

expected_output="$(printf '%s\n' "${expected_screenshots[@]}")"
actual_output="$(printf '%s\n' "${actual_screenshots[@]}")"

if [[ "$actual_output" != "$expected_output" ]]; then
  printf 'Google Play phone screenshots must match the current verified screenshot set.\n' >&2
  printf 'Expected:\n%s\n' "$expected_output" >&2
  printf 'Actual:\n%s\n' "$actual_output" >&2
  exit 1
fi

printf 'Google Play listing screenshot checks passed.\n'
