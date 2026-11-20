"""Build the Nimbo stone Mewtwo from the user's decoded regular 150.glb.

Blender: -b --python mewtwo_statue.py -- SOURCE_GLB OUTPUT_DIR [--render]
The GLB remains unchanged. Saved sources contain the retained rig and editable
stone parts; MewtwoStatue.json is a single collision-free prop for the existing
Nimbo bundle builder. Blender Z-up/front -Y -> Unity (-X,Z,-Y).
"""
import hashlib
import json
import math
import re
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree


BODY_HEIGHT = 3.35
PLINTH_HEIGHT = .62
PALETTE = {
    'body': (.52, .57, .60, 1),
    'tail': (.43, .48, .52, 1),
    'eyes': (.18, .23, .27, 1),
    'base': (.29, .34, .36, 1),
    'cap': (.41, .46, .47, 1),
    'plaque': (.14, .19, .21, 1),
    'letter': (.67, .60, .41, 1),
}
TAIL_KNOTS = [
    (0,.30,.58), (-.1,.45,.48), (-.35,.5,.55), (-.53,.5,.75),
    (-.55,.48,1.04), (-.45,.44,1.32), (-.2,.43,1.48),
    (.1,.40,1.5), (.35,.37,1.35), (.5,.34,1.1), (.5,.32,.8), (.4,.30,.5),
]


def bounds(points):
    return (Vector(tuple(min(p[k] for p in points) for k in range(3))),
            Vector(tuple(max(p[k] for p in points) for k in range(3))))


def move_to(obj, collection):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    collection.objects.link(obj)


def compact_tail(rig):
    """Rotate the supplied skin joints; never stretch or remodel the tail."""
    bones = sorted((b for b in rig.pose.bones if re.match(r'tail_\d+_', b.name)),
                   key=lambda b: int(b.name.split('_')[1]))
    if len(bones) != 12:
        raise ValueError('Expected the supplied Mewtwo 12-joint tail')
    heads = [rig.matrix_world @ b.head for b in bones]
    lengths = [(b-a).length for a,b in zip(heads, heads[1:])]
    knots = [heads[0]] + [Vector(p) for p in TAIL_KNOTS]
    curve = []
    for k in range(len(knots)-1):
        p0, p1 = knots[max(0,k-1)], knots[k]
        p2, p3 = knots[k+1], knots[min(len(knots)-1,k+2)]
        for q in range(20):
            t = q/20
            curve.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
    curve.append(knots[-1])
    distances = [0]
    for a,b in zip(curve,curve[1:]): distances.append(distances[-1]+(b-a).length)
    for i,bone in enumerate(bones[:-1]):
        distance = sum(lengths[:i+1])/sum(lengths)*distances[-1]
        j = next((j for j,d in enumerate(distances) if d >= distance), len(curve)-1)
        head = rig.matrix_world @ bone.head
        child = rig.matrix_world @ bones[i+1].head
        rotation = (child-head).rotation_difference(curve[j]-head).to_matrix().to_4x4()
        world = rig.matrix_world @ bone.matrix
        bone.matrix = rig.matrix_world.inverted() @ Matrix.Translation(head) @ rotation @ Matrix.Translation(-head) @ world
        bpy.context.view_layer.update()


def material(name, color):
    mat = bpy.data.materials.new('Mewtwo stone · '+name)
    mat.diffuse_color = color
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value = .88
    attribute = mat.node_tree.nodes.new('ShaderNodeVertexColor')
    attribute.layer_name = 'StoneColor'
    mat.node_tree.links.new(attribute.outputs['Color'], shader.inputs['Base Color'])
    return mat


