#!/usr/bin/env python3
"""Search selected Unity data files for alternative source animation clips.

Local research only. Metadata and polynomial comparisons do not prove that a
candidate is the intended animation, that it behaves identically in Unity, or
that any asset can be redistributed. This never writes clip bytes to its report.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import zipfile
import UnityPy

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('inventory', HERE / 'inventory-hashed-animation-differences.py')
inventory = importlib.util.module_from_spec(spec); spec.loader.exec_module(inventory)
spec = importlib.util.spec_from_file_location('extrema', HERE / 'verify-hybrid-animation-extrema.py')
extrema = importlib.util.module_from_spec(spec); spec.loader.exec_module(extrema)
PLAN = '72b2f5bbff0e255c2fa14af35d73b22a2c81f65bb5725cc92c00e09cffd3a975'
MANIFEST = '716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1'
OBB = 'ca3fa6b0e1d994339655331b76c23c2d4f94c2038bd6f87c825575ab28cdb919'
PC = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
FILES = ('resources.assets', 'sharedassets0.assets', 'sharedassets1.assets', 'sharedassets2.assets',
         'sharedassets3.assets', 'sharedassets4.assets', 'level0', 'level1', 'level2', 'level3', 'level4')

def sha(data): return hashlib.sha256(data).hexdigest()
def file_sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()

def shape(tree):
    t = copy.deepcopy(tree)
    t['m_Name'] = ''
    t['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'] = []
    return sha(json.dumps(t, sort_keys=True, separators=(',', ':'), allow_nan=True).encode())

def maximum(a, b):
    get = lambda t: t['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']
    x, y = extrema.curve.frames(get(a)), extrema.curve.frames(get(b))
    if len(x) != len(y): raise ValueError('different frame count')
    result = 0.0; count = 0
    for i, ((xt, xkeys), (yt, ykeys)) in enumerate(zip(x, y)):
        if xt != yt or [k for k, _ in xkeys] != [k for k, _ in ykeys]: raise ValueError('different curve mapping')
        if i + 1 == len(x): continue
        dt = x[i + 1][0] - xt
        if xt < 0 or not (0 < dt <= 60): continue
        for (_, xc), (_, yc) in zip(xkeys, ykeys):
            result = max(result, extrema.extrema(xc, yc, dt)[0]); count += 1
    if not count: raise ValueError('no comparable segments')
    return result, count

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('pc_data', type=Path); p.add_argument('obb', type=Path); p.add_argument('plan', type=Path)
    p.add_argument('manifest', type=Path); p.add_argument('output', type=Path); a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir(): p.error('output must be new')
    if file_sha(a.obb) != OBB or file_sha(a.plan) != PLAN or file_sha(a.manifest) != MANIFEST or file_sha(a.pc_data/'sharedassets1.assets') != PC:
        raise ValueError('pinned input mismatch')
    plan = a.plan.read_bytes()
    if plan[:12] != b'OBSTX003' + struct.pack('>I', 108): raise ValueError('unexpected plan')
    pos = 268; pending = []
    for _ in range(108):
        name = plan[pos:pos+32].decode('ascii'); count = struct.unpack_from('>I', plan, pos+160)[0]
        pos += 164 + count*5
        if pos > len(plan): raise ValueError('truncated plan')
        if count: pending.append(name)
    if pos != len(plan) or len(pending) != 11 or len(set(pending)) != 11: raise ValueError('unexpected pending set')
    manifest = {r['file']:r for r in json.loads(a.manifest.read_text())['animations']}
    targets = {}
    with zipfile.ZipFile(a.obb) as z:
        if len(z.namelist()) != len(set(z.namelist())): raise ValueError('duplicate OBB entry')
        for name in pending:
            raw = z.read('assets/bin/Data/' + name); rec = manifest[name]
            if sha(raw) != rec['original_sha256']: raise ValueError('target mismatch')
            objects = UnityPy.load(raw).objects
            if len(objects) != 1 or objects[0].type.name != 'AnimationClip': raise ValueError('invalid target')
            tree = objects[0].read_typetree(); targets[name] = (rec, tree, shape(tree))
    shapes = {x[2] for x in targets.values()}; candidates = []; input_hashes = {}
    for name in FILES:
        path = a.pc_data/name
        if not path.is_file(): raise ValueError('missing data file: ' + name)
        input_hashes[name] = file_sha(path)
        for obj in UnityPy.load(str(path)).objects:
            if obj.type.name != 'AnimationClip': continue
            tree = obj.read_typetree(); fingerprint = shape(tree)
            if fingerprint in shapes:
                candidates.append((name, obj.path_id, tree, fingerprint))
    report = []
    for name, (rec, tree, fingerprint) in targets.items():
        found = []; failures = 0
        for source_file, path_id, source_tree, source_shape in candidates:
            if source_shape != fingerprint: continue
            try: difference, segments = maximum(tree, source_tree)
            except (ValueError, OverflowError, struct.error): failures += 1; continue
            found.append({'file':source_file, 'path_id':path_id, 'name':source_tree['m_Name'],
                          'max_curve_difference':difference, 'segments':segments,
                          'current_source':source_file == 'sharedassets1.assets' and path_id == rec['source_id']})
        found.sort(key=lambda x:(x['max_curve_difference'], x['file'], x['path_id']))
        if not any(x['current_source'] for x in found): raise ValueError('current candidate not compared: ' + name)
        report.append({'file':name, 'name':rec['name'], 'compared':len(found), 'mapping_mismatches':failures, 'candidates':found})
    output = {'inputs_sha256':input_hashes, 'targets':report,
              'limitations':['Only the listed Unity files were searched; addressable bundles were not searched.',
                             'Different metadata, nonmatching frame mappings and sentinel intervals were excluded.',
                             'Polynomial values do not establish pose, gameplay, or rights equivalence.']}
    with a.output.open('x') as f: json.dump(output, f, indent=2); f.write('\n')
    better = sum(x['candidates'][0]['max_curve_difference'] < next(y['max_curve_difference'] for y in x['candidates'] if y['current_source']) for x in report)
    print('Searched', len(FILES), 'Unity files;', len(report), 'targets;', better, 'with a lower-difference candidate')

if __name__ == '__main__': main()
