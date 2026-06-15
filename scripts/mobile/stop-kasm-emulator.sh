#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

export HOME="${COTTON_EMULATOR_HOME:-/root}"
export USER="${COTTON_EMULATOR_USER:-root}"
export LOGNAME="${COTTON_EMULATOR_LOGNAME:-root}"

adb -s "$COTTON_ADB_SERIAL" emu kill
