"""Create an isolated foreleg-contact experiment; never overwrites the shipped model."""
import bpy, os
from mathutils import Vector

root = os.path.abspath('mods/pikachu/study')
bpy.ops.wm.open_mainfile(filepath=os.path.join(root, 'pikachu-movement.blend'))
scene = bpy.context.scene; arm = bpy.data.objects['Armature']
arms = ('LArm_Armature', 'RArm_Armature')
run = bpy.data.actions['OpenBonk run']; arm.animation_data.action = run
delta = arm.matrix_world.to_3x3().inverted() @ Vector((0, 0, -.5))
for frame in (1, 5, 9, 13, 17):
    scene.frame_set(frame)
    for name in arms:
        bone = arm.pose.bones[name]; bone.location = (0, 0, 0); bpy.context.view_layer.update()
        matrix = bone.matrix.copy(); matrix.translation += delta; bone.matrix = matrix
        bone.keyframe_insert(data_path='location', frame=frame, group=name)
for name in ('OpenBonk idle', 'OpenBonk walk study', 'OpenBonk jump', 'OpenBonk surf'):
    action = bpy.data.actions[name]; arm.animation_data.action = action
    for frame in (int(action.frame_range[0]), int(action.frame_range[1])):
        scene.frame_set(frame)
        for bone_name in arms:
            bone = arm.pose.bones[bone_name]; bone.location = (0, 0, 0)
            bone.keyframe_insert(data_path='location', frame=frame, group=bone_name)
arm.animation_data.action = run; scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root, 'pikachu-contact-study.blend'))
