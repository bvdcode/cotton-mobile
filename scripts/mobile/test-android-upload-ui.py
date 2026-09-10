#!/usr/bin/env python3
"""Exercise upload feedback and source selection on an Android emulator."""

import argparse
import json
import logging
import re
import subprocess
import time
import uuid
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

PACKAGE = "dev.cottoncloud.app.debug"
RECEIVER = f"{PACKAGE}/dev.cottoncloud.app.debug.UploadUiScenarioReceiver"
LOG_TAG = "CottonUploadUiTests"
TIMEOUT_SECONDS = 40
SCENARIOS = (
    "running",
    "source-folder",
    "source-media",
    "storage-full",
    "destination-missing",
    "review-required",
    "cloud-path-conflict",
    "pending-upload-changed",
)
FAILURE_MESSAGES = {
    "storage-full": (
        "Cotton Cloud does not have enough storage for this upload. Free cloud space "
        "or contact the server administrator, then tap Run all to retry."
    ),
    "destination-missing": (
        "A cloud folder or file is no longer available. Check the destination and "
        "your access in Cotton Cloud. If the destination folder was deleted, restore "
        "it or remove this sync setup and add it again with another cloud folder. "
        "Removing a sync setup keeps your local files."
    ),
    "review-required": (
        "A previously uploaded file changed or was renamed. Cotton uploads new files "
        "only. The cloud copy and local original are kept. To back up edited content, "
        "save it as a new file."
    ),
    "cloud-path-conflict": (
        "A different file or folder already uses this path in Cotton Cloud. Review "
        "the destination before retrying."
    ),
}
FAILURE_STATUS_PREFIXES = {
    "storage-full": "Last upload failed",
    "destination-missing": "Last upload failed",
    "review-required": "Uploaded file changed",
    "cloud-path-conflict": "Cloud path conflict",
    "pending-upload-changed": "Pending upload changed",
}
PENDING_UPLOAD_MESSAGE = (
    "Cotton will abandon incomplete upload attempts and retry the current local files. "
    "If an earlier request finishes later, Cotton will preserve the local file and "
    "report a cloud conflict."
)
OFFLINE_MESSAGES = {
    "offline-add": "Connect to the internet to add a sync folder.",
    "offline-run": "Offline. Sync needs internet.",
}


@dataclass(frozen=True)
class Viewport:
    """An emulator display configuration used for layout verification."""

    name: str
    width: int
    height: int
    density: int
    font_scale: float = 1.0


VIEWPORTS = (
    Viewport("phone", 720, 1440, 320),
    Viewport("small", 720, 1280, 360),
    Viewport("tablet", 1200, 1800, 240),
    Viewport("landscape", 1920, 1080, 240),
    Viewport("large-text", 720, 1440, 320, 1.5),
)


