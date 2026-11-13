"""Measure skinned vertex heights by dominant body/limb group over run poses."""
import bpy, os

bpy.ops.wm.open_mainfile(filepath=os.path.abspath('mods/pikachu/study/' + os.environ.get('OPENBONK_PIKA_BLEND', 'pikachu-movement.blend')))
arm = bpy.data.objects['Armature']; arm.animation_data.action = bpy.data.actions['OpenBonk run']
parts = {'left_hand': ('LArm_', 'LForeArm_', 'LHand_'), 'right_hand': ('RArm_', 'RForeArm_', 'RHand_'),
         'left_foot': ('LThigh_', 'LLeg_', 'LFoot_'), 'right_foot': ('RThigh_', 'RLeg_', 'RFoot_'),
         'body': ('Waist_', 'Hips_', 'Spine')}
for frame in (1, 5, 9, 13):
    bpy.context.scene.frame_set(frame); deps = bpy.context.evaluated_depsgraph_get()
    values = {k: [] for k in parts}
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH' or obj.name == 'OpenBonkSurfboard' or not obj.vertex_groups: continue
        evaluated = obj.evaluated_get(deps); mesh = evaluated.to_mesh()
        if len(mesh.vertices) != len(obj.data.vertices): raise RuntimeError('vertex order changed')
        for original, deformed in zip(obj.data.vertices, mesh.vertices):
            if not original.groups: continue
            dominant = max(original.groups, key=lambda g: g.weight)
            if dominant.weight < .5: continue
            name = obj.vertex_groups[dominant.group].name
            p = evaluated.matrix_world @ deformed.co
            for category, prefixes in parts.items():
                if name.startswith(prefixes): values[category].append(p.z)
        evaluated.to_mesh_clear()
    for category, z in values.items():
        print('VERTICES', frame, category, len(z), 'z_min', round(min(z), 4) if z else None, 'z_max', round(max(z), 4) if z else None)
