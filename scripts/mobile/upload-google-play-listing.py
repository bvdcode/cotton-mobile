#!/usr/bin/env python3
from __future__ import annotations

import argparse
import logging
import os
import struct
from pathlib import Path

from google_play_listing import (
    GooglePlayListingClient,
    GooglePlayListingUploadError,
    GooglePlayListingUploadOptions,
    PngInfo,
    StoreListing,
)


DEFAULT_LISTING_DIR = Path("store/google-play/default-listing")
DEFAULT_SERVICE_ACCOUNT_ENV = "GOOGLE_PLAY_SERVICE_ACCOUNT_JSON"
DEFAULT_TIMEOUT_SECONDS = 120
DEFAULT_TITLE = "Cotton Cloud"
MAX_ICON_BYTES = 1024 * 1024
MAX_SHORT_DESCRIPTION_LENGTH = 80
MAX_FULL_DESCRIPTION_LENGTH = 4000
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
logger = logging.getLogger("upload-google-play-listing")


def main() -> int:
    logging.basicConfig(format="%(levelname)s: %(message)s", level=logging.INFO)
    options = parse_arguments()
    listing = load_store_listing(options)
    validate_store_listing(listing)

    if options.dry_run:
        log_dry_run(options, listing)
        return 0

    service_account_json = load_service_account_json(options)
    client = GooglePlayListingClient(service_account_json, options.timeout_seconds)

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
    if path.is_symlink():
        raise GooglePlayListingUploadError(f"Required file must not be a symbolic link: {path}")
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
        resolve_required_file(path)
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
