#!/usr/bin/env python3
"""Generate and test local plans for Steam-to-Android texture conversion.

Heuristic mappings can have visible errors. Templates and restored OBBs still
contain other game data and must remain local. Android integration is experimental.
Requires UnityPy and its texture conversion dependencies.
"""
import argparse
from collections import defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('texture_study', ROOT / 'tools/experimental-converted-textures.py')
study = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study)
SOURCE = re.compile(r'(?:sharedassets[12]|resources)\.assets(?:\.resS)?\Z')
ENTRY = re.compile(re.escape(study.PREFIX) + r'(?:sharedassets[12]\.assets\.split(?:0|[1-9]\d*)|[0-9a-f]{32})\Z')


def sha(data): return hashlib.sha256(data).hexdigest()


def valid_resource_record(row):
    return (row.get('name') in study.RESOURCE_TEXTURES and row.get('source_name') == row['name'] and
            row.get('source_format') == 12 and row.get('target_format') == 47 and row.get('quality') is True and
            row.get('normal') is False and row.get('direct_normal') is False and
            not any(k in row for k in ('font', 'alias_group', 'alias_target_id')))


def valid_shadow_record(row):
    return (row.get('shadow') is True and row.get('group') == 'sharedassets1' and row.get('name') == 'DropShadow' and
            row.get('source_format') == 12 and row.get('target_format') == 47 and row.get('quality') is True and
            row.get('normal') is False and row.get('direct_normal') is False and
            not any(k in row for k in ('font', 'resource', 'source_name', 'alias_group', 'alias_target_id')))


def valid_collision_record(row):
    source = study.collision_source(row.get('name')); spec = study.COLLISION_TEXTURES.get(source)
    return (row.get('collision_alias') is True and row.get('group') == 'sharedassets1' and
            spec is not None and row.get('source_name') == source and
            row.get('source_format') == spec[3] and row.get('target_format') == spec[4] and
            row.get('normal') is False and row.get('quality') is False and row.get('direct_normal') is False and
            not any(k in row for k in ('font', 'resource', 'shadow', 'alias_group', 'alias_target_id')))


def valid_forest_record(row):
    spec = study.FOREST_TEXTURES.get(row.get('name'))
    return (row.get('forest_source') is True and row.get('group') == 'sharedassets1' and spec is not None and
            row.get('source_name') == spec[0] and isinstance(row.get('source_sha256'), str) and
            re.fullmatch(r'[0-9a-f]{64}', row['source_sha256']) is not None and
            row.get('source_format') == 10 and row.get('target_format') == 45 and
            row.get('normal') is False and row.get('quality') is False and row.get('direct_normal') is False and
            not any(k in row for k in ('font', 'resource', 'shadow', 'collision_alias', 'alias_group', 'alias_target_id')))


def file_hash(path):
    with path.open('rb') as stream: return hashlib.file_digest(stream, 'sha256').hexdigest()


def new_output(path, inputs):
    path = path.expanduser().absolute()
    if path.exists() or path.is_symlink() or not path.parent.is_dir(): raise ValueError('output must be new')
    path = path.parent.resolve() / path.name
    if any(path.is_relative_to(p) or p.is_relative_to(path) for p in (*inputs, ROOT)): raise ValueError('output must be separate from inputs and checkout')
    return path


def fragments(parts, target, size):
    if type(target) is not int or type(size) is not int or target < 0 or size <= 0: raise ValueError('invalid target range')
    result, base = [], 0
    for part in parts:
        lo, hi = max(target, base), min(target + size, base + part.file_size)
        if lo < hi: result.append({'entry': part.filename, 'target_offset': lo - base, 'size': hi - lo, 'converted_offset': lo - target})
        base += part.file_size
    if sum(row['size'] for row in result) != size: raise ValueError('target exceeds split files')
    return result