def box(name, center, size, mat, collection, root, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Carved chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(mat)
    obj.parent = root
    move_to(obj, collection)
    return obj


def stone_colors(parts):
    """Bake short-range cavity shading into the same colors used by Unity."""
    positions, triangles = [], []
    for obj in parts:
        mesh = obj.data
        mesh.calc_loop_triangles()
        base = len(positions)
        positions.extend(obj.matrix_world @ v.co for v in mesh.vertices)
        triangles.extend(tuple(base+i for i in t.vertices) for t in mesh.loop_triangles)
    tree = BVHTree.FromPolygons(positions, triangles, all_triangles=True)
    rays = []
    for i in range(10):
        z = .18 + .78*(i+.5)/10
        angle = i * math.pi*(3-math.sqrt(5))
        r = math.sqrt(1-z*z)
        rays.append(Vector((r*math.cos(angle),r*math.sin(angle),z)))
    cache = {}
    for obj in parts:
        mesh = obj.data
        attr = mesh.color_attributes.get('StoneColor') or mesh.color_attributes.new(name='StoneColor', type='FLOAT_COLOR', domain='CORNER')
        normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
        base_color = obj.data.materials[0].diffuse_color
        for i,loop in enumerate(mesh.loops):
            point = obj.matrix_world @ mesh.vertices[loop.vertex_index].co
            normal = (normal_matrix @ mesh.corner_normals[i].vector).normalized()
            key = tuple(round(v,4) for v in point) + tuple(round(v,3) for v in normal)
            if key not in cache:
                helper = Vector((0,0,1)) if abs(normal.z) < .9 else Vector((1,0,0))
                tangent = normal.cross(helper).normalized()
                bitangent = normal.cross(tangent)
                blocked = sum(tree.ray_cast(point+normal*.004, tangent*r.x+bitangent*r.y+normal*r.z, .30)[0] is not None for r in rays)
                # Subtle mineral variation, with no painted cracks or arbitrary
                # silhouette changes. The physical chamfers remain geometry.
                mineral = .018*math.sin(point.x*17+point.y*13+point.z*11)
                cache[key] = 1 - .25*blocked/len(rays) + mineral
            attr.data[i].color = tuple(max(0,min(1,c*cache[key])) for c in base_color[:3]) + (1,)


def unity(v):
    return dict(x=-v.x, y=v.z, z=-v.y)


def export(parts, destination):
    points, normals, colors = [], [], []
    dropped = 0
    for obj in sorted(parts,key=lambda p:p.name):
        mesh = obj.data
        mesh.calc_loop_triangles()
        normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
        for triangle in mesh.loop_triangles:
            positions = [obj.matrix_world @ mesh.vertices[i].co for i in triangle.vertices]
            if (positions[1]-positions[0]).cross(positions[2]-positions[0]).length_squared < 1e-14:
                dropped += 1
                continue
            for point,loop in zip(positions,triangle.loops):
                normal = (normal_matrix @ mesh.corner_normals[loop].vector).normalized()
                color = mesh.color_attributes['StoneColor'].data[loop].color
                points.append(unity(point)); normals.append(unity(normal))
                colors.append(dict(zip('rgba',color)))
    if len(points) > 65535 or not all(math.isfinite(x) for stream in (points,normals,colors) for v in stream for x in v.values()):
        raise ValueError('Invalid statue vertex stream')
    prop = dict(name='MewtwoStatue', points=points, normals=normals, colors=colors, collide=False)
    destination.write_text(json.dumps(dict(props=[prop]),separators=(',',':'))+'\n')
    return prop,dropped


def render_review(parts, output):
    scene = bpy.context.scene
    points = [o.matrix_world @ v.co for o in parts for v in o.data.vertices]
    low,high = bounds(points); center = (low+high)*.5; extent = high-low
    world = bpy.data.worlds.new('Nimbo daylight review'); world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (.66,.74,.83,1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .65
    scene.world = world
    light_data = bpy.data.lights.new('Soft daylight','SUN'); light_data.energy = 2.2; light_data.angle = .14
    light = bpy.data.objects.new('Soft daylight',light_data); scene.collection.objects.link(light)
    light.rotation_euler = (.5,-.5,-.5)
    camera_data = bpy.data.cameras.new('Eight-view review camera')
    camera = bpy.data.objects.new('Eight-view review camera',camera_data); scene.collection.objects.link(camera)
    scene.camera = camera; camera_data.type = 'ORTHO'; camera_data.ortho_scale = extent.z*1.24
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-.015))
    floor = bpy.context.object; floor.name = 'Review ground (not exported)'
    ground = bpy.data.materials.new('Warm neutral review ground'); ground.diffuse_color = (.38,.43,.36,1)
    floor.data.materials.append(ground)
    scene.render.engine = 'CYCLES'; scene.cycles.samples = 24; scene.cycles.use_denoising = True
    scene.render.resolution_x = 640; scene.render.resolution_y = 800; scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'; scene.view_settings.view_transform = 'AgX'
    views = []
    for i,label in enumerate(('N','NE','E','SE','S','SW','W','NW')):
        angle = i*math.tau/8
        camera.location = center + Vector((math.sin(angle),-math.cos(angle),.28))*extent.z*3
        camera.rotation_euler = (center-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath = str(output/('MewtwoStatue-'+label+'.png'))
        bpy.ops.render.render(write_still=True)
        views.append(dict(view=label, camera=list(camera.location), target=list(center), ortho_scale=camera_data.ortho_scale))
    (output/'cameras.json').write_text(json.dumps(views,indent=2)+'\n')


def main():
    args = sys.argv[sys.argv.index('--')+1:]
    source, output = Path(args[0]), Path(args[1]); output.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    bpy.context.scene.frame_set(1)
    source_objects = list(bpy.data.objects)
    meshes = [o for o in source_objects if o.type=='MESH' and len(o.data.materials)]
    if not meshes or any(len(o.data.vertices)==0 for o in meshes):
        raise ValueError('Decode Draco with gltf-transform copy before importing the source')
    rig = next(o for o in source_objects if o.type=='ARMATURE')
    source_action = rig.animation_data.action.name
    for obj in source_objects: obj.animation_data_clear()
    compact_tail(rig)
    bpy.context.view_layer.update()
    source_collection = bpy.data.collections.new('SOURCE · supplied Mewtwo rig'); bpy.context.scene.collection.children.link(source_collection)
    sculpture_collection = bpy.data.collections.new('STATUE · editable stone parts'); bpy.context.scene.collection.children.link(sculpture_collection)
    for obj in source_objects: move_to(obj,source_collection)
    root = bpy.data.objects.new('MewtwoStatue',None); sculpture_collection.objects.link(root)
    root['collision_policy'] = 'Visual only. Preserve the native Moai collider, rewards, icons and outline behavior.'
    root['source_sha256'] = hashlib.sha256(source.read_bytes()).hexdigest()
    mats = {name:material(name,color) for name,color in PALETTE.items()}
    depsgraph = bpy.context.evaluated_depsgraph_get()
    parts = []
    for original in meshes:
        evaluated = original.evaluated_get(depsgraph)
        mesh = bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=depsgraph)
        mesh.transform(original.matrix_world)
        semantic = 'eyes' if 'eye' in original.data.materials[0].name else 'tail' if original.data.materials[0].name=='body_b' else 'body'
        obj = bpy.data.objects.new('Carved '+semantic+' · '+original.name,mesh); sculpture_collection.objects.link(obj); obj.parent = root
        mesh.materials.clear(); mesh.materials.append(mats[semantic]); parts.append(obj)
    positions = [v.co.copy() for o in parts for v in o.data.vertices]
    low,high = bounds(positions); scale = BODY_HEIGHT/(high.z-low.z)
    contacts = [p for p in positions if p.z < low.z+.035]
    pivot = Vector((sum(p.x for p in contacts)/len(contacts),sum(p.y for p in contacts)/len(contacts),low.z))
    for obj in parts:
        for vertex in obj.data.vertices: vertex.co = (vertex.co-pivot)*scale + Vector((0,0,PLINTH_HEIGHT))
        if len(obj.data.polygons) > 100:
            bpy.context.view_layer.objects.active = obj; obj.select_set(True)
            modifier = obj.modifiers.new('Stone silhouette budget','DECIMATE'); modifier.ratio = .40; modifier.use_collapse_triangulate = True
            bpy.ops.object.modifier_apply(modifier=modifier.name); obj.select_set(False)
    parts.extend([
        box('Basalt foundation',(0,0,.09),(1.68,1.28,.18),mats['base'],sculpture_collection,root,.055),
        box('Plinth shaft',(0,0,.365),(1.43,1.04,.37),mats['base'],sculpture_collection,root,.035),
        box('Carved capstone',(0,0,.585),(1.61,1.20,.07),mats['cap'],sculpture_collection,root,.018),
        box('Inset dedication plaque',(0,-.532,.355),(.49,.035,.18),mats['plaque'],sculpture_collection,root,.012),
    ])
    bpy.ops.object.text_add(location=(0,-.553,.354),rotation=(math.pi/2,0,0))
    text = bpy.context.object; text.name = 'Engraved specimen number 150'; text.data.body = '150'
    text.data.align_x = 'CENTER'; text.data.align_y = 'CENTER'; text.data.size = .115; text.data.extrude = .0015; text.data.resolution_u = 2
    bpy.ops.object.convert(target='MESH'); text=bpy.context.object; text.data.materials.append(mats['letter']); text.parent=root; move_to(text,sculpture_collection); parts.append(text)
    bpy.context.view_layer.update()
    source_collection.hide_render = True; source_collection.hide_viewport = True
    stone_colors(parts)
    prop,dropped = export(parts,output/'MewtwoStatue.json')
    p=[Vector((v['x'],v['y'],v['z'])) for v in prop['points']];minimum,maximum=bounds(p)
    if abs(minimum.y)>1e-5: raise ValueError('Plinth ground datum changed')
    native=Vector((4.391074,8.439878,3.562733));size=maximum-minimum
    fit=min(native.y/size.y,native.x/size.x,native.z/size.z)
    metrics=dict(name='MewtwoStatue',source=str(source),source_sha256=root['source_sha256'],source_pose=source_action,source_frame=1,
                 triangle_count=len(prop['points'])//3,vertex_count=len(prop['points']),bounds_min=list(minimum),bounds_max=list(maximum),
                 size=list(size),native_upright_size=list(native),native_fit_scale=fit,native_fitted_size=list(size*fit),
                 collider_policy='No collider in exported prop; retain native Moai behavior and collider.',
                 components=[o.name for o in parts],dropped_degenerate_triangles=dropped)
    (output/'statue-validation.json').write_text(json.dumps(metrics,indent=2)+'\n')
    # The editable sculpture and original rig are saved without studio helpers.
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(output/'mewtwo-statue.blend'))
    if '--render' in args: render_review(parts,output)
    print('MEWTWO STATUE PASS',json.dumps(metrics),flush=True)


if __name__=='__main__': main()
