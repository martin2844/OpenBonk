import bpy, math, os, sys
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.abspath('mods/pikachu/source/PikachuAnim.fbx'))
colors={'bodyA':(1.0,0.67,0.015,1),'bodyB':(0.82,0.035,0.015,1),'material':(0.015,0.01,0.01,1),'Material.001':(1,1,1,1),'material_5':(1.0,0.68,0.02,1),'mouth':(0.35,0.025,0.025,1)}
for material in bpy.data.materials:
    color=colors.get(material.name,(1,0.7,0.02,1)); material.use_nodes=True
    nodes=material.node_tree.nodes; nodes.clear(); out=nodes.new('ShaderNodeOutputMaterial'); shader=nodes.new('ShaderNodeBsdfPrincipled'); shader.inputs['Base Color'].default_value=color; shader.inputs['Roughness'].default_value=.7; material.node_tree.links.new(shader.outputs['BSDF'],out.inputs['Surface']); material.diffuse_color=color
body=bpy.data.objects['bd0001_00_bodySkin_0']
black=bpy.data.materials.new('Ear tips'); black.diffuse_color=(0.012,0.008,0.008,1); black.use_nodes=True
black.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=black.diffuse_color
body.data.materials.append(black)
ear_groups={g.index for g in body.vertex_groups if g.name in ('LEar3_Armature','REar3_Armature')}
for polygon in body.data.polygons:
    weight=sum(sum(g.weight for g in body.data.vertices[index].groups if g.group in ear_groups) for index in polygon.vertices)/len(polygon.vertices)
    if weight>.42: polygon.material_index=len(body.data.materials)-1
armature=bpy.data.objects['Armature']
original=armature.animation_data.action
original.name='Source animation'
walk=bpy.data.actions.new('OpenBonk walk study'); armature.animation_data.action=walk
for frame, swing in ((1,0.45),(7,0),(13,-0.45),(19,0),(25,0.45)):
    for name, direction in (('LThigh_Armature',1),('RThigh_Armature',-1),('LArm_Armature',-0.6),('RArm_Armature',0.6)):
        bone=armature.pose.bones[name]; bone.rotation_mode='XYZ'; bone.rotation_euler=(swing*direction,0,0); bone.keyframe_insert(data_path='rotation_euler',frame=frame)
    waist=armature.pose.bones['Waist_Armature']; waist.rotation_mode='XYZ'; waist.rotation_euler=(0,0,0.045 if frame in (1,13,25) else 0); waist.keyframe_insert(data_path='rotation_euler',frame=frame)
scene=bpy.context.scene
scene.frame_set(1)
mesh=[obj for obj in bpy.data.objects if obj.type=='MESH']
coords=[obj.matrix_world @ Vector(p) for obj in mesh for p in obj.bound_box]
lo=Vector(tuple(min(p[i] for p in coords) for i in range(3))); hi=Vector(tuple(max(p[i] for p in coords) for i in range(3))); center=(lo+hi)/2; size=max(hi-lo)
print('STUDY_BOUNDS', list(lo),list(hi),'SIZE',size)
world=bpy.data.worlds.new('World');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.18,0.15,0.2,1);world.node_tree.nodes['Background'].inputs['Strength'].default_value=.8
bpy.ops.object.light_add(type='AREA',location=center+Vector((size,-size,size*2)));bpy.context.object.data.energy=400*size*size;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=size*3
bpy.ops.object.camera_add();camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=size*1.45
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
os.makedirs('mods/pikachu/study',exist_ok=True)
if os.environ.get('OPENBONK_SKIP_STUDY_RENDERS') != '1':
 for n in range(8):
  angle=math.pi*2*n/8; camera.location=center+Vector((math.sin(angle)*size*2,-math.cos(angle)*size*2,size*.6)); camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=f'mods/pikachu/study/view-{n}.png';bpy.ops.render.render(write_still=True)
camera.location=center+Vector((size*.65,-size*2,size*.6)); camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
if os.environ.get('OPENBONK_SKIP_STUDY_RENDERS') != '1':
 for frame in (1,13):
  scene.frame_set(frame); scene.render.filepath=f'mods/pikachu/study/walk-{frame}.png'; bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath('mods/pikachu/study/pikachu-study.blend'))
