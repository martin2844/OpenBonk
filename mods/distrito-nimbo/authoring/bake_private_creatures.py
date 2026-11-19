"""Bake locally supplied skinned GLBs into articulated mesh animation frames.

Usage: blender -b --python bake_private_creatures.py -- PRIVATE_ROOT [MODEL_ID ...]
PRIVATE_ROOT contains decoded/<id>.glb; output is creatures/<id>.json.
The source rigs are retained and posed before every mesh sample. This is
intentionally independent of optional source clips (some are missing, and the
Charizard source only contains a dizzy animation).
"""
import json
import math
import re
import sys
from pathlib import Path

import bpy
from mathutils import Quaternion, Vector

ROSTER = {
    '15': ('Beedrill', True), '16': ('Pidgey', False),
    '19': ('Rattata', False), '20': ('Raticate', False),
    '22': ('Fearow', False), '24': ('Arbok', True),
    '34': ('Nidoking', True), '6': ('Charizard', True),
    '112': ('Rhydon', True), '130': ('Gyarados', True),
}
FRAMES = 12


def bone_key(name):
    return re.sub(r'_\d+$', '', re.sub(r'^\d+\s+', '', name))


class Rig:
    def __init__(self, obj):
        self.obj = obj
        self.bones = {bone_key(b.name): b for b in obj.pose.bones}
        self.rest = {b.name: b.matrix_basis.copy() for b in obj.pose.bones}
        self.axes = {}
        for key, bone in self.bones.items():
            inverse = (obj.matrix_world @ bone.matrix).to_3x3().inverted()
            self.axes[key] = [inverse @ Vector(axis) for axis in ((1, 0, 0), (0, 1, 0), (0, 0, 1))]

    def reset(self):
        for bone in self.obj.pose.bones:
            bone.matrix_basis = self.rest[bone.name]

    def rotate(self, name, axis, angle, append=False):
        bone = self.bones.get(name)
        if bone is None:
            return False
        # Rotate around the model's anatomical world axis, expressed in this
        # bone's local basis. Imported rigs do not share bone roll conventions.
        basis = bone.matrix_basis if append else self.rest[bone.name]
        bone.matrix_basis = basis @ Quaternion(self.axes[name][axis], angle).to_matrix().to_4x4()
        return True


def pose(rigs, source_id, clip, phase):
    idle = clip == 'idle'
    gain = .26 if idle else (1.2 if clip == 'charge' else 1.0)
    s = math.sin(phase)
    for rig in rigs:
        rig.reset()
        if source_id in ('16', '22'):
            # Both flight and hovering idle flap: neither is a frozen T-pose.
            for side, sign in (('L', 1), ('R', -1)):
                rig.rotate(side + 'Arm', 1, -sign * (.55 if idle else .65) * (1 - math.cos(phase)))
                rig.rotate(side + 'ForeArm', 1, -sign * .10 * (1 - math.cos(phase - .3)))
                rig.rotate(side + 'Hand', 1, -sign * .06 * (1 - math.cos(phase - .6)))
                rig.rotate(side + 'Thigh', 0, -.18 + .08 * math.cos(phase))
            rig.rotate('Head', 0, .08 * math.sin(phase + .3))
        elif source_id == '15':
            for side, sign in (('L', 1), ('R', -1)):
                for wing in ('FeelerA', 'FeelerB'):
                    rig.rotate(side + wing, 1, sign * .85 * s)
                rig.rotate(side + 'Arm', 0, sign * .18 * s)
                rig.rotate(side + 'Thigh', 0, sign * .15 * s)
            rig.rotate('Tail1', 0, .16 * math.sin(phase - .4))
        elif source_id in ('24', '130'):
            # Propagating lateral bends through the actual weighted spine/tail.
            for key in rig.bones:
                match = re.fullmatch(r'(Spine|Tail)(\d+)', key)
                if match:
                    n = int(match.group(2))
                    rig.rotate(key, 2, gain * .13 * math.sin(phase - n * .55))
            rig.rotate('Neck', 2, gain * .15 * math.sin(phase - 1.2))
            rig.rotate('Head', 0, gain * .09 * math.sin(phase))
            rig.rotate('Jaw', 0, gain * .10 * (1 + s))
        else:
            quadruped = source_id == '19'
            for side, sign in (('L', 1), ('R', -1)):
                step = phase + (0 if sign == 1 else math.pi)
                rig.rotate(side + 'Thigh', 0, gain * .48 * math.sin(step))
                rig.rotate(side + 'Leg', 0, gain * .30 * max(0, math.sin(step + .7)))
                rig.rotate(side + 'Foot', 0, gain * -.20 * math.sin(step + .5))
                rig.rotate(side + 'Arm', 0, gain * (-.40 if quadruped else -.27) * math.sin(step))
                if source_id in ('34', '112'):
                    rig.rotate(side + 'Arm', 1, sign * .95, append=True)
                rig.rotate(side + 'ForeArm', 0, gain * .18 * math.sin(step + .7))
            rig.rotate('Spine1', 2, gain * .06 * s)
            rig.rotate('Spine', 2, gain * .06 * s)
            rig.rotate('Head', 0, gain * .10 * math.sin(phase + .5))
            for key in rig.bones:
                match = re.fullmatch(r'Tail(\d+)', key)
                if match:
                    rig.rotate(key, 2, gain * .12 * math.sin(phase - int(match.group(1)) * .6))
            if source_id == '6':
                for side, sign in (('L', 1), ('R', -1)):
                    rig.rotate(side + 'Feeler1', 1, sign * .40 * s)
    bpy.context.view_layer.update()


