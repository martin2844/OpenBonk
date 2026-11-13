import bpy, os
bpy.ops.wm.open_mainfile(filepath=os.path.abspath('mods/pikachu/study/pikachu-movement.blend'))
arm=bpy.data.objects['Armature']; arm.animation_data.action=bpy.data.actions['OpenBonk run']
for frame in (1,5,9,13,17):
    bpy.context.scene.frame_set(frame)
    for name in ('Waist_Armature','Head_Armature','LHand_Armature','RHand_Armature','LFoot_Armature','RFoot_Armature'):
        print('POSE',frame,name,tuple(round(v,3) for v in arm.matrix_world @ arm.pose.bones[name].head))
