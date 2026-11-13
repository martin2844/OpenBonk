import bpy, sys, json
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=sys.argv[sys.argv.index('--') + 1])
for obj in bpy.data.objects:
 if obj.type == 'MESH':
  print('MESH',obj.name, 'materials', [m.name if m else None for m in obj.data.materials], 'loc', list(obj.matrix_world.translation), 'bound', [list(v) for v in obj.bound_box[:2]])
for mat in bpy.data.materials:
 print('MATERIAL', mat.name, list(mat.diffuse_color))
for a in bpy.data.actions:
 print('ACTION',a.name, list(a.frame_range))
