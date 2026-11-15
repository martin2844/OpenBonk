#!/usr/bin/env python3
"""Measure whether independently compiled shaders can restore template bytes.

This desktop-only study holds private game-derived bytes in memory and writes
only a local metadata report. It does not make an APK redistributable, change
the launcher, or establish the provenance of the supplied shader bundle.
"""
import argparse
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import zipfile

import UnityPy


ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('build_content', ROOT / 'tools/compare-unity-build-content.py')
content = importlib.util.module_from_spec(spec)
spec.loader.exec_module(content)
MAX_TEMPLATE = 512 * 1024 * 1024


def digest(data):
    return hashlib.sha256(data).hexdigest()


def file_digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def shaders(bundle):
    result = {}
    for obj in UnityPy.load(str(bundle)).objects:
        if obj.type.name != 'Shader':
            continue
        name = obj.read_typetree()['m_ParsedForm']['m_Name']
        data = obj.get_raw_data()
        if not isinstance(name, str) or not name or not data or len(data) != obj.byte_size:
            raise ValueError('invalid shader object')
        key = (name, digest(data))
        if key in result:
            raise ValueError('duplicate shader object')
        result[key] = data
    if not result:
        raise ValueError('bundle contains no shaders')
    return result


def study(target, template, sources):
    if len(target) != len(template):
        raise ValueError('serialized group size differs')
    masked = bytearray(template)
    rows = []
    claimed = set()
    for obj in UnityPy.load(target).objects:
        if obj.type.name != 'Shader':
            continue
        name = obj.read_typetree()['m_ParsedForm']['m_Name']
        data = obj.get_raw_data()
        key = (name, digest(data))
        if key not in sources:
            continue
        start, size = obj.byte_start, obj.byte_size
        if start < 0 or size != len(data) or start + size > len(target) or target[start:start + size] != data:
            raise ValueError('invalid serialized shader range')
        if any(start < end and begin < start + size for begin, end in claimed):
            raise ValueError('overlapping shader objects')
        claimed.add((start, start + size))
        positions = []
        for offset, value in enumerate(masked[start:start + size]):
            if value:
                if value != data[offset]:
                    raise ValueError('template differs from reconstructed shader')
                positions.append(offset)
                masked[start + offset] = 0
        rows.append((start, key, positions))
    restored = bytearray(masked)
    for start, key, positions in rows:
        for offset in positions:
            if restored[start + offset] != 0:
                raise ValueError('overlapping restoration mask')
            restored[start + offset] = sources[key][offset]
    if restored != template:
        raise ValueError('restored serialized group differs')
    return {
        'matched_objects': len(rows),
        'masked_nonzero_bytes': sum(len(positions) for _, _, positions in rows),
        'restored_group_sha256': digest(restored),
        'shaders': [{'name': key[0], 'sha256': key[1], 'masked_nonzero_bytes': len(positions)}
                    for _, key, positions in rows],
    }


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('apk', 'obb', 'bundle', 'output'):
        p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--platform', choices=('linux', 'windows'), default='linux')
    a = p.parse_args()
    for name in ('apk', 'obb', 'bundle'):
        path = getattr(a, name).expanduser()
        if path.is_symlink() or not path.is_file():
            p.error(name + ' must be a regular file')
        setattr(a, name, path.resolve())
    output = a.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        p.error('output must be a new file in an existing directory')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in (a.apk, a.obb, a.bundle):
        p.error('output must be outside the checkout and separate from inputs')
    with zipfile.ZipFile(a.obb) as archive:
        groups = content.asset_groups(archive)
        parts = groups.get('sharedassets1.assets')
        if not parts:
            raise ValueError('OBB is missing sharedassets1.assets')
        names = [part.filename for part in parts]
        target = b''.join(archive.read(part) for part in parts)
    asset = 'assets/openbonk-diagnostic/' + ('windows/' if a.platform == 'windows' else '') + 'generated-audio-template.openbonk'
    with zipfile.ZipFile(a.apk) as archive:
        info = archive.getinfo(asset)
        if info.file_size <= 0 or info.file_size > MAX_TEMPLATE:
            raise ValueError('invalid template size')
        with zipfile.ZipFile(io.BytesIO(archive.read(info))) as template_archive:
            groups = content.asset_groups(template_archive)
            template_parts = groups.get('sharedassets1.assets')
            if not template_parts or [part.filename for part in template_parts] != names:
                raise ValueError('template group layout differs')
            template = b''.join(template_archive.read(part) for part in template_parts)
    report = study(target, template, shaders(a.bundle))
    report.update({'platform': a.platform, 'apk_sha256': file_digest(a.apk),
                   'obb_sha256': file_digest(a.obb), 'bundle_sha256': file_digest(a.bundle),
                   'limitations': 'Desktop in-memory group check only; no APK, Android restoration, provenance or rights are established.'})
    with output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print(f"{a.platform}: {report['matched_objects']} shaders; {report['masked_nonzero_bytes']} nonzero bytes; group restored exactly")


if __name__ == '__main__':
    main()
