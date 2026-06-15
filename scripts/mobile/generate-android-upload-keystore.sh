#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=android-env.sh
source "$SCRIPT_DIR/android-env.sh"

if [[ -d /data/kasm/config ]]; then
  default_output_dir="/data/kasm/config/cotton-mobile-signing"
else
  default_output_dir="$COTTON_REPO_ROOT/.mobile-signing"
fi

output_dir="${COTTON_ANDROID_SIGNING_DIR:-$default_output_dir}"
alias_name="${COTTON_ANDROID_KEYSTORE_ALIAS:-cotton-upload}"
keystore_name="${COTTON_ANDROID_KEYSTORE_NAME:-cotton-upload.keystore}"
dname="${COTTON_ANDROID_KEYSTORE_DNAME:-CN=Cotton Cloud,O=Vadim Belov,C=US}"
validity_days="${COTTON_ANDROID_KEYSTORE_VALIDITY_DAYS:-10000}"
overwrite=0

usage() {
  cat <<EOF
Usage: $(basename "$0") [--output-dir <path>] [--alias <value>] [--overwrite]

Generates an Android upload keystore outside the repository and writes:
  - $keystore_name
  - cotton-upload.secrets
  - set-github-secrets.sh

Default output directory:
  $default_output_dir
EOF
}

while [[ "$#" -gt 0 ]]; do
  case "$1" in
    --output-dir)
      output_dir="$2"
      shift 2
      ;;
    --alias)
      alias_name="$2"
      shift 2
      ;;
    --overwrite)
      overwrite=1
      shift
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

require_command() {
  local command_name="$1"
  if ! command -v "$command_name" >/dev/null 2>&1; then
    printf 'Required command not found: %s\n' "$command_name" >&2
    exit 69
  fi
}

generate_secret() {
  openssl rand -base64 36 | tr -d '\n'
}

require_command keytool
require_command openssl

umask 077
mkdir -p "$output_dir"

keystore_path="$output_dir/$keystore_name"
secrets_path="$output_dir/cotton-upload.secrets"
github_secret_script_path="$output_dir/set-github-secrets.sh"

if [[ "$overwrite" -ne 1 ]]; then
  for path in "$keystore_path" "$secrets_path" "$github_secret_script_path"; do
    if [[ -e "$path" ]]; then
      printf 'Refusing to overwrite existing file: %s\nUse --overwrite only after backing up the current upload key.\n' "$path" >&2
      exit 73
    fi
  done
fi

store_password="$(generate_secret)"
key_password="$store_password"

keytool -genkeypair \
  -keystore "$keystore_path" \
  -storepass "$store_password" \
  -keypass "$key_password" \
  -alias "$alias_name" \
  -keyalg RSA \
  -keysize 4096 \
  -validity "$validity_days" \
  -dname "$dname" \
  >/dev/null

cat > "$secrets_path" <<EOF
ANDROID_KEYSTORE_ALIAS=$alias_name
ANDROID_KEYSTORE_PASSWORD=$store_password
ANDROID_KEY_PASSWORD=$key_password
ANDROID_KEYSTORE_PATH=$keystore_path
EOF

cat > "$github_secret_script_path" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=cotton-upload.secrets
source "$SCRIPT_DIR/cotton-upload.secrets"

require_secret_value() {
  local name="$1"
  local value="$2"
  if [[ -z "$value" ]]; then
    printf 'Missing required value: %s\n' "$name" >&2
    exit 69
  fi
}

require_secret_value ANDROID_KEYSTORE_ALIAS "$ANDROID_KEYSTORE_ALIAS"
require_secret_value ANDROID_KEYSTORE_PASSWORD "$ANDROID_KEYSTORE_PASSWORD"
require_secret_value ANDROID_KEY_PASSWORD "$ANDROID_KEY_PASSWORD"
require_secret_value ANDROID_KEYSTORE_PATH "$ANDROID_KEYSTORE_PATH"

if ! command -v gh >/dev/null 2>&1; then
  printf 'Required command not found: gh\n' >&2
  exit 69
fi

if [[ ! -f "$ANDROID_KEYSTORE_PATH" ]]; then
  printf 'Keystore not found: %s\n' "$ANDROID_KEYSTORE_PATH" >&2
  exit 66
fi

base64 "$ANDROID_KEYSTORE_PATH" | tr -d '\n' | gh secret set ANDROID_KEYSTORE_BASE64
printf '%s' "$ANDROID_KEYSTORE_ALIAS" | gh secret set ANDROID_KEYSTORE_ALIAS --body-file -
printf '%s' "$ANDROID_KEYSTORE_PASSWORD" | gh secret set ANDROID_KEYSTORE_PASSWORD --body-file -
printf '%s' "$ANDROID_KEY_PASSWORD" | gh secret set ANDROID_KEY_PASSWORD --body-file -
EOF

chmod 600 "$keystore_path" "$secrets_path"
chmod 700 "$github_secret_script_path"

keytool -list -keystore "$keystore_path" -storepass "$store_password" -alias "$alias_name" >/dev/null

printf 'Generated Android upload keystore: %s\n' "$keystore_path"
printf 'Generated local secrets file: %s\n' "$secrets_path"
printf 'Generated GitHub secret helper: %s\n' "$github_secret_script_path"
