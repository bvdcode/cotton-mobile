from __future__ import annotations

import logging

from android_layout_models import MeasureOptions, Rect, UiNode
from android_layout_selection import (
    group_rows_by_top,
    resolve_first_file_content_top,
    resolve_first_tile_name_row,
    resolve_tile_name_vertical_pitch,
    select_left_to_right_non_overlapping,
    select_nodes_by_slot_centers,
)


logger = logging.getLogger("measure-android-layout")


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