# Species-wide triangle budgets: bosses retain their silhouette at close range.
BUDGETS = {'15': 1600, '16': 1400, '19': 1200, '20': 1400, '22': 1600,
           '24': 1800, '34': 2200, '6': 3200, '112': 3000, '130': 3200}


def prepare(source):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    for obj in bpy.data.objects:
        obj.animation_data_clear()
    bpy.context.view_layer.update()
    # Source helper Icospheres have no materials. Keep every actual textured
    # component, including Beedrill's previously discarded 98-vertex piece.
    meshes = sorted((o for o in bpy.data.objects if o.type == 'MESH' and len(o.data.polygons) and len(o.data.materials)), key=lambda o: o.name)
    if not meshes:
        raise ValueError(f'No renderable geometry: {source}')
    budget = BUDGETS[source.stem]
    small = sum(len(o.data.polygons) for o in meshes if len(o.data.polygons) <= 250)
    large = sum(len(o.data.polygons) for o in meshes if len(o.data.polygons) > 250)
    ratio = min(1.0, max(.1, (budget - small) / max(1, large)))
    for obj in meshes:
        if len(obj.data.polygons) > 250 and ratio < 1:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            modifier = obj.modifiers.new('Silhouette budget', 'DECIMATE')
            modifier.ratio = ratio
            modifier.use_collapse_triangulate = True
            bpy.ops.object.modifier_move_to_index(modifier=modifier.name, index=0)
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            obj.select_set(False)
    rigs = [Rig(o) for o in bpy.data.objects if o.type == 'ARMATURE']
    if not rigs:
        raise ValueError(f'Articulation requires a skin/armature: {source}')
    return meshes, rigs


