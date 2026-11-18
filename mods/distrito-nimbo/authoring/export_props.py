"""Bake props from the open Distrito Nimbo Blender scene for the Unity bundle builder."""
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def unity_vector(vector):
    return dict(x=vector.x, y=vector.z, z=-vector.y)


def bake(name, objects, pivot, collide):
    points, normals, colors = [], [], []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    if not objects:
        raise ValueError(f"No parts for {name}")
    for source in sorted(objects, key=lambda part: part.name):
        model = source.evaluated_get(depsgraph)
        mesh = model.to_mesh()
        try:
            mesh.calc_loop_triangles()
            transform = model.matrix_world
            normal_transform = transform.to_3x3().inverted().transposed()
            for face in mesh.loop_triangles:
                color = mesh.materials[face.material_index].diffuse_color
                normal = unity_vector((normal_transform @ face.normal).normalized())
                for index in face.vertices:
                    points.append(unity_vector(transform @ mesh.vertices[index].co - pivot))
                    normals.append(normal)
                    colors.append(dict(r=color[0], g=color[1], b=color[2], a=color[3]))
        finally:
            model.to_mesh_clear()
    return dict(name=name, points=points, normals=normals, colors=colors, collide=collide)


def main():
    destination = Path(sys.argv[sys.argv.index("--") + 1])
    kit = bpy.data.collections["03 Rooftop kit"]
    ac_prefix = ("AC pad", "Rooftop heat pump", "Vent blade", "Fan ring", "Fan hub")
    air_conditioner = [obj for obj in kit.objects if obj.name.startswith(ac_prefix)
                       and abs(obj.location.x - 6.5) < 1.3 and abs(obj.location.y - 4.7) < 0.9]
    antenna = [obj for obj in kit.objects if obj.name.startswith(("Signal mast", "Antenna element", "Antenna terminal", "Mast beacon"))]
    head = [obj for obj in kit.objects if obj.name.startswith(("Bald basalt head", "Plinth of the bald basalt head"))]
    props = [
        bake("TerminalHeatPump", air_conditioner, Vector((6.5, 4.7, 7.16)), True),
        bake("SignalAntenna", antenna, Vector((5.5, 20.1, 11.3)), False),
        bake("BasaltHead", head, Vector((-19.9, 1.6, 5.51)), True),
    ]
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps({"props": props}, separators=(",", ":")) + "\n", encoding="utf-8")
    for prop in props:
        print(f"{prop['name']}: {len(prop['points']) // 3} triangles")


if __name__ == "__main__":
    main()
