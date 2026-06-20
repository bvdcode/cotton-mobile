#!/usr/bin/env bash
set -euo pipefail

cotton_prepend_path() {
  local value="$1"
  if [[ -d "$value" && ":$PATH:" != *":$value:"* ]]; then
    PATH="$value:$PATH"
  fi
}

cotton_clear_android_fast_deployment_overrides() {
  local serial="$1"
  local package_id="$2"

  if adb -s "$serial" shell run-as "$package_id" rm -rf files/.__override__ files/.__tools__ >/dev/null 2>&1; then
    printf 'Cleared debug fast-deployment overrides for %s.\n' "$package_id"
  else
    printf 'Debug fast-deployment override cleanup skipped for %s.\n' "$package_id"
  fi
}

cotton_install_android_apk() {
  local serial="$1"
  local package_id="$2"
  local apk_path="$3"

  adb -s "$serial" install --no-incremental -r "$apk_path"
  cotton_clear_android_fast_deployment_overrides "$serial" "$package_id"
}

COTTON_REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
export COTTON_REPO_ROOT

export COTTON_MOBILE_PROJECT="${COTTON_MOBILE_PROJECT:-$COTTON_REPO_ROOT/src/Cotton.Mobile/Cotton.Mobile.csproj}"
export COTTON_ANDROID_FRAMEWORK="${COTTON_ANDROID_FRAMEWORK:-net10.0-android}"
export COTTON_ANDROID_CONFIGURATION="${COTTON_ANDROID_CONFIGURATION:-Debug}"
if [[ "$COTTON_ANDROID_CONFIGURATION" == "Debug" ]]; then
  COTTON_ANDROID_DEFAULT_PACKAGE_ID="dev.cottoncloud.app.debug"
else
  COTTON_ANDROID_DEFAULT_PACKAGE_ID="dev.cottoncloud.app"
fi
export COTTON_ANDROID_PACKAGE_ID="${COTTON_ANDROID_PACKAGE_ID:-$COTTON_ANDROID_DEFAULT_PACKAGE_ID}"

export COTTON_ANDROID_SDK_ROOT="${COTTON_ANDROID_SDK_ROOT:-${ANDROID_SDK_ROOT:-${ANDROID_HOME:-/home/kasm-user/Android/Sdk}}}"
export ANDROID_HOME="${ANDROID_HOME:-$COTTON_ANDROID_SDK_ROOT}"
export ANDROID_SDK_ROOT="${ANDROID_SDK_ROOT:-$COTTON_ANDROID_SDK_ROOT}"
export JAVA_HOME="${JAVA_HOME:-/usr/lib/jvm/java-21-openjdk-amd64}"

export COTTON_DOTNET_HOME="${COTTON_DOTNET_HOME:-/home/kasm-user}"
export COTTON_AVD_HOME="${COTTON_AVD_HOME:-/root/.android/avd}"
export COTTON_AVD_NAME="${COTTON_AVD_NAME:-Cotton_API36}"
export COTTON_ADB_SERIAL="${COTTON_ADB_SERIAL:-emulator-5554}"
export COTTON_ANDROID_APK="${COTTON_ANDROID_APK:-$COTTON_REPO_ROOT/src/Cotton.Mobile/bin/$COTTON_ANDROID_CONFIGURATION/$COTTON_ANDROID_FRAMEWORK/$COTTON_ANDROID_PACKAGE_ID-Signed.apk}"

if [[ -z "${XAUTHORITY:-}" && -f /home/kasm-user/.Xauthority ]]; then
  export XAUTHORITY=/home/kasm-user/.Xauthority
fi

cotton_prepend_path "$ANDROID_HOME/platform-tools"
cotton_prepend_path "$ANDROID_HOME/emulator"
cotton_prepend_path "$ANDROID_HOME/cmdline-tools/latest/bin"
cotton_prepend_path "$JAVA_HOME/bin"
export PATH
