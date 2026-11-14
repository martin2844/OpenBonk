#!/usr/bin/env python3
"""Compare selected scene behaviour references between local Unity builds.

The report contains game-derived paths, identities and names; keep it private.
Only RandomSfx, Lava and InteractablePot serialized layouts are checked. Names
and pointer identities do not establish asset contents or gameplay equivalence.
Requires UnityPy and the exact supported Unity 2023.2 Android serialized layouts.
"""
import argparse
from collections import Counter
import importlib.util
import json
from pathlib import Path
import struct
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('scene_content_inventory', ROOT / 'tools/compare-unity-build-content.py')
inventory = importlib.util.module_from_spec(spec); spec.loader.exec_module(inventory)
CLASSES = frozenset(('RandomSfx', 'Lava', 'InteractablePot'))


def normalized_tail(cls, data, resolve):
    def pointer(offset):
        if offset < 0 or offset + 12 > len(data): raise ValueError('truncated pointer')
        return resolve(*struct.unpack_from('<iq', data, offset))
    if cls == 'Lava':
        if len(data) != 12: raise ValueError('unexpected lava layout')
        return [pointer(0)]
    if cls == 'RandomSfx':
        if len(data) < 4: raise ValueError('truncated sound array')
        count = struct.unpack_from('<I', data)[0]
        if count > 64 or len(data) < 4 + count * 12: raise ValueError('invalid sound array')
        return [[pointer(4 + i * 12) for i in range(count)], data[4 + count * 12:].hex()]
    if cls == 'InteractablePot':
        if len(data) < 80: raise ValueError('unexpected pot layout')
        return [data[:20].hex(), [pointer(20 + i * 12) for i in range(5)], data[80:].hex()]
    raise ValueError('unsupported behaviour')


def read_build(paths):
    import UnityPy
    groups = {}
    for path in paths:
        with zipfile.ZipFile(path) as z:
            for name, parts in inventory.asset_groups(z).items():
                if name == 'globalgamemanagers.assets' or name.startswith(('level', 'sharedassets')):
                    if name in groups: raise ValueError('duplicate serialized group')
                    groups[name] = b''.join(z.read(part) for part in parts)
    if 'globalgamemanagers.assets' not in groups: raise ValueError('missing global game managers')
    scripts = {}
    for obj in UnityPy.load(groups['globalgamemanagers.assets']).objects:
        if obj.type.name == 'MonoScript':
            item = obj.read(); scripts[obj.path_id] = (item.m_AssemblyName, item.m_Namespace, item.m_ClassName)
    assets = {}; problems = []; result = {}
    for name, raw in groups.items():
        if not name.startswith('sharedassets'): continue
        entries = {}
        for obj in UnityPy.load(raw).objects:
            if obj.type.name not in ('GameObject', 'AudioClip'): continue
            try:
                value = obj.read(); object_name = value.m_Name
                if isinstance(object_name, str) and object_name: entries[obj.path_id] = [obj.type.name, object_name]
            except Exception as error: problems.append({'group': name, 'path_id': obj.path_id, 'error': type(error).__name__})
        assets[name] = entries
    for group, raw in groups.items():
        if not group.startswith('level'): continue
        env = UnityPy.load(raw)
        if not env.objects: raise ValueError('empty scene group')
        external = env.objects[0].assets_file.externals
        def resolve(file_id, path_id):
            if file_id == 0 and path_id == 0: return ['null']
            if file_id < 1 or file_id > len(external): raise ValueError('unsupported external pointer')
            name = str(external[file_id - 1].path).replace('\\', '/').rsplit('/', 1)[-1]
            try: return assets[name][path_id]
            except KeyError: raise ValueError('unresolved named reference') from None
        for obj in env.objects:
            if obj.type.name != 'MonoBehaviour': continue
            try:
                base = obj.read_typetree(check_read=False); script = base['m_Script']
                if script['m_FileID'] != 1 or script['m_PathID'] not in scripts:
                    raise ValueError('unresolved script reference')
                identity = scripts.get(script['m_PathID'])
                if not identity or identity[2] not in CLASSES: continue
                raw_data = obj.get_raw_data()
                if len(raw_data) < 32: raise ValueError('truncated behaviour header')
                key = (group, obj.path_id, *identity)
                result[key] = {'game_object': base['m_GameObject'], 'fields': normalized_tail(identity[2], raw_data[32:], resolve)}
            except Exception as error:
                problems.append({'group': group, 'path_id': obj.path_id, 'error': type(error).__name__, 'detail': str(error)[:120]})
    return result, problems


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('reference-apk', 'reference-obb', 'candidate-apk', 'candidate-obb', 'output'): p.add_argument('--' + name, type=Path, required=True)
    args = p.parse_args(); paths = []
    for name in ('reference_apk', 'reference_obb', 'candidate_apk', 'candidate_obb'):
        path = getattr(args, name).expanduser()
        if path.is_symlink() or not path.is_file(): p.error(name + ' must be a regular file')
        paths.append(path.resolve())
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in paths: p.error('output must be separate from the checkout and inputs')
    a, pa = read_build(paths[:2]); b, pb = read_build(paths[2:]); matched = a.keys() & b.keys()
    counts = Counter(key[-1] for key in matched if a[key] == b[key])
    def key_json(key): return list(key)
    result = {'schema': 1, 'matched_by_class': dict(sorted(counts.items())),
              'different': [key_json(key) for key in sorted(matched) if a[key] != b[key]],
              'reference_only': [key_json(key) for key in sorted(a.keys() - b.keys())],
              'candidate_only': [key_json(key) for key in sorted(b.keys() - a.keys())],
              'reference_problems': pa, 'candidate_problems': pb,
              'limitations': 'Only three specific scene behaviour layouts are checked. Named external references are matched by type and name, not content. Local object IDs and non-reference bytes are compared directly. Asset bundles, executable code and gameplay are not compared.'}
    with output.open('x', encoding='utf-8') as f: json.dump(result, f, indent=2); f.write('\n')
    print('Matched by class:', dict(counts), '; different:', len(result['different']), '; unresolved/read problems:', len(pa) + len(pb), flush=True)


if __name__ == '__main__': main()
