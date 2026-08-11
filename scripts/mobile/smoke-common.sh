#!/usr/bin/env bash

readonly COTTON_EXIT_USAGE=64
readonly COTTON_EXIT_NON_INTERACTIVE=65
readonly COTTON_EXIT_EVIDENCE=66
readonly COTTON_EXIT_VERSION_MISMATCH=67
readonly COTTON_EXIT_DEVICE_UNAVAILABLE=69
readonly COTTON_EXIT_INSTALLED_VERSION_MISMATCH=70
readonly COTTON_EXIT_COMMAND_NOT_FOUND=127
readonly COTTON_UI_DUMP_REMOTE_PATH=/sdcard/cotton-window.xml

source "$SCRIPT_DIR/smoke-arguments.sh"
source "$SCRIPT_DIR/smoke-device.sh"
source "$SCRIPT_DIR/smoke-ui.sh"
source "$SCRIPT_DIR/smoke-data.sh"
