#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import logging
import os
import sys
import xml.etree.ElementTree as ElementTree
from collections.abc import Mapping, Sequence
from pathlib import Path
from typing import TypeAlias, cast

JsonValue: TypeAlias = (
    str
    | int
    | float
    | bool
    | None
    | list["JsonValue"]
    | dict[str, "JsonValue"]
)

LOGGER = logging.getLogger("cotton.firebase_config")


def main(argv: Sequence[str] | None = None) -> int:
    configure_logging()
    repo_root = get_repo_root()
    args = parse_args(argv, repo_root)

    configuration = args.configuration.strip()
    if not configuration:
        LOGGER.error("Android build configuration is required.")
        return 64

    config_path = args.config_file
    if not config_path.is_absolute():
        config_path = repo_root / config_path

    project_path = args.project
    if not project_path.is_absolute():
        project_path = repo_root / project_path

    if not config_path.exists():
        LOGGER.error("Firebase Android config is missing: %s", config_path)
        LOGGER.error(
            "Download google-services.json from Firebase for the target Android package "
            "and place it at this path before running remote-push runtime proof."
        )
        return 66

    expected_package = args.package_id.strip() if args.package_id is not None else ""
    if not expected_package:
        expected_package = resolve_project_package_id(project_path, configuration)

    client_packages = collect_client_package_names(load_json_object(config_path))
    if not client_packages:
        LOGGER.error("Firebase Android config has no Android client package names: %s", config_path)
        return 65

    if expected_package not in client_packages:
        LOGGER.error(
            "Firebase Android config does not contain package %s for %s builds.",
            expected_package,
            configuration,
        )
        LOGGER.error("Config package names: %s", ", ".join(client_packages))
        return 65

    LOGGER.info("Firebase Android config is present: %s", config_path)
    LOGGER.info("Android configuration: %s", configuration)
    LOGGER.info("Expected package: %s", expected_package)
    LOGGER.info("Config package names: %s", ", ".join(client_packages))
    return 0


def configure_logging() -> None:
    logging.basicConfig(level=logging.INFO, format="%(message)s", stream=sys.stdout)


def parse_args(argv: Sequence[str] | None, repo_root: Path) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Validate that the Android Firebase google-services.json file exists "
            "and contains the package id for the requested Cotton Mobile build."
        )
    )
    parser.add_argument(
        "--configuration",
        default=os.environ.get("COTTON_ANDROID_CONFIGURATION", "Debug"),
        help="Android build configuration to validate. Defaults to COTTON_ANDROID_CONFIGURATION or Debug.",
    )
    parser.add_argument(
        "--package-id",
        default=os.environ.get("COTTON_ANDROID_PACKAGE_ID"),
        help="Expected Android package id. Defaults to the ApplicationId resolved from the project.",
    )
    parser.add_argument(
        "--project",
        type=Path,
        default=repo_root / "src/Cotton.Mobile/Cotton.Mobile.csproj",
        help="Path to Cotton.Mobile.csproj.",
    )
    parser.add_argument(
        "--config-file",
        type=Path,
        default=repo_root / "src/Cotton.Mobile/Platforms/Android/google-services.json",
        help="Path to google-services.json.",
    )
    return parser.parse_args(argv)


def get_repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def resolve_project_package_id(project_path: Path, configuration: str) -> str:
    application_ids = load_project_application_ids(project_path)
    configuration_key = configuration.lower()
    if configuration_key in application_ids:
        return application_ids[configuration_key]

    release_package = application_ids.get("release")
    if release_package is None:
        raise ValueError(f"Project has no default ApplicationId: {project_path}")

    return release_package


def load_project_application_ids(project_path: Path) -> dict[str, str]:
    if not project_path.exists():
        raise FileNotFoundError(f"Project file was not found: {project_path}")

    root = ElementTree.parse(project_path).getroot()
    default_application_id: str | None = None
    application_ids: dict[str, str] = {}
    for element in root.iter():
        if strip_xml_namespace(element.tag) != "ApplicationId":
            continue

        value = (element.text or "").strip()
        if not value:
            continue

        condition = element.attrib.get("Condition", "")
        condition_configuration = parse_configuration_condition(condition)
        if condition_configuration is None:
            default_application_id = value
        else:
            application_ids[condition_configuration.lower()] = value

    if default_application_id is not None:
        application_ids["release"] = default_application_id

    return application_ids


def strip_xml_namespace(tag: str) -> str:
    namespace_end = tag.rfind("}")
    return tag[namespace_end + 1 :] if namespace_end >= 0 else tag


def parse_configuration_condition(condition: str) -> str | None:
    marker = "'$(Configuration)' == '"
    start = condition.find(marker)
    if start < 0:
        return None

    value_start = start + len(marker)
    value_end = condition.find("'", value_start)
    if value_end < 0:
        return None

    return condition[value_start:value_end]


def load_json_object(config_path: Path) -> Mapping[str, JsonValue]:
    with config_path.open("r", encoding="utf-8") as config_file:
        data = cast(JsonValue, json.load(config_file))

    if not isinstance(data, dict):
        raise ValueError(f"Firebase Android config must be a JSON object: {config_path}")

    return data


def collect_client_package_names(config: Mapping[str, JsonValue]) -> list[str]:
    clients = config.get("client")
    if not isinstance(clients, list):
        return []

    package_names: list[str] = []
    for client in clients:
        client_info = get_object(client, "client_info")
        android_client_info = get_object(client_info, "android_client_info")
        package_name = get_string(android_client_info, "package_name")
        if package_name is not None and package_name not in package_names:
            package_names.append(package_name)

    return package_names


def get_object(value: JsonValue | None, key: str) -> Mapping[str, JsonValue] | None:
    if not isinstance(value, dict):
        return None

    child = value.get(key)
    return child if isinstance(child, dict) else None


def get_string(value: Mapping[str, JsonValue] | None, key: str) -> str | None:
    if value is None:
        return None

    child = value.get(key)
    return child if isinstance(child, str) and child.strip() else None


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (FileNotFoundError, ValueError, json.JSONDecodeError, ElementTree.ParseError) as exception:
        configure_logging()
        LOGGER.error("%s", exception)
        raise SystemExit(65)
