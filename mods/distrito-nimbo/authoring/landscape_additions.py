"""Author original taller Nimbo buildings and low-poly grass in Blender.

Usage: blender -b --python landscape_additions.py -- OUTPUT_DIRECTORY
Exports three independent single-mesh prefab descriptions in landscape-props.json,
plus their editable named-part library NimboLandscapeKit.blend. Unity metres,
+Y up and +Z frontage are exported from Blender Z up / -Y frontage.
The existing neighborhood library is not read or overwritten.
"""
import bpy
import json
import math
import random
import sys
from pathlib import Path
from mathutils import Vector

OUT = Path(sys.argv[sys.argv.index('--') + 1])
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
COLORS = {
    'cream': (.87, .82, .69, 1), 'chalk': (.93, .92, .83, 1),
    'sage': (.62, .73, .64, 1), 'sand': (.73, .63, .49, 1),
    'ink': (.10, .16, .19, 1), 'glass': (.24, .43, .48, 1),
    'glasslight': (.46, .61, .62, 1), 'coral': (.75, .24, .16, 1),
    'jade': (.18, .40, .35, 1), 'metal': (.51, .55, .53, 1),
    'roof': (.17, .26, .29, 1), 'soil': (.38, .31, .21, 1),
    'leaf': (.28, .46, .17, 1), 'leaflight': (.46, .63, .26, 1),
    'leafdark': (.19, .35, .13, 1),
}
MATERIALS = {}
for name, color in COLORS.items():
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = color
    material.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .84
    MATERIALS[name] = material

PARTS, COLLIDERS = {}, {}
CURRENT = None


def begin(name):
    global CURRENT
    CURRENT = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(CURRENT)
    CURRENT['front'] = 'Blender -Y / Unity +Z'
    PARTS[name] = []
    COLLIDERS[name] = []


def part(name, color):
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(MATERIALS[color])
    obj.parent = CURRENT
    PARTS[CURRENT.name].append(obj)
    return obj


def box(name, center, size, color):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = part(name, color)
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def mesh_part(name, vertices, faces, color):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(MATERIALS[color])
    obj.parent = CURRENT
    PARTS[CURRENT.name].append(obj)
    return obj


def collider(center, size):
    x, y, z = center
    sx, sy, sz = size
    COLLIDERS[CURRENT.name].append({'center': dict(x=-x, y=z, z=-y),
                                    'size': dict(x=sx, y=sz, z=sy)})


def lettering(word, center, size, color, width):
    curve = bpy.data.curves.new(word, 'FONT')
    curve.body = word
    curve.align_x = 'CENTER'
    curve.align_y = 'CENTER'
    curve.size = size
    curve.extrude = .003
    curve.resolution_u = 1
    obj = bpy.data.objects.new(word, curve)
    bpy.context.collection.objects.link(obj)
    obj.parent = CURRENT
    obj.location = center
    obj.rotation_euler.x = math.pi / 2
    obj.data.materials.append(MATERIALS[color])
    bpy.context.view_layer.update()
    if obj.dimensions.x > width:
        obj.scale *= width / obj.dimensions.x
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')
    PARTS[CURRENT.name].append(obj)


def window_front(x, y, z, width=1.35, height=1.62, balcony=False):
    # Glazing is backed and opaque. Recessed panes sit behind real jambs,
    # sill, head and central mullion instead of a flat window decal.
    box('Recessed window backing', (x, y + .10, z), (width, .08, height), 'ink')
    box('Opaque window glazing', (x, y + .052, z), (width-.16, .025, height-.16), 'glass')
    for dx in (-width/2, width/2):
        box('Window jamb', (x+dx, y, z), (.10, .23, height+.13), 'chalk')
    for dz in (-height/2, height/2):
        box('Window sill and head', (x, y-.015, z+dz), (width+.18, .28, .11), 'chalk')
    box('Window center mullion', (x, y-.025, z), (.065, .20, height), 'metal')
    if balcony:
        deck = z-height/2-.16
        box('Balcony slab', (x, y-.39, deck), (width+.32, .95, .16), 'sand')
        box('Balcony rail cap', (x, y-.81, deck+.88), (width+.34, .065, .07), 'ink')
        for dx in (-.55, -.28, 0, .28, .55):
            box('Balcony vertical', (x+dx, y-.81, deck+.45), (.035, .035, .82), 'ink')
        for dx in (-width/2-.12, width/2+.12):
            box('Balcony return', (x+dx, y-.4, deck+.88), (.06, .82, .06), 'ink')


