#!/usr/bin/env bash

set -euo pipefail

if (( $# != 5 )); then
  printf 'Usage: %s RELEASE_TAG DISPLAY_VERSION VERSION_CODE BRANCH_NAME COMMIT_SHA\n' "$0" >&2
  exit 1
fi

release_tag="$1"
display_version="$2"
version_code="$3"
branch_name="$4"
commit_sha="$5"

if [[ ! "$release_tag" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  printf 'Release tag must be a SemVer tag such as v1.0.0: %s\n' "$release_tag" >&2
  exit 1
fi

if [[ ! "$display_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  printf 'Display version must be MAJOR.MINOR.PATCH: %s\n' "$display_version" >&2
  exit 1
fi

if [[ ! "$version_code" =~ ^[0-9]+$ ]]; then
  printf 'Android versionCode must be numeric: %s\n' "$version_code" >&2
  exit 1
fi

previous_tag="$(
  git tag \
    --merged HEAD \
    --list 'v[0-9]*.[0-9]*.[0-9]*' \
    --sort=-v:refname \
    | grep -vx "$release_tag" \
    | head -1 \
    || true
)"

if [[ -n "$previous_tag" ]]; then
  log_range="$previous_tag..HEAD"
else
  log_range="HEAD"
fi

printf '# Cotton Mobile %s\n' "$display_version"
printf '\n'
printf '## Changes\n'

commit_count="$(git log --format='%h%x09%s' "$log_range" | wc -l | tr -d '[:space:]')"
if [[ "$commit_count" == "0" ]]; then
  printf '\n'
  printf '- No commit changes detected for this release.\n'
else
  printf '\n'
  git log --format='%h%x09%s' "$log_range" \
    | while IFS=$'\t' read -r short_sha subject; do
      printf -- '- %s (`%s`)\n' "$subject" "$short_sha"
    done
fi

printf '\n'
printf '## Build\n'
printf '\n'
printf -- '- Branch: `%s`\n' "$branch_name"
printf -- '- Commit: `%s`\n' "$commit_sha"
printf -- '- Android display version: `%s`\n' "$display_version"
printf -- '- Android versionCode: `%s`\n' "$version_code"

if [[ -n "$previous_tag" ]]; then
  printf -- '- Previous release tag: `%s`\n' "$previous_tag"
fi
