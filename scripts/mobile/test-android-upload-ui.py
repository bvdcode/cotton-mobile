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

    def scroll_to_end(self) -> None:
        """Reveal content below the current scrollable viewport."""
        previous = b""
        for _ in range(6):
            hierarchy = self.hierarchy()
            serialized = ET.tostring(hierarchy)
            if serialized == previous:
                return
            previous = serialized
            scrollable_bounds = [
                bounds(node)
                for node in hierarchy.iter("node")
                if node.get("scrollable") == "true"
            ]
            if not scrollable_bounds:
                return
            left, top, right, bottom = max(
                scrollable_bounds,
                key=lambda area: (area[2] - area[0]) * (area[3] - area[1]),
            )
            x = str((left + right) // 2)
            inset = (bottom - top) // 5
            self.run(
                "shell",
                "input",
                "swipe",
                x,
                str(bottom - inset),
                x,
                str(top + inset),
                "150",
            )


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


def capture_source_end(emulator: Emulator, directory: Path, name: str) -> None:
    """Verify source options remain reachable when they extend below the screen."""
    emulator.scroll_to_end()
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


def check_worker_cancellation(emulator: Emulator) -> None:
    """Require Android's worker stop callback to cancel the dispatched operation."""
    tag = "CottonCancellationProbe"
    emulator.run("shell", "input", "keyevent", "KEYCODE_HOME")
    try:
        emulator.scenario("worker-cancellation-start")
        emulator.run("shell", "am", "kill", PACKAGE)
        emulator.run("logcat", "-c")
        jobs = emulator.text("shell", "dumpsys", "jobscheduler")
        pattern = (
            r"JOB (?:(?P<namespace>[^\s:]+):|#)[^\s/]+/(?P<id>\d+):[^\n]*"
            + re.escape(
                PACKAGE + "/androidx.work.impl.background.systemjob.SystemJobService"
            )
        )
        scheduled = list(re.finditer(pattern, jobs))
        if len(scheduled) != 1:
            raise AssertionError("Expected exactly one scheduled cancellation probe.")
        job = scheduled[0]
        namespace = ["-n", job.group("namespace")] if job.group("namespace") else []
        for expected in (
            "operation-started",
            "operation-stopped:cancelled=True",
            "cancellation-callback:main=False",
        ):
            deadline = time.monotonic() + TIMEOUT_SECONDS
            while time.monotonic() < deadline:
                output = emulator.text("logcat", "-d", "-s", f"{tag}:I", "*:S")
                if expected in output:
                    break
                time.sleep(0.25)
            else:
                raise AssertionError(
                    f"Android worker did not report {expected}: {output}"
                )
            if expected == "operation-started":
                emulator.run(
                    "shell",
                    "cmd",
                    "jobscheduler",
                    "timeout",
                    *namespace,
                    PACKAGE,
                    job.group("id"),
                )
        if "system-stopped:reason=" not in output:
            raise AssertionError(
                "The dispatched operation stopped without Android's stop callback."
            )
        if "operation-thread:main=False" not in output:
            raise AssertionError("The worker started its operation on the UI thread.")
        logging.info("Passed Android worker stop and operation cancellation")
    finally:
        emulator.scenario("worker-cancellation-cleanup")
    activity = emulator.text(
        "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE
    ).splitlines()[-1]
    emulator.run("shell", "am", "start", "-W", "-n", activity)
    wait_for_sign_in(emulator)


def check_review_cancellation(emulator: Emulator) -> None:
    """Require Android's stop lifecycle to cancel the foreground review scope."""
    emulator.scenario("original-media-review-cancellation")
    deadline = time.monotonic() + TIMEOUT_SECONDS
    while time.monotonic() < deadline:
        output = emulator.text("logcat", "-d", "-s", "CottonUploadUiTests:I", "*:S")
        if "original-media-review:hash-started" in output:
            break
        time.sleep(0.1)
    else:
        raise AssertionError(f"Foreground review did not start hashing: {output}")
    emulator.run("shell", "input", "keyevent", "KEYCODE_HOME")
    deadline = time.monotonic() + TIMEOUT_SECONDS
    while time.monotonic() < deadline:
        output = emulator.text("logcat", "-d", "-s", "CottonUploadUiTests:I", "*:S")
        if "original-media-review:stopped" in output:
            break
        time.sleep(0.25)
    else:
        raise AssertionError(f"Leaving the application did not stop review: {output}")
    activity = emulator.text(
        "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE
    ).splitlines()[-1]
    emulator.run("shell", "am", "start", "-W", "-n", activity)
    wait_for_sign_in(emulator)
    logging.info("Passed foreground review cancellation on Android Home")


def check_sign_in_notification(emulator: Emulator) -> None:
    """Verify one sign-in alert is posted and cleared after session recovery."""
    if int(emulator.text("shell", "getprop", "ro.build.version.sdk")) >= 33:
        emulator.run(
            "shell", "pm", "grant", PACKAGE, "android.permission.POST_NOTIFICATIONS"
        )
    record = re.compile(
        r"NotificationRecord[^\n]*pkg=" + re.escape(PACKAGE) + r" [^\n]*id=19002 "
    )
    emulator.scenario("sign-in-restored")
    first_update: str | None = None
    for _ in range(2):
        emulator.scenario("sign-in-required")
        notifications = emulator.text("shell", "dumpsys", "notification", "--noredact")
        matches = list(record.finditer(notifications))
        if len(matches) != 1:
            raise AssertionError("Expected exactly one sign-in notification.")
        update = re.search(r"mUpdateTimeMs=(\d+)", notifications[matches[0].end() :])
        if update is None:
            raise AssertionError("Notification update timestamp is missing.")
        if first_update is None:
            first_update = update.group(1)
        elif first_update != update.group(1):
            raise AssertionError(
                "The repeated check posted the sign-in notification again."
            )
        if (
            "Photo backup is paused. Sign in again to resume uploads."
            not in notifications
        ):
            raise AssertionError(
                "Sign-in notification does not explain why uploads stopped."
            )
    emulator.scenario("sign-in-restored")
    if record.search(emulator.text("shell", "dumpsys", "notification", "--noredact")):
        raise AssertionError("Sign-in notification remained after session recovery.")
    logging.info("Passed sign-in notification delivery and recovery")


def check_permission_transition(emulator: Emulator, directory: Path, name: str) -> None:
    """Grant media access and require the row to disappear on the same screen."""
    api = int(emulator.text("shell", "getprop", "ro.build.version.sdk"))
    if api < 29:
        return
    emulator.run("shell", "pm", "clear", PACKAGE)
    activity = emulator.text(
        "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE
    ).splitlines()[-1]
    emulator.run("shell", "am", "start", "-W", "-n", activity)
    wait_for_sign_in(emulator)
    emulator.scenario("running")
    emulator.scroll_to_end()
    before = capture(emulator, directory, f"{name}-before")
    allow = next(
        node for node in before.iter("node") if node.get("text") == "Allow access"
    )
    left, top, right, bottom = bounds(allow)
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )
    dialog = emulator.hierarchy()
    permission_button = (
        ":id/permission_allow_all_button"
        if api >= 34
        else ":id/permission_allow_button"
    )
    allow_all = next(
        node
        for node in dialog.iter("node")
        if node.get("resource-id", "").endswith(permission_button)
    )
    left, top, right, bottom = bounds(allow_all)
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )
    after = capture(emulator, directory, f"{name}-after")
    texts = [node.get("text", "") for node in after.iter("node")]
    if "Allow access" in texts or "Preserve location data" in texts:
        raise AssertionError("The granted permission row is still present.")
    title = next(
        node
        for node in after.iter("node")
        if node.get("text", "").startswith("Camera backups")
    )
    parents = {child: parent for parent in after.iter() for child in parent}
    collection = parents[title]
    while collection.get("class") != "androidx.recyclerview.widget.RecyclerView":
        collection = parents[collection]
    pause = next(
        node for node in after.iter("node") if node.get("content-desc") == "Pause"
    )
    if bounds(title)[1] - bounds(collection)[1] > bounds(pause)[3] - bounds(pause)[1]:
        raise AssertionError(
            "Removing the permission row left empty space above the folder."
        )
    assert_dashboard_layout(after, "running")


