#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class FileEntry:
    path: str
    sha256: str
    size: int


def compute_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        while True:
            chunk = f.read(1024 * 1024)
            if not chunk:
                break
            h.update(chunk)
    return h.hexdigest()


def collect_entries(
    root: Path,
    output_name: str,
    exclude_extensions: set[str],
    exclude_names: set[str],
) -> list[FileEntry]:
    entries: list[FileEntry] = []
    for p in sorted(root.rglob("*")):
        if not p.is_file():
            continue
        if p.name == output_name:
            continue
        if p.name in exclude_names:
            continue
        if p.suffix.lower() in exclude_extensions:
            continue

        rel = p.relative_to(root).as_posix()
        entries.append(
            FileEntry(
                path=rel,
                sha256=compute_sha256(p),
                size=p.stat().st_size,
            )
        )
    return entries


def build_manifest(version: str, entry: str, files: list[FileEntry]) -> str:
    lines: list[str] = [
        "Manifest {",
        f'    version: "{version}"',
        f'    entry: "{entry}"',
        "",
        "    Files {",
    ]

    for file in files:
        lines.append(
            f'        File {{ path: "{file.path}" hash: "sha256:{file.sha256}" size: {file.size} }}'
        )

    lines.extend([
        "    }",
        "}",
        "",
    ])
    return "\n".join(lines)


def compute_version(files: list[FileEntry]) -> str:
    digest = hashlib.sha256()
    for file in files:
        digest.update(file.path.encode("utf-8"))
        digest.update(b"\0")
        digest.update(file.sha256.encode("utf-8"))
        digest.update(b"\0")
        digest.update(str(file.size).encode("utf-8"))
        digest.update(b"\n")
    return f"auto-{digest.hexdigest()[:16]}"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate manifest.sml for ForgeGodot sample content.")
    parser.add_argument("--root", default="docs/SampleProject", help="Root directory containing published sample files.")
    parser.add_argument("--entry", default="UI.sml", help="Entry SML file path relative to --root.")
    parser.add_argument("--output", default="manifest.sml", help="Output manifest filename.")
    parser.add_argument(
        "--exclude-ext",
        action="append",
        default=[".import", ".cs"],
        help="File extension to exclude from manifest (repeatable). Defaults: .import, .cs",
    )
    parser.add_argument(
        "--exclude-name",
        action="append",
        default=[".DS_Store"],
        help="Exact filename to exclude from manifest (repeatable). Default: .DS_Store",
    )
    args = parser.parse_args()

    root = Path(args.root).resolve()
    if not root.exists() or not root.is_dir():
        raise SystemExit(f"Root folder does not exist or is not a directory: {root}")

    entry_file = root / args.entry
    if not entry_file.exists() or not entry_file.is_file():
        raise SystemExit(f"Entry file does not exist: {entry_file}")

    exclude_extensions = {ext.lower() if ext.startswith(".") else f".{ext.lower()}" for ext in args.exclude_ext}
    exclude_names = set(args.exclude_name)

    files = collect_entries(root, args.output, exclude_extensions, exclude_names)
    version = compute_version(files)
    manifest = build_manifest(version=version, entry=args.entry, files=files)

    output_path = root / args.output
    output_path.write_text(manifest, encoding="utf-8")
    print(f"Generated {output_path} with {len(files)} file entries.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
