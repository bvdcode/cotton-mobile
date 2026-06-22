#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
notes_script="$repo_root/scripts/mobile/create-android-release-notes.sh"
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

commit_change() {
  local message="$1"

  printf '%s\n' "$message" >> fixture.txt
  git add fixture.txt
  git commit -q -m "$message"
}

cd "$workspace"
git init -q
git config user.email "mobile-release-notes-test@example.invalid"
git config user.name "Mobile Release Notes Test"

commit_change "initial release"
git tag v1.0.0
commit_change "fix release upload"
commit_change "docs update release flow"

output="$("$notes_script" v1.0.1 1.0.1 1002001 main "$(git rev-parse HEAD)")"

assert_contains "$output" "# Cotton Mobile 1.0.1"
assert_contains "$output" "## Changes"
assert_contains "$output" "- docs update release flow"
assert_contains "$output" "- fix release upload"
assert_contains "$output" "- Branch: \`main\`"
assert_contains "$output" "- Android versionCode: \`1002001\`"
assert_contains "$output" "- Previous release tag: \`v1.0.0\`"

printf 'Android release notes checks passed.\n'
