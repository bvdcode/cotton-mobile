#!/usr/bin/env bash

cotton_parse_arguments() {
  local cotton_argument
  local cotton_matched
  local cotton_option
  local cotton_spec
  local cotton_target
  local cotton_value

  while [[ $# -gt 0 ]]; do
    cotton_argument="$1"
    if [[ "$cotton_argument" == "--help" || "$cotton_argument" == "-h" ]]; then
      usage
      exit 0
    fi

    cotton_matched=0
    for cotton_spec in "${COTTON_VALUE_OPTIONS[@]:-}"; do
      IFS=: read -r cotton_option cotton_target <<< "$cotton_spec"
      if [[ "$cotton_argument" != "$cotton_option" ]]; then
        continue
      fi
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for %s.\n' "$cotton_option" >&2
        exit "$COTTON_EXIT_USAGE"
      fi

      printf -v "$cotton_target" '%s' "$2"
      shift 2
      cotton_matched=1
      break
    done
    if [[ "$cotton_matched" -eq 1 ]]; then
      continue
    fi

    for cotton_spec in "${COTTON_FLAG_OPTIONS[@]:-}"; do
      IFS=: read -r cotton_option cotton_target cotton_value <<< "$cotton_spec"
      if [[ "$cotton_argument" != "$cotton_option" ]]; then
        continue
      fi

      printf -v "$cotton_target" '%s' "$cotton_value"
      shift
      cotton_matched=1
      break
    done
    if [[ "$cotton_matched" -eq 1 ]]; then
      continue
    fi

    printf 'Unknown argument: %s\n' "$cotton_argument" >&2
    exit "$COTTON_EXIT_USAGE"
  done
}

cotton_require_command() {
  local command_name="$1"
  local message="${2:-$command_name was not found.}"

  if ! command -v "$command_name" >/dev/null 2>&1; then
    printf '%s\n' "$message" >&2
    exit "$COTTON_EXIT_COMMAND_NOT_FOUND"
  fi
}
