import bpy, sys, json
bpy.ops.wm.read_factory_settings(use_empty=True)
path = sys.argv[sys.argv.index('--') + 1]
bpy.ops.import_scene.fbx(filepath=path)
print('OPENBONK_MODEL_REPORT ' + json.dumps({'objects': [{'name': o.name, 'type': o.type, 'bones': len(o.data.bones) if o.type == 'ARMATURE' else None, 'vertices': len(o.data.vertices) if o.type == 'MESH' else None} for o in bpy.data.objects], 'actions': [{'name': a.name, 'frame_range': list(a.frame_range)} for a in bpy.data.actions], 'images': [{'name': i.name, 'path': i.filepath, 'packed': bool(i.packed_file)} for i in bpy.data.images]}))
