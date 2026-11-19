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


def texture_sampler():
    cache = {}
    def sample(material, uv):
        if material and material.use_nodes:
            for node in material.node_tree.nodes:
                if node.type != 'TEX_IMAGE' or not node.image:
                    continue
                image = node.image
                if image.name not in cache:
                    cache[image.name] = (tuple(image.size), image.channels, list(image.pixels))
                (width, height), channels, pixels = cache[image.name]
                if width and height:
                    x = min(width - 1, max(0, int((uv.x % 1) * width)))
                    y = min(height - 1, max(0, int((uv.y % 1) * height)))
                    start = (y * width + x) * channels
                    if len(pixels) >= start + 3:
                        return dict(zip('rgba', (*pixels[start:start + 3], 1.0)))
        color = material.diffuse_color if material else (1, 1, 1, 1)
        return dict(zip('rgba', (float(color[0]), float(color[1]), float(color[2]), 1.0)))
    return sample


def prepare(source):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    # Clear active/NLA actions before posing; source timing must never overwrite
    # procedural joint transforms on depsgraph evaluation.
    for obj in bpy.data.objects:
        obj.animation_data_clear()
        # Keep the imported basis: glTF coordinate-conversion transforms may
        # live on the root pose bone even when there is no animation.
    bpy.context.view_layer.update()
    meshes = [o for o in bpy.data.objects if o.type == 'MESH' and len(o.data.vertices) >= 100]
    if not meshes:
        raise ValueError(f'No renderable geometry: {source}')
    # Apply simplification ONCE in bind pose before skinning. Decimating after
    # every posed sample can change topology, making both motion and colors pop.
    for obj in meshes:
        if len(obj.data.polygons) > 600:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            modifier = obj.modifiers.new('Mobile topology', 'DECIMATE')
            modifier.ratio = 600 / len(obj.data.polygons)
            bpy.ops.object.modifier_move_to_index(modifier=modifier.name, index=0)
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            obj.select_set(False)
    rigs = [Rig(o) for o in bpy.data.objects if o.type == 'ARMATURE']
    if not rigs:
        raise ValueError(f'Articulation requires a skin/armature: {source}')
    return meshes, rigs


def geometry(meshes, sample=None):
    result = []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in meshes:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        try:
            mesh.calc_loop_triangles()
            uv_layer = mesh.uv_layers.active
            normal_matrix = evaluated.matrix_world.to_3x3().inverted().transposed()
            for triangle in mesh.loop_triangles:
                material = mesh.materials[triangle.material_index] if triangle.material_index < len(mesh.materials) else None
                for vertex, loop in zip(triangle.vertices, triangle.loops):
                    point = evaluated.matrix_world @ mesh.vertices[vertex].co
                    normal = (normal_matrix @ mesh.corner_normals[loop].vector).normalized()
                    color = sample(material, uv_layer.data[loop].uv if uv_layer else Vector((0, 0))) if sample else None
                    result.append((point, normal, color))
        finally:
            evaluated.to_mesh_clear()
    return result


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
    bind = geometry(meshes, texture_sampler())
    low = min(p.z for p, _, _ in bind)
    height = max(p.z for p, _, _ in bind) - low
    center_x = (min(p.x for p, _, _ in bind) + max(p.x for p, _, _ in bind)) / 2
    center_y = (min(p.y for p, _, _ in bind) + max(p.y for p, _, _ in bind)) / 2
    scale = 1.8 / max(.001, height)
    colors = [color for _, _, color in bind]
    clips = []
    for clip in (('walk', 'idle', 'charge') if charger else ('walk', 'idle')):
        frames = []
        for frame in range(FRAMES):
            phase = frame * math.tau / FRAMES
            pose(rigs, source_id, clip, phase)
            posed = geometry(meshes)
            if len(posed) != len(bind):
                raise ValueError('Skinning changed mesh topology')
            bob = (.025 if clip == 'idle' else .055) * (1 - math.cos(2 * phase))
            points = [dict(x=(p.x-center_x)*scale, y=(p.z-low)*scale+bob, z=-(p.y-center_y)*scale) for p, _, _ in posed]
            normals = [dict(x=n.x, y=n.z, z=-n.y) for _, n, _ in posed]
            frames.append(dict(name=f'{prefix}{clip.title()}{frame:02}', points=points, normals=normals, colors=colors, collide=False))
        clips.append(dict(name=clip, frames=frames))
    metrics = validate(clips, source_id)
    destination = root / 'creatures' / f'{source_id}.json'
    destination.parent.mkdir(exist_ok=True)
    destination.write_text(json.dumps(dict(fps=12, clips=clips), separators=(',', ':')) + '\n')
    print('ARTICULATION PASS', prefix, json.dumps(metrics), flush=True)
    return metrics


def main():
    args = sys.argv[sys.argv.index('--') + 1:]
    root = Path(args[0])
    results = {source_id: bake(root, source_id) for source_id in (args[1:] or ROSTER)}
    (root / 'animation-validation.json').write_text(json.dumps(results, indent=2) + '\n')


if __name__ == '__main__':
    main()
