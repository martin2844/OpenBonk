#!/usr/bin/env python3
"""Make a private, verifiable Unity pose-study bundle from a Steam clip.

The input and output contain game assets. This tool does not authorize sharing.
"""
import argparse
import hashlib
import json
from pathlib import Path
import UnityPy

PC_HASH = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
MANIFEST_HASH = '716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1'

def sha(data): return hashlib.sha256(data).hexdigest()

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('bundle', type=Path)
    p.add_argument('steam_asset', type=Path)
    p.add_argument('manifest', type=Path)
    p.add_argument('entry')
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir(): p.error('output must be new in an existing directory')
    manifest, data = a.manifest.read_bytes(), a.steam_asset.read_bytes()
    if sha(manifest) != MANIFEST_HASH or sha(data) != PC_HASH: raise ValueError('unexpected input hash')
    records = [r for r in json.loads(manifest)['animations'] if r['file'] == a.entry]
    if len(records) != 1: raise ValueError('entry must identify one clip')
    rec = records[0]
    sources = [o for o in UnityPy.load(data).objects if o.path_id == rec['source_id']]
    if len(sources) != 1 or sources[0].type.name != 'AnimationClip': raise ValueError('unexpected Steam clip')
    raw = sources[0].get_raw_data()
    if sha(raw) != rec['source_sha256'] or len(raw) != rec['size']: raise ValueError('Steam clip mismatch')
    bundle = UnityPy.load(str(a.bundle))
    clips = [o for o in bundle.objects if o.type.name == 'AnimationClip' and sha(o.get_raw_data()) == rec['object_sha256']]
    if len(clips) != 1 or clips[0].byte_size != len(raw): raise ValueError('reference clip mismatch')
    target_id = clips[0].path_id
    before = {(o.type.name, o.path_id): sha(o.get_raw_data()) for o in bundle.objects}
    if len(before) != len(bundle.objects): raise ValueError('duplicate object identifiers')
    clips[0].set_raw_data(raw)
    result = bundle.file.save(packer='none')
    after_objects = UnityPy.load(result).objects
    after = {(o.type.name, o.path_id): sha(o.get_raw_data()) for o in after_objects}
    before[('AnimationClip', target_id)] = rec['source_sha256']
    if len(after) != len(after_objects) or after != before: raise ValueError('bundle verification failed')
    with a.output.open('xb') as out: out.write(result)
    print('Private study bundle created; sha256', sha(result))

if __name__ == '__main__': main()
