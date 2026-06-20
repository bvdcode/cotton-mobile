#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

if [[ -d "$COTTON_DOTNET_HOME/.dotnet" ]]; then
  export HOME="$COTTON_DOTNET_HOME"
fi

export USER="${COTTON_DOTNET_USER:-${USER:-kasm-user}}"
export LOGNAME="${COTTON_DOTNET_LOGNAME:-${LOGNAME:-$USER}}"

cd "$COTTON_REPO_ROOT"

if [[ "${COTTON_REQUIRE_FIREBASE_CONFIG:-0}" == "1" ]]; then
  "$SCRIPT_DIR/check-android-firebase-config.py" \
    --configuration "$COTTON_ANDROID_CONFIGURATION" \
    --package-id "$COTTON_ANDROID_PACKAGE_ID"
fi

dotnet restore "$COTTON_MOBILE_PROJECT" \
  -p:AndroidSdkDirectory="$ANDROID_HOME" \
  -p:JavaSdkDirectory="$JAVA_HOME"

dotnet build "$COTTON_MOBILE_PROJECT" \
  -f "$COTTON_ANDROID_FRAMEWORK" \
  -c "$COTTON_ANDROID_CONFIGURATION" \
  --no-restore \
  -p:AndroidSdkDirectory="$ANDROID_HOME" \
  -p:JavaSdkDirectory="$JAVA_HOME" \
  -p:EmbedAssembliesIntoApk=true \
  "$@"

printf 'Android APK: %s\n' "$COTTON_ANDROID_APK"
