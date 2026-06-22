#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import logging
import os
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Mapping, Protocol
from urllib.parse import quote


ANDROID_PUBLISHER_SCOPE = "https://www.googleapis.com/auth/androidpublisher"
ANDROID_PUBLISHER_API_BASE_URL = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications"
ANDROID_PUBLISHER_UPLOAD_BASE_URL = "https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications"
DEFAULT_SERVICE_ACCOUNT_ENV = "GOOGLE_PLAY_SERVICE_ACCOUNT_JSON"
DEFAULT_TIMEOUT_SECONDS = 120
DEFAULT_RELEASE_NOTES_LANGUAGE = "en-US"
TRACK_READ_BACK_ATTEMPTS = 6
TRACK_READ_BACK_RETRY_DELAY_SECONDS = 10
MAX_RELEASE_NOTES_LENGTH = 500

logger = logging.getLogger("upload-google-play")


class GooglePlayUploadError(Exception):
    pass


@dataclass(frozen=True)
class GooglePlayUploadOptions:
    package_name: str
    bundle_path: Path
    track: str
    release_status: str
    release_name: str | None
    release_notes: str | None
    release_notes_language: str
    changes_not_sent_for_review: bool
    service_account_json_env: str
    service_account_json_file: Path | None
    timeout_seconds: int


@dataclass(frozen=True)
class GooglePlayTrackRelease:
    name: str | None
    status: str | None
    version_codes: tuple[int, ...]


@dataclass(frozen=True)
class GooglePlayTrackState:
    track: str
    releases: tuple[GooglePlayTrackRelease, ...]

    def contains_version_code(self, version_code: int) -> bool:
        return any(version_code in release.version_codes for release in self.releases)

    def describe(self) -> str:
        if not self.releases:
            return "no releases"

        release_descriptions: list[str] = []
        for release in self.releases:
            release_parts: list[str] = []
            if release.name:
                release_parts.append(f"name={release.name}")
            if release.status:
                release_parts.append(f"status={release.status}")
            release_parts.append(f"versionCodes={list(release.version_codes)}")
            release_descriptions.append("{" + ", ".join(release_parts) + "}")

        return "; ".join(release_descriptions)


class HttpResponse(Protocol):
    status_code: int
    content: bytes
    text: str

    def json(self) -> object:
        ...


class HttpSession(Protocol):
    def request(
        self,
        method: str,
        url: str,
        *,
        json: Mapping[str, object] | None = None,
        data: object | None = None,
        headers: Mapping[str, str] | None = None,
        params: Mapping[str, str] | None = None,
        timeout: int,
    ) -> HttpResponse:
        ...


