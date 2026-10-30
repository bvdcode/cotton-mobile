#!/usr/bin/env python3
from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SUPPORT_SCRIPT = REPOSITORY_ROOT / "scripts" / "mobile" / "smoke-support.py"
UI_XML = """<?xml version="1.0" encoding="UTF-8"?>
<hierarchy>
  <node text="Sync" content-desc="" hint="" class="android.widget.TextView"
        enabled="true" clickable="false" bounds="[10,20][210,80]" />
  <node text="" content-desc="Refresh sync folders" hint="" class="android.widget.Button"
        enabled="true" clickable="true" bounds="[220,20][320,80]" />
  <node text="2 folders set to sync" content-desc="" hint="" class="android.widget.TextView"
        enabled="true" clickable="false" bounds="[10,100][320,160]" />
</hierarchy>
"""


def run_support(*arguments: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(SUPPORT_SCRIPT), *arguments],
        check=check,
        capture_output=True,
        text=True,
    )


def main() -> int:
    with tempfile.TemporaryDirectory(prefix="cotton-smoke-support-") as temporary_directory:
        xml_path = Path(temporary_directory) / "window.xml"
        xml_path.write_text(UI_XML, encoding="utf-8")

        center = run_support(
            "node-center",
            str(xml_path),
            "Refresh sync folders",
            "--clickable",
        )
        if center.stdout.strip() != "270 50":
            raise AssertionError(f"Unexpected center: {center.stdout!r}")

        run_support("has-node", str(xml_path), "2 folders set to sync")
        partial = run_support("has-node", str(xml_path), "2 folder", check=False)
        if partial.returncode == 0:
            raise AssertionError("Exact node matching accepted a partial plural string.")

    return 0


if __name__ == "__main__":
    sys.exit(main())
