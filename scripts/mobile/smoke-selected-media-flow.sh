capture_transfer_state() {
  local prefix="$1"
  local instance_key="$2"
  local transfer_root="files/CottonTransfers/$instance_key"

  if ! cotton_adb shell run-as "$package_id" cat "$transfer_root/queue.json" \
      > "$evidence_dir/$prefix-queue.json" 2> "$evidence_dir/$prefix-queue.err"; then
    rm -f "$evidence_dir/$prefix-queue.json"
  fi

  cotton_adb shell run-as "$package_id" find "$transfer_root/Staged" \
    -maxdepth 2 -type f | sort > "$evidence_dir/$prefix-staged-files.txt" || true
}

cotton_wait_for_files_root() {
  local label="$1"
  local attempt
  local prefix
  local xml_file

  for attempt in 0 1 2 3 4 5; do
    prefix="$label-$attempt"
    cotton_capture_screen "$prefix"
    xml_file="$evidence_dir/$prefix.xml"

    if cotton_xml_has_text "$xml_file" "Add files" \
        && { cotton_xml_has_text "$xml_file" "Open transfers" || cotton_xml_has_text "$xml_file" "Transfers"; }; then
      files_root_xml="$xml_file"
      return
    fi

    if cotton_xml_has_text "$xml_file" "Navigate up"; then
      cotton_tap_node_from_xml "$xml_file" "Navigate up" exact
      sleep 2
      continue
    fi

    cotton_adb shell input keyevent KEYCODE_BACK >/dev/null 2>&1 || true
    sleep 1
    cotton_adb shell monkey -p "$package_id" 1 > "$evidence_dir/$label-relaunch-$attempt.txt" || true
    sleep 2
  done

  printf 'Files root with Add files and Transfers navigation is not visible.\n' >&2
  printf 'Evidence: %s\n' "$evidence_dir" >&2
  exit 66
}

validate_package_version() {
  local package_file="$evidence_dir/03-package.txt"

  if [[ -n "$expected_version_code" ]] \
      && ! grep -E "versionCode=$expected_version_code( |$)" "$package_file" >/dev/null; then
    printf 'Installed versionCode does not match expected value: %s\n' "$expected_version_code" >&2
    printf 'Evidence: %s\n' "$package_file" >&2
    exit 66
  fi

  if [[ -n "$expected_version_name" ]] \
      && ! grep -F "versionName=$expected_version_name" "$package_file" >/dev/null; then
    printf 'Installed versionName does not match expected value: %s\n' "$expected_version_name" >&2
    printf 'Evidence: %s\n' "$package_file" >&2
    exit 66
  fi
}

