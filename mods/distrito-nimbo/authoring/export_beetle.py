"""Bake the editable Blender gait into Unity-coordinate mesh frames."""
import json
from pathlib import Path
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from export_props import bake


def main():
    destination = Path(sys.argv[sys.argv.index("--") + 1])
    scene = bpy.context.scene
    root = bpy.data.objects["Beacon beetle"]
    meshes = [child for child in root.children_recursive if child.type == "MESH"]
    clips = []
    for name in ("Walk", "Idle", "Charge"):
        start = scene.timeline_markers[name].frame
        frames = []
        for frame in range(start, start + 12):
            scene.frame_set(frame)
            frames.append(bake(f"BeaconBeetle{name}{frame - start:02}", meshes, Vector(), False))
        clips.append({"name": name.lower(), "frames": frames})
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps({"fps": scene.render.fps, "clips": clips}, separators=(",", ":")) + "\n",
                           encoding="utf-8")


if __name__ == "__main__":
    main()
