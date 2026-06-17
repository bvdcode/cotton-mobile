#!/usr/bin/env python3
from __future__ import annotations

import argparse
import logging
import os
import re
import subprocess
from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree


DEFAULT_PACKAGE_ID = "dev.cottoncloud.app"
DEFAULT_REMOTE_XML_PATH = "/sdcard/cotton-layout.xml"
DETAIL_TEXT_PATTERN = re.compile(
    r"^(Folder|On device|[0-9]+(?:\.[0-9]+)?\s+(?:B|KB|MB|GB)\s+.+)$",
    re.IGNORECASE,
)
BOUNDS_PATTERN = re.compile(r"^\[(\d+),(\d+)\]\[(\d+),(\d+)\]$")

logger = logging.getLogger("measure-android-layout")


class AndroidLayoutMeasureError(Exception):
    pass


@dataclass(frozen=True)
class MeasureOptions:
    serial: str | None
    package_id: str
    xml_path: Path
    screenshot_path: Path | None
    skip_capture: bool
    remote_xml_path: str


@dataclass(frozen=True)
class Rect:
    left: int
    top: int
    right: int
    bottom: int

    @property
    def width(self) -> int:
        return self.right - self.left

    @property
    def height(self) -> int:
        return self.bottom - self.top

    @classmethod
    def parse(cls, value: str) -> Rect:
        match = BOUNDS_PATTERN.match(value)
        if match is None:
            raise AndroidLayoutMeasureError(f"Invalid bounds value: {value}")

        left, top, right, bottom = (int(group) for group in match.groups())
        return cls(left, top, right, bottom)

    def format(self) -> str:
        return f"[{self.left},{self.top}][{self.right},{self.bottom}]"


@dataclass(frozen=True)
class UiNode:
    text: str
    content_description: str
    class_name: str
    package_name: str
    rect: Rect


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
        default="/tmp/cotton-android-layout.xml",
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


def capture_layout(options: MeasureOptions) -> None:
    options.xml_path.parent.mkdir(parents=True, exist_ok=True)
    run_adb(options.serial, "shell", "uiautomator", "dump", options.remote_xml_path)
    run_adb(options.serial, "pull", options.remote_xml_path, str(options.xml_path))
    logger.info("Pulled UIAutomator XML to %s.", options.xml_path)

    if options.screenshot_path is None:
        return

    options.screenshot_path.parent.mkdir(parents=True, exist_ok=True)
    screenshot = run_adb_binary(options.serial, "exec-out", "screencap", "-p")
    options.screenshot_path.write_bytes(screenshot)
    logger.info("Captured screenshot to %s.", options.screenshot_path)


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
    document = ElementTree.parse(xml_path)
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


def report_metrics(nodes: list[UiNode], options: MeasureOptions) -> None:
    screen = resolve_screen_rect(nodes)
    logger.info("Screen: %sx%s px.", screen.width, screen.height)

    header_nodes = find_header_nodes(nodes)
    for name, node in header_nodes:
        logger.info("%s: %s %sx%s text=%r.", name, node.rect.format(), node.rect.width, node.rect.height, node.text)

    toolbar_nodes = find_toolbar_nodes(nodes)
    if toolbar_nodes:
        toolbar_bottom = max(node.rect.bottom for node in toolbar_nodes)
        toolbar_right = max(node.rect.right for node in toolbar_nodes)
        toolbar_widths = "/".join(str(node.rect.width) for node in toolbar_nodes)
        logger.info(
            "Toolbar: bottom=%s px, control widths=%s px, right remainder=%s px.",
            toolbar_bottom,
            toolbar_widths,
            screen.right - toolbar_right,
        )
    else:
        toolbar_bottom = 0
        logger.info("Toolbar: no Search/Sort/View controls found.")

    first_content_top = resolve_first_file_content_top(nodes, toolbar_bottom)
    if first_content_top is not None:
        logger.info(
            "File content start: y=%s px, toolbar gap=%s px, %.1f%% of screen height.",
            first_content_top,
            first_content_top - toolbar_bottom,
            first_content_top / screen.height * 100,
        )

    tile_slots = resolve_first_tile_slot_row(nodes, toolbar_bottom, screen)
    if tile_slots:
        tile_grid_top = min(node.rect.top for node in tile_slots)
        logger.info(
            "Tile grid start: y=%s px, toolbar gap=%s px, %.1f%% of screen height.",
            tile_grid_top,
            tile_grid_top - toolbar_bottom,
            tile_grid_top / screen.height * 100,
        )
        report_horizontal_row("Tile slots", tile_slots, screen)

        tile_cards = resolve_nested_tile_row(nodes, tile_slots, 0.90, 1.00, 0.80, 1.00)
        if tile_cards:
            report_horizontal_row("Tile cards", tile_cards, screen)

        tile_content = resolve_nested_tile_row(nodes, tile_slots, 0.75, 0.95, 0.70, 0.95)
        if tile_content:
            report_horizontal_row("Tile content", tile_content, screen)

        tile_images = resolve_first_tile_image_row(nodes, tile_slots)
        if tile_images:
            report_horizontal_row("Tile images", tile_images, screen)

    first_row_names = resolve_first_tile_name_row(nodes, toolbar_bottom)
    if first_row_names:
        report_horizontal_row("Tile text", first_row_names, screen)

    pitch = resolve_tile_name_vertical_pitch(nodes, toolbar_bottom)
    if pitch is not None:
        logger.info("Tile name row vertical pitch: %s px.", pitch)

    if options.screenshot_path is not None:
        logger.info("Screenshot evidence: %s.", options.screenshot_path)
    logger.info("XML evidence: %s.", options.xml_path)


