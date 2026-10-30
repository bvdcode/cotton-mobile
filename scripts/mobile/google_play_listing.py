from __future__ import annotations

import logging
from dataclasses import dataclass
from pathlib import Path

from google_play_publisher import (
    ANDROID_PUBLISHER_UPLOAD_BASE_URL,
    AndroidPublisherClient,
    GooglePlayPublisherError,
)


logger = logging.getLogger("upload-google-play-listing")


class GooglePlayListingUploadError(GooglePlayPublisherError):
    pass


@dataclass(frozen=True)
class GooglePlayListingUploadOptions:
    package_name: str
    listing_dir: Path
    language: str
    title: str
    changes_not_sent_for_review: bool
    service_account_json_env: str
    service_account_json_file: Path | None
    timeout_seconds: int
    dry_run: bool


@dataclass(frozen=True)
class PngInfo:
    width: int
    height: int
    color_type: int
    file_size: int

    @property
    def color_description(self) -> str:
        if self.color_type == 2:
            return "rgb"
        if self.color_type == 6:
            return "rgba"
        return f"png-color-type-{self.color_type}"


@dataclass(frozen=True)
class StoreListing:
    title: str
    short_description: str
    full_description: str
    icon_path: Path
    feature_graphic_path: Path
    phone_screenshot_paths: tuple[Path, ...]


class GooglePlayListingClient(AndroidPublisherClient):
    def __init__(self, service_account_json: str, timeout_seconds: int) -> None:
        super().__init__(service_account_json, timeout_seconds, GooglePlayListingUploadError, logger)

    def update_listing(
        self,
        package_name: str,
        edit_id: str,
        language: str,
        listing: StoreListing,
    ) -> None:
        body: dict[str, object] = {
            "language": language,
            "title": listing.title,
            "shortDescription": listing.short_description,
            "fullDescription": listing.full_description,
        }
        logger.info("Updating Google Play listing text for %s.", language)
        self._request(
            "PUT",
            f"{self._application_url(package_name)}/edits/{self._quote_path(edit_id)}/listings/{self._quote_path(language)}",
            json_body=body,
        )

    def replace_images(
        self,
        package_name: str,
        edit_id: str,
        language: str,
        image_type: str,
        image_paths: tuple[Path, ...],
    ) -> None:
        logger.info("Deleting existing %s images for %s.", image_type, language)
        self._request(
            "DELETE",
            (
                f"{self._application_url(package_name)}/edits/{self._quote_path(edit_id)}"
                f"/listings/{self._quote_path(language)}/{self._quote_path(image_type)}"
            ),
        )

        for image_path in image_paths:
            self.upload_image(package_name, edit_id, language, image_type, image_path)

    def upload_image(
        self,
        package_name: str,
        edit_id: str,
        language: str,
        image_type: str,
        image_path: Path,
    ) -> None:
        upload_url = (
            f"{ANDROID_PUBLISHER_UPLOAD_BASE_URL}/"
            f"{self._quote_path(package_name)}/edits/{self._quote_path(edit_id)}"
            f"/listings/{self._quote_path(language)}/{self._quote_path(image_type)}"
        )
        logger.info("Uploading %s image %s.", image_type, image_path)
        with image_path.open("rb") as image_stream:
            self._request(
                "POST",
                upload_url,
                data=image_stream,
                headers={"Content-Type": "image/png"},
                params={"uploadType": "media"},
            )
