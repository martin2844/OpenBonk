import bpy, os
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath=os.path.abspath('mods/pikachu/study/pikachu-study.blend'))
arm=bpy.data.objects['Armature']
for bone in arm.data.bones:
    if any(k in bone.name for k in ('Waist','Thigh','Arm_','Head','Ear1','Foot','Hand')):
        print('RIG', bone.name, 'parent', bone.parent.name if bone.parent else None, 'head', tuple(round(v,3) for v in arm.matrix_world @ bone.head_local), 'tail', tuple(round(v,3) for v in arm.matrix_world @ bone.tail_local))
