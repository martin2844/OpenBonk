#!/usr/bin/env python3
"""Compare locally compiled shader objects to a private Unity Android OBB.

Reports contain game-derived names and hashes and must stay local. Equality
does not establish source provenance, distribution rights or visual parity.
Requires UnityPy.
"""
import argparse
from collections import defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
from zipfile import ZipFile
import UnityPy

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('build_content', ROOT / 'tools/compare-unity-build-content.py')
content = importlib.util.module_from_spec(spec)
spec.loader.exec_module(content)


def shader_objects(objects):
    by_name = defaultdict(list)
    for obj in objects:
        if obj.type.name != 'Shader':
            continue
        name = obj.read_typetree().get('m_ParsedForm', {}).get('m_Name')
        if not isinstance(name, str) or not name:
            raise ValueError('unnamed shader')
        raw = obj.get_raw_data()
        if len(raw) != obj.byte_size:
            raise ValueError('invalid shader size')
        by_name[name].append((len(raw), hashlib.sha256(raw).hexdigest()))
    return by_name


def compare(source, target):
    rows = []
    for name, candidates in sorted(source.items()):
        targets = target.get(name, [])
        rows.append({'name': name, 'source_objects': len(candidates), 'target_objects': len(targets),
                     'matching_objects': sum(item in candidates for item in targets),
                     'source_sha256': sorted({item[1] for item in candidates}),
                     'target_sha256': sorted({item[1] for item in targets})})
    return rows


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--bundle', required=True, type=Path)
    p.add_argument('--obb', required=True, type=Path)
    p.add_argument('--report', required=True, type=Path)
    a = p.parse_args()
    inputs = []
    for path in (a.bundle, a.obb):
        if path.is_symlink() or not path.is_file():
            p.error('inputs must be regular files')
        inputs.append(path.resolve())
    report = a.report.expanduser().absolute()
    if report.exists() or report.is_symlink() or not report.parent.is_dir():
        p.error('report must be a new file')
    report = report.parent.resolve() / report.name
    if report.is_relative_to(ROOT) or report in inputs:
        p.error('report must be outside the checkout and inputs')
    source = shader_objects(UnityPy.load(str(inputs[0])).objects)
    if not source:
        raise ValueError('bundle has no readable shaders')
    with ZipFile(inputs[1]) as archive:
        groups = content.asset_groups(archive)
        parts = groups.get('sharedassets1.assets')
        if not parts:
            raise ValueError('OBB is missing sharedassets1.assets')
        target = shader_objects(UnityPy.load(b''.join(archive.read(part) for part in parts)).objects)
    rows = compare(source, target)
    data = {'source_shader_names': len(source), 'rows': rows,
            'limitations': 'Compares serialized objects by name, size and SHA-256; not provenance, rights, usage or rendered parity.'}
    with report.open('x', encoding='utf-8') as f:
        json.dump(data, f, indent=2)
        f.write('\n')
    exact = sum(row['matching_objects'] for row in rows)
    print(f'Compared {len(rows)} source shader names; {exact} target objects matched. Report remains local.')


if __name__ == '__main__':
    main()