class Emulator:
    """Run commands against one explicitly selected emulator."""

    def __init__(self, serial: str, adb: str) -> None:
        if not re.fullmatch(r"emulator-\d+", serial):
            raise ValueError("Upload UI checks must target an emulator serial.")
        self._command = [adb, "-s", serial]

    def run(self, *arguments: str) -> bytes:
        """Run one ADB command with a bounded execution time."""
        result = subprocess.run(
            [*self._command, *arguments],
            check=True,
            capture_output=True,
            timeout=TIMEOUT_SECONDS,
        )
        return result.stdout

    def text(self, *arguments: str) -> str:
        """Return decoded output for a text ADB command."""
        return self.run(*arguments).decode("utf-8", errors="replace").strip()

    def hierarchy(self) -> ET.Element:
        """Read the current accessibility hierarchy."""
        deadline = time.monotonic() + TIMEOUT_SECONDS
        while time.monotonic() < deadline:
            result = self.run(
                "shell", "uiautomator", "dump", "/sdcard/cotton-upload-ui.xml"
            )
            if b"dumped to:" in result:
                content = self.run("exec-out", "cat", "/sdcard/cotton-upload-ui.xml")
                if content.lstrip().startswith(b"<?xml"):
                    return ET.fromstring(content)
            time.sleep(0.5)
        raise TimeoutError(
            "Android did not provide the current accessibility hierarchy."
        )

    def configure(self, viewport: Viewport, theme: str) -> None:
        """Apply a viewport and color scheme."""
        self.run("shell", "wm", "size", f"{viewport.width}x{viewport.height}")
        self.run("shell", "wm", "density", str(viewport.density))
        self.run(
            "shell", "settings", "put", "system", "font_scale", str(viewport.font_scale)
        )
        self.run("shell", "cmd", "uimode", "night", theme)

    def scenario(self, name: str) -> None:
        """Display an opt-in debug scenario and await its assertions."""
        request_id = uuid.uuid4().hex
        self.run(
            "shell",
            "am",
            "broadcast",
            "-n",
            RECEIVER,
            "--es",
            "scenario",
            name,
            "--es",
            "request-id",
            request_id,
        )
        deadline = time.monotonic() + TIMEOUT_SECONDS
        while time.monotonic() < deadline:
            output = self.text("logcat", "-d", "-s", f"{LOG_TAG}:I", "*:S")
            if f"{request_id}:failed:" in output:
                raise RuntimeError(output)
            if f"{request_id}:passed:{name}" in output:
                return
            time.sleep(0.25)
        raise TimeoutError(f"UI scenario did not complete: {name}")


def capture(emulator: Emulator, directory: Path, name: str) -> ET.Element:
    """Save a screenshot and matching accessibility hierarchy."""
    hierarchy = emulator.hierarchy()
    ET.ElementTree(hierarchy).write(directory / f"{name}.xml", encoding="utf-8")
    (directory / f"{name}.png").write_bytes(emulator.run("exec-out", "screencap", "-p"))
    if not any(node.get("package") == PACKAGE for node in hierarchy.iter("node")):
        raise AssertionError(f"Upload application is no longer visible: {name}")
    return hierarchy


def assert_message(hierarchy: ET.Element, expected: str) -> None:
    """Require the complete action feedback to appear in the UI."""
    texts = [node.get("text", "") for node in hierarchy.iter("node")]
    if expected not in texts:
        raise AssertionError(
            f"Action feedback is missing: {expected}; visible text: {texts}"
        )


def bounds(node: ET.Element) -> tuple[int, int, int, int]:
    """Parse one accessibility node's screen bounds."""
    values = list(map(int, re.findall(r"\d+", node.attrib["bounds"])))
    if len(values) != 4:
        raise AssertionError(
            f"Unexpected accessibility bounds: {node.attrib['bounds']}"
        )
    return values[0], values[1], values[2], values[3]


def assert_dashboard_layout(hierarchy: ET.Element, scenario: str) -> None:
    """Require stable text geometry and a card-width progress indicator."""
    nodes = list(hierarchy.iter("node"))
    if scenario == "running":
        progress = [
            node for node in nodes if node.get("class") == "android.widget.ProgressBar"
        ]
        if len(progress) != 1:
            raise AssertionError("The running upload progress indicator is missing.")
        parents = {child: parent for parent in hierarchy.iter() for child in parent}
        container = parents[progress[0]]
        while container.get("class") != "androidx.recyclerview.widget.RecyclerView":
            container = parents[container]
        progress_left, _, progress_right, _ = bounds(progress[0])
        container_left, _, container_right, _ = bounds(container)
        if progress_right - progress_left < (container_right - container_left) * 0.8:
            raise AssertionError(
                "The running upload progress indicator does not span the card."
            )

    if scenario in FAILURE_STATUS_PREFIXES:
        status_prefix = FAILURE_STATUS_PREFIXES[scenario]
        statuses = [
            node for node in nodes if node.get("text", "").startswith(status_prefix)
        ]
        titles = [
            node for node in nodes if node.get("text", "").startswith("Camera backups")
        ]
        if len(statuses) != 1 or len(titles) != 1:
            raise AssertionError("The upload card title or status is missing.")
        if statuses[0].get("class") != "android.widget.TextView":
            raise AssertionError(
                "The upload status uses a control with different text geometry."
            )
        title_left, _, _, title_bottom = bounds(titles[0])
        status_left, status_top, _, _ = bounds(statuses[0])
        if title_left != status_left or status_top < title_bottom:
            raise AssertionError("The upload card title and status are misaligned.")