def apply(raw, rows, converted=None):
    result = bytearray(raw); previous = 0
    for fragment, row in sorted(rows, key=lambda x: x[0]['target_offset']):
        start, size, pos = fragment['target_offset'], fragment['size'], fragment['converted_offset']
        if any(type(x) is not int for x in (start, size, pos)) or start < previous or size <= 0 or start + size > len(result) or pos < 0 or pos + size > row['target_size']:
            raise ValueError('invalid or overlapping texture fragment')
        if converted is None:
            result[start:start + size] = bytes(size)
        else:
            if any(result[start:start + size]): raise ValueError('texture template not blank')
            payload = converted[row['id']]
            result[start:start + size] = payload[pos:pos + size]
        previous = start + size
    return bytes(result)


def convert_texture(payload, row, quality=None):
    from UnityPy.export import Texture2DConverter as converter
    if row.get('quality', False) and quality is None: raise ValueError('quality encoder is required')
    # The tested Texture2D formats do not depend on platform blobs or platform.
    # Decoder and encoder versions must reproduce the planned output hash.
    source_mips = study.mip_slices(payload, row['source_format'], row['width'], row['height'], row['mips'])
    chunks = []
    for w, h, data in source_mips:
        if row.get('quality', False):
            encoded, fmt = quality.encode(data, w, h, row['source_format']), 47
        else:
            image = converter.parse_image_data(data, w, h, row['source_format'], (2023, 2, 22, 0), 19)
            if row.get('normal', False): image = study.normal_image(image)
            encoded, fmt = converter.image_to_texture2d(image, row['target_format'], 13, None)
        if int(fmt) != row['target_format'] or len(encoded) != study.mip_size(row['target_format'], w, h): raise ValueError('converted mip differs')
        chunks.append(encoded)
    result = b''.join(chunks)
    if len(result) != row['target_size'] or sha(result) != row['target_sha256']: raise ValueError('converted texture differs from plan')
    return result


