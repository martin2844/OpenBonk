"""Blender QA of the actual indexed JSON/atlas consumed by Unity.

Usage: blender -b --python render_private_creatures.py -- PRIVATE_ROOT [SPECIES...]
Captures frames 00, 03, 06 and 09 of the baked walk, with a fixed camera per species.
This validates exported geometry and textures; Unity lighting is verified separately.
"""
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def render(root, species):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    data = json.loads((root / 'creatures' / f'{species}.json').read_text())
    frames = data['clips'][0]['frames']
    image = bpy.data.images.load(str(root / 'creatures' / data['texture']))
    material = bpy.data.materials.new(data['material'])
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value = .9
    uv = nodes.new('ShaderNodeTexCoord')
    wrap = nodes.new('ShaderNodeVectorMath'); wrap.operation = 'FRACTION'
    scale = nodes.new('ShaderNodeAttribute'); scale.attribute_name = 'atlasScale'
    offset = nodes.new('ShaderNodeAttribute'); offset.attribute_name = 'atlasOffset'
    multiply = nodes.new('ShaderNodeVectorMath'); multiply.operation = 'MULTIPLY'
    add = nodes.new('ShaderNodeVectorMath'); add.operation = 'ADD'
    texture = nodes.new('ShaderNodeTexImage'); texture.image = image; texture.extension = 'EXTEND'
    links.new(uv.outputs['UV'], wrap.inputs[0])
    links.new(wrap.outputs['Vector'], multiply.inputs[0])
    links.new(scale.outputs['Vector'], multiply.inputs[1])
    links.new(multiply.outputs['Vector'], add.inputs[0])
    links.new(offset.outputs['Vector'], add.inputs[1])
    links.new(add.outputs['Vector'], texture.inputs['Vector'])
    links.new(texture.outputs['Color'], shader.inputs['Base Color'])
    if data.get('alphaClip'):
        cutoff = nodes.new('ShaderNodeMath'); cutoff.operation = 'GREATER_THAN'; cutoff.inputs[1].default_value = .5
        links.new(texture.outputs['Alpha'], cutoff.inputs[0]); links.new(cutoff.outputs[0], shader.inputs['Alpha'])

    selected = [frames[i] for i in (0, 3, 6, 9)]
    all_points = [Vector((-p['x'], -p['z'], p['y'])) for f in selected for p in f['points']]
    minimum = Vector(tuple(min(p[i] for p in all_points) for i in range(3)))
    maximum = Vector(tuple(max(p[i] for p in all_points) for i in range(3)))
    center = (minimum + maximum) * .5
    extent = maximum - minimum
    size = max(extent)
    world = bpy.data.worlds.new('Sunny studio'); world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (.6, .7, .85, 1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .65
    bpy.context.scene.world = world
    camera_data = bpy.data.cameras.new('Fixed exported-pose camera')
    camera = bpy.data.objects.new('Camera', camera_data); bpy.context.collection.objects.link(camera)
    camera.location = center + Vector((.55, -2.9, 1.25)) * size
    camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera_data.type = 'ORTHO'; camera_data.ortho_scale = size * 1.3
    bpy.context.scene.camera = camera
    light_data = bpy.data.lights.new('Sun', 'SUN'); light_data.energy = 2.2; light_data.angle = .2
    light = bpy.data.objects.new('Sun', light_data); bpy.context.collection.objects.link(light)
    light.rotation_euler = (.4, -.6, -.5)
    bpy.ops.mesh.primitive_plane_add(size=size*200, location=(0, 0, minimum.z-.02))
    floor = bpy.context.object
    floor_material = bpy.data.materials.new('Warm neutral floor'); floor_material.diffuse_color = (.42, .47, .43, 1)
    floor.data.materials.append(floor_material)

    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'; scene.cycles.samples = 24
    scene.render.resolution_x = scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = 'AgX'
    scene.render.image_settings.file_format = 'PNG'
    output = root / 'textured-previews'; output.mkdir(exist_ok=True)
    for f in selected:
        points = [(-p['x'], -p['z'], p['y']) for p in f['points']]
        normals = [Vector((-p['x'], -p['z'], p['y'])) for p in f['normals']]
        faces = []
        for index in range(0, len(f['indices']), 3):
            a, b, c = f['indices'][index:index+3]
            if (Vector(points[b])-Vector(points[a])).cross(Vector(points[c])-Vector(points[a])).dot(normals[a]+normals[b]+normals[c]) < 0:
                b, c = c, b
            faces.append((a,b,c))
        mesh = bpy.data.meshes.new(f['name']); mesh.from_pydata(points, [], faces); mesh.update()
        mesh.normals_split_custom_set_from_vertices(normals)
        uv_layer = mesh.uv_layers.new()
        for polygon in mesh.polygons:
            polygon.use_smooth = True
            for loop in polygon.loop_indices:
                vertex = mesh.loops[loop].vertex_index
                uv_layer.data[loop].uv = tuple(f['uv'][vertex][k] for k in ('x','y'))
        for name, keys in [('atlasOffset',('x','y')), ('atlasScale',('z','w'))]:
            attribute = mesh.attributes.new(name, 'FLOAT_VECTOR', 'POINT')
            for i, rect in enumerate(f['atlasRects']): attribute.data[i].vector = (rect[keys[0]],rect[keys[1]],0)
        obj = bpy.data.objects.new(f['name'], mesh); bpy.context.collection.objects.link(obj)
        mesh.materials.append(material)
        scene.render.filepath = str(output / (f['name']+'.png'))
        bpy.ops.render.render(write_still=True)
        bpy.data.objects.remove(obj, do_unlink=True); bpy.data.meshes.remove(mesh)
    (output / (str(species)+'-camera.json')).write_text(json.dumps(dict(center=list(center), extent=list(extent), camera=list(camera.location), ortho_scale=camera_data.ortho_scale), indent=2))


if __name__ == '__main__':
    args = sys.argv[sys.argv.index('--')+1:]
    for species in args[1:] or ('16','112','130'):
        render(Path(args[0]), species)
