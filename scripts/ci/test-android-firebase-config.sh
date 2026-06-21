#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
checker="$repo_root/scripts/mobile/check-android-firebase-config.py"
workspace="$(mktemp -d)"

cleanup() {
  rm -rf "$workspace"
}

trap cleanup EXIT

fail() {
  printf '%s\n' "$1" >&2
  exit 1
}

assert_contains() {
  local haystack="$1"
  local needle="$2"

  if [[ "$haystack" != *"$needle"* ]]; then
    printf 'Expected output to contain: %s\n' "$needle" >&2
    printf 'Actual output:\n%s\n' "$haystack" >&2
    exit 1
  fi
}

write_config() {
  local path="$1"
  local content="$2"

  printf '%s\n' "$content" > "$path"
}

run_checker() {
  local config_file="$1"

  "$checker" \
    --configuration Debug \
    --package-id dev.cottoncloud.app.debug \
    --config-file "$config_file"
}

valid_config='{
  "project_info": {
    "project_number": "123456789012",
    "project_id": "cotton-mobile-test"
  },
  "client": [
    {
      "client_info": {
        "mobilesdk_app_id": "1:123456789012:android:abcdef123456",
        "android_client_info": {
          "package_name": "dev.cottoncloud.app.debug"
        }
      },
      "api_key": [
        {
          "current_key": "firebase-key-debug"
        }
      ]
    }
  ]
}'

valid_file="$workspace/google-services-valid.json"
write_config "$valid_file" "$valid_config"
output="$(run_checker "$valid_file")"
assert_contains "$output" "Firebase Android config is present"
assert_contains "$output" "Expected package: dev.cottoncloud.app.debug"
assert_contains "$output" "Selected client Firebase resources are present."
assert_contains "$output" "Selected client API keys: 1"

package_only_file="$workspace/google-services-package-only.json"
write_config "$package_only_file" '{
  "client": [
    {
      "client_info": {
        "android_client_info": {
          "package_name": "dev.cottoncloud.app.debug"
        }
      }
    }
  ]
}'

if output="$(run_checker "$package_only_file" 2>&1)"; then
  fail "Expected package-only config validation to fail."
fi
assert_contains "$output" "project_info/project_number"
assert_contains "$output" "client[dev.cottoncloud.app.debug]/client_info/mobilesdk_app_id"
assert_contains "$output" "no api_key/current_key"

mismatch_file="$workspace/google-services-mismatch.json"
write_config "$mismatch_file" "${valid_config/dev.cottoncloud.app.debug/dev.cottoncloud.app}"
if output="$(run_checker "$mismatch_file" 2>&1)"; then
  fail "Expected package mismatch validation to fail."
fi
assert_contains "$output" "does not contain package dev.cottoncloud.app.debug"
assert_contains "$output" "Config package names: dev.cottoncloud.app"

app_id_mismatch_file="$workspace/google-services-app-id-mismatch.json"
write_config "$app_id_mismatch_file" "${valid_config/1:123456789012:android:abcdef123456/1:999999999999:android:abcdef123456}"
if output="$(run_checker "$app_id_mismatch_file" 2>&1)"; then
  fail "Expected app id project-number mismatch validation to fail."
fi
assert_contains "$output" "mobilesdk_app_id does not match project_info/project_number"

missing_api_key_file="$workspace/google-services-missing-api-key.json"
write_config "$missing_api_key_file" "${valid_config/\"api_key\"/\"unused_api_key\"}"
if output="$(run_checker "$missing_api_key_file" 2>&1)"; then
  fail "Expected missing api key validation to fail."
fi
assert_contains "$output" "no api_key/current_key"

printf 'Android Firebase config validation checks passed.\n'
