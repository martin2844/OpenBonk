#!/usr/bin/env python3
"""Compare locally compiled shader objects with the private Android template.

Matching hashes establish serialized identity, not ownership or distribution rights.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile

import UnityPy

ENTRIES = {
    'ProBuilder/Standard Vertex Color': '1ccecd9b89b8a4f14bfb64f29ddfcc81',
    'MK/Toon/Built-in/Standard/Simple': '39354b8a5f5520b4e85f2c9fe7791c60',
}


def shader_objects(data):
    result = {}
    for obj in UnityPy.load(data).objects:
        if obj.type.name != 'Shader':
            continue
        name = obj.read_typetree()['m_ParsedForm']['m_Name']
        if name in result:
            raise ValueError('duplicate shader ' + name)
        tree = obj.read_typetree()
        result[name] = (obj.get_raw_data(), tree['m_Dependencies'])
    return result


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('base', type=Path)
    p.add_argument('bundle', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink():
        p.error('output already exists')
    compiled = shader_objects(str(a.bundle))
    report = []
    with zipfile.ZipFile(a.base) as base:
        if len(base.namelist()) != len(set(base.namelist())):
            raise ValueError('duplicate template entry')
        for name, entry in ENTRIES.items():
            target = shader_objects(base.read('assets/bin/Data/' + entry))
            if set(target) != {name} or name not in compiled:
                raise ValueError('missing or unexpected shader: ' + name)
            (old, old_deps), (new, new_deps) = target[name], compiled[name]
            row = {'name': name, 'target_sha256': sha(old), 'compiled_sha256': sha(new),
                   'target_bytes': len(old), 'compiled_bytes': len(new), 'byte_identical': old == new}
            if len(old) == len(new) and len(old_deps) == len(new_deps) and old_deps:
                def encoded(deps):
                    return b''.join(struct.pack('<iq', d['m_FileID'], d['m_PathID']) for d in deps)
                old_refs, new_refs = encoded(old_deps), encoded(new_deps)
                old_pos, new_pos = old.find(old_refs), new.find(new_refs)
                if (old_pos >= 0 and old_pos == new_pos and old.count(old_refs) == 1
                        and new.count(new_refs) == 1):
                    patched = new[:new_pos] + old_refs + new[new_pos + len(new_refs):]
                    row['dependency_reference_offset'] = old_pos
                    row['dependency_reference_bytes'] = len(old_refs)
                    row['identical_with_template_references'] = patched == old
            report.append(row)
    with a.output.open('x', encoding='utf-8') as f:
        json.dump({'shaders': report, 'limitations': [
            'Object identity does not establish rights or source provenance.',
            'Non-identical objects have not been compared visually or semantically.']}, f, indent=2)
        f.write('\n')
    for item in report:
        status = ('identical' if item['byte_identical'] else
                  'identical after restoring dependency references' if item.get('identical_with_template_references')
                  else 'different')
        print(item['name'], status)


if __name__ == '__main__':
    main()
