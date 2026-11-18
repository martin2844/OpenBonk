"""Author an original animated rooftop beetle. Run in background Blender."""
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector


def color(name, rgba):
    material = bpy.data.materials.new(name)
    material.diffuse_color = rgba
    return material


def joint(name, parent, location):
    control = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(control)
    control.parent = parent
    control.location = location
    control.empty_display_size = .14
    return control


def finish(name, material, parent):
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    obj.parent = parent
    return obj


def volume(name, location, size, material, parent):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4, location=location)
    obj = finish(name, material, parent)
    obj.scale = size
    return obj


def block(name, location, size, material, parent):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = finish(name, material, parent)
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = obj.modifiers.new("Trimmed edges", "BEVEL")
    bevel.width = min(size) * .21
    bevel.segments = 1
    return obj


def segment(name, start, end, widths, material, parent):
    a, b = Vector(start), Vector(end)
    bpy.ops.mesh.primitive_cone_add(vertices=5, radius1=widths[0], radius2=widths[1],
                                   depth=(b - a).length, location=(a + b) * .5)
    obj = finish(name, material, parent)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = (b - a).to_track_quat("Z", "Y")
    return obj


def make_beetle():
    chassis = color("Dark petrol chassis", (.033, .071, .083, 1))
    plate = color("Warm ivory ceramic", (.81, .72, .49, 1))
    warning = color("Burnt orange trim", (.8, .23, .07, 1))
    lamp = color("Pear green optical glass", (.72, .96, .30, 1))
    steel = color("Cool blue knee caps", (.19, .36, .44, 1))
    root = joint("Beacon beetle", None, (0, 0, 0))
    shell = joint("Chassis suspension", root, (0, 0, 0))
    volume("Tapered underside", (0, .08, .79), (.63, .98, .37), chassis, shell)
    block("Transverse bumper", (0, -.61, .72), (1.19, .2, .22), warning, shell)
    head = joint("Optic pitch", shell, (0, -.82, .73))
    block("Head case", (0, -.16, 0), (.75, .54, .42), chassis, head)
    block("Transverse glass", (0, -.437, .04), (.56, .072, .16), lamp, head)
    block("Central brow", (0, -.38, .17), (.63, .22, .10), plate, head)
    pivots = []
    for side in (-1, 1):
        cover = volume("Ceramic wing case", (side * .33, .18, 1.06), (.38, .86, .39), plate, shell)
        cover.rotation_euler.z = math.radians(side * -8)
        block("Signal stripe", (side * .38, .46, 1.37), (.23, .26, .066), warning, shell)
        segment("Feelers", (side * .27, -.24, .16), (side * .47, -.5, .44), (.064, .027), steel, head)
        volume("Feeler tips", (side * .47, -.5, .44), (.058, .058, .055), lamp, head)
        for index, y in enumerate((-.46, .04, .50)):
            bend = -.29 if index == 0 else .34 if index == 2 else .04
            knee_point = (side * .43, bend, -.17)
            hip = joint("Hip", shell, (side * .47, y, .76))
            segment("Thigh", (0, 0, 0), knee_point, (.13, .108), chassis, hip)
            knee = joint("Knee", hip, knee_point)
            volume("Kneecap", (0, 0, 0), (.145, .137, .14), steel, knee)
            toe = (side * .16, bend * .55, -.55)
            segment("Shin", (0, 0, 0), toe, (.086, .051), chassis, knee)
            block("Foot pad", (toe[0], toe[1] - .045, toe[2]), (.19, .25, .083), warning, knee)
            pivots.append((side, index, hip, knee))

    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1, 45
    scene.render.fps = 12
    for label, frame in (("Walk", 1), ("Idle", 17), ("Charge", 33)):
        scene.timeline_markers.new(label, frame=frame)
    for frame in range(1, 46):
        if frame in (14, 15, 16, 30, 31, 32):
            continue
        walk = frame <= 13
        charge = frame >= 33
        phase = math.tau * ((frame - 1) % 16) / 12
        shell.location.z = .022 * math.cos(phase * 2) if walk else .018 * math.sin(phase)
        shell.rotation_euler.x = -.20 * math.sin(phase * .5) if charge else .028 * math.sin(phase)
        shell.keyframe_insert("location", frame=frame)
        shell.keyframe_insert("rotation_euler", frame=frame)
        head.rotation_euler.x = .13 * math.sin(phase * .5) if charge else -.045 * math.sin(phase)
        head.keyframe_insert("rotation_euler", frame=frame)
        for side, index, hip, knee in pivots:
            stride = phase + (index % 2 + (side < 0)) * math.pi
            lift = max(0, math.cos(stride))
            hip.rotation_euler.z = side * (.24 * math.sin(stride) if walk else .04 * math.sin(phase))
            hip.rotation_euler.y = -side * (.32 * lift if walk else .075 * math.sin(phase))
            knee.rotation_euler.y = side * (.20 * lift if walk else .035 * math.cos(phase))
            hip.keyframe_insert("rotation_euler", frame=frame)
            knee.keyframe_insert("rotation_euler", frame=frame)
    scene.frame_set(1)


def preview(destination):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x, scene.render.resolution_y = 1000, 800
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "BOTH"
    scene.display.shading.background_type = "WORLD"
    scene.world.color = (.047, .059, .075)
    bpy.ops.object.camera_add(location=(3.7, -5.6, 3.2))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0, 0, .73)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 4.5
    scene.camera = camera
    scene.render.filepath = str(destination)


def main():
    model, render = map(Path, sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.world = bpy.data.worlds.new("Studio")
    make_beetle()
    preview(render)
    bpy.ops.wm.save_as_mainfile(filepath=str(model))


if __name__ == "__main__":
    main()
