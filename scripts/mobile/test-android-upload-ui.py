#!/usr/bin/env python3
"""Exercise upload feedback and backup setup actions on an Android emulator."""

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

from PIL import Image

PACKAGE = "dev.cottoncloud.app.debug"
RECEIVER = f"{PACKAGE}/dev.cottoncloud.app.debug.UploadUiScenarioReceiver"
LOG_TAG = "CottonUploadUiTests"
TIMEOUT_SECONDS = 40
LOGGER = logging.getLogger(__name__)
SCENARIOS = (
    "running",
    "dashboard-empty",
    "dashboard-folder-only",
    "dashboard-media",
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
REPLACE_CLOUD_CONFLICT_MESSAGE = (
    "Cotton will replace cloud files that conflict with files on this device. "
    "Existing cloud content remains available in version history. If a cloud file "
    "changes before replacement, Cotton will stop without overwriting it."
)
OFFLINE_MESSAGES = {
    "offline-add": "Connect to the internet to set up backup.",
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
            if not self._scroll_forward(hierarchy):
                return

    def scroll_until_message(self, expected: str) -> None:
        """Reveal one message in the current scrollable viewport."""
        previous = b""
        for _ in range(6):
            hierarchy = self.hierarchy()
            if expected in [node.get("text", "") for node in hierarchy.iter("node")]:
                return
            serialized = ET.tostring(hierarchy)
            if serialized == previous or not self._scroll_forward(hierarchy):
                break
            previous = serialized
        raise AssertionError(f"Scrollable content did not reveal: {expected}")

    def _scroll_forward(self, hierarchy: ET.Element) -> bool:
        """Move the largest visible scrollable container forward once."""
        scrollable_bounds = [
            bounds(node)
            for node in hierarchy.iter("node")
            if node.get("scrollable") == "true"
        ]
        if not scrollable_bounds:
            return False
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
        return True


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


def assert_dialog_backdrop(directory: Path, background: str, dialog: str) -> None:
    """Require a half-opacity black scrim outside the dialog in either theme."""
    with (
        Image.open(directory / f"{background}.png") as before,
        Image.open(directory / f"{dialog}.png") as after,
    ):
        if before.size != after.size:
            raise AssertionError("Opening a dialog changed the display dimensions.")
        width, height = before.size
        for x in (width // 32, width - width // 32 - 1):
            y = height // 2
            area = (x, y, x + 1, y + 1)
            original = before.convert("RGB").crop(area).tobytes()
            overlay = after.convert("RGB").crop(area).tobytes()
            if any(
                abs(actual - initial / 2) > 3
                for initial, actual in zip(original, overlay, strict=True)
            ):
                raise AssertionError(
                    f"Incorrect dialog backdrop in {dialog} at ({x}, {y}): "
                    f"background={tuple(original)}, dialog={tuple(overlay)}"
                )


def icon_frame_near(hierarchy: ET.Element, reference: ET.Element) -> ET.Element:
    """Find the icon frame vertically nearest to a reference label."""
    parents = {child: parent for parent in hierarchy.iter() for child in parent}
    frames = [
        parents[node]
        for node in hierarchy.iter("node")
        if node.get("class") == "android.widget.ImageView"
    ]
    if not frames:
        raise AssertionError("No icon frame is visible.")
    _, top, _, bottom = bounds(reference)
    reference_center = (top + bottom) // 2
    return min(
        frames,
        key=lambda frame: abs(
            (bounds(frame)[1] + bounds(frame)[3]) // 2 - reference_center
        ),
    )


def assert_background_notice_geometry(
    hierarchy: ET.Element, directory: Path, screenshot: str
) -> None:
    """Require the warning and sync row to share columns and visible contrast."""
    title = next(
        node
        for node in hierarchy.iter("node")
        if node.get("text") == "Background uploads are restricted"
    )
    sync_title = next(
        node
        for node in hierarchy.iter("node")
        if node.get("text", "").startswith("Camera backups")
    )
    notice_frame = bounds(icon_frame_near(hierarchy, title))
    sync_frame = bounds(icon_frame_near(hierarchy, sync_title))
    if notice_frame[:1] + notice_frame[2:3] != sync_frame[:1] + sync_frame[2:3]:
        raise AssertionError(
            f"Warning icon column {notice_frame[0], notice_frame[2]} does not "
            f"match sync icon column {sync_frame[0], sync_frame[2]}."
        )
    if bounds(title)[0] != bounds(sync_title)[0]:
        raise AssertionError("Warning text does not align with sync text.")
    dismiss = next(
        node
        for node in hierarchy.iter("node")
        if node.get("content-desc") == "Dismiss background upload warning"
    )
    pause = next(
        node for node in hierarchy.iter("node") if node.get("content-desc") == "Pause"
    )
    if bounds(dismiss)[2] != bounds(pause)[2]:
        raise AssertionError("Warning dismiss action does not align with sync action.")

    with Image.open(directory / f"{screenshot}.png") as image:
        rgb = image.convert("RGB")
        left, top, right, bottom = notice_frame
        frame_color = read_rgb_pixel(rgb, ((left + right) // 2, top + 8))
        card_color = read_rgb_pixel(rgb, (right + 12, (top + bottom) // 2))
    if color_contrast(frame_color, card_color) < 3:
        raise AssertionError(
            f"Warning icon container lacks contrast: {frame_color} on {card_color}."
        )


def read_rgb_pixel(
    image: Image.Image,
    point: tuple[int, int],
) -> tuple[int, int, int]:
    """Read one pixel from an image converted to RGB mode."""

    pixel = image.getpixel(point)
    if not isinstance(pixel, tuple) or len(pixel) != 3:
        raise AssertionError(f"Expected an RGB pixel at {point}, received {pixel!r}.")

    red, green, blue = pixel
    return red, green, blue


def color_contrast(first: tuple[int, int, int], second: tuple[int, int, int]) -> float:
    """Calculate WCAG contrast for two sRGB colors."""

    def luminance(color: tuple[int, int, int]) -> float:
        channels = [
            value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4
            for component in color
            for value in [component / 255]
        ]
        return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2]

    lighter, darker = sorted((luminance(first), luminance(second)), reverse=True)
    return (lighter + 0.05) / (darker + 0.05)


def find_action_button(hierarchy: ET.Element, text: str) -> ET.Element:
    """Find one enabled native action button by its visible label."""
    buttons = [
        node
        for node in hierarchy.iter("node")
        if node.get("class") == "android.widget.Button"
        and node.get("text") == text
        and node.get("clickable") == "true"
        and node.get("enabled") == "true"
    ]
    if len(buttons) != 1:
        raise AssertionError(f"Expected one enabled action button: {text}")
    return buttons[0]


def wait_for_message(emulator: Emulator, expected: str) -> None:
    """Wait until an asynchronous native dialog exposes its content."""
    deadline = time.monotonic() + TIMEOUT_SECONDS
    while time.monotonic() < deadline:
        hierarchy = emulator.hierarchy()
        if expected in [node.get("text", "") for node in hierarchy.iter("node")]:
            return
        time.sleep(0.25)
    raise AssertionError(f"Action feedback did not appear: {expected}")


def wait_for_message_absence(emulator: Emulator, unexpected: str) -> None:
    """Wait until an asynchronous UI update removes text from the hierarchy."""
    deadline = time.monotonic() + TIMEOUT_SECONDS
    while time.monotonic() < deadline:
        hierarchy = emulator.hierarchy()
        if unexpected not in [node.get("text", "") for node in hierarchy.iter("node")]:
            return
        time.sleep(0.25)
    raise AssertionError(f"Action feedback remained visible: {unexpected}")


def bounds(node: ET.Element) -> tuple[int, int, int, int]:
    """Parse one accessibility node's screen bounds."""
    values = list(map(int, re.findall(r"\d+", node.attrib["bounds"])))
    if len(values) != 4:
        raise AssertionError(
            f"Unexpected accessibility bounds: {node.attrib['bounds']}"
        )
    return values[0], values[1], values[2], values[3]


def tap_node(emulator: Emulator, node: ET.Element) -> None:
    """Tap the center of one accessibility node."""
    left, top, right, bottom = bounds(node)
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )


def background_app_op_mode(emulator: Emulator) -> str:
    """Read the current Android background execution app-op mode."""
    output = emulator.text(
        "shell", "cmd", "appops", "get", PACKAGE, "RUN_ANY_IN_BACKGROUND"
    )
    match = re.search(
        r"(?:RUN_ANY_IN_BACKGROUND:|Default mode:)\s+(allow|deny|ignore|default)",
        output,
    )
    if match is None:
        raise AssertionError(f"Cannot read background app-op mode: {output}")
    return match.group(1)


def media_access_permissions(emulator: Emulator) -> tuple[str, ...]:
    """Return the permissions required to read original media on this device."""
    api = int(emulator.text("shell", "getprop", "ro.build.version.sdk"))
    read_permissions = (
        (
            "android.permission.READ_MEDIA_IMAGES",
            "android.permission.READ_MEDIA_VIDEO",
        )
        if api >= 33
        else ("android.permission.READ_EXTERNAL_STORAGE",)
    )
    return (*read_permissions, "android.permission.ACCESS_MEDIA_LOCATION")


def set_media_access(emulator: Emulator, granted: bool) -> None:
    """Grant or revoke complete media access for an isolated UI scenario."""
    action = "grant" if granted else "revoke"
    permissions = media_access_permissions(emulator)
    ordered_permissions = permissions if granted else tuple(reversed(permissions))
    for permission in ordered_permissions:
        emulator.run("shell", "pm", action, PACKAGE, permission)


def set_background_app_op_mode(emulator: Emulator, mode: str) -> None:
    """Set the Android background execution app-op mode."""
    emulator.run(
        "shell", "cmd", "appops", "set", PACKAGE, "RUN_ANY_IN_BACKGROUND", mode
    )


def check_tab_navigation(
    emulator: Emulator, directory: Path, name: str, hierarchy: ET.Element
) -> None:
    """Exercise UraniumUI tab navigation and preserve the cached dashboard."""
    profile = find_action_button(hierarchy, "Profile")
    tap_node(emulator, profile)
    wait_for_message(emulator, "Your Cotton Cloud connection and app settings.")
    profile_hierarchy = capture(emulator, directory, f"{name}-profile")
    assert_message(profile_hierarchy, "Profile")

    sync = find_action_button(profile_hierarchy, "Sync")
    tap_node(emulator, sync)
    wait_for_message(emulator, "Syncing 4 of 10 changes…")
    returned = capture(emulator, directory, f"{name}-sync-return")
    assert_dashboard_layout(returned, "running")


def assert_dashboard_layout(hierarchy: ET.Element, scenario: str) -> None:
    """Require stable text geometry and a card-width progress indicator."""
    nodes = list(hierarchy.iter("node"))
    texts = [node.get("text", "") for node in nodes]
    photo_actions = [text for text in texts if text == "Back up photos and videos"]
    folder_actions = [text for text in texts if text == "Back up a folder"]
    if scenario == "dashboard-empty":
        assert_message(hierarchy, "Protect your photos")
        if len(photo_actions) != 1 or len(folder_actions) != 1:
            raise AssertionError(
                "The empty dashboard does not expose both backup actions."
            )
    elif scenario == "dashboard-folder-only":
        assert_message(hierarchy, "Photo and video backup")
        if len(photo_actions) != 1:
            raise AssertionError(
                "The folder-only dashboard does not offer photo backup."
            )
        photo_title = next(
            node for node in nodes if node.get("text") == "Photo and video backup"
        )
        sync_title = next(
            node for node in nodes if node.get("text", "").startswith("Camera backups")
        )
        photo_frame = bounds(icon_frame_near(hierarchy, photo_title))
        sync_frame = bounds(icon_frame_near(hierarchy, sync_title))
        if (photo_frame[0], photo_frame[2]) != (sync_frame[0], sync_frame[2]) or bounds(
            photo_title
        )[0] != bounds(sync_title)[0]:
            raise AssertionError("Photo backup callout does not align with sync rows.")
    elif scenario == "dashboard-media" and photo_actions:
        raise AssertionError("Photo backup is offered after it is already configured.")
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
    action_text = (
        "Replace cloud file"
        if scenario == "cloud-path-conflict"
        else "Show sync details"
    )
    action = find_action_button(hierarchy, action_text)
    left, top, right, bottom = bounds(action)
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )
    if scenario == "cloud-path-conflict":
        assert_message(
            hierarchy,
            "Sync incomplete. 3 items need attention. Review the folders below.",
        )
        title = "Replace conflicting files in Camera backups?"
        wait_for_message(emulator, title)
        dialog = capture(emulator, directory, f"{name}-replacement")
        assert_dialog_backdrop(directory, name, f"{name}-replacement")
        assert_message(dialog, title)
        assert_message(dialog, REPLACE_CLOUD_CONFLICT_MESSAGE)
        find_action_button(dialog, "Replace cloud file")
        cancel = find_action_button(dialog, "Cancel")
        left, top, right, bottom = bounds(cancel)
        emulator.run(
            "shell",
            "input",
            "tap",
            str((left + right) // 2),
            str((top + bottom) // 2),
        )
        return

    wait_for_message(emulator, "Sync details for Camera backups")
    dialog = capture(emulator, directory, f"{name}-details")
    assert_dialog_backdrop(directory, name, f"{name}-details")
    assert_message(dialog, "Sync details for Camera backups")
    assert_message(dialog, FAILURE_MESSAGES[scenario])
    close = find_action_button(dialog, "Close")
    left, top, right, bottom = bounds(close)
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )


def capture_pending_upload(
    emulator: Emulator, directory: Path, name: str, hierarchy: ET.Element
) -> None:
    """Verify that only a changed pending upload offers the recovery action."""
    action = find_action_button(hierarchy, "Retry uploads")
    left, top, right, bottom = bounds(action)
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )
    wait_for_message(emulator, "Resolve pending upload for Camera backups?")
    dialog = capture(emulator, directory, f"{name}-recovery")
    assert_dialog_backdrop(directory, name, f"{name}-recovery")
    assert_message(dialog, "Resolve pending upload for Camera backups?")
    assert_message(dialog, PENDING_UPLOAD_MESSAGE)
    cancel = find_action_button(dialog, "Cancel")
    find_action_button(dialog, "Retry uploads")
    left, top, right, bottom = bounds(cancel)
    emulator.run(
        "shell", "input", "tap", str((left + right) // 2), str((top + bottom) // 2)
    )


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


def check_worker_cancellation(emulator: Emulator, budget: bool = False) -> None:
    """Require Android's worker stop callback to cancel the dispatched operation."""
    tag = "CottonCancellationProbe"
    emulator.run("shell", "input", "keyevent", "KEYCODE_HOME")
    try:
        scenario = "worker-cancellation-start"
        if budget:
            scenario = "worker-budget-start"
        emulator.scenario(scenario)
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
        scheduled_start = emulator.text(
            "shell",
            "cmd",
            "jobscheduler",
            "run",
            "-f",
            *namespace,
            PACKAGE,
            job.group("id"),
        )
        LOGGER.info("Forced scheduled worker: %s", scheduled_start)
        expected_results = [
            "operation-started",
            "operation-stopped:cancelled=True",
            "cancellation-callback:main=False",
        ]
        if budget:
            expected_results.append("window-ended:retry")
        else:
            expected_results.append("stop-log-released")
        for expected in expected_results:
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
            if expected == "operation-started" and not budget:
                system_stop = emulator.text(
                    "shell",
                    "cmd",
                    "jobscheduler",
                    "timeout",
                    *namespace,
                    PACKAGE,
                    job.group("id"),
                )
                LOGGER.info("Requested system worker stop: %s", system_stop)
        if not budget and "system-stopped:reason=" not in output:
            raise AssertionError(
                "The dispatched operation stopped without Android's stop callback."
            )
        if "operation-thread:main=False" not in output:
            raise AssertionError("The worker started its operation on the UI thread.")
        if budget:
            if "system-stopped:reason=" in output:
                raise AssertionError("Android stopped the job before its own window.")
            LOGGER.info("Passed worker execution window and deferred retry")
        else:
            returned = re.search(r"stop-callback-returned:milliseconds=(\d+)", output)
            if returned is None or int(returned.group(1)) >= 500:
                raise AssertionError(f"Worker stop waited for diagnostic I/O: {output}")
            if output.index("operation-stopped:") > output.index("stop-log-released"):
                raise AssertionError(f"Diagnostic I/O delayed cancellation: {output}")
            LOGGER.info("Passed Android worker stop and operation cancellation")
    finally:
        emulator.scenario("worker-cancellation-cleanup")
    activity = emulator.text(
        "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE
    ).splitlines()[-1]
    emulator.run("shell", "am", "start", "-W", "-n", activity)
    wait_for_sign_in(emulator)


def check_native_job_stop(emulator: Emulator) -> None:
    """Require the native stop callback to return while diagnostic I/O is blocked."""
    if int(emulator.text("shell", "getprop", "ro.build.version.sdk")) < 31:
        return
    tag = "CottonCancellationProbe"
    emulator.scenario("native-stop-start")
    emulator.run("logcat", "-c")
    try:
        emulator.run("shell", "cmd", "jobscheduler", "run", "-f", PACKAGE, "1129598210")
        for expected in ("native-job-started", "stop-log-released"):
            deadline = time.monotonic() + TIMEOUT_SECONDS
            while time.monotonic() < deadline:
                output = emulator.text("logcat", "-d", "-s", f"{tag}:I", "*:S")
                if expected in output:
                    break
                time.sleep(0.1)
            else:
                raise AssertionError(f"Native stop probe missed {expected}: {output}")
            if expected == "native-job-started":
                emulator.run(
                    "shell", "cmd", "jobscheduler", "timeout", PACKAGE, "1129598210"
                )
        returned = re.search(
            r"native-stop-returned:milliseconds=(\d+):main=True", output
        )
        if returned is None or int(returned.group(1)) >= 500:
            raise AssertionError(f"Native stop blocked the main thread: {output}")
        if "stop-log-blocked:main=False" not in output:
            raise AssertionError(f"Native stop logged on the main thread: {output}")
        if output.index("native-stop-returned:") > output.index("stop-log-released"):
            raise AssertionError(f"Native stop waited for diagnostic I/O: {output}")
        LOGGER.info("Passed native job stop with blocked diagnostic I/O")
    finally:
        emulator.scenario("native-stop-cleanup")


def check_background_restriction(emulator: Emulator) -> None:
    """Distinguish normal battery optimization from a real background restriction."""
    entries = emulator.text("shell", "cmd", "deviceidle", "whitelist").splitlines()
    originally_exempt = any(f",{PACKAGE}," in entry for entry in entries)
    original_app_op_mode = background_app_op_mode(emulator)
    try:
        emulator.run("shell", "cmd", "deviceidle", "whitelist", f"-{PACKAGE}")
        set_background_app_op_mode(emulator, "allow")
        emulator.scenario("background-unrestricted")
        set_background_app_op_mode(emulator, "deny")
        emulator.scenario("background-restricted")
        LOGGER.info("Passed optimized and restricted background mode detection")
    finally:
        set_background_app_op_mode(emulator, original_app_op_mode)
        prefix = "+" if originally_exempt else "-"
        emulator.run("shell", "cmd", "deviceidle", "whitelist", f"{prefix}{PACKAGE}")


def check_background_notice(emulator: Emulator, directory: Path) -> None:
    """Render, dismiss, restore, and resolve the background restriction warning."""
    entries = emulator.text("shell", "cmd", "deviceidle", "whitelist").splitlines()
    originally_exempt = any(f",{PACKAGE}," in entry for entry in entries)
    original_app_op_mode = background_app_op_mode(emulator)
    activity = emulator.text(
        "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE
    ).splitlines()[-1]
    emulator.configure(VIEWPORTS[0], "no")
    try:
        set_media_access(emulator, granted=True)
        emulator.run("shell", "cmd", "deviceidle", "whitelist", f"-{PACKAGE}")
        set_background_app_op_mode(emulator, "allow")
        emulator.run("shell", "am", "force-stop", PACKAGE)
        emulator.run("shell", "am", "start", "-W", "-n", activity)
        wait_for_sign_in(emulator)
        emulator.scenario("dashboard-folder-only")
        wait_for_message(emulator, "Ready")
        optimized = capture(emulator, directory, "battery-notice-optimized")
        if any(
            node.get("text") == "Background uploads are restricted"
            for node in optimized.iter("node")
        ):
            raise AssertionError("Normal optimized mode shows a restriction warning.")

        set_background_app_op_mode(emulator, "deny")
        emulator.run("shell", "input", "keyevent", "KEYCODE_HOME")
        emulator.run("shell", "am", "start", "-W", "-n", activity)
        emulator.scenario("dashboard-folder-only")
        wait_for_message(emulator, "Background uploads are restricted")
        restricted = capture(emulator, directory, "battery-notice-restricted")
        assert_message(restricted, "Background uploads are restricted")
        assert_message(
            restricted,
            "Android is severely limiting Cotton in the background. "
            "Automatic uploads may not start until you open the app.",
        )
        assert_message(restricted, "Settings")
        dismiss = [
            node
            for node in restricted.iter("node")
            if node.get("content-desc") == "Dismiss background upload warning"
        ]
        if len(dismiss) != 1:
            raise AssertionError(
                "The background restriction warning is not dismissible."
            )
        assert_background_notice_geometry(
            restricted, directory, "battery-notice-restricted"
        )
        tap_node(emulator, dismiss[0])
        wait_for_message_absence(emulator, "Background uploads are restricted")
        dismissed = capture(emulator, directory, "battery-notice-dismissed")
        if any(
            node.get("text") == "Background uploads are restricted"
            for node in dismissed.iter("node")
        ):
            raise AssertionError(
                "Dismissed background restriction warning remained visible."
            )

        emulator.run("shell", "am", "force-stop", PACKAGE)
        emulator.run("shell", "am", "start", "-W", "-n", activity)
        wait_for_sign_in(emulator)
        emulator.scenario("dashboard-folder-only")
        wait_for_message(emulator, "Background uploads are restricted")
        restored = capture(emulator, directory, "battery-notice-restored")
        assert_message(restored, "Background uploads are restricted")

        set_background_app_op_mode(emulator, "allow")
        emulator.run("shell", "input", "keyevent", "KEYCODE_HOME")
        emulator.run("shell", "am", "start", "-W", "-n", activity)
        emulator.scenario("dashboard-folder-only")
        wait_for_message(emulator, "Ready")
        wait_for_message_absence(emulator, "Background uploads are restricted")
        resolved = capture(emulator, directory, "battery-notice-resolved")
        if any(
            node.get("text") == "Background uploads are restricted"
            for node in resolved.iter("node")
        ):
            raise AssertionError(
                "Resolved background restriction warning remained visible."
            )

        emulator.configure(VIEWPORTS[-1], "yes")
        set_background_app_op_mode(emulator, "deny")
        emulator.run("shell", "input", "keyevent", "KEYCODE_HOME")
        emulator.run("shell", "am", "start", "-W", "-n", activity)
        emulator.scenario("dashboard-folder-only")
        wait_for_message(emulator, "Background uploads are restricted")
        large_text_top = capture(
            emulator, directory, "battery-notice-large-text-dark-top"
        )
        if not any(
            node.get("content-desc") == "Dismiss background upload warning"
            for node in large_text_top.iter("node")
        ):
            raise AssertionError(
                "The large-text background restriction warning lost its dismiss action."
            )
        emulator.scroll_until_message("Settings")
        large_text_action = capture(
            emulator, directory, "battery-notice-large-text-dark-action"
        )
        assert_message(large_text_action, "Settings")
        LOGGER.info("Passed background restriction warning lifecycle")
    finally:
        set_media_access(emulator, granted=False)
        set_background_app_op_mode(emulator, original_app_op_mode)
        prefix = "+" if originally_exempt else "-"
        emulator.run("shell", "cmd", "deviceidle", "whitelist", f"{prefix}{PACKAGE}")
        emulator.run("shell", "input", "keyevent", "KEYCODE_HOME")
        emulator.run("shell", "am", "start", "-W", "-n", activity)


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
    LOGGER.info("Passed foreground review cancellation on Android Home")


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
    LOGGER.info("Passed sign-in notification delivery and recovery")


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
    preceding_content_bottom = max(
        [bounds(collection)[1]]
        + [
            bounds(node)[3]
            for node in collection.iter("node")
            if node.get("content-desc") == "Review Android background activity settings"
        ]
    )
    if (
        bounds(title)[1] - preceding_content_bottom
        > bounds(pause)[3] - bounds(pause)[1]
    ):
        raise AssertionError(
            "Removing the permission row left empty space above the folder."
        )
    assert_dashboard_layout(after, "running")


def check_original_prompt(
    emulator: Emulator, directory: Path, name: str, accept: bool
) -> None:
    """Render the native recovery question and exercise both decisions."""
    emulator.scenario("running")
    capture(emulator, directory, f"{name}-background")
    emulator.scenario("original-media-prompt")
    hierarchy = capture(emulator, directory, name)
    assert_dialog_backdrop(directory, f"{name}-background", name)
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
    check_background_restriction(emulator)
    check_worker_cancellation(emulator)
    check_worker_cancellation(emulator, budget=True)
    check_native_job_stop(emulator)
    check_review_cancellation(emulator)
    check_background_notice(emulator, directory)
    completed: list[str] = []
    viewports = VIEWPORTS if full else VIEWPORTS[:1]
    dashboard_scenarios = (
        "running",
        "dashboard-empty",
        "dashboard-folder-only",
        "dashboard-media",
    )
    display_scenarios = dashboard_scenarios if dashboard_only else SCENARIOS
    for viewport in viewports:
        for theme in ("no", "yes"):
            theme_name = "light" if theme == "no" else "dark"
            emulator.configure(viewport, theme)
            permission_case = f"{viewport.name}-{theme_name}-permission-transition"
            check_permission_transition(emulator, directory, permission_case)
            completed.append(permission_case)
            LOGGER.info("Passed %s", permission_case)
            prompt_case = f"{viewport.name}-{theme_name}-original-media-prompt"
            check_original_prompt(emulator, directory, prompt_case, theme == "no")
            completed.append(prompt_case)
            LOGGER.info("Passed %s", prompt_case)
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
                    check_tab_navigation(emulator, directory, name, hierarchy)
                if scenario in FAILURE_MESSAGES:
                    capture_failure(emulator, directory, name, scenario, hierarchy)
                if scenario == "pending-upload-changed":
                    capture_pending_upload(emulator, directory, name, hierarchy)
                completed.append(name)
                LOGGER.info("Passed %s", name)
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
