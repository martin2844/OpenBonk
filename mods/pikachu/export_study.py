import bpy, os
bpy.ops.wm.open_mainfile(filepath=os.path.abspath('mods/pikachu/study/pikachu-study.blend'))
source=bpy.data.actions.get('Source animation')
if source: bpy.data.actions.remove(source)
for o in bpy.context.selected_objects:o.select_set(False)
for o in bpy.context.scene.objects:
 if o.type in {'MESH','ARMATURE','EMPTY'}:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.abspath('mods/pikachu/study/pikachu-study.fbx'),use_selection=True,object_types={'ARMATURE','EMPTY','MESH'},bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,add_leaf_bones=False,path_mode='AUTO')
