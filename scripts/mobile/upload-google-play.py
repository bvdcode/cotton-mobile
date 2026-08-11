#!/usr/bin/env python3
from __future__ import annotations

import argparse
import logging
import os
from pathlib import Path

from google_play_bundle import (
    GooglePlayBundleClient,
    GooglePlayUploadError,
    GooglePlayUploadOptions,
)


DEFAULT_SERVICE_ACCOUNT_ENV = "GOOGLE_PLAY_SERVICE_ACCOUNT_JSON"
DEFAULT_TIMEOUT_SECONDS = 120
DEFAULT_RELEASE_NOTES_LANGUAGE = "en-US"
MAX_RELEASE_NOTES_LENGTH = 500
logger = logging.getLogger("upload-google-play")


def main() -> int:
    logging.basicConfig(format="%(levelname)s: %(message)s", level=logging.INFO)
    options = parse_arguments()
    service_account_json = load_service_account_json(options)
    client = GooglePlayBundleClient(service_account_json, options.timeout_seconds)

    edit_id: str | None = None
    try:
        edit_id = client.create_edit(options.package_name)
        version_code = client.upload_bundle(options.package_name, edit_id, options.bundle_path)
        client.update_track(
            options.package_name,
            edit_id,
            options.track,
            version_code,
            options.release_status,
            options.release_name,
            options.release_notes,
            options.release_notes_language,
        )
        client.commit_edit(
            options.package_name,
            edit_id,
            options.changes_not_sent_for_review,
        )
        edit_id = None
        client.verify_track_contains_version_code(
            options.package_name,
            options.track,
            version_code,
        )
    except Exception:
        if edit_id:
            client.delete_edit(options.package_name, edit_id)
        raise

    logger.info("Google Play upload completed.")
    return 0


def parse_arguments() -> GooglePlayUploadOptions:
    parser = argparse.ArgumentParser(description="Upload a signed Android App Bundle to Google Play.")
    parser.add_argument("--package-name", required=True, help="Android application id, for example dev.cottoncloud.app.")
    parser.add_argument("--bundle", required=True, type=Path, help="Path to the signed .aab file.")
    parser.add_argument("--track", default="internal", help="Google Play track name.")
    parser.add_argument(
        "--release-status",
        choices=["completed", "draft"],
        default="completed",
        help="Google Play release status.",
    )
    parser.add_argument("--release-name", help="Optional Google Play release name.")
    parser.add_argument(
        "--release-notes-file",
        type=Path,
        help="Optional text file for Google Play release notes.",
    )
    parser.add_argument(
        "--release-notes-language",
        default=DEFAULT_RELEASE_NOTES_LANGUAGE,
        help="BCP-47 language tag for release notes.",
    )
    parser.add_argument(
        "--changes-not-sent-for-review",
        action="store_true",
        help="Commit changes without sending them for review.",
    )
    parser.add_argument(
        "--service-account-json-env",
        default=DEFAULT_SERVICE_ACCOUNT_ENV,
        help="Environment variable that contains Google service account JSON.",
    )
    parser.add_argument(
        "--service-account-json-file",
        type=Path,
        help="Path to a Google service account JSON file. Overrides the environment variable.",
    )
    parser.add_argument(
        "--timeout-seconds",
        type=int,
        default=DEFAULT_TIMEOUT_SECONDS,
        help="HTTP timeout in seconds.",
    )
    args = parser.parse_args()

    bundle_path = args.bundle.expanduser().resolve()
    if not bundle_path.is_file():
        raise GooglePlayUploadError(f"AAB file does not exist: {bundle_path}")

    if args.timeout_seconds <= 0:
        raise GooglePlayUploadError("--timeout-seconds must be a positive integer.")

    release_notes = read_optional_release_notes(args.release_notes_file)
    release_notes_language = args.release_notes_language.strip()
    if release_notes and not release_notes_language:
        raise GooglePlayUploadError("--release-notes-language is required when release notes are provided.")

    return GooglePlayUploadOptions(
        package_name=args.package_name,
        bundle_path=bundle_path,
        track=args.track,
        release_status=args.release_status,
        release_name=args.release_name,
        release_notes=release_notes,
        release_notes_language=release_notes_language,
        changes_not_sent_for_review=args.changes_not_sent_for_review,
        service_account_json_env=args.service_account_json_env,
        service_account_json_file=args.service_account_json_file,
        timeout_seconds=args.timeout_seconds,
    )


def read_optional_release_notes(path: Path | None) -> str | None:
    if path is None:
        return None

    release_notes_path = path.expanduser().resolve()
    if not release_notes_path.is_file():
        raise GooglePlayUploadError(f"Release notes file does not exist: {release_notes_path}")

    release_notes = release_notes_path.read_text(encoding="utf-8").strip()
    if not release_notes:
        raise GooglePlayUploadError(f"Release notes file is empty: {release_notes_path}")
    if len(release_notes) > MAX_RELEASE_NOTES_LENGTH:
        raise GooglePlayUploadError(
            f"Release notes file is {len(release_notes)} characters; "
            f"Google Play allows at most {MAX_RELEASE_NOTES_LENGTH}: {release_notes_path}"
        )

    return release_notes


def load_service_account_json(options: GooglePlayUploadOptions) -> str:
    if options.service_account_json_file:
        return options.service_account_json_file.expanduser().read_text(encoding="utf-8")

    service_account_json = os.environ.get(options.service_account_json_env)
    if not service_account_json:
        raise GooglePlayUploadError(
            f"{options.service_account_json_env} environment variable is required."
        )

    return service_account_json


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except GooglePlayUploadError as exception:
        logger.error("%s", exception)
        raise SystemExit(1)