class AndroidPublisherClient:
    def __init__(self, service_account_json: str, timeout_seconds: int) -> None:
        self._timeout_seconds = timeout_seconds
        service_account, authorized_session_type = self._load_google_auth_dependencies()
        service_account_info = self._parse_service_account_json(service_account_json)
        credentials = service_account.Credentials.from_service_account_info(
            service_account_info,
            scopes=[ANDROID_PUBLISHER_SCOPE],
        )
        self._session: HttpSession = authorized_session_type(credentials)

    def create_edit(self, package_name: str) -> str:
        response = self._request(
            "POST",
            f"{self._application_url(package_name)}/edits",
            json_body={},
        )
        edit_id = response.get("id")
        if not isinstance(edit_id, str) or not edit_id:
            raise GooglePlayUploadError("Google Play edit response did not include an edit id.")

        logger.info("Created Google Play edit %s.", edit_id)
        return edit_id

    def upload_bundle(self, package_name: str, edit_id: str, bundle_path: Path) -> int:
        upload_url = (
            f"{ANDROID_PUBLISHER_UPLOAD_BASE_URL}/"
            f"{self._quote_path(package_name)}/edits/{self._quote_path(edit_id)}/bundles"
        )
        logger.info("Uploading AAB %s.", bundle_path)
        with bundle_path.open("rb") as bundle_stream:
            response = self._request(
                "POST",
                upload_url,
                data=bundle_stream,
                headers={"Content-Type": "application/octet-stream"},
                params={"uploadType": "media"},
            )

        version_code = response.get("versionCode")
        if not isinstance(version_code, int):
            raise GooglePlayUploadError("Google Play bundle upload response did not include versionCode.")

        logger.info("Uploaded AAB versionCode %s.", version_code)
        return version_code

    def update_track(
        self,
        package_name: str,
        edit_id: str,
        track: str,
        version_code: int,
        release_status: str,
        release_name: str | None,
        release_notes: str | None,
        release_notes_language: str,
    ) -> None:
        release: dict[str, object] = {
            "versionCodes": [str(version_code)],
            "status": release_status,
        }
        if release_name:
            release["name"] = release_name
        if release_notes:
            release["releaseNotes"] = [
                {
                    "language": release_notes_language,
                    "text": release_notes,
                }
            ]

        body: dict[str, object] = {
            "track": track,
            "releases": [release],
        }

        logger.info("Updating Google Play track %s with versionCode %s.", track, version_code)
        self._request(
            "PUT",
            f"{self._application_url(package_name)}/edits/{self._quote_path(edit_id)}/tracks/{self._quote_path(track)}",
            json_body=body,
        )

    def get_track(self, package_name: str, edit_id: str, track: str) -> GooglePlayTrackState:
        response = self._request(
            "GET",
            f"{self._application_url(package_name)}/edits/{self._quote_path(edit_id)}/tracks/{self._quote_path(track)}",
        )

        return self._parse_track_state(response, track)

    def commit_edit(self, package_name: str, edit_id: str, changes_not_sent_for_review: bool) -> None:
        params: dict[str, str] = {}
        if changes_not_sent_for_review:
            params["changesNotSentForReview"] = "true"

        logger.info("Committing Google Play edit %s.", edit_id)
        self._request(
            "POST",
            f"{self._application_url(package_name)}/edits/{self._quote_path(edit_id)}:commit",
            params=params,
        )

    def delete_edit(self, package_name: str, edit_id: str) -> None:
        logger.info("Deleting Google Play edit %s.", edit_id)
        response = self._session.request(
            "DELETE",
            f"{self._application_url(package_name)}/edits/{self._quote_path(edit_id)}",
            timeout=self._timeout_seconds,
        )
        if response.status_code >= 400:
            logger.warning("Could not delete Google Play edit %s: %s", edit_id, response.text)

    def verify_track_contains_version_code(self, package_name: str, track: str, version_code: int) -> None:
        for attempt in range(1, TRACK_READ_BACK_ATTEMPTS + 1):
            track_state = self.read_track_state(package_name, track)
            logger.info(
                "Google Play track %s read-back attempt %s/%s: %s.",
                track,
                attempt,
                TRACK_READ_BACK_ATTEMPTS,
                track_state.describe(),
            )

            if track_state.contains_version_code(version_code):
                logger.info("Google Play track %s contains committed versionCode %s.", track, version_code)
                return

            if attempt < TRACK_READ_BACK_ATTEMPTS:
                time.sleep(TRACK_READ_BACK_RETRY_DELAY_SECONDS)

        raise GooglePlayUploadError(
            f"Google Play track {track} did not include committed versionCode {version_code} "
            f"after {TRACK_READ_BACK_ATTEMPTS} read-back attempts."
        )

    def read_track_state(self, package_name: str, track: str) -> GooglePlayTrackState:
        edit_id = self.create_edit(package_name)
        try:
            return self.get_track(package_name, edit_id, track)
        finally:
            self.delete_edit(package_name, edit_id)

    def _request(
        self,
        method: str,
        url: str,
        *,
        json_body: Mapping[str, object] | None = None,
        data: object | None = None,
        headers: Mapping[str, str] | None = None,
        params: Mapping[str, str] | None = None,
    ) -> dict[str, object]:
        response = self._session.request(
            method,
            url,
            json=json_body,
            data=data,
            headers=headers,
            params=params,
            timeout=self._timeout_seconds,
        )
        self._raise_for_error(response)
        if not response.content:
            return {}

        body = response.json()
        if not isinstance(body, dict):
            raise GooglePlayUploadError("Google Play API response was not a JSON object.")

        return body

    @staticmethod
    def _raise_for_error(response: HttpResponse) -> None:
        if response.status_code < 400:
            return

        try:
            error_body = response.json()
        except ValueError:
            error_body = response.text

        raise GooglePlayUploadError(
            f"Google Play API request failed with HTTP {response.status_code}: {error_body}"
        )

    @staticmethod
    def _parse_service_account_json(service_account_json: str) -> dict[str, object]:
        try:
            service_account_info = json.loads(service_account_json)
        except json.JSONDecodeError as exception:
            raise GooglePlayUploadError("Service account JSON is not valid JSON.") from exception

        if not isinstance(service_account_info, dict):
            raise GooglePlayUploadError("Service account JSON root must be an object.")

        return service_account_info

    @staticmethod
    def _parse_track_state(response: Mapping[str, object], requested_track: str) -> GooglePlayTrackState:
        track = response.get("track")
        if not isinstance(track, str) or not track:
            track = requested_track

        releases_value = response.get("releases", [])
        if not isinstance(releases_value, list):
            raise GooglePlayUploadError("Google Play track response releases field was not a list.")

        releases: list[GooglePlayTrackRelease] = []
        for release_value in releases_value:
            if not isinstance(release_value, dict):
                raise GooglePlayUploadError("Google Play track response included a non-object release.")

            name_value = release_value.get("name")
            name = name_value if isinstance(name_value, str) and name_value else None

            status_value = release_value.get("status")
            status = status_value if isinstance(status_value, str) and status_value else None

            version_codes_value = release_value.get("versionCodes", [])
            if not isinstance(version_codes_value, list):
                raise GooglePlayUploadError("Google Play track release versionCodes field was not a list.")

            version_codes: list[int] = []
            for version_code_value in version_codes_value:
                if isinstance(version_code_value, int):
                    version_codes.append(version_code_value)
                    continue
                if isinstance(version_code_value, str) and version_code_value.isdecimal():
                    version_codes.append(int(version_code_value))
                    continue

                raise GooglePlayUploadError("Google Play track release included an invalid versionCode.")

            releases.append(
                GooglePlayTrackRelease(
                    name=name,
                    status=status,
                    version_codes=tuple(version_codes),
                )
            )

        return GooglePlayTrackState(
            track=track,
            releases=tuple(releases),
        )

    @staticmethod
    def _load_google_auth_dependencies() -> tuple[object, type[HttpSession]]:
        try:
            from google.auth.transport.requests import AuthorizedSession
            from google.oauth2 import service_account
        except ModuleNotFoundError as exception:
            raise GooglePlayUploadError(
                "Google Play upload dependencies are missing. "
                "Install them with: python3 -m pip install google-auth requests"
            ) from exception

        return service_account, AuthorizedSession

    @staticmethod
    def _application_url(package_name: str) -> str:
        return f"{ANDROID_PUBLISHER_API_BASE_URL}/{AndroidPublisherClient._quote_path(package_name)}"

    @staticmethod
    def _quote_path(value: str) -> str:
        return quote(value, safe="")


def main() -> int:
    logging.basicConfig(format="%(levelname)s: %(message)s", level=logging.INFO)
    options = parse_arguments()
    service_account_json = load_service_account_json(options)
    client = AndroidPublisherClient(service_account_json, options.timeout_seconds)

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