def shop_ground(width, depth, color, tower):
    box('Ground plinth', (0, 0, .11), (width+.28, depth+.25, .22), 'metal')
    front = -depth/2
    # Full closed backing is set behind piers; storefront glazing occupies
    # independent recessed bays and is not an enterable interior.
    box('Shop core', (0, .13, 1.655), (width-.20, depth-.26, 3.09), color)
    for x in (-width/2+.08, width/2-.08):
        box('Shop structural pier', (x, front-.035, 1.61), (.22, .35, 3.0), 'chalk')
    for x in (-1.72, 0, 1.72):
        box('Shop glazed opening backing', (x, front-.035, 1.30), (1.48, .12, 2.18), 'ink')
        box('Shop tinted glass', (x, front-.105, 1.30), (1.31, .022, 1.96), 'glasslight')
        box('Shop glass transom', (x, front-.12, 1.65), (1.34, .035, .065), 'metal')
    box('Door center rail', (0, front-.145, 1.27), (.06, .07, 1.99), 'chalk')
    box('Door handle', (.31, front-.22, 1.16), (.05, .11, .28), 'metal')
    box('Shop sign backing', (0, front-.06, 2.76), (width-.16, .23, .56), 'jade' if tower else 'coral')
    lettering('NIMBO MART' if tower else 'KISSA NIMBO', (0, front-.19, 2.76), .35, 'chalk', width-.6)
    canopy = box('Shop canopy', (0, front-.43, 2.43), (width+.25, 1.0, .12), 'jade' if tower else 'coral')
    canopy.rotation_euler.x = -.11
    for x in (-2.4, -1.6, -.8, 0, .8, 1.6, 2.4):
        stripe = box('Canopy pale stripe', (x, front-.43, 2.50), (.18, 1.0, .025), 'chalk')
        stripe.rotation_euler.x = -.11


def building(name, residential_floors, tower):
    begin(name)
    width, depth = (5.9, 4.8) if tower else (5.8, 4.6)
    wall = 'sage' if tower else 'cream'
    shop_ground(width, depth, wall, tower)
    floor_height = 3.05
    for floor in range(residential_floors):
        base = 3.2 + floor*floor_height
        setback = .13 if tower and floor >= 3 else 0
        box('Floor %d volume' % (floor+1), (0, setback, base+floor_height/2),
            (width, depth-setback*2, floor_height), wall)
        box('Floor %d string course' % (floor+1), (0, setback, base+.04),
            (width+.18, depth-setback*2+.18, .16), 'chalk')
        face = -depth/2 + setback*2-.04
        for x in (-1.64, 0, 1.64):
            window_front(x, face-.10, base+1.51, 1.30, 1.62,
                         balcony=(not tower and floor%2 == 0))
        # Side windows have their own frame volumes, with visible jamb depth.
        for side in (-1, 1):
            for y in (-.8, 1.1):
                box('Side window shadow', (side*(width/2+.018), y, base+1.5), (.05, 1.28, 1.61), 'ink')
                box('Side window pane', (side*(width/2+.052), y, base+1.5), (.018, 1.11, 1.44), 'glass')
                for dz in (-.84, .84):
                    box('Side sill and lintel', (side*(width/2+.06), y, base+1.5+dz), (.21, 1.45, .10), 'chalk')
        if floor % 2 == 1:
            box('Air conditioner condenser', (width/2+.19, 1.0, base+.48), (.38, .92, .50), 'chalk')
            for y in (.72, .86, 1.0, 1.14, 1.28):
                box('Condenser grill', (width/2+.389, y, base+.48), (.013, .045, .35), 'metal')
    top = 3.2 + residential_floors*floor_height
    box('Roof cap slab', (0, 0, top+.12), (width+.38, depth+.30, .24), 'chalk')
    for side in (-1, 1):
        box('Roof parapet side', (side*(width/2+.10), 0, top+.51), (.12, depth+.23, .67), 'chalk')
        box('Roof parapet end', (0, side*(depth/2+.06), top+.51), (width+.25, .12, .67), 'chalk')
    box('Roof dark waterproofing', (0, 0, top+.26), (width-.1, depth-.1, .04), 'roof')
    house_height = 1.55 if tower else 1.20
    house_center = top+.28+house_height/2
    box('Stair roof house', (.92, .86, house_center), (1.85, 1.58, house_height), wall)
    box('Roof house cap', (.92, .86, top+.28+house_height+.085), (2.04, 1.75, .17), 'roof')
    if tower:
        for x in (-2.22, -1.32):
            box('Rooftop sign support', (x, .35, top+.58), (.07, .10, .60), 'ink')
        box('Narrow rooftop sign frame', (-1.77, .35, top+1.30), (1.20, .22, 1.55), 'ink')
        box('Narrow rooftop sign enamel', (-1.77, .21, top+1.30), (1.04, .025, 1.39), 'jade')
        lettering('N', (-1.77, .18, top+1.30), .92, 'chalk', .85)
    # Grounded solid volume excludes awning/balcony apertures and signs.
    collider((0, 0, top/2), (width, depth, top))
    collider((0, 0, top+.12), (width+.38, depth+.30, .24))
    collider((0, 0, top+.26), (width-.1, depth-.1, .04))
    collider((.92, .86, house_center), (1.85, 1.58, house_height))
    collider((.92, .86, top+.28+house_height+.085), (2.04, 1.75, .17))