def resolve_screen_rect(nodes: list[UiNode]) -> Rect:
    return max(nodes, key=lambda node: node.rect.width * node.rect.height).rect


def find_header_nodes(nodes: list[UiNode]) -> list[tuple[str, UiNode]]:
    visible_text_nodes = [
        node
        for node in nodes
        if node.text and node.class_name.endswith("TextView") and node.rect.height > 0
    ]
    visible_text_nodes.sort(key=lambda node: (node.rect.top, node.rect.left))
    return [(f"Header text {index + 1}", node) for index, node in enumerate(visible_text_nodes[:2])]


def find_toolbar_nodes(nodes: list[UiNode]) -> list[UiNode]:
    descriptions = {"Search files", "Close file search", "Clear file search", "Sort files", "Change file view"}
    toolbar_nodes = [
        node
        for node in nodes
        if node.class_name.endswith("Button") and node.content_description in descriptions
    ]
    toolbar_nodes.sort(key=lambda node: node.rect.left)
    return toolbar_nodes


def report_horizontal_row(name: str, row: list[UiNode], screen: Rect) -> None:
    margins = (
        row[0].rect.left,
        screen.right - row[-1].rect.right,
    )
    gaps = [
        second.rect.left - first.rect.right
        for first, second in zip(row, row[1:])
    ]
    widths = [node.rect.width for node in row]
    heights = [node.rect.height for node in row]
    logger.info(
        "%s: %s columns, widths=%s px, heights=%s px, margins=%s/%s px, gaps=%s px.",
        name,
        len(row),
        "/".join(str(width) for width in widths),
        "/".join(str(height) for height in heights),
        margins[0],
        margins[1],
        "/".join(str(gap) for gap in gaps) if gaps else "n/a",
    )


def resolve_first_tile_slot_row(nodes: list[UiNode], toolbar_bottom: int, screen: Rect) -> list[UiNode]:
    candidates = [
        node
        for node in nodes
        if node.class_name.endswith("ViewGroup")
        and not node.text
        and not node.content_description
        and node.rect.top > toolbar_bottom
        and node.rect.width > screen.width * 0.12
        and node.rect.width < screen.width * 0.7
        and node.rect.height > screen.height * 0.08
        and node.rect.height < screen.height * 0.35
    ]

    for row in group_rows_by_top(candidates, tolerance=4):
        non_overlapping = select_left_to_right_non_overlapping(row)
        if len(non_overlapping) >= 2:
            return non_overlapping

    return []


def resolve_nested_tile_row(
    nodes: list[UiNode],
    tile_slots: list[UiNode],
    min_width_ratio: float,
    max_width_ratio: float,
    min_height_ratio: float,
    max_height_ratio: float,
) -> list[UiNode]:
    if not tile_slots:
        return []

    average_slot_width = sum(node.rect.width for node in tile_slots) / len(tile_slots)
    average_slot_height = sum(node.rect.height for node in tile_slots) / len(tile_slots)
    slot_top = min(node.rect.top for node in tile_slots)
    slot_bottom = max(node.rect.bottom for node in tile_slots)
    slot_centers = [(node.rect.left + node.rect.right) / 2 for node in tile_slots]
    slot_rects = {
        (node.rect.left, node.rect.top, node.rect.right, node.rect.bottom)
        for node in tile_slots
    }

    candidates = [
        node
        for node in nodes
        if node.class_name.endswith("ViewGroup")
        and not node.text
        and not node.content_description
        and (node.rect.left, node.rect.top, node.rect.right, node.rect.bottom) not in slot_rects
        and node.rect.top >= slot_top
        and node.rect.bottom <= slot_bottom
        and average_slot_width * min_width_ratio <= node.rect.width <= average_slot_width * max_width_ratio
        and average_slot_height * min_height_ratio <= node.rect.height <= average_slot_height * max_height_ratio
    ]

    for row in group_rows_by_top(candidates, tolerance=4):
        matching = select_nodes_by_slot_centers(row, slot_centers)
        if len(matching) == len(tile_slots):
            return matching

    return []


