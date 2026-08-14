#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
policy_script="$repo_root/scripts/mobile/resolve-android-release-policy.sh"
workspace="$(mktemp -d)"

cleanup() {
  rm -rf "$workspace"
}

trap cleanup EXIT

assert_contains() {
  local haystack="$1"
  local needle="$2"

  if [[ "$haystack" != *"$needle"* ]]; then
    printf 'Expected output to contain: %s\n' "$needle" >&2
    printf 'Actual output:\n%s\n' "$haystack" >&2
    exit 1
  fi
}

assert_policy() {
  local branch_name="$1"
  local expected_track="$2"
  local expected_publish="$3"
  local expected_upload="$4"
  local output_file="$workspace/$branch_name-output"
  local output

  output="$(GITHUB_OUTPUT="$output_file" "$policy_script" "$branch_name")"

  assert_contains "$output" "Google Play track: $expected_track"
  assert_contains "$output" "Publish GitHub release: $expected_publish"
  assert_contains "$output" "Upload to Google Play: $expected_upload"

  output="$(<"$output_file")"
  assert_contains "$output" "google_play_track=$expected_track"
  assert_contains "$output" "publish_github_release=$expected_publish"
  assert_contains "$output" "upload_to_google_play=$expected_upload"
}

assert_policy develop alpha false true
assert_policy main production true false

if "$policy_script" feature/unplanned >"$workspace/unsupported-output" 2>&1; then
  printf 'Unsupported branches must fail release policy resolution.\n' >&2
  exit 1
fi

assert_contains "$(<"$workspace/unsupported-output")" "Unsupported Android release branch: feature/unplanned"

printf 'Android release policy checks passed.\n'
