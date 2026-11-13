import bpy, collections
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='mods/pikachu/source/PikachuAnim.fbx')
for obj in bpy.data.objects:
 if obj.type!='MESH': continue
 counts=collections.Counter()
 for v in obj.data.vertices:
  for g in v.groups:
   name=obj.vertex_groups[g.group].name
   if g.weight>0.5 and ('Ear' in name or 'Tail' in name): counts[name]+=1
 print(obj.name,counts)