validate_selected_media_queue() {
  local queue_path="$evidence_dir/60-after-picker-queue.json"
  local staged_path="$evidence_dir/60-after-picker-staged-files.txt"
  local item_path="$evidence_dir/61-selected-media-items.json"
  local expected_path="$evidence_dir/14-expected-transfer-items.json"

  if [[ ! -f "$queue_path" ]]; then
    printf 'Transfer queue metadata was not captured.\n' >&2
    printf 'Evidence: %s\n' "$evidence_dir/60-after-picker-queue.err" >&2
    exit 66
  fi

  if [[ ! -f "$expected_path" ]]; then
    printf 'Expected transfer item metadata was not captured.\n' >&2
    printf 'Evidence: %s\n' "$expected_path" >&2
    exit 66
  fi

  python3 - "$queue_path" "$staged_path" "$item_path" "$expected_path" "$kind" <<'PY'
import json
import sys

queue_path, staged_path, item_path, expected_path, kind = sys.argv[1:6]
expected_items = json.load(open(expected_path, encoding="utf-8"))
data = json.load(open(queue_path, encoding="utf-8"))
staged_text = open(staged_path, encoding="utf-8").read()
items = data.get("items", [])
matches = []

for expected in expected_items:
    seeded_name = expected["seededName"]
    picker_name = expected["pickerName"]
    aliases = {seeded_name, picker_name}
    item = next((candidate for candidate in reversed(items) if candidate.get("displayName") in aliases), None)
    if item is None:
        raise SystemExit(f"Missing selected-media transfer for {seeded_name} with aliases {sorted(aliases)!r}")

    source = item.get("source") or {}
    destination = item.get("destination") or {}
    content_type = str(item.get("contentType", ""))
    status = item.get("status")

    if source.get("kind") != 3:
        raise SystemExit(f"Transfer source kind is not SelectedMedia for {seeded_name}: {source!r}")
    if not source.get("sourceId"):
        raise SystemExit(f"Transfer source id is missing for {seeded_name}")
    if source.get("sourceId") in aliases:
        raise SystemExit(f"Transfer source id should not store the display name for {seeded_name}")
    if not destination:
        raise SystemExit(f"Transfer destination is missing for {seeded_name}")
    if kind == "photo" and not content_type.startswith("image/"):
        raise SystemExit(f"Unexpected photo content type for {seeded_name}: {content_type!r}")
    if kind == "video" and not content_type.startswith("video/"):
        raise SystemExit(f"Unexpected video content type for {seeded_name}: {content_type!r}")
    if status not in (0, 1, 2, 3, 4):
        raise SystemExit(f"Unexpected transfer status for {seeded_name}: {status!r}")
    if status != 3 and not any(alias in staged_text for alias in aliases):
        raise SystemExit(f"Waiting transfer staged file is missing for {seeded_name}")

    matches.append(item)

with open(item_path, "w", encoding="utf-8") as handle:
    json.dump(matches, handle, indent=2)
    handle.write("\n")

print(json.dumps(
    {
        "validated": len(matches),
        "kind": kind,
        "items": expected_items,
        "statuses": [item.get("status") for item in matches],
    },
    indent=2,
))
PY
}

open_transfers_page() {
  if cotton_xml_has_text "$files_root_xml" "Open transfers"; then
    cotton_tap_node_from_xml "$files_root_xml" "Open transfers" contains
  else
    cotton_tap_node_from_xml "$files_root_xml" "Transfers" exact
  fi
}

write_metadata() {
  {
    printf 'package=%s\n' "$package_id"
    printf 'serial=%s\n' "$serial"
    printf 'instance=%s\n' "$instance_uri"
    printf 'kind=%s\n' "$kind"
    printf 'count=%s\n' "$count"
    printf 'run_id=%s\n' "$run_id"
    printf 'media_names=%s\n' "$(IFS=,; printf '%s' "${media_names[*]}")"
    printf 'maui_media_picker_docs=https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device-media/picker?view=net-maui-10.0\n'
    printf 'android_photo_picker_docs=https://developer.android.com/training/data-storage/shared/photo-picker\n'
    printf 'android_shared_media_docs=https://developer.android.com/training/data-storage/shared/media\n'
    printf 'android_adb_docs=https://developer.android.com/tools/adb\n'
    printf 'android_ui_automator_docs=https://developer.android.com/training/testing/other-components/ui-automator\n'
  } > "$evidence_dir/00-metadata.txt"
}

write_checklist() {
  {
    printf '# Selected Media Upload Smoke\n\n'
    printf '%s\n' '- [ ] Installed package/version matches the intended build.'
    printf -- '- [ ] Seeded %s item(s) are visible in Android MediaStore.\n' "$kind"
    printf '%s\n' '- [ ] Files root is visible with Add files and Transfers navigation.'
    printf '%s\n' '- [ ] Add files opens the upload action sheet.'
    printf '%s\n' '- [ ] Upload opens the media/source action sheet.'
    printf -- '- [ ] %s picker opens through the native selected-media flow.\n' "$kind"
    printf '%s\n' '- [ ] Operator selects all seeded items and confirms the picker.'
    printf '%s\n' '- [ ] Files returns without a fatal app crash.'
    printf '%s\n' '- [ ] Transfers queue contains SelectedMedia items for all seeded names.'
    printf '%s\n' '- [ ] Waiting selected-media transfers have staged files.'
    printf '%s\n' '- [ ] Transfers page opens after queueing.'
    printf '%s\n\n' '- [ ] Logcat contains no fatal runtime crash for this run.'
    printf 'Seeded items:\n'
    local name
    for name in "${media_names[@]}"; do
      printf -- '- %s\n' "$name"
    done
  } > "$evidence_dir/00-checklist.md"
}