def resolve_first_tile_image_row(nodes: list[UiNode], tile_slots: list[UiNode]) -> list[UiNode]:
    if not tile_slots:
        return []

    slot_top = min(node.rect.top for node in tile_slots)
    slot_bottom = max(node.rect.bottom for node in tile_slots)
    slot_centers = [(node.rect.left + node.rect.right) / 2 for node in tile_slots]
    average_slot_width = sum(node.rect.width for node in tile_slots) / len(tile_slots)

    candidates = [
        node
        for node in nodes
        if node.class_name.endswith("ImageView")
        and node.rect.top >= slot_top
        and node.rect.bottom <= slot_bottom
        and node.rect.width >= average_slot_width * 0.70
        and node.rect.width <= average_slot_width
    ]

    for row in group_rows_by_top(candidates, tolerance=6):
        matching = select_nodes_by_slot_centers(row, slot_centers)
        if len(matching) >= 2:
            return matching

    return []


def group_rows_by_top(nodes: list[UiNode], tolerance: int) -> list[list[UiNode]]:
    rows: list[list[UiNode]] = []
    for node in sorted(nodes, key=lambda item: (item.rect.top, item.rect.left)):
        for row in rows:
            if abs(row[0].rect.top - node.rect.top) <= tolerance:
                row.append(node)
                break
        else:
            rows.append([node])

    for row in rows:
        row.sort(key=lambda item: item.rect.left)

    rows.sort(key=lambda row: (row[0].rect.top, row[0].rect.left))
    return rows


def select_left_to_right_non_overlapping(nodes: list[UiNode]) -> list[UiNode]:
    selected: list[UiNode] = []
    for node in sorted(nodes, key=lambda item: (item.rect.left, -item.rect.width)):
        if selected and node.rect.left < selected[-1].rect.right:
            continue

        selected.append(node)

    return selected


def select_nodes_by_slot_centers(nodes: list[UiNode], slot_centers: list[float]) -> list[UiNode]:
    selected: list[UiNode] = []
    used_indexes: set[int] = set()

    for center in slot_centers:
        nearest_index: int | None = None
        nearest_distance: float | None = None
        for index, node in enumerate(nodes):
            if index in used_indexes:
                continue

            if not (node.rect.left <= center <= node.rect.right):
                continue

            node_center = (node.rect.left + node.rect.right) / 2
            distance = abs(node_center - center)
            if nearest_distance is None or distance < nearest_distance:
                nearest_distance = distance
                nearest_index = index

        if nearest_index is None:
            continue

        used_indexes.add(nearest_index)
        selected.append(nodes[nearest_index])

    selected.sort(key=lambda item: item.rect.left)
    return selected


def resolve_first_file_content_top(nodes: list[UiNode], toolbar_bottom: int) -> int | None:
    content_nodes = [
        node
        for node in nodes
        if node.rect.top > toolbar_bottom
        and (node.text or node.content_description)
        and not is_toolbar_or_header_node(node)
    ]
    if not content_nodes:
        return None

    return min(node.rect.top for node in content_nodes)


def resolve_first_tile_name_row(nodes: list[UiNode], toolbar_bottom: int) -> list[UiNode]:
    names = resolve_tile_name_nodes(nodes, toolbar_bottom)
    if not names:
        return []

    first_top = min(node.rect.top for node in names)
    row = [node for node in names if abs(node.rect.top - first_top) <= 8]
    row.sort(key=lambda node: node.rect.left)
    return row


def resolve_tile_name_vertical_pitch(nodes: list[UiNode], toolbar_bottom: int) -> int | None:
    names = resolve_tile_name_nodes(nodes, toolbar_bottom)
    row_tops: list[int] = []
    for node in names:
        if all(abs(node.rect.top - existing) > 8 for existing in row_tops):
            row_tops.append(node.rect.top)

    row_tops.sort()
    if len(row_tops) < 2:
        return None

    return row_tops[1] - row_tops[0]


def resolve_tile_name_nodes(nodes: list[UiNode], toolbar_bottom: int) -> list[UiNode]:
    tile_names = [
        node
        for node in nodes
        if node.text
        and node.class_name.endswith("TextView")
        and node.rect.top > toolbar_bottom
        and not is_toolbar_or_header_node(node)
        and not is_tile_detail_text(node.text)
        and node.text != "DIR"
    ]
    tile_names.sort(key=lambda node: (node.rect.top, node.rect.left))
    return tile_names


def is_toolbar_or_header_node(node: UiNode) -> bool:
    return node.content_description in {
        "Up",
        "Refresh",
        "Account",
        "Search files",
        "Close file search",
        "Clear file search",
        "Sort files",
        "Change file view",
    }


def is_tile_detail_text(text: str) -> bool:
    return DETAIL_TEXT_PATTERN.match(text.strip()) is not None


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AndroidLayoutMeasureError as error:
        logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")
        logger.error("%s", error)
        raise SystemExit(1) from error
