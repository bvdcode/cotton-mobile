#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  printf 'Usage: %s <android-platform> <android-build-tools>\n' "$0" >&2
  exit 2
fi

android_platform="$1"
android_build_tools="$2"
sdkmanager_bin="${ANDROID_HOME:-}/cmdline-tools/latest/bin/sdkmanager"
if [[ ! -x "$sdkmanager_bin" ]]; then
  sdkmanager_bin="$(command -v sdkmanager)"
fi

set +o pipefail
yes | "$sdkmanager_bin" --licenses >/dev/null
set -o pipefail
"$sdkmanager_bin" --install "$android_platform" "$android_build_tools" platform-tools

dotnet workload install maui-android
