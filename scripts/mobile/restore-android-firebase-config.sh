#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

configuration="$COTTON_ANDROID_CONFIGURATION"
package_id="$COTTON_ANDROID_PACKAGE_ID"
config_file="$COTTON_REPO_ROOT/src/Cotton.Mobile/Platforms/Android/google-services.json"
source_file=""
source_env_name="ANDROID_FIREBASE_GOOGLE_SERVICES_JSON"

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Restores the ignored Android Firebase google-services.json file from a local
source file or an environment variable, then validates the package id.

Options:
  --configuration NAME  Android build configuration. Defaults to COTTON_ANDROID_CONFIGURATION.
  --package-id ID       Expected Android package id. Defaults to COTTON_ANDROID_PACKAGE_ID.
  --config-file PATH    Destination google-services.json path.
  --source-file PATH    Source google-services.json path.
  --source-env NAME     Environment variable containing google-services.json content.
                       Defaults to ANDROID_FIREBASE_GOOGLE_SERVICES_JSON.
  --help, -h            Show this help.

The config content is not printed. The destination should stay ignored by git.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --configuration.\n' >&2
        exit 64
      fi
      configuration="$2"
      shift 2
      ;;
    --package-id)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --package-id.\n' >&2
        exit 64
      fi
      package_id="$2"
      shift 2
      ;;
    --config-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --config-file.\n' >&2
        exit 64
      fi
      config_file="$2"
      shift 2
      ;;
    --source-file)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --source-file.\n' >&2
        exit 64
      fi
      source_file="$2"
      shift 2
      ;;
    --source-env)
      if [[ $# -lt 2 ]]; then
        printf 'Missing value for --source-env.\n' >&2
        exit 64
      fi
      source_env_name="$2"
      shift 2
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      printf 'Unknown argument: %s\n' "$1" >&2
      exit 64
      ;;
  esac
done

if [[ -z "$configuration" ]]; then
  printf 'Android build configuration is required.\n' >&2
  exit 64
fi

if [[ -z "$package_id" ]]; then
  printf 'Expected Android package id is required.\n' >&2
  exit 64
fi

if [[ ! "$source_env_name" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
  printf 'Invalid --source-env name: %s\n' "$source_env_name" >&2
  exit 64
fi

if [[ -n "$source_file" && ! -f "$source_file" ]]; then
  printf 'Firebase Android config source file was not found: %s\n' "$source_file" >&2
  exit 66
fi

if [[ -z "$source_file" && -z "${!source_env_name:-}" ]]; then
  printf 'Firebase Android config content is required.\n' >&2
  printf 'Provide --source-file PATH or set %s.\n' "$source_env_name" >&2
  exit 66
fi

if [[ "$config_file" != /* ]]; then
  config_file="$COTTON_REPO_ROOT/$config_file"
fi

mkdir -p "$(dirname "$config_file")"
tmp_file="$(mktemp "${config_file}.tmp.XXXXXX")"
cleanup_tmp() {
  rm -f "$tmp_file"
}
trap cleanup_tmp EXIT

if [[ -n "$source_file" ]]; then
  cp "$source_file" "$tmp_file"
else
  printf '%s' "${!source_env_name}" > "$tmp_file"
fi
chmod 600 "$tmp_file"

"$SCRIPT_DIR/check-android-firebase-config.py" \
  --configuration "$configuration" \
  --package-id "$package_id" \
  --config-file "$tmp_file"

mv "$tmp_file" "$config_file"
trap - EXIT
chmod 600 "$config_file"

printf 'Firebase Android config restored: %s\n' "$config_file"
