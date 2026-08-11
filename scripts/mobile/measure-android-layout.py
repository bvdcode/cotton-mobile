#!/usr/bin/env python3
from __future__ import annotations

import argparse
import logging
import os
import tempfile
from pathlib import Path

from android_layout_capture import capture_layout, load_nodes
from android_layout_models import AndroidLayoutMeasureError, MeasureOptions
from android_layout_report import report_metrics


DEFAULT_PACKAGE_ID = "dev.cottoncloud.app"
DEFAULT_REMOTE_XML_PATH = "/sdcard/cotton-layout.xml"


def main() -> int:
    logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")
    options = parse_arguments()

    if not options.skip_capture:
        capture_layout(options)

    nodes = load_nodes(options.xml_path, options.package_id)
    if not nodes:
        raise AndroidLayoutMeasureError(f"No nodes found for package {options.package_id}.")

    report_metrics(nodes, options)
    return 0


def parse_arguments() -> MeasureOptions:
    parser = argparse.ArgumentParser(description="Measure visible Android layout bounds for Cotton Mobile.")
    parser.add_argument(
        "--serial",
        default=os.environ.get("COTTON_ADB_SERIAL"),
        help="ADB serial. Defaults to COTTON_ADB_SERIAL when set.",
    )
    parser.add_argument(
        "--package-id",
        default=os.environ.get("COTTON_ANDROID_PACKAGE_ID", DEFAULT_PACKAGE_ID),
        help="Android package id to measure.",
    )
    parser.add_argument(
        "--xml",
        default=Path(tempfile.gettempdir()) / "cotton-android-layout.xml",
        type=Path,
        help="Local UIAutomator XML path.",
    )
    parser.add_argument(
        "--screenshot",
        type=Path,
        help="Optional local screenshot path.",
    )
    parser.add_argument(
        "--skip-capture",
        action="store_true",
        help="Read the local XML file without calling adb.",
    )
    parser.add_argument(
        "--remote-xml",
        default=DEFAULT_REMOTE_XML_PATH,
        help="Remote device path used for UIAutomator XML dump.",
    )
    args = parser.parse_args()

    return MeasureOptions(
        serial=args.serial,
        package_id=args.package_id.strip(),
        xml_path=args.xml.expanduser().resolve(),
        screenshot_path=args.screenshot.expanduser().resolve() if args.screenshot is not None else None,
        skip_capture=args.skip_capture,
        remote_xml_path=args.remote_xml,
    )


if __name__ == "__main__":
    raise SystemExit(main())
