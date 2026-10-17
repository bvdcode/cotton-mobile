#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
publish_script="$repo_root/scripts/mobile/publish-android-github-release.sh"
workspace="$(mktemp -d)"

cleanup() {
  rm -rf "$workspace"
}

trap cleanup EXIT

assert_contains() {
  local haystack="$1"
  local needle="$2"

  if [[ "$haystack" != *"$needle"* ]]; then
    printf 'Expected output to contain: %s\nActual output:\n%s\n' "$needle" "$haystack" >&2
    exit 1
  fi
}

mkdir -p "$workspace/bin" "$workspace/source" "$workspace/assets"
git init --bare -q "$workspace/origin.git"
git init -q "$workspace/source"
cd "$workspace/source"
git config user.email "mobile-release-test@example.invalid"
git config user.name "Mobile Release Test"
git remote add origin "$workspace/origin.git"
printf 'release\n' > fixture.txt
git add fixture.txt
git commit -q -m "release"
sha="$(git rev-parse HEAD)"

printf 'signed APK\n' > "$workspace/assets/CottonCloud-Android.apk"
printf 'signed AAB\n' > "$workspace/assets/CottonCloud-Android.aab"
printf 'Release notes\n' > "$workspace/assets/release-notes.md"

cat > "$workspace/bin/gh" <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail
printf '%s\n' "$*" >> "$RELEASE_COMMANDS"
if [[ "$1 $2" == "release view" ]]; then
  exit "$RELEASE_VIEW_EXIT_CODE"
fi
SCRIPT
chmod +x "$workspace/bin/gh"
export PATH="$workspace/bin:$PATH"
export RELEASE_COMMANDS="$workspace/commands"
export RELEASE_VIEW_EXIT_CODE=1

"$publish_script" v1.2.3 "$workspace/assets" 1.2.3 develop "$sha"
output="$(<"$RELEASE_COMMANDS")"
assert_contains "$output" "release create v1.2.3 --title Cotton Mobile 1.2.3 --notes-file $workspace/assets/release-notes.md --target develop --latest --prerelease=false --verify-tag"
assert_contains "$output" "release upload v1.2.3 $workspace/assets/CottonCloud-Android.apk $workspace/assets/CottonCloud-Android.aab --clobber"
remote_sha="$(git ls-remote origin refs/tags/v1.2.3 | cut -f1)"
if [[ "$remote_sha" != "$sha" ]]; then
  printf 'Published tag must point to the release commit.\n' >&2
  exit 1
fi

export RELEASE_VIEW_EXIT_CODE=0
: > "$RELEASE_COMMANDS"
"$publish_script" v1.2.3 "$workspace/assets" 1.2.3 develop "$sha"
output="$(<"$RELEASE_COMMANDS")"
assert_contains "$output" "release edit v1.2.3 --title Cotton Mobile 1.2.3 --notes-file $workspace/assets/release-notes.md --target develop --latest --prerelease=false"
assert_contains "$output" "release upload v1.2.3 $workspace/assets/CottonCloud-Android.apk $workspace/assets/CottonCloud-Android.aab --clobber"

printf 'next\n' >> fixture.txt
git add fixture.txt
git commit -q -m "next release"
next_sha="$(git rev-parse HEAD)"
: > "$RELEASE_COMMANDS"
if "$publish_script" v1.2.3 "$workspace/assets" 1.2.3 main "$next_sha" > "$workspace/conflict" 2>&1; then
  printf 'An existing release tag must not move to another commit.\n' >&2
  exit 1
fi
assert_contains "$(<"$workspace/conflict")" "Release tag v1.2.3 already points to $sha, not $next_sha."
if [[ -s "$RELEASE_COMMANDS" ]]; then
  printf 'A tag conflict must stop before publishing release assets.\n' >&2
  exit 1
fi

export RELEASE_VIEW_EXIT_CODE=1
: > "$RELEASE_COMMANDS"
"$publish_script" v1.2.4 "$workspace/assets" 1.2.4 main "$next_sha"
output="$(<"$RELEASE_COMMANDS")"
assert_contains "$output" "release create v1.2.4 --title Cotton Mobile 1.2.4 --notes-file $workspace/assets/release-notes.md --target main --latest --prerelease=false --verify-tag"

printf 'Android GitHub release publication checks passed.\n'
