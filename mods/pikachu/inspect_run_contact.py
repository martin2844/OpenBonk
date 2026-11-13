"""Study posed limb endpoints for alternative foreleg pitches without saving."""
import bpy, math, os
from mathutils import Quaternion, Vector

bpy.ops.wm.open_mainfile(filepath=os.path.abspath('mods/pikachu/study/pikachu-movement.blend'))
arm = bpy.data.objects['Armature']
arm.animation_data.action = bpy.data.actions['OpenBonk run']
def pitch(name, angle):
    axis = arm.data.bones[name].matrix_local.to_3x3().inverted() @ (arm.matrix_world.to_3x3().inverted() @ Vector((1, 0, 0)))
    arm.pose.bones[name].rotation_euler = Quaternion(axis.normalized(), angle).to_euler('XYZ')
for angle in (.5, .8, 1.1):
  for elbow in (-1.0, -.5, 0.0, .5, 1.0):
    bpy.context.scene.frame_set(5)
    for side in ('L', 'R'):
        pitch(side+'Arm_Armature', angle)
        pitch(side+'ForeArm_Armature', elbow)
    bpy.context.view_layer.update()
    for name in ('LHand_Armature', 'RHand_Armature', 'EffHand02_Armature', 'EffHand01_Armature'):
        p = arm.matrix_world @ arm.pose.bones[name].head
        print('CONTACT', angle, elbow, name, *(round(v, 3) for v in p))
