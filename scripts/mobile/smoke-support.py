#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import uuid
from collections.abc import Sequence
from dataclasses import dataclass
from pathlib import Path
from urllib.parse import urlparse
from xml.etree import ElementTree


BOUNDS_PATTERN = re.compile(r"^\[(\d+),(\d+)\]\[(\d+),(\d+)\]$")
NODE_ATTRIBUTES = ("text", "content-desc", "hint")
ROW_ACTION_UPPER_TOLERANCE = 24
ROW_ACTION_MAX_VERTICAL_DISTANCE = 520


class SmokeSupportError(Exception):
    pass


@dataclass(frozen=True)
class Rect:
    left: int
    top: int
    right: int
    bottom: int

    @classmethod
    def parse(cls, value: str) -> Rect:
        match = BOUNDS_PATTERN.fullmatch(value)
        if match is None:
            raise SmokeSupportError(f"Invalid Android bounds: {value}")

        left, top, right, bottom = (int(group) for group in match.groups())
        return cls(left, top, right, bottom)

    @property
    def center(self) -> tuple[int, int]:
        return ((self.left + self.right) // 2, (self.top + self.bottom) // 2)


def main(argv: Sequence[str] | None = None) -> int:
    parser = create_argument_parser()
    args = parser.parse_args(argv)

    try:
        if args.command == "node-center":
            print(*find_node_center(args.xml, args.needle, args.mode, args.clickable))
        elif args.command == "row-point":
            print(*find_row_point(args.xml, args.needle))
        elif args.command == "editable-point":
            print(*find_editable_point(args.xml))
        elif args.command == "row-action-point":
            print(*find_row_action_point(args.xml, args.item, args.action))
        elif args.command == "instance-key":
            print(create_instance_key(args.instance))
        elif args.command == "cached-folder":
            folder_id, folder_name = find_cached_folder(args.cache, args.name)
            print(f"{folder_id}\t{folder_name}")
        elif args.command == "uuid":
            print(uuid.uuid4())
        else:
            parser.error(f"Unsupported command: {args.command}")
    except (OSError, KeyError, ValueError, SmokeSupportError) as exception:
        parser.exit(66, f"{exception}\n")

    return 0


def create_argument_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Shared data helpers for Cotton Android smoke tests.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    node_parser = subparsers.add_parser("node-center", help="Find the center of an Android UI node.")
    node_parser.add_argument("xml", type=Path)
    node_parser.add_argument("needle")
    node_parser.add_argument("--mode", choices=("exact", "contains"), default="contains")
    node_parser.add_argument("--clickable", action="store_true")

    row_parser = subparsers.add_parser("row-point", help="Find a safe tap point for a file row.")
    row_parser.add_argument("xml", type=Path)
    row_parser.add_argument("needle")

    editable_parser = subparsers.add_parser(
        "editable-point",
        help="Find the center of the active Android text field.",
    )
    editable_parser.add_argument("xml", type=Path)

    row_action_parser = subparsers.add_parser(
        "row-action-point",
        help="Find the action associated with an Android file row.",
    )
    row_action_parser.add_argument("xml", type=Path)
    row_action_parser.add_argument("item")
    row_action_parser.add_argument("action")

    instance_parser = subparsers.add_parser("instance-key", help="Create a normalized instance key.")
    instance_parser.add_argument("instance")

    folder_parser = subparsers.add_parser("cached-folder", help="Find a folder in a cached root listing.")
    folder_parser.add_argument("cache", type=Path)
    folder_parser.add_argument("name")

    subparsers.add_parser("uuid", help="Create a random transfer identifier.")
    return parser


def load_android_nodes(xml_path: Path) -> list[ElementTree.Element]:
    root = ElementTree.parse(xml_path).getroot()  # nosec B314: input is a local UIAutomator dump.
    return list(root.iter("node"))


def find_node_center(
    xml_path: Path,
    needle: str,
    mode: str,
    clickable: bool = False,
) -> tuple[int, int]:
    nodes = load_android_nodes(xml_path)
    eligibility = (
        (lambda node: node.attrib.get("clickable") == "true"),
        (lambda node: node.attrib.get("enabled") == "true"),
    ) if clickable else (lambda node: True,)
    for is_eligible in eligibility:
        for node in nodes:
            if not is_eligible(node):
                continue
            values = (node.attrib.get(attribute, "") for attribute in NODE_ATTRIBUTES)
            if any(matches(value, needle, mode) for value in values):
                bounds = node.attrib.get("bounds")
                if bounds:
                    return Rect.parse(bounds).center

    raise SmokeSupportError(f"Could not find Android UI node: {needle}")


def find_row_point(xml_path: Path, needle: str) -> tuple[int, int]:
    nodes = load_android_nodes(xml_path)
    target = next((node for node in nodes if node.attrib.get("text", "") == needle), None)
    if target is None:
        raise SmokeSupportError(f"Could not find Android row text: {needle}")

    target_bounds = Rect.parse(target.attrib["bounds"])
    target_x, target_y = target_bounds.center
    candidates: list[tuple[int, int, Rect]] = []
    for node in nodes:
        if node.attrib.get("long-clickable") != "true" and node.attrib.get("clickable") != "true":
            continue

        bounds = Rect.parse(node.attrib["bounds"])
        if bounds.top <= target_y <= bounds.bottom and bounds.left <= target_x <= bounds.right:
            candidates.append((bounds.bottom - bounds.top, bounds.right - bounds.left, bounds))

    if not candidates:
        return (target_x, target_y)

    _, _, row_bounds = min(candidates, key=lambda candidate: (candidate[0], candidate[1]))
    row_x = min(max(row_bounds.left + 88, row_bounds.left + 1), row_bounds.right - 1)
    return (row_x, (row_bounds.top + row_bounds.bottom) // 2)


def find_editable_point(xml_path: Path) -> tuple[int, int]:
    for node in load_android_nodes(xml_path):
        class_name = node.attrib.get("class", "")
        if "EditText" in class_name or node.attrib.get("focused") == "true":
            return Rect.parse(node.attrib["bounds"]).center

    raise SmokeSupportError("Could not find editable Android UI node.")


def find_row_action_point(
    xml_path: Path,
    item_name: str,
    action_text: str,
) -> tuple[int, int]:
    bounded_nodes = [
        (node, Rect.parse(node.attrib["bounds"]))
        for node in load_android_nodes(xml_path)
        if node.attrib.get("bounds")
    ]
    target_nodes = [
        (node, bounds)
        for node, bounds in bounded_nodes
        if any(value == item_name for value in node_values(node))
    ]
    if not target_nodes:
        target_nodes = [
            (node, bounds)
            for node, bounds in bounded_nodes
            if any(item_name in value for value in node_values(node))
        ]
    action_nodes = [
        (node, bounds)
        for node, bounds in bounded_nodes
        if node.attrib.get("enabled", "true") == "true"
        and any(value == action_text for value in node_values(node))
    ]

    matches: list[tuple[int, int, tuple[int, int]]] = []
    for _, target_bounds in target_nodes:
        target_x, target_y = target_bounds.center
        for _, action_bounds in action_nodes:
            action_x, action_y = action_bounds.center
            vertical_distance = abs(action_y - target_y)
            if action_y < target_bounds.top - ROW_ACTION_UPPER_TOLERANCE:
                continue
            if vertical_distance > ROW_ACTION_MAX_VERTICAL_DISTANCE:
                continue
            horizontal_distance = abs(action_x - target_x)
            matches.append((vertical_distance, horizontal_distance, (action_x, action_y)))

    if not matches:
        raise SmokeSupportError(f"Could not find {action_text} for row: {item_name}")

    return min(matches)[2]


def node_values(node: ElementTree.Element) -> tuple[str, ...]:
    return tuple(node.attrib.get(attribute, "") for attribute in NODE_ATTRIBUTES)


def matches(value: str, needle: str, mode: str) -> bool:
    if mode == "exact":
        return value == needle
    if mode == "contains":
        return needle in value
    raise SmokeSupportError(f"Unsupported node match mode: {mode}")


def create_instance_key(instance_uri: str) -> str:
    uri = urlparse(instance_uri)
    scheme = uri.scheme.lower()
    if scheme not in ("http", "https") or not uri.hostname:
        raise SmokeSupportError("Instance URI must include an HTTP or HTTPS scheme and host.")

    host = uri.hostname.lower()
    uses_default_port = (scheme == "http" and uri.port in (None, 80)) or (
        scheme == "https" and uri.port in (None, 443)
    )
    authority = host if uses_default_port else f"{host}:{uri.port}"
    path = "" if uri.path in ("", "/") else uri.path.rstrip("/")
    scope = f"{scheme}://{authority}{path}"
    return hashlib.sha256(scope.encode("utf-8")).hexdigest()


def find_cached_folder(cache_path: Path, folder_name: str) -> tuple[str, str]:
    with cache_path.open(encoding="utf-8") as cache_file:
        data = json.load(cache_file)
    if not isinstance(data, dict):
        raise SmokeSupportError("Cached root listing must contain a JSON object.")

    entries = data.get("entries", [])
    if not isinstance(entries, list):
        raise SmokeSupportError("Cached root listing entries must contain a JSON array.")

    for entry in entries:
        if not isinstance(entry, dict):
            continue
        if entry.get("name") == folder_name and entry.get("type") == 0:
            folder_id = entry.get("id")
            if isinstance(folder_id, str):
                return (folder_id, folder_name)

    raise SmokeSupportError(f"Destination folder not found in cached root listing: {folder_name}")


if __name__ == "__main__":
    sys.exit(main())
