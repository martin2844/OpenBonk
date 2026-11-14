#!/usr/bin/env python3
"""Study exact partial RGBA texture reconstruction from local Steam inputs.

The template still contains game data and must remain private. This desktop
experiment is not integrated with the Android launcher. Requires UnityPy.
"""
import argparse
import base64
from collections import defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parent.parent

def load(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'tools' / file)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module

study = load('partial_texture_study', 'experimental-converted-textures.py')
objects = load('partial_texture_objects', 'experimental-exact-object-plan.py')
masker = load('partial_texture_masker', 'experimental-animation-mask.py')


def sha(data): return hashlib.sha256(data).hexdigest()


def converted(source, width, height, mips):
    from UnityPy.export import Texture2DConverter as cv
    chunks = []
    for w, h, data in study.mip_slices(bytes(source.get_image_data()), int(source.m_TextureFormat), width, height, mips):
        image = cv.parse_image_data(data, w, h, source.m_TextureFormat, source.object_reader.version, source.object_reader.platform)
        raw, fmt = cv.image_to_texture2d(image, 4, 13, None)
        if int(fmt) != 4 or len(raw) != w * h * 4: raise ValueError('invalid RGBA conversion')
        chunks.append(raw)
    return b''.join(chunks)


def fragment_rows(parts, offset, original, decoded, texture_id):
    if len(original) != len(decoded): raise ValueError('texture sizes differ')
    rows = []; base = 0
    for part in parts:
        lo, hi = max(offset, base), min(offset + len(original), base + part.file_size)
        if lo < hi:
            start, size = lo - offset, hi - lo
            before = original[start:start + size]; source = decoded[start:start + size]
            masked, bits, count = masker.mask_equal(before, source)
            if count:
                rows.append({'entry': part.filename, 'target_offset': lo - base, 'size': size,
                             'texture_id': texture_id, 'converted_offset': start, 'mask': base64.b64encode(bits).decode('ascii'),
                             'masked_bytes': count, 'original_sha256': sha(before), 'masked_sha256': sha(masked)})
        base += part.file_size
    if offset + len(original) > base: raise ValueError('texture exceeds asset group')
    return rows


def apply(raw, rows, decoded, restore=False):
    result = bytearray(raw); end = 0
    for row in sorted(rows, key=lambda x: x['target_offset']):
        start, size, pos, key = (row[k] for k in ('target_offset', 'size', 'converted_offset', 'texture_id'))
        if any(type(v) is not int for v in (start, size, pos, key)) or start < end or size <= 0 or start + size > len(result) or pos < 0 or key not in decoded or pos + size > len(decoded[key]):
            raise ValueError('invalid or overlapping fragment')
        bits = base64.b64decode(row['mask'], validate=True)
        if len(bits) != (size + 7) // 8 or (size % 8 and bits[-1] >> (size % 8)) or sum(x.bit_count() for x in bits) != row['masked_bytes']:
            raise ValueError('invalid mask')
        current = bytes(result[start:start + size]); source = decoded[key][pos:pos + size]
        if restore:
            if sha(current) != row['masked_sha256']: raise ValueError('masked fragment differs')
            output = masker.restore_masked(current, source, bits)
            if sha(output) != row['original_sha256']: raise ValueError('restored fragment differs')
        else:
            if sha(current) != row['original_sha256']: raise ValueError('original fragment differs')
            output, expected, count = masker.mask_equal(current, source)
            if expected != bits or count != row['masked_bytes'] or sha(output) != row['masked_sha256']: raise ValueError('mask differs')
        result[start:start + size] = output; end = start + size
    return bytes(result)


def sources(folder):
    import UnityPy
    index = defaultdict(list); by_id = {}
    for group in study.GROUPS:
        file = folder / (group + '.assets')
        if file.is_symlink() or not file.is_file(): raise ValueError('missing regular Steam asset file')
        for obj in UnityPy.load(str(file)).objects:
            if obj.type.name != 'Texture2D': continue
            texture = obj.read(); item = (group, obj.path_id, texture)
            index[(texture.m_Name, texture.m_Width, texture.m_Height)].append(item)
            by_id[(group, obj.path_id)] = texture
    return index, by_id


