#!/usr/bin/env python3
"""Inventory shaders referenced by a local preload table and candidate sources.

Names and matching local shader declarations do not establish provenance,
licensing, semantic equivalence, or permission to redistribute a build.
"""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import re
import zipfile

import UnityPy


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('preload', type=Path)
    p.add_argument('obb', type=Path)
    p.add_argument('shader_directory', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new')
    preload_bytes = a.preload.read_bytes()
    preloads = [o for o in UnityPy.load(preload_bytes).objects if o.type.name == 'PreloadData']
    if len(preloads) != 1:
        raise ValueError('expected exactly one PreloadData object')
    obj = preloads[0]
    names = [str(x.path).replace('\\', '/').rsplit('/', 1)[-1].lower() for x in obj.assets_file.externals]
    if len(names) != len(set(names)):
        raise ValueError('ambiguous external paths')
    refs = defaultdict(set)
    for ref in obj.read_typetree()['m_Assets']:
        fid, pid = ref['m_FileID'], ref['m_PathID']
        if fid <= 0 or fid > len(names):
            raise ValueError('invalid preload reference')
        name = names[fid - 1]
        if re.fullmatch('[0-9a-f]{32}', name):
            if pid in refs[name]:
                raise ValueError('duplicate reference')
            refs[name].add(pid)
    declarations = defaultdict(list)
    for path in sorted(a.shader_directory.rglob('*.shader')):
        data = path.read_bytes()
        match = re.search(rb'\bShader\s+"([^"\r\n]+)"', data)
        if match:
            declarations[match.group(1).decode('utf-8')].append({'path': str(path), 'sha256': sha(data)})
    rows = []
    with zipfile.ZipFile(a.obb) as archive:
        if len(archive.namelist()) != len(set(archive.namelist())):
            raise ValueError('duplicate archive entry')
        for filename, ids in sorted(refs.items()):
            member = 'assets/bin/Data/' + filename
            objects = UnityPy.load(archive.read(member)).objects
            by_id = {o.path_id: o for o in objects}
            if len(by_id) != len(objects) or not ids <= by_id.keys():
                raise ValueError('preload object missing')
            for pid in sorted(ids):
                target = by_id[pid]
                if target.type.name != 'Shader':
                    continue
                shader = target.read()
                name = shader.m_ParsedForm.m_Name
                rows.append({'archive_entry': member, 'path_id': pid, 'name': name,
                             'object_bytes': len(target.get_raw_data()),
                             'object_sha256': sha(target.get_raw_data()),
                             'serialized_platform_ids': list(shader.platforms),
                             'local_declarations': declarations.get(name, [])})
    if not rows:
        raise ValueError('no preloaded shaders found')
    with a.obb.open('rb') as f:
        obb_sha = hashlib.file_digest(f, 'sha256').hexdigest()
    report = {'preload_sha256': sha(preload_bytes), 'obb_sha256': obb_sha, 'shader_count': len(rows),
              'shaders': rows, 'limitations': 'Only hashed external objects referenced by this preload are counted. Matching names are leads, not proof of code identity, origin or licensing.'}
    with a.output.open('x', encoding='utf-8') as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
        f.write('\n')
    print(f"{len(rows)} shaders, {sum(bool(r['local_declarations']) for r in rows)} with local declarations")


if __name__ == '__main__':
    main()
