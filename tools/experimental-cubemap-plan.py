#!/usr/bin/env python3
"""Generate and restore a private Steam cubemap conversion plan.

Templates and restored OBBs retain game data and must remain local. Requires
UnityPy, texture encoding dependencies and a local Unity Crunch decoder.
The experimental conversion is not proof of visual fidelity.
"""
import argparse
from collections import defaultdict
from contextlib import ExitStack
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('converted_plan', ROOT / 'tools/experimental-converted-texture-plan.py')
converted = importlib.util.module_from_spec(spec)
spec.loader.exec_module(converted)
study = converted.study


def convert_cube(payload, row, decoder, runner=None, workdir=None):
    from UnityPy.export import Texture2DConverter as converter
    if row['source_format'] == 29:
        with tempfile.TemporaryDirectory(prefix='.cube-decode-', dir=workdir) as temp:
            src, dst = Path(temp) / 'input.crn', Path(temp) / 'decoded.bin'
            src.write_bytes(payload)
            subprocess.run(([runner] if runner else []) + [str(decoder), str(src), str(dst)], check=True, stdout=subprocess.DEVNULL)
            decoded = dst.read_bytes()
    elif row['source_format'] == 12: decoded = payload
    else: raise ValueError('unsupported cubemap source format')
    if len(decoded) != 6 * 262144 or converted.sha(decoded) != row['decoded_sha256']: raise ValueError('decoded cubemap differs')
    output = []
    for face in range(6):
        data = decoded[face * 262144:(face + 1) * 262144]
        image = converter.parse_image_data(data, 512, 512, 12, (2023, 2, 22, 0), 19)
        encoded, fmt = converter.image_to_texture2d(image, 45, 13, None)
        if int(fmt) != 45 or len(encoded) != 131072: raise ValueError('cubemap face differs')
        output.append(encoded)
    result = b''.join(output)
    if len(result) != row['target_size'] or converted.sha(result) != row['target_sha256']: raise ValueError('converted cubemap differs')
    return result


def decode_hash(payload, source_format, decoder, runner, workdir):
    if source_format == 12: return converted.sha(payload)
    if source_format != 29: raise ValueError('unsupported cubemap source')
    with tempfile.TemporaryDirectory(prefix='.cube-decode-', dir=workdir) as temp:
        src, dst = Path(temp) / 'input.crn', Path(temp) / 'decoded.bin'; src.write_bytes(payload)
        subprocess.run(([runner] if runner else []) + [str(decoder), str(src), str(dst)], check=True, stdout=subprocess.DEVNULL)
        decoded = dst.read_bytes()
        if len(decoded) != 6 * 262144: raise ValueError('decoded cubemap size differs')
        return converted.sha(decoded)


