#!/usr/bin/env python3
"""Inventory animation clips in a local Steam Addressables directory.

The report contains file hashes, asset names and identifiers, not asset bytes.
It is a diagnostic inventory, not a provenance, fidelity or rights assessment.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import UnityPy

MAX_BUNDLE = 128 * 1024 * 1024
MAX_OBJECTS = 100000

def sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('directory', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if not a.directory.is_dir() or a.directory.is_symlink(): p.error('directory must exist and not be a symlink')
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir(): p.error('output must be new')
    bundles = sorted(a.directory.rglob('*.bundle'))
    if not bundles or len(bundles) > 1000: raise ValueError('invalid bundle count')
    results = []
    for path in bundles:
        if path.is_symlink() or not path.is_file() or not 0 < path.stat().st_size <= MAX_BUNDLE: raise ValueError('invalid bundle: ' + path.name)
        env = UnityPy.load(str(path)); objects = list(env.objects)
        if not objects or len(objects) > MAX_OBJECTS: raise ValueError('invalid object count: ' + path.name)
        counts = collections.Counter(o.type.name for o in objects); clips = []
        for obj in objects:
            if obj.type.name == 'AnimationClip':
                clip = obj.read(); clips.append({'path_id':obj.path_id, 'name':clip.m_Name,
                                                 'object_sha256':hashlib.sha256(obj.get_raw_data()).hexdigest()})
        results.append({'file':str(path.relative_to(a.directory)), 'size':path.stat().st_size,
                        'sha256':sha(path), 'object_count':len(objects), 'types':dict(sorted(counts.items())), 'clips':clips})
    report = {'bundles':results, 'clip_count':sum(len(r['clips']) for r in results),
              'limitations':['Only .bundle files beneath the supplied directory were inspected.',
                             'A readable inventory does not establish rights, animation equivalence or source provenance.']}
    with a.output.open('x') as f: json.dump(report, f, indent=2); f.write('\n')
    print('Inventoried', len(results), 'bundles;', report['clip_count'], 'animation clips')

if __name__ == '__main__': main()
