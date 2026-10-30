#!/usr/bin/env bash

umask 077

readonly COTTON_EXIT_USAGE=64
readonly COTTON_EXIT_EVIDENCE=66
readonly COTTON_EXIT_DEVICE_UNAVAILABLE=69
readonly COTTON_EXIT_COMMAND_NOT_FOUND=127
readonly COTTON_UI_DUMP_REMOTE_PATH=/sdcard/cotton-window.xml

source "$SCRIPT_DIR/smoke-arguments.sh"
source "$SCRIPT_DIR/smoke-device.sh"
source "$SCRIPT_DIR/smoke-ui.sh"