def generate(args):
    import UnityPy
    source, obb, manifest, decoder = (p.resolve(strict=True) for p in (args.steam_data, args.obb, args.manifest, args.decoder))
    output = converted.new_output(args.output, (source, obb, manifest, decoder)); report = json.loads(manifest.read_text())
    if report.get('schema') != 1 or not isinstance(report.get('cubemaps'), list) or not 0 < len(report['cubemaps']) <= 32: raise ValueError('invalid cubemap study')
    selected = {}
    for row in report['cubemaps']:
        if type(row.get('target_id')) is not int or row['target_id'] in selected: raise ValueError('duplicate cubemap')
        selected[row['target_id']] = row
    asset, resource = source / 'sharedassets1.assets', source / 'sharedassets1.assets.resS'
    if any(not p.is_file() or p.is_symlink() for p in (asset, resource, decoder)): raise ValueError('missing regular input')
    index = defaultdict(list)
    for obj in UnityPy.load(str(asset)).objects:
        if obj.type.name == 'Cubemap':
            cube = obj.read(); index[(cube.m_Name, cube.m_Width, cube.m_Height)].append(cube)
    rows = []; output.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(obb) as zin:
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate OBB entry')
            parts = study.parts(zin, 'sharedassets1'); data = b''.join(zin.read(i) for i in parts)
            objects = {o.path_id: o for o in UnityPy.load(data).objects if o.type.name == 'Cubemap'}
            for key, prior in selected.items():
                if key not in objects: raise ValueError('missing target cubemap')
                obj = objects[key]; dst = obj.read(); matches = index[(dst.m_Name, dst.m_Width, dst.m_Height)]
                if len(matches) != 1 or dst.m_Name != prior['name']: raise ValueError('cubemap source mapping differs')
                src = matches[0]
                if (any(getattr(dst, k, None) != getattr(src, k, None) for k in ('m_MipCount', 'm_ImageCount', 'm_ColorSpace')) or
                        (dst.m_Width, dst.m_Height, dst.m_MipCount, dst.m_ImageCount, int(dst.m_TextureFormat)) != (512, 512, 1, 6, 45) or
                        int(src.m_TextureFormat) != prior['source_format']): raise ValueError('cubemap metadata differs')
                source_bytes, target = bytes(src.get_image_data()), bytes(dst.image_data); stream = src.m_StreamData
                if stream.path != resource.name or stream.offset < 0 or stream.size != len(source_bytes) or len(target) != 6 * 131072: raise ValueError('unsupported cubemap source')
                with resource.open('rb') as f:
                    f.seek(stream.offset)
                    if f.read(len(source_bytes)) != source_bytes: raise ValueError('cubemap source offset differs')
                body = obj.get_raw_data(); pos = body.find(target)
                if pos < 0 or body.count(target) != 1: raise ValueError('unsupported cubemap target')
                row = {'id': len(rows), 'source': resource.name, 'source_offset': stream.offset, 'source_size': len(source_bytes),
                       'source_sha256': converted.sha(source_bytes), 'source_format': int(src.m_TextureFormat),
                       'decoded_sha256': decode_hash(source_bytes, int(src.m_TextureFormat), decoder, args.runner, output),
                       'target_size': len(target), 'target_sha256': converted.sha(target),
                       'fragments': converted.fragments(parts, obj.byte_start + pos, len(target))}
                if convert_cube(source_bytes, row, decoder, args.runner, output) != target: raise ValueError('converted cubemap differs')
                rows.append(row)
            grouped = defaultdict(list)
            for row in rows:
                for fragment in row['fragments']: grouped[fragment['entry']].append((fragment, row))
            with zipfile.ZipFile(output / 'template.obb', 'x') as zout:
                for info in infos:
                    raw = zin.read(info)
                    zout.writestr(info, converted.apply(raw, grouped[info.filename]) if info.filename in grouped else raw)
        plan = {'schema': 1, 'kind': 'experimental-converted-steam-cubemap-offsets', 'template_sha256': converted.file_hash(output / 'template.obb'),
                'sources': {p.name: converted.file_hash(p) for p in (asset, resource)}, 'records': rows}
        (output / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(output)
        raise
    print(f'Masked {sum(r["target_size"] for r in rows)} bytes from {len(rows)} cubemaps; template remains private.')


def restore(args):
    source, template, plan_file, decoder = (p.resolve(strict=True) for p in (args.steam_data, args.template, args.plan, args.decoder))
    output = converted.new_output(args.output, (source, template, plan_file, decoder)); plan = json.loads(plan_file.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-converted-steam-cubemap-offsets' or plan.get('template_sha256') != converted.file_hash(template): raise ValueError('plan or template differs')
    rows, hashes = plan.get('records'), plan.get('sources')
    if not isinstance(rows, list) or not 0 < len(rows) <= 32 or not isinstance(hashes, dict) or set(hashes) != {'sharedassets1.assets', 'sharedassets1.assets.resS'}: raise ValueError('invalid plan')
    if any(not (source / n).is_file() or (source / n).is_symlink() or converted.file_hash(source / n) != h for n, h in hashes.items()): raise ValueError('Steam input differs')
    if not decoder.is_file() or decoder.is_symlink(): raise ValueError('invalid decoder')
    grouped, payloads = defaultdict(list), {}
    with (source / 'sharedassets1.assets.resS').open('rb') as resource:
        for i, row in enumerate(rows):
            if (row.get('id') != i or row.get('source') != 'sharedassets1.assets.resS' or type(row.get('source_offset')) is not int or row['source_offset'] < 0 or
                    type(row.get('source_size')) is not int or not 0 < row['source_size'] <= 4 * 1024 * 1024 or row.get('target_size') != 6 * 131072): raise ValueError('invalid cubemap record')
            fragments = row.get('fragments')
            if not isinstance(fragments, list) or not fragments: raise ValueError('missing cubemap fragments')
            pos = 0
            for frag in fragments:
                if not re.fullmatch(r'assets/bin/Data/sharedassets1\.assets\.split(?:0|[1-9]\d*)', frag.get('entry', '')) or frag.get('converted_offset') != pos or type(frag.get('size')) is not int or frag['size'] <= 0: raise ValueError('invalid cubemap fragment')
                pos += frag['size']; grouped[frag['entry']].append((frag, row))
            if pos != row['target_size']: raise ValueError('incomplete cubemap fragments')
            resource.seek(row['source_offset']); payload = resource.read(row['source_size'])
            if len(payload) != row['source_size'] or converted.sha(payload) != row['source_sha256']: raise ValueError('cubemap source differs')
            result = convert_cube(payload, row, decoder, args.runner, output.parent)
            if len(result) != row['target_size'] or converted.sha(result) != row['target_sha256']: raise ValueError('converted cubemap differs')
            payloads[i] = result
    created = False
    try:
        with ExitStack() as stack:
            zin = stack.enter_context(zipfile.ZipFile(template)); infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate OBB entry')
            target = stack.enter_context(output.open('xb')); created = True
            zout = stack.enter_context(zipfile.ZipFile(target, 'w')); seen = set()
            for info in infos:
                raw = zin.read(info)
                if info.filename in grouped: raw = converted.apply(raw, grouped[info.filename], payloads); seen.add(info.filename)
                zout.writestr(info, raw)
            if seen != set(grouped): raise ValueError('missing cubemap split')
    except BaseException:
        if created: output.unlink(missing_ok=True)
        raise
    print(f'Restored {len(rows)} cubemaps; output remains private.')


def main():
    p = argparse.ArgumentParser(description=__doc__); sub = p.add_subparsers(dest='command', required=True)
    for command in ('generate', 'restore'):
        s = sub.add_parser(command); s.add_argument('--steam-data', type=Path, required=True); s.add_argument('--output', type=Path, required=True)
        s.add_argument('--decoder', type=Path, required=True); s.add_argument('--runner')
        if command == 'generate': s.add_argument('--obb', type=Path, required=True); s.add_argument('--manifest', type=Path, required=True)
        else: s.add_argument('--template', type=Path, required=True); s.add_argument('--plan', type=Path, required=True)
    args = p.parse_args(); (generate if args.command == 'generate' else restore)(args)


if __name__ == '__main__': main()
