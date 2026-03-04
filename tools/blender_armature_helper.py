#!/usr/bin/env python3
"""
Blender helper for rig display after GLB/GLTF import.

Use cases:
- Mixamo/Meshy imports where armature display looks like "golfball"
- Keep rig/animation data but make viewport readable

Run examples:
  blender --python tools/blender_armature_helper.py -- \
      --import "/abs/path/model.glb" --display stick

  blender --python tools/blender_armature_helper.py -- \
      --display hide
"""

from __future__ import annotations

import argparse
import os
import sys

import bpy


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Adjust Blender armature viewport display.")
    parser.add_argument("--import", dest="import_path", default="", help="Optional GLB/GLTF to import first.")
    parser.add_argument(
        "--display",
        default="stick",
        choices=["stick", "octahedral", "wire", "bbone", "envelope", "hide"],
        help="Armature viewport display mode.",
    )
    parser.add_argument(
        "--target",
        default="all",
        choices=["all", "selected"],
        help="Apply to all armatures or selected armatures only.",
    )
    return parser.parse_args(argv)


def import_gltf_if_requested(path: str) -> None:
    if not path:
        return
    ext = os.path.splitext(path)[1].lower()
    if ext not in {".glb", ".gltf"}:
        raise ValueError(f"Unsupported file type: {ext}")
    bpy.ops.import_scene.gltf(filepath=path)


def iter_target_armatures(target: str):
    if target == "selected":
        for obj in bpy.context.selected_objects:
            if obj and obj.type == "ARMATURE":
                yield obj
        return
    for obj in bpy.data.objects:
        if obj.type == "ARMATURE":
            yield obj


def apply_display(objects, mode: str) -> int:
    count = 0
    if mode == "hide":
        for obj in objects:
            obj.hide_set(True)
            obj.hide_viewport = True
            count += 1
        return count

    map_mode = {
        "stick": "STICK",
        "octahedral": "OCTAHEDRAL",
        "wire": "WIRE",
        "bbone": "BBONE",
        "envelope": "ENVELOPE",
    }
    display_type = map_mode[mode]
    for obj in objects:
        obj.hide_set(False)
        obj.hide_viewport = False
        obj.data.display_type = display_type
        count += 1
    return count


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    import_gltf_if_requested(args.import_path)
    armatures = list(iter_target_armatures(args.target))
    count = apply_display(armatures, args.display)
    print(f"[blender_armature_helper] mode={args.display} target={args.target} armatures={count}")
    return 0


if __name__ == "__main__":
    # Blender passes args before/after "--". Keep only user args after separator.
    raw = sys.argv
    user_argv = raw[raw.index("--") + 1 :] if "--" in raw else []
    raise SystemExit(main(user_argv))
