#!/usr/bin/env python3
"""Transplant reconstructed objects only when original target bytes match.

Local research: output retains game content and is not for distribution.
Object identity alone does not prove that references or gameplay are valid.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import zipfile
import UnityPy

spec = importlib.util.spec_from_file_location('archive_compare', Path(__file__).with_name('compare-android-asset-objects.py'))
archive_compare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(archive_compare)


def digest(data): return hashlib.sha256(data).hexdigest()


def read_asset(archive, asset):
    with zipfile.ZipFile(archive) as z:
        if len(z.namelist()) != len(set(z.namelist())): raise ValueError('duplicate archive entry')
        return archive_compare.load_archive(z, asset)


def objects(env):
    result = {}
    for o in env.objects:
        if o.path_id in result: raise ValueError('duplicate path ID')
        result[o.path_id] = o
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('old_archive', type=Path); p.add_argument('converted_archive', type=Path)
    p.add_argument('new_archive', type=Path); p.add_argument('asset')
    p.add_argument('output', type=Path, help='new asset path'); p.add_argument('--report', type=Path, required=True)
    p.add_argument('--texture-payload-manifest', type=Path, help='optional pinned exceptions for specific matching texture layouts')
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or a.report.exists() or a.report.is_symlink() or not a.output.parent.is_dir() or not a.report.parent.is_dir(): p.error('output and report must be new')
    old_data, converted_data, new_data = (read_asset(path, a.asset) for path in (a.old_archive, a.converted_archive, a.new_archive))
    old_env, converted_env, new_env = (UnityPy.load(data) for data in (old_data, converted_data, new_data))
    old, converted, new = (objects(env) for env in (old_env, converted_env, new_env))
    if old.keys() != converted.keys() or old.keys() != new.keys(): raise ValueError('asset object IDs differ')
    exceptions = {}
    if a.texture_payload_manifest:
        manifest = json.loads(a.texture_payload_manifest.read_text())
        if manifest.get('version') != 1 or manifest.get('asset') != a.asset: raise ValueError('invalid texture payload manifest')
        entries = manifest.get('textures')
        if not isinstance(entries, list) or not entries: raise ValueError('empty texture payload manifest')
        for item in entries:
            if not isinstance(item.get('path_id'), int) or item['path_id'] in exceptions: raise ValueError('invalid texture payload ID')
            if any(not isinstance(item.get(k), str) or len(item[k]) != 64 or any(c not in '0123456789abcdef' for c in item[k]) for k in ('old_sha256', 'new_sha256', 'converted_sha256')): raise ValueError('invalid texture payload hash')
            exceptions[item['path_id']] = item
    used = set(); changes = []; expected = {}
    for path_id in sorted(old):
        o, c, n = old[path_id], converted[path_id], new[path_id]
        if o.type.name != c.type.name or o.type.name != n.type.name: raise ValueError(f'object type mismatch at {path_id}')
        ob, cb, nb = o.get_raw_data(), c.get_raw_data(), n.get_raw_data()
        if ob != cb:
            method = 'identical original object'
            if ob != nb:
                rec = exceptions.get(path_id)
                if o.type.name != 'Texture2D' or not rec or (digest(ob), digest(nb), digest(cb)) != (rec['old_sha256'], rec['new_sha256'], rec['converted_sha256']):
                    raise ValueError(f'converted object {path_id} ({o.type.name}) changed in new build; refusing transplant')
                ot, ct, nt = o.read(), c.read(), n.read()
                fields = ('m_Name', 'm_Width', 'm_Height', 'm_MipCount', 'm_ImageCount', 'm_ColorSpace')
                if any(getattr(ot, k) != getattr(ct, k) or getattr(ct, k) != getattr(nt, k) for k in fields): raise ValueError('texture identity or dimensions changed')
                if ct.m_TextureFormat != nt.m_TextureFormat: raise ValueError('converted and new texture formats differ')
                cp, np = bytes(ct.image_data), bytes(nt.image_data)
                if not cp or len(cp) != len(np) or cb.count(cp) != 1 or nb.count(np) != 1: raise ValueError('ambiguous texture payload')
                co, no = cb.index(cp), nb.index(np)
                if cb[:co] != nb[:no] or cb[co + len(cp):] != nb[no + len(np):]: raise ValueError('texture non-payload metadata differs')
                cb = nb[:no] + cp + nb[no + len(np):]
                method = 'pinned matching texture payload'; used.add(path_id)
            changes.append({'path_id': path_id, 'type': o.type.name, 'original_sha256': digest(ob), 'converted_sha256': digest(cb), 'method': method})
            n.set_raw_data(cb); expected[path_id] = digest(cb)
        else: expected[path_id] = digest(nb)
    if used != set(exceptions): raise ValueError('unused texture payload exception')
    if not changes: raise ValueError('no converted objects found')
    result = new_env.file.save()
    verified = objects(UnityPy.load(result))
    if verified.keys() != new.keys() or any(verified[k].type.name != new[k].type.name or digest(verified[k].get_raw_data()) != expected[k] for k in expected):
        raise ValueError('post-save object verification failed')
    report = {'kind': 'local research only, not for distribution', 'asset': a.asset, 'old_sha256': digest(old_data), 'converted_sha256': digest(converted_data), 'new_sha256': digest(new_data), 'output_sha256': digest(result), 'object_count': len(new), 'changed_objects': changes, 'limitations': ['Input retains game content.', 'Matching object bytes do not validate cross-file references or gameplay.', 'Only this asset was reconstructed.']}
    # Exclusive creation prevents clobbering existing work; clean our asset if a write fails.
    asset_created = False; report_created = False
    try:
        with a.output.open('xb') as f:
            asset_created = True
            f.write(result)
        with a.report.open('x') as f:
            report_created = True
            json.dump(report, f, indent=2); f.write('\n')
    except BaseException:
        if asset_created: a.output.unlink(missing_ok=True)
        if report_created: a.report.unlink(missing_ok=True)
        raise
    print('transplanted', len(changes), 'objects;', len(new)-len(changes), 'new-build objects preserved; SHA-256', report['output_sha256'])


if __name__ == '__main__': main()
