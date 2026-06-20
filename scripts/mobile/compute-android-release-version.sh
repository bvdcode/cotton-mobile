#!/usr/bin/env bash

set -euo pipefail

readonly MAX_ANDROID_VERSION_CODE=2100000000
readonly GITVERSION_CONFIG_PATH="${GITVERSION_CONFIG_PATH:-GitVersion.yml}"

display_version="${1:-}"

read_next_version() {
  local config_path="$1"
  local next_version

  if [[ ! -f "$config_path" ]]; then
    printf 'GitVersion config was not found: %s\n' "$config_path" >&2
    exit 1
  fi

  next_version="$(
    sed -nE \
      "s/^[[:space:]]*next-version:[[:space:]]*['\"]?([0-9]+\\.[0-9]+\\.[0-9]+)['\"]?[[:space:]]*$/\\1/p" \
      "$config_path" \
      | head -1
  )"

  if [[ -z "$next_version" ]]; then
    printf 'GitVersion config must declare next-version as MAJOR.MINOR.PATCH: %s\n' "$config_path" >&2
    exit 1
  fi

  printf '%s' "$next_version"
}

max_release_patch_for_tags() {
  local major="$1"
  local minor="$2"
  local minimum_patch="$3"
  local max_patch=""
  local tag
  shift 3

  while IFS= read -r tag; do
    if [[ "$tag" =~ ^v?([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
      local tag_major=$((10#${BASH_REMATCH[1]}))
      local tag_minor=$((10#${BASH_REMATCH[2]}))
      local tag_patch=$((10#${BASH_REMATCH[3]}))
      if (( tag_major == major && tag_minor == minor && tag_patch >= minimum_patch )); then
        if [[ -z "$max_patch" ]] || (( tag_patch > max_patch )); then
          max_patch="$tag_patch"
        fi
      fi
    fi
  done < <(git tag "$@")

  if [[ -n "$max_patch" ]]; then
    printf '%s' "$max_patch"
  fi
}

resolve_display_version() {
  local base_version="$1"

  if [[ ! "$base_version" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
    printf 'Android display version must be a SemVer MAJOR.MINOR.PATCH value: %s\n' "$base_version" >&2
    exit 1
  fi

  local major=$((10#${BASH_REMATCH[1]}))
  local minor=$((10#${BASH_REMATCH[2]}))
  local base_patch=$((10#${BASH_REMATCH[3]}))

  if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    printf '%s' "$base_version"
    return
  fi

  local head_patch
  head_patch="$(max_release_patch_for_tags "$major" "$minor" "$base_patch" --points-at HEAD)"
  if [[ -n "$head_patch" ]]; then
    printf '%s.%s.%s' "$major" "$minor" "$head_patch"
    return
  fi

  local latest_patch
  latest_patch="$(max_release_patch_for_tags "$major" "$minor" "$base_patch" --list)"
  if [[ -z "$latest_patch" ]]; then
    latest_patch="$base_patch"
  else
    latest_patch=$((latest_patch + 1))
  fi

  printf '%s.%s.%s' "$major" "$minor" "$latest_patch"
}

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

if [[ -z "$display_version" ]]; then
  display_version="$(resolve_display_version "$(read_next_version "$GITVERSION_CONFIG_PATH")")"
fi

if [[ ! "$display_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  printf 'Android display version must be a SemVer MAJOR.MINOR.PATCH value: %s\n' "$display_version" >&2
  exit 1
fi

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

release_name="Cotton Mobile ${display_version}"

if [[ -n "${GITHUB_ENV:-}" ]]; then
  {
    printf 'APPLICATION_DISPLAY_VERSION=%s\n' "$display_version"
    printf 'ANDROID_DISPLAY_VERSION_TAG=v%s\n' "$display_version"
    printf 'ANDROID_VERSION_CODE=%s\n' "$android_version_code"
    printf 'GOOGLE_PLAY_RELEASE_NAME=%s\n' "$release_name"
  } >> "$GITHUB_ENV"
fi

printf 'Android display version: %s\n' "$display_version"
printf 'Android display version tag: v%s\n' "$display_version"
printf 'Android versionCode: %s\n' "$android_version_code"
printf 'Google Play release name: %s\n' "$release_name"
