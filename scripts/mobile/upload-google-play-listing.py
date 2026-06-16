#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import logging
import os
import struct
from dataclasses import dataclass
from pathlib import Path
from typing import Mapping, Protocol
from urllib.parse import quote


ANDROID_PUBLISHER_SCOPE = "https://www.googleapis.com/auth/androidpublisher"
ANDROID_PUBLISHER_API_BASE_URL = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications"
ANDROID_PUBLISHER_UPLOAD_BASE_URL = "https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications"
DEFAULT_LISTING_DIR = Path("store/google-play/default-listing")
DEFAULT_SERVICE_ACCOUNT_ENV = "GOOGLE_PLAY_SERVICE_ACCOUNT_JSON"
DEFAULT_TIMEOUT_SECONDS = 120
DEFAULT_TITLE = "Cotton Cloud"
MAX_ICON_BYTES = 1024 * 1024
MAX_SHORT_DESCRIPTION_LENGTH = 80
MAX_FULL_DESCRIPTION_LENGTH = 4000
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"

logger = logging.getLogger("upload-google-play-listing")


class GooglePlayListingUploadError(Exception):
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
        response = self._request("POST", f"{self._application_url(package_name)}/edits", json_body={})
        edit_id = response.get("id")
        if not isinstance(edit_id, str) or not edit_id:
            raise GooglePlayListingUploadError("Google Play edit response did not include an edit id.")

        logger.info("Created Google Play edit %s.", edit_id)
        return edit_id

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
            raise GooglePlayListingUploadError("Google Play API response was not a JSON object.")

        return body

    @staticmethod
    def _raise_for_error(response: HttpResponse) -> None:
        if response.status_code < 400:
            return

        try:
            error_body = response.json()
        except ValueError:
            error_body = response.text

        raise GooglePlayListingUploadError(
            f"Google Play API request failed with HTTP {response.status_code}: {error_body}"
        )

    @staticmethod
    def _parse_service_account_json(service_account_json: str) -> dict[str, object]:
        try:
            service_account_info = json.loads(service_account_json)
        except json.JSONDecodeError as exception:
            raise GooglePlayListingUploadError("Service account JSON is not valid JSON.") from exception

        if not isinstance(service_account_info, dict):
            raise GooglePlayListingUploadError("Service account JSON root must be an object.")

        return service_account_info

    @staticmethod
    def _load_google_auth_dependencies() -> tuple[object, type[HttpSession]]:
        try:
            from google.auth.transport.requests import AuthorizedSession
            from google.oauth2 import service_account
        except ModuleNotFoundError as exception:
            raise GooglePlayListingUploadError(
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
    listing = load_store_listing(options)
    validate_store_listing(listing)

    if options.dry_run:
        log_dry_run(options, listing)
        return 0

    service_account_json = load_service_account_json(options)
    client = AndroidPublisherClient(service_account_json, options.timeout_seconds)

    edit_id: str | None = None
    try:
        edit_id = client.create_edit(options.package_name)
        client.update_listing(options.package_name, edit_id, options.language, listing)
        client.replace_images(options.package_name, edit_id, options.language, "icon", (listing.icon_path,))
        client.replace_images(
            options.package_name,
            edit_id,
            options.language,
            "featureGraphic",
            (listing.feature_graphic_path,),
        )
        client.replace_images(
            options.package_name,
            edit_id,
            options.language,
            "phoneScreenshots",
            listing.phone_screenshot_paths,
        )
        client.commit_edit(options.package_name, edit_id, options.changes_not_sent_for_review)
    except Exception:
        if edit_id:
            client.delete_edit(options.package_name, edit_id)
        raise

    logger.info("Google Play listing upload completed.")
    return 0


def parse_arguments() -> GooglePlayListingUploadOptions:
    parser = argparse.ArgumentParser(description="Upload Google Play default listing text and images.")
    parser.add_argument("--package-name", required=True, help="Android application id, for example dev.cottoncloud.app.")
    parser.add_argument(
        "--listing-dir",
        type=Path,
        default=DEFAULT_LISTING_DIR,
        help="Directory containing default listing text and graphics.",
    )
    parser.add_argument("--language", default="en-US", help="Google Play listing language tag.")
    parser.add_argument("--title", default=DEFAULT_TITLE, help="Google Play listing title.")
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
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Validate local listing files and print planned Google Play operations without network calls.",
    )
    args = parser.parse_args()

    listing_dir = args.listing_dir.expanduser().resolve()
    if not listing_dir.is_dir():
        raise GooglePlayListingUploadError(f"Listing directory does not exist: {listing_dir}")

    if args.timeout_seconds <= 0:
        raise GooglePlayListingUploadError("--timeout-seconds must be a positive integer.")

    return GooglePlayListingUploadOptions(
        package_name=args.package_name,
        listing_dir=listing_dir,
        language=args.language,
        title=args.title.strip(),
        changes_not_sent_for_review=args.changes_not_sent_for_review,
        service_account_json_env=args.service_account_json_env,
        service_account_json_file=args.service_account_json_file,
        timeout_seconds=args.timeout_seconds,
        dry_run=args.dry_run,
    )


