#!/usr/bin/env python3
"""Mask and restore exact Steam mesh buffers in a private research OBB.

Other mesh fields and game data remain in the template. Matching bytes and
geometry do not establish provenance or a redistributable APK. Generation
requires UnityPy; restoration uses the Python standard library.
"""
import argparse
from collections import Counter, defaultdict
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
SOURCE = re.compile(r'(?:sharedassets\d+\.assets|level\d+)(?:\.resS)?\Z')
ENTRY = re.compile(re.escape(study.PREFIX) + r'sharedassets[12]\.assets\.split(?:0|[1-9]\d*)\Z')
SOURCE_ASSETS = ('sharedassets0.assets', 'sharedassets1.assets', 'sharedassets2.assets', 'sharedassets4.assets', 'level1', 'level2', 'level4')


def sha(data): return hashlib.sha256(data).hexdigest()


def file_hash(path):
    with path.open('rb') as stream: return hashlib.file_digest(stream, 'sha256').hexdigest()


def geometry(mesh):
    return (mesh.m_VertexData.m_VertexCount,
            tuple((c.stream, c.offset, c.format, c.dimension) for c in mesh.m_VertexData.m_Channels), mesh.m_IndexFormat)


def buffers(mesh):
    from UnityPy.helpers.ResourceReader import get_resource_data
    vertices = bytes(mesh.m_VertexData.m_DataSize or b'')
    stream = mesh.m_StreamData
    if not vertices and stream and stream.path:
        if not SOURCE.fullmatch(stream.path): raise ValueError('unsupported mesh stream')
        vertices = bytes(get_resource_data(stream.path, mesh.object_reader.assets_file, stream.offset, stream.size))
    return vertices, bytes(mesh.m_IndexBuffer or [])


def source_reference(folder, source_name, obj, mesh, data, is_vertex):
    if not data: return None
    stream = mesh.m_StreamData
    if is_vertex and not mesh.m_VertexData.m_DataSize and stream and stream.path:
        filename, offset = stream.path, stream.offset
        if not SOURCE.fullmatch(filename) or stream.size != len(data): return None
    else:
        raw = obj.get_raw_data(); pos = raw.find(data)
        if pos < 0 or raw.count(data) != 1: return None
        filename, offset = source_name, obj.byte_start + pos
    path = folder / filename
    if not SOURCE.fullmatch(filename) or not path.is_file() or path.is_symlink() or offset < 0: raise ValueError('invalid Steam buffer source')
    with path.open('rb') as f:
        f.seek(offset)
        if f.read(len(data)) != data: raise ValueError('Steam mesh offset differs')
    return filename, offset


def new_output(path, inputs):
    path = path.expanduser().absolute()
    if path.exists() or path.is_symlink() or not path.parent.is_dir(): raise ValueError('output must be new')
    path = path.parent.resolve() / path.name
    if any(path.is_relative_to(p) or p.is_relative_to(path) for p in (*inputs, ROOT)): raise ValueError('output must be separate from inputs and checkout')
    return path


def fragments(parts, target, payload, source, source_offset):
    if target < 0 or not payload or source_offset < 0 or not SOURCE.fullmatch(source): raise ValueError('invalid mesh range')
    result = []; base = 0
    for part in parts:
        lo, hi = max(target, base), min(target + len(payload), base + part.file_size)
        if lo < hi:
            offset = lo - target
            result.append({'entry': part.filename, 'target_offset': lo - base, 'size': hi - lo,
                           'source': source, 'source_offset': source_offset + offset, 'sha256': sha(payload[offset:offset + hi - lo])})
        base += part.file_size
    if sum(row['size'] for row in result) != len(payload): raise ValueError('mesh range exceeds splits')
    return result


def apply(raw, rows, streams=None):
    out = bytearray(raw); end = 0
    for row in sorted(rows, key=lambda r: r['target_offset']):
        start, size, offset = row['target_offset'], row['size'], row['source_offset']
        if any(type(v) is not int for v in (start, size, offset)) or start < end or size <= 0 or start + size > len(out) or offset < 0 or not SOURCE.fullmatch(row['source']):
            raise ValueError('invalid or overlapping mesh range')
        if streams is None:
            if sha(out[start:start + size]) != row['sha256']: raise ValueError('mesh target differs')
            out[start:start + size] = bytes(size)
        else:
            if any(out[start:start + size]): raise ValueError('mesh template is not blank')
            f = streams[row['source']]; f.seek(offset); data = f.read(size)
            if len(data) != size or sha(data) != row['sha256']: raise ValueError('Steam mesh buffer differs')
            out[start:start + size] = data
        end = start + size
    return bytes(out)


