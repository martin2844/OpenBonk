#!/usr/bin/env python3
"""Rebuild research shader packs from a local Unity build cache.

The plans contain hashes and ranges, not the compiled shader bodies. The cache,
plans and outputs are local research inputs and are not cleared for distribution.
Requires UnityPy. This does not compile shaders or make the launcher distributable.
"""
import argparse
import hashlib
import importlib.util
import io
from pathlib import Path
import shutil
import struct
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parents[1]
PINS = {
    'builtin-shaders.bin': '4016359642b21aa4f8689cab79b19b4ddc9c90b10362dd4d4072eaca9dca9bae',
    'shared1-shaders.bin': '673f1003a80d65b951c96dcb09fa6401fe72d9c6c2f9cffb55658a6da34bd9d1',
}


def digest(data):
    return hashlib.sha256(data).digest()


def parse_plan(data, kind):
    if digest(data).hex() != PINS[kind]:
        raise ValueError('unexpected shader plan: ' + kind)
    magic, asset_size, count = struct.unpack_from('>8sII', data)
    builtin = kind == 'builtin-shaders.bin'
    if magic != (b'OBBSH001' if builtin else b'OBS1S001') or count != (3 if builtin else 55):
        raise ValueError('invalid shader plan header')
    pack_hash = data[80:112]
    offset = 112
    rows = []
    for i in range(count):
        if builtin:
            ident, dst, size = struct.unpack_from('>III', data, offset)
            source = data[offset + 12:offset + 44]
            if ident != i + 1:
                raise ValueError('unexpected builtin identifier')
            name = f'shader-{ident}.bin'
            offset += 44
        else:
            dst, size, low, high = struct.unpack_from('>IIII', data, offset)
            source = data[offset + 16:offset + 48]
            if not 0 <= low <= high <= size:
                raise ValueError('invalid shader reference range')
            name = f'shader-{i:03d}.bin'
            offset += 112
        if size <= 0 or size > 2 * 1024 * 1024 or dst > asset_size - size or len(source) != 32:
            raise ValueError('invalid shader range')
        rows.append((name, size, source))
    if offset != len(data):
        raise ValueError('unexpected shader plan length')
    return pack_hash, rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('plans_zip', type=Path)
    parser.add_argument('unity_cache', type=Path)
    parser.add_argument('output_dir', type=Path)
    args = parser.parse_args()
    out = args.output_dir.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir():
        parser.error('output directory must be new and have an existing parent')

    spec = importlib.util.spec_from_file_location('openbonk_shader_pins', ROOT / 'tools/pinned-shader-bundles.py')
    pins = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(pins)
    # Unity can produce different bundle wrapper bytes for identical serialized
    # shader objects. Validate the selected objects against the pinned plans and
    # the resulting archives against their pinned hashes below.
    for name in pins.PINS:
        path = args.unity_cache / name
        if path.is_symlink() or not path.is_file() or path.stat().st_size > 8 * 1024 * 1024:
            raise ValueError('missing or oversized local shader bundle: ' + name)
    with zipfile.ZipFile(args.plans_zip) as plans:
        names = plans.namelist()
        for name in PINS:
            if names.count(name) != 1 or plans.getinfo(name).file_size > 16384:
                raise ValueError('missing or duplicate shader plan: ' + name)
        specs = {name: parse_plan(plans.read(name), name) for name in PINS}

    wanted = {h for _, rows in specs.values() for _, _, h in rows}
    values = {}
    for filename in pins.PINS:
        for obj in UnityPy.load(str(args.unity_cache / filename)).objects:
            if obj.type.name not in ('Shader', 'ComputeShader'):
                continue
            raw = obj.get_raw_data()
            key = digest(raw)
            if key in wanted:
                values[key] = raw
    if set(values) != wanted:
        raise ValueError('local Unity cache is missing required compiled shaders')

    outputs = {}
    for plan_name, (expected, rows) in specs.items():
        memory = io.BytesIO()
        with zipfile.ZipFile(memory, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
            for name, size, h in rows:
                value = values[h]
                if len(value) != size:
                    raise ValueError('compiled shader size differs')
                info = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0))
                info.compress_type = zipfile.ZIP_DEFLATED
                archive.writestr(info, value)
        result = memory.getvalue()
        if digest(result) != expected:
            raise ValueError('pack bytes differ; check the ZIP compressor version')
        outputs[plan_name.replace('shaders.bin', 'shader-pack.zip')] = result

    out.mkdir(mode=0o700)
    try:
        for name, data in outputs.items():
            with (out / name).open('xb') as stream:
                stream.write(data)
    except BaseException:
        shutil.rmtree(out)
        raise
    for name, data in outputs.items():
        print(name, len(data), digest(data).hex())


if __name__ == '__main__':
    main()
