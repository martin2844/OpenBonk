"""Create original movement studies on the existing rig and export the Android model."""
import bpy, math, os
from mathutils import Vector, Quaternion
root=os.path.abspath('mods/pikachu/study')
bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'pikachu-study.blend'))
arm=bpy.data.objects['Armature']
# Keep the third-party source action out of the exported model.
for action in list(bpy.data.actions):
    if action.name == 'Source animation': bpy.data.actions.remove(action)
bones=['Waist_Armature','Head_Armature','LThigh_Armature','RThigh_Armature','LArm_Armature','RArm_Armature','LForeArm_Armature','RForeArm_Armature','LEar1_Armature','REar1_Armature']
def world_x_rotation(name, angle):
    # Convert the same model-space pitch to each bone's local rest basis.
    local_axis = arm.data.bones[name].matrix_local.to_3x3().inverted() @ (arm.matrix_world.to_3x3().inverted() @ Vector((1, 0, 0)))
    return Quaternion(local_axis.normalized(), angle).to_euler('XYZ')
def pose(action, frame, values):
    arm.animation_data.action=action
    bpy.context.scene.frame_set(frame)
    for name in bones:
        bone=arm.pose.bones[name]; bone.rotation_mode='XYZ'
        bone.rotation_euler=values.get(name,(0,0,0))
        bone.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
    waist = arm.pose.bones['Waist_Armature']
    local_down = waist.bone.matrix_local.to_3x3().inverted() @ (arm.matrix_world.to_3x3().inverted() @ Vector((0, 0, -.45)))
    waist.location = local_down if action.name == 'OpenBonk run' else (0, 0, 0)
    waist.keyframe_insert(data_path='location',frame=frame,group='Waist_Armature')
    # Lower the forelegs for the quadruped run. Explicit zero keys on every
    # other clip prevent this pose from carrying into idle, jump or surf.
    for name in ('LArm_Armature', 'RArm_Armature'):
        bone = arm.pose.bones[name]; bone.location = (0, 0, 0)
        bpy.context.view_layer.update()
        if action.name == 'OpenBonk run':
            delta = arm.matrix_world.to_3x3().inverted() @ Vector((0, 0, -.5))
            matrix = bone.matrix.copy(); matrix.translation += delta; bone.matrix = matrix
        bone.keyframe_insert(data_path='location', frame=frame, group=name)
def action(name, frames):
    a=bpy.data.actions.new(name); a.use_fake_user=True
    for frame, values in frames: pose(a,frame,values)
    return a
# The original walk action predates the run's crouch translation. Explicitly
# reset it on walk transitions so a previous run cannot leave the rig lowered.
arm.animation_data.action=bpy.data.actions['OpenBonk walk study']
for frame in (1, 7, 13, 19, 25):
    waist=arm.pose.bones['Waist_Armature']; waist.location=(0,0,0); waist.keyframe_insert(data_path='location',frame=frame,group='Waist_Armature')
    for name in ('Head_Armature', 'LForeArm_Armature', 'RForeArm_Armature'):
        bone=arm.pose.bones[name]; bone.rotation_mode='XYZ'; bone.rotation_euler=(0,0,0); bone.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
    for name in ('LArm_Armature', 'RArm_Armature'):
        bone=arm.pose.bones[name]; bone.location=(0,0,0); bone.keyframe_insert(data_path='location',frame=frame,group=name)
action('OpenBonk idle', [(1, {'LArm_Armature':(-.10,0,.12),'RArm_Armature':(-.10,0,-.12)}),
    (25, {'Waist_Armature':(.025,0,0),'LArm_Armature':(-.15,0,.14),'RArm_Armature':(-.15,0,-.14),'LEar1_Armature':(-.04,0,0),'REar1_Armature':(-.04,0,0)}),
    (49, {'LArm_Armature':(-.10,0,.12),'RArm_Armature':(-.10,0,-.12)})])