def generate(args):
    import UnityPy
    source, obb = args.steam_data.resolve(strict=True), args.obb.resolve(strict=True)
    output = new_output(args.output, (source, obb)); index = defaultdict(list)
    for name in SOURCE_ASSETS:
        path = source / name
        if not path.is_file() or path.is_symlink(): raise ValueError('missing Steam asset')
        for obj in UnityPy.load(str(path)).objects:
            if obj.type.name != 'Mesh': continue
            mesh = obj.read(); vertices, indices = buffers(mesh)
            if not vertices and not indices: continue
            vref = source_reference(source, name, obj, mesh, vertices, True)
            iref = source_reference(source, name, obj, mesh, indices, False)
            if (vertices and vref is None) or (indices and iref is None): continue
            index[(geometry(mesh), sha(vertices), sha(indices))].append((vref, iref))
    rows = []; outcomes = Counter(); output.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(obb) as zin:
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate ZIP entry')
            for group in study.GROUPS:
                parts = study.parts(zin, group); data = b''.join(zin.read(p) for p in parts)
                for obj in UnityPy.load(data).objects:
                    if obj.type.name != 'Mesh': continue
                    mesh = obj.read(); vertices, indices = buffers(mesh)
                    if not vertices and not indices: outcomes['empty'] += 1; continue
                    raw = obj.get_raw_data()
                    if (vertices and raw.count(vertices) != 1) or (indices and raw.count(indices) != 1): outcomes['unsupported_target'] += 1; continue
                    matches = index[(geometry(mesh), sha(vertices), sha(indices))]
                    if not matches: outcomes['unmatched'] += 1; continue
                    # Identical bytes and layout are sufficient even if several
                    # source objects point to different copies of the buffers.
                    vref, iref = sorted(matches, key=lambda pair: (pair[0] or ('', 0), pair[1] or ('', 0)))[0]
                    for payload, ref in ((vertices, vref), (indices, iref)):
                        if payload:
                            position = obj.byte_start + raw.index(payload)
                            if data[position:position + len(payload)] != payload: raise ValueError('target mesh offset differs')
                            rows.extend(fragments(parts, position, payload, *ref))
                    outcomes['matched'] += 1
            if not rows: raise ValueError('no exact mesh buffers found')
            grouped = defaultdict(list)
            for row in rows: grouped[row['entry']].append(row)
            with zipfile.ZipFile(output / 'template.obb', 'x') as zout:
                for info in infos:
                    raw = zin.read(info)
                    zout.writestr(info, apply(raw, grouped[info.filename]) if info.filename in grouped else raw)
        used = sorted({r['source'] for r in rows})
        plan = {'schema': 1, 'kind': 'experimental-steam-mesh-buffer-offsets',
                'template_sha256': file_hash(output / 'template.obb'),
                'sources': {name: file_hash(source / name) for name in used}, 'outcomes': dict(outcomes), 'records': rows}
        (output / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(output)
        raise
    print(f'Masked {sum(r["size"] for r in rows)} bytes from {outcomes["matched"]} meshes; other game data remains.')
    print('Not selected:', {k: v for k, v in outcomes.items() if k != 'matched'})


def restore(args):
    from contextlib import ExitStack
    source, template, plan_file = (p.resolve(strict=True) for p in (args.steam_data, args.template, args.plan))
    output = new_output(args.output, (source, template, plan_file)); plan = json.loads(plan_file.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-steam-mesh-buffer-offsets' or plan.get('template_sha256') != file_hash(template): raise ValueError('plan or template differs')
    rows, hashes = plan.get('records'), plan.get('sources')
    if not isinstance(rows, list) or not rows or len(rows) > 10000 or not isinstance(hashes, dict) or not hashes: raise ValueError('invalid plan')
    if any(not SOURCE.fullmatch(name) or not (source / name).is_file() or (source / name).is_symlink() or file_hash(source / name) != value for name, value in hashes.items()): raise ValueError('Steam source differs')
    grouped = defaultdict(list)
    for row in rows:
        if row.get('source') not in hashes or not ENTRY.fullmatch(row.get('entry', '')): raise ValueError('invalid plan reference')
        grouped[row['entry']].append(row)
    created = False
    try:
        with ExitStack() as stack:
            zin = stack.enter_context(zipfile.ZipFile(template)); infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate ZIP entry')
            streams = {name: stack.enter_context((source / name).open('rb')) for name in hashes}
            target = stack.enter_context(output.open('xb')); created = True
            zout = stack.enter_context(zipfile.ZipFile(target, 'w')); seen = set()
            for info in infos:
                raw = zin.read(info)
                if info.filename in grouped: raw = apply(raw, grouped[info.filename], streams); seen.add(info.filename)
                zout.writestr(info, raw)
            if seen != set(grouped): raise ValueError('missing planned entries')
    except BaseException:
        if created: output.unlink(missing_ok=True)
        raise
    print(f'Restored {len(rows)} mesh fragments; output remains private.')


def main():
    p = argparse.ArgumentParser(description=__doc__); sub = p.add_subparsers(dest='command', required=True)
    for command in ('generate', 'restore'):
        s = sub.add_parser(command); s.add_argument('--steam-data', type=Path, required=True); s.add_argument('--output', type=Path, required=True)
        if command == 'generate': s.add_argument('--obb', type=Path, required=True)
        else: s.add_argument('--template', type=Path, required=True); s.add_argument('--plan', type=Path, required=True)
    args = p.parse_args(); (generate if args.command == 'generate' else restore)(args)


if __name__ == '__main__': main()