def grass_patch():
    begin('NimboGrassPatch')
    rng = random.Random(2211)
    for cluster, (cx, cy, radius) in enumerate(((-1.8,-.9,1.0), (.1,-1.2,.95),
                                               (1.75,.30,.95), (-.8,1.15,1.1))):
        for tuft in range(18):
            angle, distance = rng.random()*math.tau, math.sqrt(rng.random())*radius
            x, y = cx+math.cos(angle)*distance, cy+math.sin(angle)*distance
            for blade in range(3):
                theta = rng.random()*math.tau
                height, width = rng.uniform(.20,.58), rng.uniform(.055,.095)
                dx, dy = math.cos(theta)*width, math.sin(theta)*width
                bend = rng.uniform(.09,.20)
                verts = [(x-dx,y-dy,0), (x+dx,y+dy,0),
                         (x-dy*.45,y+dx*.45,.025),
                         (x+math.cos(theta)*bend,y+math.sin(theta)*bend,height)]
                mesh_part('Patch %d grass tuft %d blade %d' % (cluster,tuft,blade),
                          verts, [(0,2,1),(0,1,3),(1,2,3),(2,0,3)],
                          ('leaf','leaflight','leafdark')[(tuft+blade)%3])


building('NimboApartmentCorner', 4, False)
building('NimboShopTower', 5, True)
grass_patch()
bpy.context.view_layer.update()


def bake(name):
    points, normals, colors = [], [], []
    deps = bpy.context.evaluated_depsgraph_get()
    for source in PARTS[name]:
        evaluated = source.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        normal_matrix = evaluated.matrix_world.to_3x3().inverted().transposed()
        for tri in mesh.loop_triangles:
            color = mesh.materials[tri.material_index].diffuse_color
            for index, loop in zip(tri.vertices, tri.loops):
                p = evaluated.matrix_world @ mesh.vertices[index].co
                n = (normal_matrix @ mesh.corner_normals[loop].vector).normalized()
                points.append(dict(x=-p.x, y=p.z, z=-p.y))
                normals.append(dict(x=-n.x, y=n.z, z=-n.y))
                colors.append(dict(zip('rgba', color)))
        evaluated.to_mesh_clear()
    return dict(name=name, points=points, normals=normals, colors=colors,
                colliders=COLLIDERS[name], collide=False)


props = [bake(name) for name in PARTS]
metrics = []
for prop in props:
    lo = {axis:min(p[axis] for p in prop['points']) for axis in 'xyz'}
    hi = {axis:max(p[axis] for p in prop['points']) for axis in 'xyz'}
    assert len(prop['points']) <= 60000 and abs(lo['y']) < .001
    assert hi['x']-lo['x'] < 8 and hi['z']-lo['z'] < 8 and hi['y'] <= 24
    metrics.append(dict(name=prop['name'], triangles=len(prop['points'])//3,
                        boundsMin=lo, boundsMax=hi, colliders=len(prop['colliders'])))
(OUT/'landscape-props.json').write_text(json.dumps(dict(props=props),separators=(',',':')))
(OUT/'landscape-metrics.json').write_text(json.dumps(metrics,indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'NimboLandscapeKit.blend'))
print(json.dumps(metrics, indent=2))
