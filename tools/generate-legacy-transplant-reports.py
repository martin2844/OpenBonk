#!/usr/bin/env python3
"""Recreate three pinned legacy object-transplant reports from local archives.

The inputs and resulting reports contain game-derived data and remain private.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import UnityPy

ROOT = Path(__file__).resolve().parent.parent


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


helper = module('legacy_builder', ROOT/'tools/rebuild-legacy-shader-base.py')
transplant = module('legacy_transplant', ROOT/'analysis/data-conversion/transplant-matching-assets-prototype.py')
REPORTS = (
    ('no-rewired-shared0-transplant.json', '65daa0be53d0058d178b05d458e9af5d5283ee4f4c7bfd7c7ee0651233fe2cfe'),
    ('no-rewired-shared1-safe-transplant.json', '418ef5f586a86229c2d1ea6ab754532b187d32efe38975ec16998016a2d3bb5a'),
    ('no-rewired-shared2-safe-transplant.json', '686dc6c8f588337c565b3c37eb05a9cb95dc5f91ef40eb12b9a1f6e28c07bdbd'),
)
EXCEPTION_HASHES = ('ac5211b0c39cf2edb080ae3cd7611a160d7aa1b8fe1e4c258e3c379988703f9d',
                    'b424b33fc84c27c2e01ab04901a7da6f8ea92910263f3fd419022571c00d3347',
                    'ffbc65a2e87cce915efcaf331020e80996567d45aefde3decfe785644b0dc83c')


def exception_manifest(old, new, converted):
    hashes = []
    for archive in (old, new, converted):
        data = transplant.read_asset(archive, 'sharedassets1.assets')
        obj = transplant.objects(UnityPy.load(data))[1072]
        if obj.type.name != 'Texture2D':
            raise ValueError('unexpected exception object type')
        hashes.append(hashlib.sha256(obj.get_raw_data()).hexdigest())
    if tuple(hashes) != EXCEPTION_HASHES:
        raise ValueError('texture exception input differs')
    return {'version': 1, 'asset': 'sharedassets1.assets', 'textures': [
        dict(path_id=1072, old_sha256=hashes[0], new_sha256=hashes[1], converted_sha256=hashes[2])]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('old-apk', 'old-obb', 'converted-obb', 'new-apk', 'new-obb'):
        parser.add_argument('--'+name, required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    archives = [getattr(args, name).resolve(strict=True) for name in ('old_apk', 'old_obb', 'converted_obb', 'new_apk', 'new_obb')]
    output = args.output.expanduser().absolute()
    if not all(p.is_file() for p in archives) or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('input archives must exist and output must be new')
    output = output.parent.resolve()/output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (*archives, ROOT)):
        parser.error('output must be separate from checkout and inputs')
    old_apk, old_obb, converted, new_apk, new_obb = archives
    with tempfile.TemporaryDirectory(prefix='.openbonk-transplant-reports-', dir=output.parent) as directory:
        tmp = Path(directory)
        exception = tmp/'texture-exception.json'
        exception.write_text(json.dumps(exception_manifest(old_obb, new_obb, converted), indent=2)+'\n')
        for i, (name, expected) in enumerate(REPORTS):
            report, asset = tmp/name, tmp/f'output-{i}.assets'
            extra = ['--texture-payload-manifest', exception] if i == 1 else []
            helper.run('analysis/data-conversion/transplant-matching-assets-prototype.py',
                       old_apk if i == 0 else old_obb, converted, new_apk if i == 0 else new_obb,
                       f'sharedassets{i}.assets', asset, '--report', report, *extra)
            if i == 0:
                doc = json.loads(report.read_text())
                for row in doc['changed_objects']:
                    if row.pop('method', None) != 'identical original object':
                        raise ValueError('unexpected legacy shared0 transplant method')
                report.write_text(json.dumps(doc, indent=2)+'\n')
            if hashlib.sha256(report.read_bytes()).hexdigest() != expected:
                raise ValueError('generated transplant report differs: '+name)
        output.mkdir(mode=0o700)
        try:
            for name, _ in REPORTS:
                shutil.copyfile(tmp/name, output/name)
        except BaseException:
            shutil.rmtree(output)
            raise
    print('Generated three pinned transplant reports; keep them private.')


if __name__ == '__main__':
    main()