def generate(args):
    import UnityPy
    source = args.steam_data.resolve(strict=True); obb = args.obb.resolve(strict=True)
    out = objects.new_output(args.output, (source, obb)); index, _ = sources(source)
    rows = []; textures = []; decoded = {}; out.mkdir(mode=0o700)
    with zipfile.ZipFile(obb) as zin:
        infos = masker.zip_entries(zin); groups = objects.asset_groups(zin)
        for group in ('sharedassets1.assets', 'sharedassets2.assets'):
            parts = groups[group]; raw = b''.join(zin.read(p) for p in parts)
            for obj in UnityPy.load(raw).objects:
                if obj.type.name != 'Texture2D': continue
                target = obj.read()
                if int(target.m_TextureFormat) != 4: continue
                item = study.select_source(target, index[(target.m_Name, target.m_Width, target.m_Height)], texture=lambda x: x[2])
                if item is None: continue
                group_name, source_id, src = item
                if int(src.m_TextureFormat) not in (1, 3, 4, 10, 12, 25): continue
                body = obj.get_raw_data(); payload = bytes(target.image_data); pos = body.find(payload)
                if not payload or pos < 0 or body.count(payload) != 1: continue
                try: output = converted(src, target.m_Width, target.m_Height, target.m_MipCount)
                except (ValueError, TypeError, IndexError, NotImplementedError, RuntimeError): continue
                ident = len(textures); fragments = fragment_rows(parts, obj.byte_start + pos, payload, output, ident)
                if not fragments: continue
                source_data = bytes(src.get_image_data())
                textures.append({'id': ident, 'source_group': group_name, 'source_id': source_id,
                                 'width': target.m_Width, 'height': target.m_Height, 'mips': target.m_MipCount,
                                 'source_format': int(src.m_TextureFormat), 'source_sha256': sha(source_data),
                                 'converted_sha256': sha(output)})
                decoded[ident] = output; rows.extend(fragments)
        if not rows: raise ValueError('no matching RGBA bytes')
        grouped = defaultdict(list)
        for row in rows: grouped[row['entry']].append(row)
        with zipfile.ZipFile(out / 'template.obb', 'x') as zout:
            for info in infos:
                raw = zin.read(info); zout.writestr(info, apply(raw, grouped[info.filename], decoded) if info.filename in grouped else raw)
    plan = {'schema': 1, 'kind': 'experimental-partial-rgba', 'template_sha256': masker.file_digest(out / 'template.obb'),
            'textures': textures, 'records': rows}
    (out / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    print('Masked', sum(r['masked_bytes'] for r in rows), 'nonzero bytes from', len(textures), 'textures. Template remains private.')


def restore(args):
    source = args.steam_data.resolve(strict=True); template = args.template.resolve(strict=True); plan_file = args.plan.resolve(strict=True)
    out = objects.new_output(args.output, (source, template, plan_file)); plan = json.loads(plan_file.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-partial-rgba' or plan.get('template_sha256') != masker.file_digest(template): raise ValueError('plan or template differs')
    _, by_id = sources(source); decoded = {}
    for row in plan['textures']:
        key = row['id']
        if type(key) is not int or key in decoded or row['source_group'] not in study.GROUPS: raise ValueError('invalid texture identity')
        tex = by_id[(row['source_group'], row['source_id'])]
        if (tex.m_Width, tex.m_Height, tex.m_MipCount, int(tex.m_TextureFormat), sha(bytes(tex.get_image_data()))) != (row['width'], row['height'], row['mips'], row['source_format'], row['source_sha256']): raise ValueError('Steam texture differs')
        output = converted(tex, row['width'], row['height'], row['mips'])
        if sha(output) != row['converted_sha256']: raise ValueError('converted texture differs')
        decoded[key] = output
    grouped = defaultdict(list)
    for row in plan['records']:
        if not objects.SPLIT.fullmatch(row['entry'].removeprefix(objects.PREFIX)) or not row['entry'].startswith(objects.PREFIX): raise ValueError('invalid template entry')
        grouped[row['entry']].append(row)
    if not grouped: raise ValueError('empty plan')
    seen = set()
    try:
        with zipfile.ZipFile(template) as zin, zipfile.ZipFile(out, 'x') as zout:
            for info in masker.zip_entries(zin):
                raw = zin.read(info)
                if info.filename in grouped: raw = apply(raw, grouped[info.filename], decoded, True); seen.add(info.filename)
                zout.writestr(info, raw)
        if seen != set(grouped): raise ValueError('missing template entry')
    except BaseException:
        if out.is_file(): out.unlink()
        raise
    print('Restored partial RGBA fragments; compare archive entries separately. Output remains private.')


def export_android(args):
    """Bind desktop selections to exact streamed Steam offsets for Android."""
    source = args.steam_data.resolve(strict=True); template = args.template.resolve(strict=True); plan_file = args.plan.resolve(strict=True)
    out = objects.new_output(args.output, (source, template, plan_file)); plan = json.loads(plan_file.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-partial-rgba' or plan.get('template_sha256') != masker.file_digest(template): raise ValueError('plan or template differs')
    _, by_id = sources(source); records = []; used = {}; grouped = defaultdict(list)
    for fragment in plan['records']: grouped[fragment['texture_id']].append(fragment)
    for i, item in enumerate(plan['textures']):
        if item['id'] != i or item['source_group'] not in study.GROUPS or i not in grouped: raise ValueError('invalid texture identity')
        tex = by_id[(item['source_group'], item['source_id'])]; data = bytes(tex.get_image_data()); stream = tex.m_StreamData
        if (tex.m_Width, tex.m_Height, tex.m_MipCount, int(tex.m_TextureFormat), sha(data)) != (item['width'], item['height'], item['mips'], item['source_format'], item['source_sha256']): raise ValueError('Steam texture differs')
        name, offset, size = stream.path, stream.offset, stream.size
        if not objects.SOURCE.fullmatch(name) or not name.endswith('.assets.resS') or offset < 0 or size != len(data): raise ValueError('unsupported Steam stream')
        path = source / name
        if path.is_symlink() or not path.is_file(): raise ValueError('invalid Steam stream')
        with path.open('rb') as f:
            f.seek(offset)
            if f.read(size) != data: raise ValueError('Steam stream differs')
        output = converted(tex, item['width'], item['height'], item['mips'])
        if sha(output) != item['converted_sha256']: raise ValueError('converted texture differs')
        used[name] = masker.file_digest(path)
        fragments = [{k: value for k, value in row.items() if k != 'texture_id'} for row in grouped[i]]
        records.append({'id': i, 'source': name, 'source_offset': offset, 'source_size': size, 'source_sha256': sha(data),
                        'width': item['width'], 'height': item['height'], 'mips': item['mips'], 'source_format': item['source_format'],
                        'target_format': 4, 'target_size': len(output), 'target_sha256': sha(output), 'fragments': fragments})
    if not records or len(grouped) != len(records): raise ValueError('invalid fragment mapping')
    result = {'schema': 1, 'kind': 'experimental-partial-rgba-texture-offsets', 'template_sha256': plan['template_sha256'], 'sources': used, 'records': records}
    with out.open('x', encoding='utf-8') as f: json.dump(result, f, separators=(',', ':')); f.write('\n')
    print('Exported local Android offset plan; Android restoration remains unverified.')


def main():
    p = argparse.ArgumentParser(description=__doc__); sub = p.add_subparsers(dest='action', required=True)
    g = sub.add_parser('generate'); g.add_argument('--steam-data', type=Path, required=True); g.add_argument('--obb', type=Path, required=True); g.add_argument('--output', type=Path, required=True)
    r = sub.add_parser('restore'); r.add_argument('--steam-data', type=Path, required=True); r.add_argument('--template', type=Path, required=True); r.add_argument('--plan', type=Path, required=True); r.add_argument('--output', type=Path, required=True)
    e = sub.add_parser('export-android'); e.add_argument('--steam-data', type=Path, required=True); e.add_argument('--template', type=Path, required=True); e.add_argument('--plan', type=Path, required=True); e.add_argument('--output', type=Path, required=True)
    a = p.parse_args(); {'generate': generate, 'restore': restore, 'export-android': export_android}[a.action](a)

if __name__ == '__main__': main()