def capture_failure(
    emulator: Emulator, directory: Path, name: str, scenario: str, hierarchy: ET.Element
) -> None:
    """Open the status action and verify its complete explanation."""
    status_prefix = FAILURE_STATUS_PREFIXES[scenario]
    actions = [
        node
        for node in hierarchy.iter("node")
        if node.get("text", "").startswith(status_prefix)
        and node.get("enabled") == "true"
    ]
    if len(actions) != 1:
        raise AssertionError("The upload status details action is missing.")
    left, top, right, bottom = map(int, re.findall(r"\d+", actions[0].attrib["bounds"]))
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )
    dialog = capture(emulator, directory, f"{name}-details")
    assert_message(dialog, "Sync details for Camera backups")
    assert_message(dialog, FAILURE_MESSAGES[scenario])
    close = [node for node in dialog.iter("node") if node.get("text") == "Close"]
    if len(close) != 1 or close[0].get("enabled") != "true":
        raise AssertionError("The failure dialog cannot be closed.")
    left, top, right, bottom = bounds(close[0])
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )


def capture_pending_upload(
    emulator: Emulator, directory: Path, name: str, hierarchy: ET.Element
) -> None:
    """Verify that only a changed pending upload offers the recovery action."""
    actions = [
        node
        for node in hierarchy.iter("node")
        if node.get("text", "").startswith(
            FAILURE_STATUS_PREFIXES["pending-upload-changed"]
        )
        and node.get("enabled") == "true"
    ]
    if len(actions) != 1:
        raise AssertionError("The pending upload recovery action is missing.")
    left, top, right, bottom = bounds(actions[0])
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )
    dialog = capture(emulator, directory, f"{name}-recovery")
    assert_message(dialog, "Resolve pending upload for Camera backups?")
    assert_message(dialog, PENDING_UPLOAD_MESSAGE)
    cancel = [node for node in dialog.iter("node") if node.get("text") == "Cancel"]
    resolve = [
        node
        for node in dialog.iter("node")
        if node.get("text") == "Resolve pending upload"
    ]
    if len(cancel) != 1 or len(resolve) != 1:
        raise AssertionError("The pending upload recovery confirmation is incomplete.")
    left, top, right, bottom = bounds(cancel[0])
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )


def capture_source_end(
    emulator: Emulator, directory: Path, name: str, viewport: Viewport
) -> None:
    """Verify source options remain reachable when they extend below the screen."""
    previous = b""
    for _ in range(6):
        serialized = ET.tostring(emulator.hierarchy())
        if serialized == previous:
            break
        previous = serialized
        emulator.run(
            "shell",
            "input",
            "swipe",
            str(viewport.width // 2),
            str(viewport.height * 4 // 5),
            str(viewport.width // 2),
            str(viewport.height // 3),
            "150",
        )
    hierarchy = capture(emulator, directory, f"{name}-end")
    if name.endswith("source-folder"):
        assert_message(hierarchy, "Delete originals after upload")
        switches = [
            node
            for node in hierarchy.iter("node")
            if node.get("class") == "android.widget.Switch"
        ]
        if len(switches) != 1 or switches[0].get("checked") != "false":
            raise AssertionError("Deleting originals must be off by default.")


def wait_for_sign_in(emulator: Emulator) -> None:
    """Wait for normal startup to finish before setting a scenario."""
    deadline = time.monotonic() + TIMEOUT_SECONDS
    texts: list[str] = []
    while time.monotonic() < deadline:
        hierarchy = emulator.hierarchy()
        texts = [node.get("text", "") for node in hierarchy.iter("node")]
        if "Connect" in texts:
            return
        time.sleep(0.5)
    raise TimeoutError(f"Application did not reach sign-in. Visible text: {texts}")


def run_checks(
    emulator: Emulator, directory: Path, full: bool, dashboard_only: bool
) -> None:
    """Check actual command feedback and capture affected pages."""
    directory.mkdir(parents=True, exist_ok=True)
    emulator.run("shell", "svc", "wifi", "disable")
    emulator.run("shell", "svc", "data", "disable")
    emulator.run("shell", "am", "force-stop", PACKAGE)
    activity = emulator.text(
        "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE
    ).splitlines()[-1]
    if not activity.startswith(f"{PACKAGE}/"):
        raise RuntimeError(f"Upload test application is not installed: {activity}")
    emulator.run("shell", "am", "start", "-W", "-n", activity)
    wait_for_sign_in(emulator)
    completed: list[str] = []
    viewports = VIEWPORTS if full else VIEWPORTS[:1]
    display_scenarios = ("running",) if dashboard_only else SCENARIOS
    for viewport in viewports:
        for theme in ("no", "yes"):
            theme_name = "light" if theme == "no" else "dark"
            emulator.configure(viewport, theme)
            scenarios = (
                (*OFFLINE_MESSAGES, *display_scenarios)
                if viewport.name == "phone"
                else display_scenarios
            )
            for scenario in scenarios:
                emulator.scenario(scenario)
                name = f"{viewport.name}-{theme_name}-{scenario}"
                hierarchy = capture(emulator, directory, name)
                if scenario in OFFLINE_MESSAGES:
                    assert_message(hierarchy, OFFLINE_MESSAGES[scenario])
                assert_dashboard_layout(hierarchy, scenario)
                if scenario == "running":
                    pause_buttons = [
                        node
                        for node in hierarchy.iter("node")
                        if node.get("content-desc") == "Pause"
                    ]
                    if (
                        len(pause_buttons) != 1
                        or pause_buttons[0].get("enabled") != "true"
                    ):
                        raise AssertionError(
                            "Pause button is unavailable during upload."
                        )
                    assert_message(hierarchy, "Syncing 4 of 10 changes…")
                if scenario.startswith("source-"):
                    capture_source_end(emulator, directory, name, viewport)
                if scenario in FAILURE_MESSAGES:
                    capture_failure(emulator, directory, name, scenario, hierarchy)
                if scenario == "pending-upload-changed":
                    capture_pending_upload(emulator, directory, name, hierarchy)
                completed.append(name)
                logging.info("Passed %s", name)
    (directory / "results.json").write_text(
        json.dumps({"passed": completed}, indent=2) + "\n", encoding="utf-8"
    )


def main() -> None:
    """Parse emulator options and restore its configuration after testing."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--serial", required=True)
    parser.add_argument("--adb", default="adb")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--full", action="store_true")
    parser.add_argument("--dashboard-only", action="store_true")
    arguments = parser.parse_args()
    logging.basicConfig(level=logging.INFO, format="%(message)s")
    emulator = Emulator(arguments.serial, arguments.adb)
    try:
        run_checks(emulator, arguments.output, arguments.full, arguments.dashboard_only)
    finally:
        emulator.run("shell", "am", "force-stop", PACKAGE)
        emulator.run("shell", "wm", "size", "reset")
        emulator.run("shell", "wm", "density", "reset")
        emulator.run("shell", "settings", "put", "system", "font_scale", "1.0")
        emulator.run("shell", "cmd", "uimode", "night", "auto")
        emulator.run("shell", "svc", "wifi", "enable")
        emulator.run("shell", "svc", "data", "enable")


if __name__ == "__main__":
    main()
