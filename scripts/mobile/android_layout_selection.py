from __future__ import annotations

import re

from android_layout_models import UiNode


DETAIL_TEXT_PATTERN = re.compile(
    r"^(Folder|On device|[0-9]+(?:\\.[0-9]+)?\\s+(?:B|KB|MB|GB)\\s+.+)$",
    re.IGNORECASE,
)


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
