#!/usr/bin/env python3
from __future__ import annotations

import subprocess
import sys
import tempfile
import uuid
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SUPPORT_SCRIPT = REPOSITORY_ROOT / "scripts" / "mobile" / "smoke-support.py"
UI_XML = """<?xml version="1.0" encoding="UTF-8"?>
<hierarchy>
  <node text="Search" content-desc="" hint="" class="android.widget.EditText"
        focused="true" enabled="true" clickable="true" long-clickable="false"
        bounds="[10,20][210,80]" />
  <node text="fixture.txt" content-desc="" hint="" class="android.widget.TextView"
        focused="false" enabled="true" clickable="false" long-clickable="false"
        bounds="[40,100][200,160]" />
  <node text="" content-desc="fixture row" hint="" class="android.view.View"
        focused="false" enabled="true" clickable="true" long-clickable="true"
        bounds="[20,80][500,180]" />
  <node text="Restore" content-desc="" hint="" class="android.widget.Button"
        focused="false" enabled="true" clickable="true" long-clickable="false"
        bounds="[520,90][700,170]" />
</hierarchy>
"""


def run_support(*arguments: str) -> str:
    result = subprocess.run(
        [sys.executable, str(SUPPORT_SCRIPT), *arguments],
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def require_equal(actual: str, expected: str) -> None:
    if actual != expected:
        raise AssertionError(f"Expected {expected!r}, received {actual!r}.")


def main() -> int:
    with tempfile.TemporaryDirectory(prefix="cotton-smoke-support-") as temporary_directory:
        temporary_path = Path(temporary_directory)
        xml_path = temporary_path / "window.xml"
        xml_path.write_text(UI_XML, encoding="utf-8")
        cache_path = temporary_path / "root.json"
        cache_path.write_text(
            '{"entries":[{"id":"folder-id","name":"Fixture","type":0}]}',
            encoding="utf-8",
        )

        require_equal(run_support("node-center", str(xml_path), "Search", "--mode", "exact"), "110 50")
        require_equal(run_support("row-point", str(xml_path), "fixture.txt"), "108 130")
        require_equal(run_support("editable-point", str(xml_path)), "110 50")
        require_equal(
            run_support("row-action-point", str(xml_path), "fixture.txt", "Restore"),
            "610 130",
        )
        require_equal(
            run_support("instance-key", "HTTPS://Example.COM:443/path/"),
            "5faa4bf4918ff56562141cc328545ec8f7b6dd27470cbdf4a7487593b3e83738",
        )
        require_equal(run_support("cached-folder", str(cache_path), "Fixture"), "folder-id\tFixture")
        uuid.UUID(run_support("uuid"))

    return 0


if __name__ == "__main__":
    sys.exit(main())
