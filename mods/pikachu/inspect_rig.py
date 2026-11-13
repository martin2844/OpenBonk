import bpy, sys
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='mods/pikachu/source/PikachuAnim.fbx')
a=next(o for o in bpy.data.objects if o.type=='ARMATURE')
for b in a.data.bones:
 print('BONE',b.name,'parent',b.parent.name if b.parent else None,'head',list(b.head_local),'tail',list(b.tail_local))
