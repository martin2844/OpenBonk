"""Render eight fixed phases of the authored run from two views, without altering the blend."""
import bpy, math, os
from mathutils import Vector

root = os.path.abspath('mods/pikachu/study')
blend = os.environ.get('OPENBONK_PIKA_BLEND', 'pikachu-movement.blend')
prefix = os.environ.get('OPENBONK_PIKA_PREFIX', 'run-cycle')
bpy.ops.wm.open_mainfile(filepath=os.path.join(root, blend))
scene = bpy.context.scene
bpy.data.objects['Armature'].animation_data.action = bpy.data.actions['OpenBonk run']
bpy.data.objects['OpenBonkSurfboard'].hide_render = True
scene.render.engine = 'CYCLES'; scene.cycles.samples = 8
scene.render.resolution_x = scene.render.resolution_y = 320
scene.render.resolution_percentage = 100
camera = scene.camera; camera.data.type = 'ORTHO'; camera.data.ortho_scale = 17
center = Vector((0, -.4, 4.5))
bpy.ops.mesh.primitive_plane_add(size=100, location=(0, 0, -.08))
floor = bpy.context.object; floor.name = 'Study floor only'
material = bpy.data.materials.new('Study floor'); material.diffuse_color = (.22, .28, .22, 1); floor.data.materials.append(material)
for name, angle in [('side', 90), ('front', 0)]:
    radians = math.radians(angle)
    camera.location = center + Vector((math.sin(radians)*20, -math.cos(radians)*20, 3.5))
    camera.rotation_euler = (center-camera.location).to_track_quat('-Z', 'Y').to_euler()
    for frame in range(1, 17, 2):
        scene.frame_set(frame)
        scene.render.filepath = os.path.join(root, f'{prefix}-{name}-{frame:02}.png')
        bpy.ops.render.render(write_still=True)
