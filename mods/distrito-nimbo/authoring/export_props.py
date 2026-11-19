"""Bake props from the open Distrito Nimbo Blender scene for the Unity bundle builder."""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from district_landmarks import landmarks


def unity_vector(vector):
    return dict(x=vector.x, y=vector.z, z=-vector.y)


def bake(name, objects, pivot, collide, material_colors=None):
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
                material = mesh.materials[face.material_index]
                color = material_colors.get(material.name, material.diffuse_color) if material_colors else material.diffuse_color
                for index, loop in zip(face.vertices, face.loops):
                    local_normal = mesh.corner_normals[loop].vector if mesh.polygons[face.polygon_index].use_smooth else face.normal
                    normal = unity_vector((normal_transform @ local_normal).normalized())
                    points.append(unity_vector(transform @ mesh.vertices[index].co - pivot))
                    normals.append(normal)
                    colors.append(dict(r=color[0], g=color[1], b=color[2], a=color[3]))
        finally:
            model.to_mesh_clear()
    return dict(name=name, points=points, normals=normals, colors=colors, collide=collide)


def capsule(name, dome):
    # A recognizable capsule ball: even shell halves, a dark equatorial band
    # and a round release button facing the street on both sides.
    radius, sides = .67, 28
    latitudes = (-85, -75, -65, -55, -45, -35, -25, -15, -5, 5, 15, 25, 35, 45, 55, 65, 75, 85)
    points = []
    for degrees in latitudes:
        latitude = math.radians(degrees)
        for index in range(sides):
            angle = math.tau * index / sides
            points.append((radius * math.cos(latitude) * math.cos(angle),
                           radius * math.cos(latitude) * math.sin(angle),
                           radius * (1 + math.sin(latitude))))
    faces = [tuple(reversed(range(sides)))]
    for ring in range(len(latitudes) - 1):
        start = ring * sides
        for index in range(sides):
            other = (index + 1) % sides
            faces.append((start + index, start + other, start + sides + other, start + sides + index))
    faces.append(tuple(range(len(points) - sides, len(points))))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(points, [], faces)
    mesh.update()
    for label, rgba in (("Shell", (.96, .95, .92, 1)), ("Band", (.05, .05, .06, 1)),
                        ("Dome", dome), ("Button", (.08, .08, .09, 1))):
        material = bpy.data.materials.new(label)
        material.diffuse_color = rgba
        mesh.materials.append(material)
    # Band rows sit between latitudes -15 and 15; the release button is a
    # diamond of shell faces centered on the band at sides 7 and 21.
    band_rows = (7, 8, 9)
    for face in mesh.polygons:
        if face.index == 0:
            continue
        ring, side = divmod(face.index - 1, sides)
        if ring in band_rows:
            material_index = 1
        elif ring < band_rows[0]:
            material_index = 0
        else:
            material_index = 2
        for center in (7, 21):
            distance = min((side - center) % sides, sides - (side - center) % sides)
            if ring in band_rows and distance <= 2 - abs(ring - 8):
                material_index = 3 if (ring == 8 and distance == 0) else 0
        face.material_index = material_index
        face.use_smooth = 0 < face.index < len(faces) - 1
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    lowest = min(point[2] for point in points)
    return bake(name, [obj], Vector((0, 0, lowest)), False)


