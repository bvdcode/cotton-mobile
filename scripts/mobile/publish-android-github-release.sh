#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 6 ]]; then
  printf 'Usage: %s <tag> <release-dir> <display-version> <ref-name> <sha> <event-name>\n' "$0" >&2
  exit 2
fi

tag="$1"
release_dir="$2"
display_version="$3"
ref_name="$4"
sha="$5"
event_name="$6"

if [[ ! "$tag" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  printf 'GitHub Release tag must be a SemVer tag such as v1.0.0.\n' >&2
  exit 1
fi

git fetch --force --tags origin
if git rev-parse -q --verify "refs/tags/$tag" >/dev/null; then
  tag_target="$(git rev-list -n 1 "$tag")"
  if [[ "$tag_target" != "$sha" ]]; then
    printf 'Release tag %s already points to %s, not %s.\n' "$tag" "$tag_target" "$sha" >&2
    exit 1
  fi

  printf 'Release tag %s already points to this commit.\n' "$tag"
else
  git tag "$tag" "$sha"
  git push origin "refs/tags/$tag"
fi

release_flags=(
  --title "Cotton Mobile $display_version"
  --notes-file "$release_dir/release-notes.md"
)
if [[ "$event_name" == "push" && "$ref_name" == "develop" ]]; then
  release_flags+=(--prerelease)
else
  release_flags+=(--latest)
fi

if gh release view "$tag" >/dev/null 2>&1; then
  gh release edit "$tag" "${release_flags[@]}"
else
  gh release create "$tag" "${release_flags[@]}" --verify-tag
fi

release_assets=("$release_dir/CottonCloud-Android.apk")
if [[ -f "$release_dir/CottonCloud-Android.aab" ]]; then
  release_assets+=("$release_dir/CottonCloud-Android.aab")
fi
gh release upload "$tag" "${release_assets[@]}" --clobber