def material_image(material):
    if material and material.use_nodes:
        bsdf = next((n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if bsdf:
            pending = [link.from_node for link in bsdf.inputs['Base Color'].links]
            seen = set()
            while pending:
                node = pending.pop()
                if node in seen: continue
                seen.add(node)
                if node.type == 'TEX_IMAGE' and node.image: return node.image
                for socket in node.inputs:
                    pending.extend(link.from_node for link in socket.links)
    return None


def atlas(meshes, destination, boss):
    import numpy as np
    materials = sorted({m for o in meshes for m in o.data.materials}, key=lambda m: m.name)
    side = 1024 if boss else 512
    pad = 4
    tiles = []
    alpha_clip = False
    for material in materials:
        image = material_image(material)
        if image:
            width, height = image.size
            pixels = np.array(image.pixels[:], dtype=np.float32).reshape(height, width, image.channels)
            # Preserve full native face textures; body maps on common mobs may
            # be halved to keep their complete material atlas at 512x512.
            maximum = 512 if boss else 248
            factor = min(1, maximum / max(width, height))
            w, h = max(1, round(width*factor)), max(1, round(height*factor))
            pixels = pixels[(np.arange(h)*height//h)[:,None], (np.arange(w)*width//w)[None,:]]
            if pixels.shape[2] < 4:
                pixels = np.concatenate((pixels[:,:,:3], np.ones((h,w,1),dtype=np.float32)),axis=2)
        else:
            w=h=4
            pixels=np.tile(np.array(material.diffuse_color,dtype=np.float32),(h,w,1))
        if destination.stem == 'CharizardAtlas' and material.name in ('Material_15', 'Material_16'):
            # These two source flame layers contain grayscale intensity masks,
            # exported as base color with no emission/color factors. Reconstruct
            # their fire palette without recoloring the body, wings or eyes.
            intensity = np.clip(pixels[:,:,:3].mean(axis=2), 0, 1)
            pixels[:,:,0] = .95 + .05 * intensity
            pixels[:,:,1] = .055 + .84 * intensity**1.15
            pixels[:,:,2] = .008 + .06 * intensity**2
        # Only the imported iris overlays opt into transparency. Body maps
        # contain transparent backgrounds even though their glTF is OPAQUE.
        masked = material.use_nodes and any(n.type == 'BSDF_PRINCIPLED' and n.inputs['Alpha'].is_linked for n in material.node_tree.nodes)
        if masked: alpha_clip = True
        else: pixels[:,:,3]=1
        tiles.append((material.name,w,h,pixels))
    tiles.sort(key=lambda t: (-t[2], -t[1], t[0]))
    x=y=0; rowheight=0; usedwidth=usedheight=0; placed=[]
    for name,w,h,pixels in tiles:
        if x+w+2*pad > side:
            x=0;y+=rowheight;rowheight=0
        if y+h+2*pad > side: raise ValueError('Atlas exceeds budget: '+destination.name)
        placed.append((name,x+pad,y+pad,w,h,pixels))
        x+=w+2*pad;rowheight=max(rowheight,h+2*pad)
        usedwidth=max(usedwidth,x);usedheight=max(usedheight,y+rowheight)
    width=2**math.ceil(math.log2(max(2,usedwidth)))
    height=2**math.ceil(math.log2(max(2,usedheight)))
    canvas=np.ones((height,width,4),dtype=np.float32)
    rects={}
    for name,x,y,w,h,pixels in placed:
        canvas[y-pad:y+h+pad,x-pad:x+w+pad]=np.pad(pixels,((pad,pad),(pad,pad),(0,0)),mode='edge')
        rects[name]=(x,y,w,h,width,height)
    image=bpy.data.images.new(destination.stem,width=width,height=height,alpha=alpha_clip)
    image.colorspace_settings.name='sRGB'
    image.pixels.foreach_set(canvas.ravel());image.filepath_raw=str(destination);image.file_format='PNG';image.save()
    return rects, [width,height], alpha_clip


def geometry(meshes):
    result=[]
    depsgraph=bpy.context.evaluated_depsgraph_get()
    for object_index,obj in enumerate(meshes):
        evaluated=obj.evaluated_get(depsgraph);mesh=evaluated.to_mesh()
        try:
            mesh.calc_loop_triangles();uv_layer=mesh.uv_layers.active
            normal_matrix=evaluated.matrix_world.to_3x3().inverted().transposed()
            for triangle in mesh.loop_triangles:
                material=mesh.materials[triangle.material_index]
                for vertex,loop in zip(triangle.vertices,triangle.loops):
                    point=evaluated.matrix_world @ mesh.vertices[vertex].co
                    normal=(normal_matrix @ mesh.corner_normals[loop].vector).normalized()
                    uv=uv_layer.data[loop].uv.copy() if uv_layer else Vector((.5,.5))
                    result.append((point,normal,uv,material.name,object_index,vertex))
        finally:evaluated.to_mesh_clear()
    return result


def indexed_bind(bind, rects):
    mapping={};representatives=[];indices=[];uvs=[];regions=[]
    for i,(point,normal,uv,material,obj,vertex) in enumerate(bind):
        key=(obj,vertex,tuple(round(n,5) for n in normal),tuple(round(v,7) for v in uv),material)
        if key not in mapping:
            mapping[key]=len(representatives);representatives.append(i)
            x,y,w,h,aw,ah=rects[material]
            # Keep repeated source UVs; shader wraps inside this material's
            # padded atlas region (Raticate eyes use UV coordinates above 1).
            uvs.append(dict(x=uv.x,y=uv.y))
            regions.append(dict(x=(x+.5)/aw,y=(y+.5)/ah,z=(w-1)/aw,w=(h-1)/ah))
        indices.append(mapping[key])
    return representatives,indices,uvs,regions


def validate(clips, source_id):
    metrics = {}
    for clip in clips:
        frames = [[Vector((p['x'], p['y'], p['z'])) for p in f['points']] for f in clip['frames']]
        if len({len(frame) for frame in frames}) != 1:
            raise ValueError('Topology changed between animation frames')
        # Rigid translation AND rigid rotation preserve all pairwise distances.
        # This catches the old static-mesh bob/sway implementation.
        indices = list(range(0, len(frames[0]), max(1, len(frames[0]) // 70)))
        pairs = [(a, b) for a in indices for b in indices if a < b]
        rest = [(frames[0][a] - frames[0][b]).length for a, b in pairs]
        deformation = max(abs((frame[a] - frame[b]).length - d)
                          for frame in frames[1:] for (a, b), d in zip(pairs, rest))
        if deformation < (.01 if clip['name'] == 'idle' else .06):
            raise ValueError(f'{source_id} {clip["name"]}: no meaningful joint articulation ({deformation})')
        entry = {'pair_distance_change': round(deformation, 4)}
        if source_id in ('16', '22'):
            # Track same rest-pose wingtip vertices relative to torso centroid.
            points = frames[0]
            center = sorted(range(len(points)), key=lambda i: abs(points[i].x))[:max(3, len(points)//10)]
            tips = sorted(range(len(points)), key=lambda i: abs(points[i].x), reverse=True)[:max(3, len(points)//20)]
            heights = [sum(frame[i].y for i in tips)/len(tips) - sum(frame[i].y for i in center)/len(center) for frame in frames]
            entry['wingtip_vertical_excursion'] = round(max(heights) - min(heights), 4)
            first_floor = min(p.y for p in frames[0])
            full_floor = min(p.y for frame in frames for p in frame)
            entry['below_first_frame_floor'] = round(max(0, first_floor - full_floor), 4)
            if entry['below_first_frame_floor'] > .10:
                raise ValueError(f'{source_id} {clip["name"]}: flap intersects floor: {entry}')
            if entry['wingtip_vertical_excursion'] < .4:
                raise ValueError(f'{source_id} {clip["name"]}: wing flap too small: {entry}')
        metrics[clip['name']] = entry
    return metrics


def bake(root, source_id):
    prefix, charger = ROSTER[source_id]
    meshes, rigs = prepare(root / 'decoded' / f'{source_id}.glb')
    double_sided = any(not m.use_backface_culling for o in meshes for m in o.data.materials)
    destination=root/'creatures'/f'{source_id}.json';destination.parent.mkdir(exist_ok=True)
    texture=destination.with_name(f'{prefix}Atlas.png')
    rects,texture_size,alpha_clip=atlas(meshes,texture,source_id == '6')
    bind=geometry(meshes)
    representatives,indices,uvs,regions=indexed_bind(bind,rects)
    low=min(v[0].z for v in bind);height=max(v[0].z for v in bind)-low
    center_x=(min(v[0].x for v in bind)+max(v[0].x for v in bind))/2
    center_y=(min(v[0].y for v in bind)+max(v[0].y for v in bind))/2
    scale=1.8/max(.001,height);clips=[]
    for clip in (('walk','idle','charge') if charger else ('walk','idle')):
        frames=[]
        for frame in range(FRAMES):
            phase=frame*math.tau/FRAMES;pose(rigs,source_id,clip,phase);posed=geometry(meshes)
            if len(posed)!=len(bind):raise ValueError('Skinning changed mesh topology')
            bob=(.025 if clip=='idle' else .055)*(1-math.cos(2*phase))
            # Reflect into Unity's handedness; keep front +Z and texture detail
            # in its original left/right orientation. Builder fixes winding.
            points=[dict(x=-(posed[i][0].x-center_x)*scale,y=(posed[i][0].z-low)*scale+bob,z=-(posed[i][0].y-center_y)*scale) for i in representatives]
            normals=[dict(x=-posed[i][1].x,y=posed[i][1].z,z=-posed[i][1].y) for i in representatives]
            if not all(math.isfinite(v) for p in points+normals for v in p.values()):raise ValueError('Nonfinite frame')
            frames.append(dict(name=f'{prefix}{clip.title()}{frame:02}',points=points,normals=normals,uv=uvs,atlasRects=regions,indices=indices,collide=False))
        clips.append(dict(name=clip,frames=frames))
    metrics=validate(clips,source_id)
    metrics['mesh']=dict(vertices=len(representatives),triangles=len(indices)//3,source_components=len(meshes),atlas=texture_size,alpha_clip=alpha_clip,double_sided=double_sided,
                         raw_mesh_bytes=len(clips)*FRAMES*(len(representatives)*48+len(indices)*2))
    destination.write_text(json.dumps(dict(fps=12,material='Nimbo'+prefix,texture=texture.name,alphaClip=alpha_clip,doubleSided=double_sided,clips=clips),separators=(',',':'))+'\n')
    print('TEXTURED ARTICULATION PASS',prefix,json.dumps(metrics),flush=True)
    return metrics


def main():
    args = sys.argv[sys.argv.index('--') + 1:]
    root = Path(args[0])
    receipt = root / 'animation-validation.json'
    results = json.loads(receipt.read_text()) if args[1:] and receipt.exists() else {}
    results.update({source_id: bake(root, source_id) for source_id in (args[1:] or ROSTER)})
    (root / 'animation-validation.json').write_text(json.dumps(results, indent=2) + '\n')


if __name__ == '__main__':
    main()