def check_original_prompt(
    emulator: Emulator, directory: Path, name: str, accept: bool
) -> None:
    """Render the native recovery question and exercise both decisions."""
    emulator.scenario("running")
    emulator.scenario("original-media-prompt")
    hierarchy = capture(emulator, directory, name)
    texts = [node.get("text", "") for node in hierarchy.iter("node")]
    if "Restore location data?" not in texts or not any(
        "300 cloud files" in text for text in texts
    ):
        raise AssertionError("The original media question is incomplete.")
    label = "Yes, restore" if accept else "Keep current copies"
    button = next(
        node
        for node in hierarchy.iter("node")
        if node.get("text", "").casefold() == label.casefold()
    )
    left, top, right, bottom = bounds(button)
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )
    deadline = time.monotonic() + TIMEOUT_SECONDS
    expected = f"original-media-prompt:accepted={accept}"
    while time.monotonic() < deadline:
        output = emulator.text("logcat", "-d", "-s", "CottonUploadUiTests:I", "*:S")
        if expected in output:
            return
        time.sleep(0.25)
    raise AssertionError(f"The native question did not report its decision: {output}")


def run_checks(
    emulator: Emulator, directory: Path, full: bool, dashboard_only: bool
) -> None:
    """Check actual command feedback and capture affected pages."""
    directory.mkdir(parents=True, exist_ok=True)
    emulator.run("shell", "cmd", "connectivity", "airplane-mode", "enable")
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
    check_sign_in_notification(emulator)
    check_worker_cancellation(emulator)
    check_review_cancellation(emulator)
    completed: list[str] = []
    viewports = VIEWPORTS if full else VIEWPORTS[:1]
    display_scenarios = ("running",) if dashboard_only else SCENARIOS
    for viewport in viewports:
        for theme in ("no", "yes"):
            theme_name = "light" if theme == "no" else "dark"
            emulator.configure(viewport, theme)
            permission_case = f"{viewport.name}-{theme_name}-permission-transition"
            check_permission_transition(emulator, directory, permission_case)
            completed.append(permission_case)
            logging.info("Passed %s", permission_case)
            prompt_case = f"{viewport.name}-{theme_name}-original-media-prompt"
            check_original_prompt(emulator, directory, prompt_case, theme == "no")
            completed.append(prompt_case)
            logging.info("Passed %s", prompt_case)
            scenarios = (
                (*OFFLINE_MESSAGES, *display_scenarios)
                if viewport.name == "phone"
                else display_scenarios
            )
            for scenario in scenarios:
                emulator.scenario(scenario)
                name = f"{viewport.name}-{theme_name}-{scenario}"
                if scenario == "running" or scenario in FAILURE_STATUS_PREFIXES:
                    emulator.scroll_to_end()
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
                    capture_source_end(emulator, directory, name)
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
        emulator.run("shell", "cmd", "connectivity", "airplane-mode", "disable")
        emulator.run("shell", "svc", "wifi", "enable")
        emulator.run("shell", "svc", "data", "enable")


if __name__ == "__main__":
    main()
