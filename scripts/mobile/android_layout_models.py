from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path


BOUNDS_PATTERN = re.compile(r"^\\[(\\d+),(\\d+)\\]\\[(\\d+),(\\d+)\\]$")


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