def access_house(name, plaster, roof_color):
    parts = []
    materials = {}

    def piece(label, location, size, color, tilt=0):
        bpy.ops.mesh.primitive_cube_add(size=1, location=location)
        obj = bpy.context.object
        obj.name = f"{name} {label}"
        obj.scale = size
        obj.rotation_euler.x = tilt
        if color not in materials:
            material = bpy.data.materials.new(obj.name)
            material.diffuse_color = color
            materials[color] = material
        obj.data.materials.append(materials[color])
        parts.append(obj)

    masonry = (.91, .88, .77, 1)
    steel = (.17, .30, .42, 1)
    glass = (.25, .61, .81, 1)
    shade = (.60, .77, .86, 1)
    piece("Plinth", (0, 0, .12), (4.2, 4.65, .24), masonry)
    piece("Walls", (0, 0, 1.24), (3.8, 4.2, 2.2), plaster)
    piece("Sill", (0, 0, .38), (3.9, 4.3, .24), masonry)
    piece("Eaves", (0, 0, 2.27), (4.27, 4.56, .2), steel)
    for side in (-1, 1):
        piece("Sloped standing seam roof", (0, side * 1.14, 2.53), (4.37, 2.52, .20), roof_color, -side * .17)
        for x in (-1.47, -.73, 0, .73, 1.47):
            seam = tuple(channel * .85 for channel in roof_color[:3]) + (1,)
            piece("Seam", (x, side * 1.14, 2.65), (.048, 2.41, .065), seam, -side * .17)
    piece("Capped ridge", (0, 0, 2.83), (4.40, .20, .17), roof_color)
    for side in (-1, 1):
        x = side * 2.13
        mesh = bpy.data.meshes.new(f"{name} end wall")
        mesh.from_pydata([(x, -2.24, 2.33), (x, 2.24, 2.33), (x, 0, 2.72)],
                         [], [(0, 1, 2) if side > 0 else (2, 1, 0)])
        mesh.update()
        mesh.materials.append(materials[plaster])
        obj = bpy.data.objects.new(f"{name} end wall", mesh)
        bpy.context.scene.collection.objects.link(obj)
        parts.append(obj)
    piece("Door surround", (-.88, -2.12, .97), (1.26, .10, 1.66), masonry)
    piece("Blue service door", (-.88, -2.19, .96), (1.04, .10, 1.5), steel)
    piece("Door glass", (-.88, -2.256, 1.34), (.71, .03, .36), glass)
    piece("Canopy", (-.88, -2.35, 1.90), (1.55, .48, .13), roof_color)
    for position in ((.86, -2.14, 1.38), (.86, 2.14, 1.38)):
        x, y, z = position
        side = -1 if y < 0 else 1
        piece("Window frame", position, (1.42, .10, .70), steel)
        piece("Window glass", (x, y + side * .063, z), (1.23, .042, .54), glass)
        piece("Mullion", (x, y + side * .096, z), (.072, .021, .55), shade)
    return bake(name, parts, Vector(), True)


def main():
    destination = Path(sys.argv[sys.argv.index("--") + 1])
    kit = bpy.data.collections["03 Rooftop kit"]
    ac_prefix = ("AC pad", "Rooftop heat pump", "Vent blade", "Fan ring", "Fan hub")
    air_conditioner = [obj for obj in kit.objects if obj.name.startswith(ac_prefix)
                       and abs(obj.location.x - 6.5) < 1.3 and abs(obj.location.y - 4.7) < 0.9]
    antenna = [obj for obj in kit.objects if obj.name.startswith(("Signal mast", "Antenna element", "Antenna terminal", "Mast beacon"))]
    head = [obj for obj in kit.objects if obj.name.startswith(("Bald basalt head", "Plinth of the bald basalt head"))]
    terracotta = {
        "Basalt sculpture": (.46, .16, .065, 1),
        "Shell concrete": (.62, .27, .12, 1),
        "Vents and rails": (.13, .046, .026, 1),
    }
    props = [
        bake("TerminalHeatPump", air_conditioner, Vector((6.5, 4.7, 7.16)), True),
        bake("SignalAntenna", antenna, Vector((5.5, 20.1, 11.3)), False),
        bake("BasaltHead", head, Vector((-19.9, 1.6, 5.51)), True, terracotta),
        capsule("CargoCapsule", (.83, .03, .06, 1)),
        capsule("SilverCargoCapsule", (.16, .35, .74, 1)),
        access_house("SageAccessHouse", (.96, .76, .35, 1), (.29, .59, .39, 1)),
        access_house("LilacAccessHouse", (.95, .85, .63, 1), (.47, .40, .69, 1)),
    ]
    for root, objects in landmarks():
        props.append(bake(root.name, objects, Vector(), True))
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps({"props": props}, separators=(",", ":")) + "\n", encoding="utf-8")
    for prop in props:
        print(f"{prop['name']}: {len(prop['points']) // 3} triangles")


if __name__ == "__main__":
    main()
