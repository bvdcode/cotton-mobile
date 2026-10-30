#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
compute_script="$repo_root/scripts/mobile/compute-android-release-version.sh"
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

commit_change() {
  local message="$1"

  printf '%s\n' "$message" >> release-version-fixture.txt
  git add GitVersion.yml release-version-fixture.txt
  git commit -q -m "$message"
}

run_compute() {
  local run_number="$1"
  local run_attempt="$2"
  local version_code_base="$3"
  local published_floor="$4"
  local display_version="${5:-}"

  GITHUB_RUN_NUMBER="$run_number" \
    GITHUB_RUN_ATTEMPT="$run_attempt" \
    ANDROID_VERSION_CODE_BASE="$version_code_base" \
    ANDROID_VERSION_CODE_PUBLISHED_FLOOR="$published_floor" \
    "$compute_script" "$display_version"
}

cd "$workspace"
git init -q
git config user.email "mobile-version-test@example.invalid"
git config user.name "Mobile Version Test"
printf 'next-version: 1.0.0\n' > GitVersion.yml
commit_change "initial"

output="$(run_compute 1 1 1001000 1000449)"
assert_contains "$output" "Android display version: 1.0.0"
assert_contains "$output" "Android display version tag: v1.0.0"
assert_contains "$output" "Android versionCode: 1002001"
assert_contains "$output" "Google Play release name: Cotton Mobile 1.0.0"

git tag v1.0.0
output="$(run_compute 2 3 1001000 1000449)"
assert_contains "$output" "Android display version: 1.0.0"
assert_contains "$output" "Android versionCode: 1003003"

commit_change "next release"
output="$(run_compute 3 1 1001000 1000449)"
assert_contains "$output" "Android display version: 1.0.1"
assert_contains "$output" "Google Play release name: Cotton Mobile 1.0.1"

git tag v1.0.3
commit_change "after skipped patch"
output="$(run_compute 4 1 1001000 1000449)"
assert_contains "$output" "Android display version: 1.0.4"
assert_contains "$output" "Android display version tag: v1.0.4"

output="$(run_compute 5 1 1001000 1000449 2.3.4)"
assert_contains "$output" "Android display version: 2.3.4"
assert_contains "$output" "Google Play release name: Cotton Mobile 2.3.4"

if output="$(run_compute 1 1 1001000 1002001 2>&1)"; then
  fail "Expected published-floor validation to fail."
fi
assert_contains "$output" "Computed Android versionCode must be greater than the latest published floor 1002001."

if output="$(run_compute 2100000 1 1001000 1000449 2>&1)"; then
  fail "Expected Google Play maximum versionCode validation to fail."
fi
assert_contains "$output" "Computed Android versionCode must not exceed the Google Play maximum 2100000000."

printf 'Android release version computation checks passed.\n'
