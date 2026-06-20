#!/usr/bin/env bash

set -euo pipefail

readonly MAX_ANDROID_VERSION_CODE=2100000000

display_version="${1:-}"

if [[ -z "$display_version" ]]; then
  printf 'Usage: %s MAJOR.MINOR.PATCH\n' "$0" >&2
  exit 2
fi

if [[ ! "$display_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  printf 'Android display version must be a SemVer MAJOR.MINOR.PATCH value: %s\n' "$display_version" >&2
  exit 1
fi

require_numeric_env() {
  local name="$1"
  local value="${!name:-}"

  if [[ ! "$value" =~ ^[0-9]+$ ]]; then
    printf '%s must be numeric.\n' "$name" >&2
    exit 1
  fi

  printf '%s' "$value"
}

run_number="$(require_numeric_env GITHUB_RUN_NUMBER)"
run_attempt="$(require_numeric_env GITHUB_RUN_ATTEMPT)"
version_code_base="$(require_numeric_env ANDROID_VERSION_CODE_BASE)"
published_floor="$(require_numeric_env ANDROID_VERSION_CODE_PUBLISHED_FLOOR)"

if (( run_number <= 0 )); then
  printf 'GITHUB_RUN_NUMBER must be greater than zero.\n' >&2
  exit 1
fi

if (( run_attempt <= 0 )); then
  printf 'GITHUB_RUN_ATTEMPT must be greater than zero.\n' >&2
  exit 1
fi

android_version_code=$((version_code_base + run_number * 1000 + run_attempt))

if (( android_version_code <= published_floor )); then
  printf 'Computed Android versionCode must be greater than the latest published floor %s.\n' "$published_floor" >&2
  exit 1
fi

if (( android_version_code > MAX_ANDROID_VERSION_CODE )); then
  printf 'Computed Android versionCode must not exceed the Google Play maximum %s.\n' "$MAX_ANDROID_VERSION_CODE" >&2
  exit 1
fi

release_name="Cotton Mobile ${display_version} (${android_version_code})"

if [[ -n "${GITHUB_ENV:-}" ]]; then
  {
    printf 'APPLICATION_DISPLAY_VERSION=%s\n' "$display_version"
    printf 'ANDROID_VERSION_CODE=%s\n' "$android_version_code"
    printf 'GOOGLE_PLAY_RELEASE_NAME=%s\n' "$release_name"
  } >> "$GITHUB_ENV"
fi

printf 'GitVersion display version: %s\n' "$display_version"
printf 'Android versionCode: %s\n' "$android_version_code"
printf 'Google Play release name: %s\n' "$release_name"
