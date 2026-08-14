#!/usr/bin/env bash

set -euo pipefail

if (( $# != 1 )); then
  printf 'Usage: %s BRANCH_NAME\n' "$0" >&2
  exit 1
fi

branch_name="$1"

case "$branch_name" in
  develop)
    google_play_track="alpha"
    publish_github_release="false"
    upload_to_google_play="true"
    ;;
  main)
    google_play_track="production"
    publish_github_release="true"
    upload_to_google_play="false"
    ;;
  *)
    printf 'Unsupported Android release branch: %s\n' "$branch_name" >&2
    exit 1
    ;;
esac

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    printf 'google_play_track=%s\n' "$google_play_track"
    printf 'publish_github_release=%s\n' "$publish_github_release"
    printf 'upload_to_google_play=%s\n' "$upload_to_google_play"
  } >> "$GITHUB_OUTPUT"
fi

printf 'Google Play track: %s\n' "$google_play_track"
printf 'Publish GitHub release: %s\n' "$publish_github_release"
printf 'Upload to Google Play: %s\n' "$upload_to_google_play"
