#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
detect_script="$repo_root/scripts/mobile/detect-android-release-changes.sh"
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

commit_all() {
  local message="$1"

  git add .
  git commit -q -m "$message"
  git rev-parse HEAD
}

cd "$workspace"
git init -q
git config user.email "mobile-release-change-test@example.invalid"
git config user.name "Mobile Release Change Test"

mkdir -p src/Cotton.Mobile src/Cotton.Mobile.Core scripts/mobile .github/workflows
printf '# Cotton Mobile\n' > README.md
printf 'next-version: 1.0.0\n' > GitVersion.yml
printf '<Project />\n' > src/Cotton.Mobile/Cotton.Mobile.csproj
base="$(commit_all "initial")"

printf 'Updated README\n' > README.md
docs_head="$(commit_all "docs")"
output="$("$detect_script" "$base" "$docs_head")"
assert_contains "$output" "Android release required: false"

printf 'name: Mobile Android\n' > .github/workflows/mobile-android.yml
workflow_head="$(commit_all "workflow")"
output="$("$detect_script" "$docs_head" "$workflow_head")"
assert_contains "$output" "Android release required: true"

printf '#!/usr/bin/env python3\n' > scripts/mobile/upload-google-play.py
upload_script_head="$(commit_all "upload script")"
output="$("$detect_script" "$workflow_head" "$upload_script_head")"
assert_contains "$output" "Android release required: true"

printf '#!/usr/bin/env bash\n' > scripts/mobile/detect-android-release-changes.sh
detector_head="$(commit_all "detector")"
output="$("$detect_script" "$upload_script_head" "$detector_head")"
assert_contains "$output" "Android release required: true"

printf '<Project><PropertyGroup /></Project>\n' > src/Cotton.Mobile/Cotton.Mobile.csproj
app_head="$(commit_all "app")"
output="$("$detect_script" "$detector_head" "$app_head")"
assert_contains "$output" "Android release required: true"

printf 'namespace Cotton.Mobile.Core { public class Marker { } }\n' > src/Cotton.Mobile.Core/Marker.cs
core_head="$(commit_all "core")"
output="$("$detect_script" "$app_head" "$core_head")"
assert_contains "$output" "Android release required: true"

printf 'next-version: 1.0.1\n' > GitVersion.yml
version_head="$(commit_all "version")"
output="$("$detect_script" "$core_head" "$version_head")"
assert_contains "$output" "Android release required: true"

printf 'Android release change filter checks passed.\n'
