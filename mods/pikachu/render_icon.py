import bpy, os
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath=os.path.abspath('mods/pikachu/study/pikachu-movement.blend'))
scene=bpy.context.scene
arm=bpy.data.objects['Armature']; arm.animation_data.action=bpy.data.actions['OpenBonk idle']; scene.frame_set(1)
bpy.data.objects['OpenBonkSurfboard'].hide_render=True
# A tight, mostly frontal crop: the face fills the selection icon and the
# upper ears can leave the frame, as on several original character portraits.
center=Vector((0,-.55,6.95)); camera=scene.camera
camera.location=center+Vector((1.3,-20,1.1)); camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler(); camera.data.ortho_scale=5.6
scene.render.engine='CYCLES'; scene.cycles.samples=32; scene.render.resolution_x=256; scene.render.resolution_y=256; scene.render.resolution_percentage=100; scene.render.film_transparent=True; scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'; scene.render.filepath=os.path.abspath('mods/pikachu/study/portrait-face-render.png')
bpy.ops.render.render(write_still=True)
