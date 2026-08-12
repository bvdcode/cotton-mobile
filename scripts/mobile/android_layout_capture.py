from __future__ import annotations

import logging
import subprocess
from pathlib import Path
from xml.etree import ElementTree

from android_layout_models import AndroidLayoutMeasureError, MeasureOptions, Rect, UiNode


logger = logging.getLogger("measure-android-layout")


def capture_layout(options: MeasureOptions) -> None:
    options.xml_path.parent.mkdir(parents=True, exist_ok=True)
    try:
        run_adb(options.serial, "shell", "uiautomator", "dump", options.remote_xml_path)
        run_adb(options.serial, "pull", options.remote_xml_path, str(options.xml_path))
    finally:
        remove_remote_layout(options.serial, options.remote_xml_path)
    logger.info("Pulled UIAutomator XML to %s.", options.xml_path)

    if options.screenshot_path is None:
        return

    options.screenshot_path.parent.mkdir(parents=True, exist_ok=True)
    screenshot = run_adb_binary(options.serial, "exec-out", "screencap", "-p")
    options.screenshot_path.write_bytes(screenshot)
    logger.info("Captured screenshot to %s.", options.screenshot_path)


def remove_remote_layout(serial: str | None, remote_xml_path: str) -> None:
    try:
        run_adb(serial, "shell", "rm", "-f", remote_xml_path)
    except AndroidLayoutMeasureError as exception:
        logger.warning("Could not remove remote UIAutomator XML: %s", exception)


def run_adb(serial: str | None, *arguments: str) -> str:
    command = create_adb_command(serial, arguments)
    completed = subprocess.run(command, check=False, capture_output=True, text=True)
    if completed.returncode != 0:
        raise AndroidLayoutMeasureError(completed.stderr.strip() or completed.stdout.strip())

    return completed.stdout.strip()


def run_adb_binary(serial: str | None, *arguments: str) -> bytes:
    command = create_adb_command(serial, arguments)
    completed = subprocess.run(command, check=False, capture_output=True)
    if completed.returncode != 0:
        stderr = completed.stderr.decode("utf-8", errors="replace").strip()
        stdout = completed.stdout.decode("utf-8", errors="replace").strip()
        raise AndroidLayoutMeasureError(stderr or stdout)

    return completed.stdout


def create_adb_command(serial: str | None, arguments: tuple[str, ...]) -> list[str]:
    command = ["adb"]
    if serial:
        command.extend(("-s", serial))

    command.extend(arguments)
    return command


def load_nodes(xml_path: Path, package_id: str) -> list[UiNode]:
    document = ElementTree.parse(xml_path)  # nosec B314: input is a local UIAutomator dump.
    nodes: list[UiNode] = []

    for element in document.getroot().iter("node"):
        package_name = element.attrib.get("package", "")
        if package_name and package_name != package_id:
            continue

        bounds = element.attrib.get("bounds")
        if not bounds:
            continue

        nodes.append(
            UiNode(
                text=element.attrib.get("text", ""),
                content_description=element.attrib.get("content-desc", ""),
                class_name=element.attrib.get("class", ""),
                package_name=package_name,
                rect=Rect.parse(bounds),
            )
        )

    return nodes
