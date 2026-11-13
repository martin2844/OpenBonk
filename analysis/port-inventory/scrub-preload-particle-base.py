#!/usr/bin/env python3
"""Create a local research base with selected preload particle bodies blank.

The output retains unreviewed game and third-party material; do not distribute.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import tempfile
import zipfile
import zlib

BASE_APK = '8e7fb232d255905840cc34b8efd3a72b0dbc3693c8a34635ff85f5c95e3b0892'
BASE_OBB = 'ca99de9c1fb7a0d917a99cd2fc4fe0f421a1e44b4b7e2eef395551e8cbe3110d'
MANIFEST = 'c062e92a1acc22e52c050ffb542f434086c61d9f2ae1addb7136685fa2c444d7'
OBJECTS_MANIFEST = '825618022a8c01d0e24e74300a7261f1196d8950b44073946a60af89ae82ac33'
EXTRA_MANIFEST = 'ccc8f077900aa731066a21c1a94b0081b11fc02a495bb20b6d884ed5d6bc59a5'
spec = importlib.util.spec_from_file_location('repair', Path(__file__).with_name('repair-split-guid-probe.py'))
repair = importlib.util.module_from_spec(spec)
spec.loader.exec_module(repair)


def sha_bytes(b):
    return hashlib.sha256(b).hexdigest()


def require(path, expected):
    if not path.is_file() or repair.sha(path) != expected:
        raise ValueError('incompatible input: ' + str(path))


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('base', type=Path)
    p.add_argument('particle_template', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--include-gameobjects', action='store_true')
    p.add_argument('--include-extra-components', action='store_true')
    a = p.parse_args()
    if a.include_extra_components and not a.include_gameobjects:
        p.error('--include-extra-components requires --include-gameobjects')
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new')
    out = a.output.resolve()
    for source in (a.base, a.particle_template):
        path = source.resolve()
        if out.is_relative_to(path) or path.is_relative_to(out):
            p.error('output must be separate from inputs')
    apk, obb = a.base/'base.apk', a.base/'base.obb'
    manifest = a.particle_template/'manifest.json'
    expected_manifest = EXTRA_MANIFEST if a.include_extra_components else OBJECTS_MANIFEST if a.include_gameobjects else MANIFEST
    require(apk, BASE_APK); require(obb, BASE_OBB); require(manifest, expected_manifest)
    inventory = json.loads(manifest.read_text())
    if (inventory.get('objects' if a.include_gameobjects else 'particles') != (635 if a.include_extra_components else 604 if a.include_gameobjects else 101)
            or len(inventory.get('files', {})) != (68 if a.include_extra_components else 47 if a.include_gameobjects else 31)):
        raise ValueError('invalid particle inventory')
    ancillary = ['asset-manifest.json', 'hashed-manifest.json', 'shared0-font-metadata-manifest.json',
                 'shared2-animation-manifest.json', 'shared2-gamepad-manifest.json', 'shared4-mesh-manifest.json',
                 'shared4-particle-manifest.json', 'shared4-particle-reference-manifest.json', 'shared4-texture-manifest.json']
    if any(not (a.base/x).is_file() for x in ancillary):
        raise ValueError('missing base manifest')
    with tempfile.TemporaryDirectory(prefix='.openbonk-particles-', dir=a.output.parent) as tmp:
        tmp = Path(tmp); new_apk, new_obb = tmp/'base.apk', tmp/'base.obb'
        with zipfile.ZipFile(apk) as az, zipfile.ZipFile(obb) as oz:
            if any(len(z.namelist()) != len(set(z.namelist())) for z in (az, oz)):
                raise ValueError('duplicate archive entry')
            if az.read('assets/unity_obb_guid') != oz.read('unity_obb_guid'):
                raise ValueError('mismatched pair')
            dex = az.read('classes.dex'); old = BASE_OBB.encode('ascii')
            previous, following = repair.dex_bounds(dex, old)
            replacements = {}
            for name, rec in inventory['files'].items():
                if not re.fullmatch('[0-9a-f]{32}', name):
                    raise ValueError('invalid entry name')
                entry = 'assets/bin/Data/' + name
                original = oz.read(entry); blank = (a.particle_template/name).read_bytes()
                if sha_bytes(original) != rec['original_sha256'] or sha_bytes(blank) != rec['template_sha256'] or len(blank) != len(original):
                    raise ValueError('particle asset mismatch')
                replacements[entry] = blank
            with zipfile.ZipFile(new_obb, 'x', allowZip64=True) as dest:
                for info in oz.infolist():
                    dest.writestr(copy.copy(info), replacements.get(info.filename, oz.read(info)))
                dest.comment = b'00000000'
            h = hashlib.sha256()
            with new_obb.open('rb') as f:
                remaining = new_obb.stat().st_size - 8
                while remaining:
                    block = f.read(min(1048576, remaining))
                    if not block:
                        raise ValueError('short output OBB')
                    h.update(block); remaining -= len(block)
            for nonce in range(1_000_000):
                comment = f'{nonce:08x}'.encode(); candidate = h.copy(); candidate.update(comment)
                new = candidate.hexdigest().encode('ascii')
                if previous < new < following and new != old:
                    break
            else:
                raise ValueError('cannot preserve DEX string order')
            with new_obb.open('r+b') as f:
                f.seek(-8, 2); f.write(comment)
            if repair.sha(new_obb).encode() != new:
                raise ValueError('OBB hash mismatch')
            patched = bytearray(dex.replace(old, new))
            patched[12:32] = hashlib.sha1(patched[32:]).digest()
            patched[8:12] = (zlib.adler32(patched[12:]) & 0xffffffff).to_bytes(4, 'little')
            with zipfile.ZipFile(new_apk, 'x', allowZip64=True) as dest:
                for info in az.infolist():
                    if not repair.jar_signature(info.filename):
                        dest.writestr(copy.copy(info), bytes(patched) if info.filename == 'classes.dex' else az.read(info))
        with zipfile.ZipFile(new_apk) as az, zipfile.ZipFile(new_obb) as oz:
            if az.testzip() is not None or oz.testzip() is not None or az.read('assets/unity_obb_guid') != oz.read('unity_obb_guid') or az.read('classes.dex').count(new) != 1:
                raise ValueError('invalid output pair')
            if any(sha_bytes(oz.read(name)) != sha_bytes(data) for name, data in replacements.items()):
                raise ValueError('output entry mismatch')
        report = {'kind': 'local research only, not for distribution', 'apk_sha256': repair.sha(new_apk),
                  'obb_sha256': repair.sha(new_obb), 'particle_manifest_sha256': expected_manifest,
                  'particle_bodies': 101, 'gameobject_bodies': 503 if a.include_gameobjects else 0,
                  'extra_component_bodies': 31 if a.include_extra_components else 0,
                  'files': 68 if a.include_extra_components else 47 if a.include_gameobjects else 31,
                  'limitations': 'Unsigned base; other game content, metadata, dependencies and code remain.'}
        (tmp/'report.json').write_text(json.dumps(report, indent=2)+'\n')
        a.output.mkdir()
        try:
            for path in (new_apk, new_obb, tmp/'report.json', manifest):
                shutil.copyfile(path, a.output/('preload-particle-manifest.json' if path == manifest else path.name))
            for name in ancillary:
                shutil.copyfile(a.base/name, a.output/name)
        except BaseException:
            shutil.rmtree(a.output); raise
        print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