run=[]
for f,s in [(1,.55),(5,0),(9,-.55),(13,0),(17,.55)]:
    # Pikachu faces model -Y. Pitch the torso forward and counter-pitch the
    # head, then alternate opposite fore/hind limbs as a low quadruped gait.
    run.append((f,{'Waist_Armature':world_x_rotation('Waist_Armature', .86),
                   'Head_Armature':world_x_rotation('Head_Armature', -.40),
                   'LThigh_Armature':world_x_rotation('LThigh_Armature', s),
                   'RThigh_Armature':world_x_rotation('RThigh_Armature', -s),
                   'LArm_Armature':world_x_rotation('LArm_Armature', 1.1-s*.65),
                   'RArm_Armature':world_x_rotation('RArm_Armature', 1.1+s*.65),
                   'LForeArm_Armature':world_x_rotation('LForeArm_Armature', .25),
                   'RForeArm_Armature':world_x_rotation('RForeArm_Armature', .25),
                   'LEar1_Armature':(-.12,0,0),'REar1_Armature':(-.12,0,0)}))
action('OpenBonk run',run)
action('OpenBonk jump',[(1,{'LThigh_Armature':(.3,0,.12),'RThigh_Armature':(.3,0,-.12),'LArm_Armature':(-.5,0,.25),'RArm_Armature':(-.5,0,-.25),'Waist_Armature':(-.1,0,0)}),(12,{'LThigh_Armature':(.5,0,.18),'RThigh_Armature':(.5,0,-.18),'LArm_Armature':(-.8,0,.4),'RArm_Armature':(-.8,0,-.4),'Waist_Armature':(-.18,0,0)}),(24,{'LThigh_Armature':(.3,0,.12),'RThigh_Armature':(.3,0,-.12),'LArm_Armature':(-.5,0,.25),'RArm_Armature':(-.5,0,-.25),'Waist_Armature':(-.1,0,0)})])
action('OpenBonk surf',[(1,{'LThigh_Armature':(.38,0,.25),'RThigh_Armature':(-.2,0,-.25),'LArm_Armature':(0,0,.7),'RArm_Armature':(0,0,-.7),'Waist_Armature':(.2,0,.12)}),(13,{'LThigh_Armature':(.3,0,.25),'RThigh_Armature':(-.15,0,-.25),'LArm_Armature':(0,0,.6),'RArm_Armature':(0,0,-.6),'Waist_Armature':(.15,0,-.12)}),(25,{'LThigh_Armature':(.38,0,.25),'RThigh_Armature':(-.2,0,-.25),'LArm_Armature':(0,0,.7),'RArm_Armature':(0,0,-.7),'Waist_Armature':(.2,0,.12)})])
# Board is an independent mesh; the game switches it on only for surf/slide.
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
pts=[o.matrix_world @ Vector(p) for o in meshes for p in o.bound_box]
lo=Vector(tuple(min(p[i] for p in pts) for i in range(3))); hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
print('MODEL_BOUNDS',tuple(lo),tuple(hi))
bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=8,location=((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z+.22))
board=bpy.context.object; board.name='OpenBonkSurfboard'; board.scale=((hi.x-lo.x)*.42,(hi.y-lo.y)*.68,.12)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
mat=bpy.data.materials.new('Surfboard blue'); mat.diffuse_color=(.03,.45,.8,1); mat.use_nodes=True; mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=mat.diffuse_color; board.data.materials.append(mat)
arm.animation_data.action=bpy.data.actions['OpenBonk walk study']; bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'pikachu-movement.blend'))
for o in bpy.context.selected_objects:o.select_set(False)
for o in bpy.context.scene.objects:
    if o.type in {'MESH','ARMATURE','EMPTY'}:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(root,'pikachu-movement.fbx'),use_selection=True,object_types={'ARMATURE','EMPTY','MESH'},bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,add_leaf_bones=False,path_mode='AUTO')
