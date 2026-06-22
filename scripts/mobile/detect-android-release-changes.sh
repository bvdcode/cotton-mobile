#!/usr/bin/env bash

set -euo pipefail

if (( $# != 2 )); then
  printf 'Usage: %s BASE_REF HEAD_REF\n' "$0" >&2
  exit 1
fi

base_ref="$1"
head_ref="$2"
release_required="false"

if [[ "$base_ref" =~ ^0+$ ]]; then
  changed_paths="$(git ls-tree -r --name-only "$head_ref")"
else
  changed_paths="$(git diff --name-only "$base_ref" "$head_ref")"
fi

while IFS= read -r path; do
  case "$path" in
    GitVersion.yml|src/Cotton.Mobile/*|src/Cotton.Mobile.Core/*)
      release_required="true"
      break
      ;;
  esac
done <<< "$changed_paths"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  printf 'release_required=%s\n' "$release_required" >> "$GITHUB_OUTPUT"
fi

printf 'Android release required: %s\n' "$release_required"
