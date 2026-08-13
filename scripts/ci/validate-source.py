#!/usr/bin/env python3

import logging
import re
import sys
from pathlib import Path
from re import Pattern


LOGGER = logging.getLogger(__name__)
REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIRECTORIES = (
    REPOSITORY_ROOT / ".github" / "workflows",
    REPOSITORY_ROOT / "src",
    REPOSITORY_ROOT / "tests",
    REPOSITORY_ROOT / "scripts",
)
SOURCE_SUFFIXES = frozenset(
    {".cs", ".css", ".py", ".sh", ".ts", ".tsx", ".xaml", ".yaml", ".yml"}
)
IGNORED_DIRECTORIES = frozenset({"bin", "obj"})
MAX_LOGICAL_LINES = 300

SEALED_TYPE_PATTERN = re.compile(r"\bsealed\s+(?:class|record)\b")
FILE_SCOPED_NAMESPACE_PATTERN = re.compile(r"^\s*namespace\s+[\w.]+\s*;", re.MULTILINE)
IMPLICIT_LOCAL_TYPE_PATTERN = re.compile(r"\bvar\s+[A-Za-z_]\w*\s*=")
SILENT_SWITCH_FALLBACK_PATTERN = re.compile(r"^\s*_\s*=>(?!\s*throw\b)", re.MULTILINE)
EMPTY_CATCH_PATTERN = re.compile(
    r"\bcatch\s*(?:\([^)]*\))?\s*(?:when\s*\([^)]*\))?\s*\{\s*\}",
    re.MULTILINE,
)
TYPE_DECLARATION_PATTERN = re.compile(
    r"^\s*(?:(?:public|internal|private|protected|abstract|partial|static|readonly|"
    r"ref|unsafe|new|sealed)\s+)*"
    r"(?:class|enum|interface|struct|record(?:\s+(?:class|struct))?)\s+\w+",
    re.MULTILINE,
)
VIEWPORT_UNIT_PATTERN = re.compile(r"(?<![A-Za-z0-9_.])\d+(?:\.\d+)?d?v[hw]\b", re.IGNORECASE)
TYPESCRIPT_FORBIDDEN_PATTERNS: tuple[tuple[str, Pattern[str]], ...] = (
    ("any", re.compile(r"\bany\b")),
    ("unknown", re.compile(r"\bunknown\b")),
    ("nested assertion", re.compile(r"\bas\s+unknown\s+as\b")),
    ("localStorage", re.compile(r"\blocalStorage\b")),
    ("console", re.compile(r"\bconsole\s*\.")),
)
XAML_VISIBLE_TEXT_PATTERN = re.compile(
    r'(?:\bText|\bTitle|\bPlaceholder|SemanticProperties\.Description|'
    r'AutomationProperties\.Name|\bContent)\s*=\s*"([^"]*)"'
)


def iter_source_files() -> list[Path]:
    source_files: list[Path] = []
    for source_directory in SOURCE_DIRECTORIES:
        for path in source_directory.rglob("*"):
            if not path.is_file() or path.suffix.lower() not in SOURCE_SUFFIXES:
                continue
            if any(part in IGNORED_DIRECTORIES for part in path.parts):
                continue
            source_files.append(path)
    return sorted(source_files)


def count_logical_lines(content: str, suffix: str) -> int:
    comment_prefixes = ("//", "/*", "*", "*/")
    if suffix == ".xaml":
        comment_prefixes = ("<!--", "-->")
    elif suffix in {".py", ".sh", ".yaml", ".yml"}:
        comment_prefixes = ("#",)

    return sum(
        1
        for line in content.splitlines()
        if line.strip() and not line.lstrip().startswith(comment_prefixes)
    )


def relative_path(path: Path) -> str:
    return path.relative_to(REPOSITORY_ROOT).as_posix()


def line_number(content: str, offset: int) -> int:
    return content.count("\n", 0, offset) + 1


def validate_file_size(path: Path, content: str) -> list[str]:
    logical_lines = count_logical_lines(content, path.suffix.lower())
    if logical_lines <= MAX_LOGICAL_LINES:
        return []
    return [
        f"{relative_path(path)} has {logical_lines} logical lines; maximum is {MAX_LOGICAL_LINES}."
    ]


def validate_csharp(path: Path, content: str) -> list[str]:
    violations: list[str] = []
    type_declarations = TYPE_DECLARATION_PATTERN.findall(content)
    if len(type_declarations) > 1:
        violations.append(
            f"{relative_path(path)} declares {len(type_declarations)} types; maximum is one."
        )
    for label, pattern in (
        ("sealed type", SEALED_TYPE_PATTERN),
        ("file-scoped namespace", FILE_SCOPED_NAMESPACE_PATTERN),
        ("implicit local type", IMPLICIT_LOCAL_TYPE_PATTERN),
        ("non-throwing switch fallback", SILENT_SWITCH_FALLBACK_PATTERN),
        ("empty catch block", EMPTY_CATCH_PATTERN),
    ):
        for match in pattern.finditer(content):
            violations.append(
                f"{relative_path(path)}:{line_number(content, match.start())} contains {label}."
            )
    violations.extend(validate_csharp_brace_spacing(path, content))
    return violations


def validate_csharp_brace_spacing(path: Path, content: str) -> list[str]:
    violations: list[str] = []
    lines = content.splitlines()
    for index, line in enumerate(lines):
        if line.strip() == "{" and index + 1 < len(lines) and not lines[index + 1].strip():
            violations.append(
                f"{relative_path(path)}:{index + 2} contains an empty line after an opening brace."
            )
        if line.strip() == "}" and index > 0 and not lines[index - 1].strip():
            violations.append(
                f"{relative_path(path)}:{index + 1} contains an empty line before a closing brace."
            )
    return violations


def validate_typescript(path: Path, content: str) -> list[str]:
    violations: list[str] = []
    for label, pattern in TYPESCRIPT_FORBIDDEN_PATTERNS:
        for match in pattern.finditer(content):
            violations.append(
                f"{relative_path(path)}:{line_number(content, match.start())} contains forbidden {label}."
            )
    return violations


def validate_xaml(path: Path, content: str) -> list[str]:
    violations: list[str] = []
    for match in XAML_VISIBLE_TEXT_PATTERN.finditer(content):
        value = match.group(1).strip()
        if value.startswith("{") or not re.search(r"[A-Za-z]", value):
            continue
        violations.append(
            f"{relative_path(path)}:{line_number(content, match.start())} contains literal visible text."
        )
    return violations


def validate_viewport_units(path: Path, content: str) -> list[str]:
    return [
        f"{relative_path(path)}:{line_number(content, match.start())} contains a viewport unit."
        for match in VIEWPORT_UNIT_PATTERN.finditer(content)
    ]


def validate_source_file(path: Path) -> list[str]:
    content = path.read_text(encoding="utf-8")
    suffix = path.suffix.lower()
    violations = validate_file_size(path, content)
    violations.extend(validate_viewport_units(path, content))
    if suffix == ".cs":
        violations.extend(validate_csharp(path, content))
    elif suffix in {".ts", ".tsx"}:
        violations.extend(validate_typescript(path, content))
    elif suffix == ".xaml":
        violations.extend(validate_xaml(path, content))
    return violations


def main() -> int:
    logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")
    source_files = iter_source_files()
    violations = [
        violation
        for path in source_files
        for violation in validate_source_file(path)
    ]
    if violations:
        for violation in violations:
            LOGGER.error(violation)
        return 1

    LOGGER.info("Validated %d source files.", len(source_files))
    return 0


if __name__ == "__main__":
    sys.exit(main())
