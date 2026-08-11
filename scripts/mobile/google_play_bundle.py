from __future__ import annotations

import logging
import time
from collections.abc import Mapping
from dataclasses import dataclass
from pathlib import Path

from google_play_publisher import (
    ANDROID_PUBLISHER_UPLOAD_BASE_URL,
    AndroidPublisherClient,
    GooglePlayPublisherError,
)


TRACK_READ_BACK_ATTEMPTS = 6
TRACK_READ_BACK_RETRY_DELAY_SECONDS = 10
logger = logging.getLogger("upload-google-play")


class GooglePlayUploadError(GooglePlayPublisherError):
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


class GooglePlayBundleClient(AndroidPublisherClient):
    def __init__(self, service_account_json: str, timeout_seconds: int) -> None:
        super().__init__(service_account_json, timeout_seconds, GooglePlayUploadError, logger)

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