def load_store_listing(options: GooglePlayListingUploadOptions) -> StoreListing:
    graphics_dir = options.listing_dir / "graphics"
    phone_screenshots_dir = graphics_dir / "phone-screenshots"
    phone_screenshot_paths = tuple(sorted(phone_screenshots_dir.glob("*.png")))

    return StoreListing(
        title=options.title,
        short_description=read_trimmed_text(options.listing_dir / "short-description.txt"),
        full_description=read_trimmed_text(options.listing_dir / "full-description.txt"),
        icon_path=resolve_required_file(graphics_dir / "icon.png"),
        feature_graphic_path=resolve_required_file(graphics_dir / "feature-graphic.png"),
        phone_screenshot_paths=phone_screenshot_paths,
    )


def read_trimmed_text(path: Path) -> str:
    return resolve_required_file(path).read_text(encoding="utf-8").strip()


def resolve_required_file(path: Path) -> Path:
    if not path.is_file():
        raise GooglePlayListingUploadError(f"Required file does not exist: {path}")

    return path


def validate_store_listing(listing: StoreListing) -> None:
    if not listing.title:
        raise GooglePlayListingUploadError("Listing title is required.")
    if not listing.short_description:
        raise GooglePlayListingUploadError("Short description is required.")
    if len(listing.short_description) > MAX_SHORT_DESCRIPTION_LENGTH:
        raise GooglePlayListingUploadError(
            f"Short description is {len(listing.short_description)} characters; "
            f"maximum is {MAX_SHORT_DESCRIPTION_LENGTH}."
        )
    if not listing.full_description:
        raise GooglePlayListingUploadError("Full description is required.")
    if len(listing.full_description) > MAX_FULL_DESCRIPTION_LENGTH:
        raise GooglePlayListingUploadError(
            f"Full description is {len(listing.full_description)} characters; "
            f"maximum is {MAX_FULL_DESCRIPTION_LENGTH}."
        )

    validate_icon_png(listing.icon_path)
    validate_feature_graphic_png(listing.feature_graphic_path)
    validate_phone_screenshots(listing.phone_screenshot_paths)


def validate_icon_png(path: Path) -> None:
    info = read_png_info(path)
    if info.width != 512 or info.height != 512:
        raise GooglePlayListingUploadError(f"App icon must be 512x512px: {path}")
    if info.file_size > MAX_ICON_BYTES:
        raise GooglePlayListingUploadError(f"App icon must be at most 1024KB: {path}")
    if info.color_type != 6:
        raise GooglePlayListingUploadError(f"App icon must be 32-bit RGBA PNG: {path}")


def validate_feature_graphic_png(path: Path) -> None:
    info = read_png_info(path)
    if info.width != 1024 or info.height != 500:
        raise GooglePlayListingUploadError(f"Feature graphic must be 1024x500px: {path}")
    if info.color_type != 2:
        raise GooglePlayListingUploadError(f"Feature graphic must be 24-bit RGB PNG without alpha: {path}")


def validate_phone_screenshots(paths: tuple[Path, ...]) -> None:
    if len(paths) < 2:
        raise GooglePlayListingUploadError("At least two phone screenshots are required.")

    for path in paths:
        info = read_png_info(path)
        shorter_side = min(info.width, info.height)
        longer_side = max(info.width, info.height)
        if shorter_side < 320:
            raise GooglePlayListingUploadError(f"Screenshot minimum dimension must be at least 320px: {path}")
        if longer_side > 3840:
            raise GooglePlayListingUploadError(f"Screenshot maximum dimension must be at most 3840px: {path}")
        if longer_side > shorter_side * 2:
            raise GooglePlayListingUploadError(f"Screenshot long side cannot exceed twice the short side: {path}")
        if info.color_type != 2:
            raise GooglePlayListingUploadError(f"Screenshot must be 24-bit RGB PNG without alpha: {path}")


def read_png_info(path: Path) -> PngInfo:
    with path.open("rb") as png_file:
        header = png_file.read(33)

    if len(header) < 33 or not header.startswith(PNG_SIGNATURE):
        raise GooglePlayListingUploadError(f"File is not a PNG image: {path}")
    if header[12:16] != b"IHDR":
        raise GooglePlayListingUploadError(f"PNG image does not start with an IHDR chunk: {path}")

    width, height = struct.unpack(">II", header[16:24])
    color_type = header[25]
    return PngInfo(
        width=width,
        height=height,
        color_type=color_type,
        file_size=path.stat().st_size,
    )


def log_dry_run(options: GooglePlayListingUploadOptions, listing: StoreListing) -> None:
    logger.info("Dry run succeeded for %s %s.", options.package_name, options.language)
    logger.info("Title: %s", listing.title)
    logger.info("Short description: %s", listing.short_description)
    logger.info("Full description characters: %s", len(listing.full_description))
    log_image("icon", listing.icon_path)
    log_image("featureGraphic", listing.feature_graphic_path)
    for path in listing.phone_screenshot_paths:
        log_image("phoneScreenshots", path)


def log_image(image_type: str, path: Path) -> None:
    info = read_png_info(path)
    logger.info(
        "%s: %sx%s %s %s bytes %s",
        image_type,
        info.width,
        info.height,
        info.color_description,
        info.file_size,
        path,
    )


def load_service_account_json(options: GooglePlayListingUploadOptions) -> str:
    if options.service_account_json_file:
        return options.service_account_json_file.expanduser().read_text(encoding="utf-8")

    service_account_json = os.environ.get(options.service_account_json_env)
    if not service_account_json:
        raise GooglePlayListingUploadError(
            f"{options.service_account_json_env} environment variable is required."
        )

    return service_account_json


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except GooglePlayListingUploadError as exception:
        logger.error("%s", exception)
        raise SystemExit(1)
