#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import sys
from collections.abc import Sequence
from pathlib import Path
from xml.etree import ElementTree


BOUNDS_PATTERN = re.compile(r"^\[(\d+),(\d+)\]\[(\d+),(\d+)\]$")
NODE_ATTRIBUTES = ("text", "content-desc", "hint")


class SmokeSupportError(Exception):
    pass


def main(argv: Sequence[str] | None = None) -> int:
    parser = create_argument_parser()
    arguments = parser.parse_args(argv)

    try:
        if arguments.command == "has-node":
            find_node(arguments.xml, arguments.needle, arguments.mode, clickable=False)
        elif arguments.command == "node-center":
            node = find_node(
                arguments.xml,
                arguments.needle,
                arguments.mode,
                arguments.clickable,
            )
            print(*parse_center(node.attrib.get("bounds", "")))
        else:
            parser.error(f"Unsupported command: {arguments.command}")
    except (OSError, ElementTree.ParseError, SmokeSupportError) as exception:
        parser.exit(66, f"{exception}\n")

    return 0


def create_argument_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Inspect Android UIAutomator XML.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    exists_parser = subparsers.add_parser("has-node")
    configure_node_arguments(exists_parser)

    center_parser = subparsers.add_parser("node-center")
    configure_node_arguments(center_parser)
    center_parser.add_argument("--clickable", action="store_true")
    return parser


def configure_node_arguments(parser: argparse.ArgumentParser) -> None:
    parser.add_argument("xml", type=Path)
    parser.add_argument("needle")
    parser.add_argument("--mode", choices=("exact", "contains"), default="exact")


def find_node(
    xml_path: Path,
    needle: str,
    mode: str,
    clickable: bool,
) -> ElementTree.Element:
    root = ElementTree.parse(xml_path).getroot()  # nosec B314: local UIAutomator output.
    for node in root.iter("node"):
        if clickable and node.attrib.get("clickable") != "true":
            continue

        values = (node.attrib.get(attribute, "") for attribute in NODE_ATTRIBUTES)
        if any(matches(value, needle, mode) for value in values):
            return node

    raise SmokeSupportError(f"Could not find Android UI node: {needle}")


def matches(value: str, needle: str, mode: str) -> bool:
    if mode == "exact":
        return value == needle
    if mode == "contains":
        return needle in value
    raise SmokeSupportError(f"Unsupported node match mode: {mode}")


def parse_center(bounds: str) -> tuple[int, int]:
    match = BOUNDS_PATTERN.fullmatch(bounds)
    if match is None:
        raise SmokeSupportError(f"Invalid Android bounds: {bounds}")

    left, top, right, bottom = (int(group) for group in match.groups())
    return ((left + right) // 2, (top + bottom) // 2)


if __name__ == "__main__":
    sys.exit(main())
