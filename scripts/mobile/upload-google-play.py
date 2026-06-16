#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import logging
import os
from dataclasses import dataclass
from pathlib import Path
from typing import Mapping, Protocol
from urllib.parse import quote


ANDROID_PUBLISHER_SCOPE = "https://www.googleapis.com/auth/androidpublisher"
ANDROID_PUBLISHER_API_BASE_URL = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications"
ANDROID_PUBLISHER_UPLOAD_BASE_URL = "https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications"
DEFAULT_SERVICE_ACCOUNT_ENV = "GOOGLE_PLAY_SERVICE_ACCOUNT_JSON"
DEFAULT_TIMEOUT_SECONDS = 120

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
    changes_not_sent_for_review: bool
    service_account_json_env: str
    service_account_json_file: Path | None
    timeout_seconds: int


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
    ) -> None:
        release: dict[str, object] = {
            "versionCodes": [str(version_code)],
            "status": release_status,
        }
        if release_name:
            release["name"] = release_name

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
        logger.info("Deleting failed Google Play edit %s.", edit_id)
        response = self._session.request(
            "DELETE",
            f"{self._application_url(package_name)}/edits/{self._quote_path(edit_id)}",
            timeout=self._timeout_seconds,
        )
        if response.status_code >= 400:
            logger.warning("Could not delete Google Play edit %s: %s", edit_id, response.text)

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
        )
        client.commit_edit(
            options.package_name,
            edit_id,
            options.changes_not_sent_for_review,
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

    return GooglePlayUploadOptions(
        package_name=args.package_name,
        bundle_path=bundle_path,
        track=args.track,
        release_status=args.release_status,
        release_name=args.release_name,
        changes_not_sent_for_review=args.changes_not_sent_for_review,
        service_account_json_env=args.service_account_json_env,
        service_account_json_file=args.service_account_json_file,
        timeout_seconds=args.timeout_seconds,
    )


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