def generate(args):
    import UnityPy
    source, obb, manifest = (p.resolve(strict=True) for p in (args.steam_data, args.obb, args.manifest))
    output = new_output(args.output, (source, obb, manifest))
    quality = study.load_quality_encoder(args.quality_library) if getattr(args, 'quality_library', None) else None
    report = json.loads(manifest.read_text()); candidates = report.get('textures')
    if report.get('schema') != 1 or not isinstance(candidates, list) or not candidates: raise ValueError('invalid study report')
    chosen = {g: {} for g in study.GROUPS}
    for row in candidates:
        group, key = row['group'], row['target_id']
        if 'shadow' in row and not valid_shadow_record(row):
            raise ValueError('invalid shadow texture mapping')
        if 'collision_alias' in row and not valid_collision_record(row): raise ValueError('invalid collision texture mapping')
        if 'forest_source' in row and not valid_forest_record(row): raise ValueError('invalid forest texture mapping')
        if group in study.GROUPS and 'source_name' in row and row.get('collision_alias') is not True and row.get('forest_source') is not True:
            raise ValueError('unexpected source name in shared texture')
        if group not in study.GROUPS and not re.fullmatch(r'[0-9a-f]{32}', group): raise ValueError('invalid texture group')
        if group in study.GROUPS and ('font' in row or 'resource' in row): raise ValueError('resource markers require a standalone texture')
        if type(key) is not int or key in chosen.setdefault(group, {}): raise ValueError('invalid texture record')
        chosen[group][key] = row
    for group, records in chosen.items():
        if group in study.GROUPS: continue
        for row in records.values():
            if row.get('resource') is True:
                if not valid_resource_record(row): raise ValueError('invalid resource texture mapping')
                continue
            if 'resource' in row: raise ValueError('invalid resource marker')
            if row.get('font') is True:
                if (row.get('name') not in study.FONT_ALIASES or row.get('source_name') != study.FONT_ALIASES[row['name']] or
                        row.get('source_format') != 1 or row.get('target_format') != 47 or row.get('quality') is not True or
                        row.get('normal') is not False or row.get('direct_normal') is not False or
                        any(k in row for k in ('alias_group', 'alias_target_id'))):
                    raise ValueError('invalid font texture mapping')
                continue
            if 'font' in row: raise ValueError('invalid font marker')
            if not any(k in row for k in ('alias_group', 'alias_target_id', 'source_name')): continue
            parent = chosen.get(row.get('alias_group'), {}).get(row.get('alias_target_id'))
            if row.get('alias_group') not in study.GROUPS or parent is None or row.get('source_name') != parent['name'] or any(row.get(k) != parent.get(k) for k in ('source_format', 'target_format', 'normal', 'bytes')) or any(row.get(k, False) != parent.get(k, False) for k in ('quality', 'direct_normal')):
                raise ValueError('invalid standalone texture alias')
    index = defaultdict(list); resources_index = defaultdict(list)
    for group in (*study.GROUPS, *(['resources'] if any(r.get('font') is True or r.get('resource') is True for r in candidates) else [])):
        path = source / (group + '.assets')
        if not path.is_file() or path.is_symlink(): raise ValueError('missing Steam asset file')
        for obj in UnityPy.load(str(path)).objects:
            if obj.type.name == 'Texture2D':
                tex = obj.read()
                if group == 'resources' and tex.m_Name not in set(study.FONT_ALIASES.values()) | study.RESOURCE_TEXTURES: continue
                (resources_index if group == 'resources' else index)[(tex.m_Name, tex.m_Width, tex.m_Height)].append((group, obj, tex))
    rows = []; output.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(obb) as zin:
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate ZIP entry')
            for group in chosen:
                parts = study.parts(zin, group); data = b''.join(zin.read(info) for info in parts)
                objects = {o.path_id: o for o in UnityPy.load(data).objects if o.type.name == 'Texture2D'}
                for key, prior in chosen[group].items():
                    if key not in objects: raise ValueError('missing target texture')
                    obj = objects[key]; tex = obj.read()
                    found = (resources_index if prior.get('font') is True or prior.get('resource') is True else index)[(prior.get('source_name', tex.m_Name), tex.m_Width, tex.m_Height)]
                    chosen_source = (next((item for item in found if item[0] == 'sharedassets1' and sha(bytes(item[2].get_image_data())) == prior['source_sha256']), None)
                                     if prior.get('forest_source') is True else
                                     study.select_shadow_source(tex, found, texture=lambda item: item[2]) if prior.get('shadow') is True
                                     else study.select_source(tex, found, texture=lambda item: item[2]))
                    if chosen_source is None or tex.m_Name != prior['name']: raise ValueError('source mapping differs')
                    src_group, src_obj, src = chosen_source
                    if prior.get('forest_source') is True:
                        spec = study.FOREST_TEXTURES[prior['name']]
                        if (src_group != 'sharedassets1' or (tex.m_Width, tex.m_Height, tex.m_MipCount, tex.m_ImageCount) != (*spec[1:], 1)
                                or src.m_Name != spec[0]):
                            raise ValueError('forest texture metadata differs')
                    if prior.get('shadow') is True and (src_group != 'sharedassets1' or (tex.m_Width, tex.m_Height, tex.m_MipCount) != (512, 512, 1)):
                        raise ValueError('shadow texture metadata differs')
                    if prior.get('collision_alias') is True and (src_group != 'sharedassets1' or (tex.m_Width, tex.m_Height, tex.m_MipCount) != study.COLLISION_TEXTURES[prior['source_name']][:3]):
                        raise ValueError('collision texture metadata differs')
                    if prior.get('font') is True and (src_group != 'resources' or (tex.m_Width, tex.m_Height, tex.m_MipCount) != (1024, 1024, 1)):
                        raise ValueError('font texture metadata differs')
                    if prior.get('resource') is True and (src_group != 'resources' or (tex.m_Width, tex.m_Height, tex.m_MipCount) != (512, 512, 10)):
                        raise ValueError('resource texture metadata differs')
                    if (any(getattr(tex, k, None) != getattr(src, k, None) for k in ('m_MipCount', 'm_ImageCount', 'm_ColorSpace')) or
                            int(tex.m_TextureFormat) != prior['target_format'] or int(src.m_TextureFormat) != prior['source_format']):
                        raise ValueError('texture metadata differs')
                    payload, target = bytes(src.get_image_data()), bytes(tex.image_data)
                    if len(target) != prior['bytes']: raise ValueError('target length differs')
                    stream = src.m_StreamData
                    if stream.path:
                        name, offset = stream.path, stream.offset
                        if not SOURCE.fullmatch(name) or stream.size != len(payload): raise ValueError('unexpected Steam stream')
                    else:
                        body = src_obj.get_raw_data(); pos = body.find(payload)
                        if not payload or pos < 0 or body.count(payload) != 1: raise ValueError('unsupported source layout')
                        name, offset = src_group + '.assets', src_obj.byte_start + pos
                    path = source / name
                    if not path.is_file() or path.is_symlink() or offset < 0: raise ValueError('invalid Steam source')
                    with path.open('rb') as f:
                        f.seek(offset)
                        if f.read(len(payload)) != payload: raise ValueError('Steam source offset differs')
                    target_body = obj.get_raw_data(); target_pos = target_body.find(target)
                    if not target or target_pos < 0 or target_body.count(target) != 1: raise ValueError('unsupported target layout')
                    row = {'id': len(rows), 'source': name, 'source_offset': offset, 'source_size': len(payload),
                           'source_sha256': sha(payload), 'width': tex.m_Width, 'height': tex.m_Height,
                           'mips': tex.m_MipCount, 'source_format': int(src.m_TextureFormat), 'target_format': int(tex.m_TextureFormat), 'normal': prior.get('normal', False), 'quality': prior.get('quality', False), 'direct_normal': prior.get('direct_normal', False),
                           'target_size': len(target), 'target_sha256': sha(target),
                           'fragments': fragments(parts, obj.byte_start + target_pos, len(target))}
                    # Verify the record is actually reproducible before blanking it.
                    if type(row['quality']) is not bool or (row['quality'] and (row['normal'] or row['target_format'] != 47)):
                        raise ValueError('invalid quality texture metadata')
                    if type(row['direct_normal']) is not bool or (row['direct_normal'] and (row['normal'] or row['quality'] or row['target_format'] != 45 or row['source_format'] not in (3, 4, 10, 25))):
                        raise ValueError('invalid direct normal metadata')
                    if convert_texture(payload, row, quality) != target: raise ValueError('converted target differs')
                    rows.append(row)
            if not rows: raise ValueError('no textures selected')
            with zipfile.ZipFile(output / 'template.obb', 'x') as zout:
                for info in infos:
                    raw = zin.read(info)
                    selected = [(fragment, row) for row in rows for fragment in row['fragments'] if fragment['entry'] == info.filename]
                    zout.writestr(info, apply(raw, selected) if selected else raw)
        used = sorted({row['source'] for row in rows})
        plan = {'schema': 1, 'kind': 'experimental-converted-steam-texture-offsets',
                'sources': {name: file_hash(source / name) for name in used},
                'template_sha256': file_hash(output / 'template.obb'), 'records': rows}
        (output / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(output)
        raise
    print(f'Masked {sum(row["target_size"] for row in rows)} bytes from {len(rows)} converted textures. Template remains private.')


def restore(args, convert=convert_texture):
    from contextlib import ExitStack
    source, template, plan_file = (p.resolve(strict=True) for p in (args.steam_data, args.template, args.plan))
    output = new_output(args.output, (source, template, plan_file))
    quality = study.load_quality_encoder(args.quality_library) if getattr(args, 'quality_library', None) else None
    plan = json.loads(plan_file.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-converted-steam-texture-offsets' or plan.get('template_sha256') != file_hash(template): raise ValueError('plan or template differs')
    rows, hashes = plan.get('records'), plan.get('sources')
    if not isinstance(rows, list) or not rows or len(rows) > 5000 or not isinstance(hashes, dict) or not hashes: raise ValueError('invalid plan')
    if any(not SOURCE.fullmatch(name) or not (source / name).is_file() or (source / name).is_symlink() or file_hash(source / name) != value for name, value in hashes.items()): raise ValueError('Steam source differs')
    by_entry = defaultdict(list); converted = {}; total = 0
    with ExitStack() as stack:
        streams = {name: stack.enter_context((source / name).open('rb')) for name in hashes}
        for i, row in enumerate(rows):
            if row.get('id') != i or row.get('source') not in streams: raise ValueError('invalid texture id or source')
            offset, size = row['source_offset'], row['source_size']
            if type(offset) is not int or type(size) is not int or offset < 0 or size <= 0 or size > 64 * 1024 * 1024: raise ValueError('invalid source range')
            if type(row['target_size']) is not int or not 0 < row['target_size'] <= 64 * 1024 * 1024: raise ValueError('invalid target size')
            if (any(type(row.get(k)) is not int for k in ('width', 'height', 'mips', 'source_format', 'target_format')) or
                    not 0 < row['width'] <= 4096 or not 0 < row['height'] <= 4096 or not 0 < row['mips'] <= 32 or
                    row['source_format'] not in (1, 3, 4, 10, 12, 25) or type(row.get('normal', False)) is not bool or
                    type(row.get('quality', False)) is not bool or
                    type(row.get('direct_normal', False)) is not bool or
                    (row.get('direct_normal', False) and (row.get('normal', False) or row.get('quality', False) or row['target_format'] != 45 or row['source_format'] not in (3, 4, 10, 25))) or
                    (row.get('quality', False) and (row.get('normal', False) or row['target_format'] != 47 or row['width'] > 2048 or row['height'] > 2048)) or
                    (row.get('normal', False) and (row['source_format'], row['target_format']) != (12, 34)) or
                    (not row.get('normal', False) and row['target_format'] not in (4, 45, 47))):
                raise ValueError('invalid texture conversion metadata')
            total += row['target_size']
            if total > 256 * 1024 * 1024: raise ValueError('texture limit exceeded')
            fragments_list = row.get('fragments')
            if not isinstance(fragments_list, list) or not fragments_list: raise ValueError('missing fragments')
            position = 0
            for frag in sorted(fragments_list, key=lambda f: f['converted_offset']):
                if not ENTRY.fullmatch(frag.get('entry', '')) or frag['converted_offset'] != position or type(frag['size']) is not int or frag['size'] <= 0: raise ValueError('invalid fragment sequence')
                position += frag['size']; by_entry[frag['entry']].append((frag, row))
            if position != row['target_size']: raise ValueError('incomplete texture fragments')
            stream = streams[row['source']]; stream.seek(offset); payload = stream.read(size)
            if len(payload) != size or sha(payload) != row['source_sha256']: raise ValueError('source texture differs')
            result = convert_texture(payload, row, quality) if row.get('quality', False) else convert(payload, row)
            if len(result) != row['target_size'] or sha(result) != row['target_sha256']: raise ValueError('converted texture differs')
            converted[i] = result
    created = False
    try:
        with zipfile.ZipFile(template) as zin, output.open('xb') as target:
            created = True; infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate ZIP entry')
            seen = set()
            with zipfile.ZipFile(target, 'w') as zout:
                for info in infos:
                    raw = zin.read(info)
                    if info.filename in by_entry:
                        raw = apply(raw, by_entry[info.filename], converted); seen.add(info.filename)
                    zout.writestr(info, raw)
            if seen != set(by_entry): raise ValueError('missing texture entry')
    except BaseException:
        if created: output.unlink(missing_ok=True)
        raise
    print(f'Restored {len(rows)} converted textures; output remains private.')


def main():
    p = argparse.ArgumentParser(description=__doc__); sub = p.add_subparsers(dest='command', required=True)
    for command in ('generate', 'restore'):
        s = sub.add_parser(command); s.add_argument('--steam-data', type=Path, required=True); s.add_argument('--output', type=Path, required=True)
        s.add_argument('--quality-library', type=Path, help='optional experimental desktop quality encoder')
        if command == 'generate': s.add_argument('--obb', type=Path, required=True); s.add_argument('--manifest', type=Path, required=True)
        else: s.add_argument('--template', type=Path, required=True); s.add_argument('--plan', type=Path, required=True)
    args = p.parse_args(); (generate if args.command == 'generate' else restore)(args)


if __name__ == '__main__': main()
