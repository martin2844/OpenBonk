"""Bake props from the open Distrito Nimbo Blender scene for the Unity bundle builder."""
import json
import math
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


def capsule(name, body, edge):
    vertices, faces, palette = [], [], []

    def piece(rings, color, center=(0, 0)):
        offset = len(vertices)
        sides = 8
        for height, radius in rings:
            for side in range(sides):
                radians = (side + 0.5) * math.tau / sides
                vertices.append((center[0] + math.cos(radians) * radius,
                                 center[1] + math.sin(radians) * radius, height))
        faces.append(tuple(offset + i for i in reversed(range(sides))))
        palette.append(color)
        for ring in range(len(rings) - 1):
            start = offset + ring * sides
            for i in range(sides):
                adjacent = (i + 1) % sides
                faces.append((start + i, start + adjacent, start + sides + adjacent, start + sides + i))
                palette.append(color)
        top = offset + (len(rings) - 1) * sides
        faces.append(tuple(top + i for i in range(sides)))
        palette.append(color)

    dark = (0.036, 0.064, 0.072, 1)
    light = (0.78, 1, 0.87, 1)
    piece(((0, .46), (.08, .61), (.25, .64), (.32, .58)), dark)
    piece(((.24, .58), (.38, .64), (.98, .64), (1.12, .52)), body)
    piece(((.49, .65), (.56, .67), (.65, .67), (.71, .65)), edge)
    piece(((1.10, .52), (1.18, .48), (1.25, .39)), dark)
    for x in (-.2, 0, .2):
        piece(((1.25, .065), (1.28, .06)), edge, (x, -.20))
    piece(((1.25, .20), (1.30, .20), (1.37, .14)), light, (0, .08))

    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    slots = list(dict.fromkeys(palette))
    for color in slots:
        material = bpy.data.materials.new(name)
        material.diffuse_color = color
        mesh.materials.append(material)
    for face, color in zip(mesh.polygons, palette):
        face.material_index = slots.index(color)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return bake(name, [obj], Vector((0, 0, 0)), False)


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
        capsule("CargoCapsule", (.085, .57, .45, 1), (1, .45, .10, 1)),
        capsule("SilverCargoCapsule", (.56, .67, .82, 1), (.33, .57, .98, 1)),
    ]
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps({"props": props}, separators=(",", ":")) + "\n", encoding="utf-8")
    for prop in props:
        print(f"{prop['name']}: {len(prop['points']) // 3} triangles")


if __name__ == "__main__":
    main()
