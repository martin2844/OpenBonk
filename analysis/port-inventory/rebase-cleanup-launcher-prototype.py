#!/usr/bin/env python3
"""Transplant a pinned launcher DEX into the tested local reconstructed build.

The result retains game and third-party material and uses an experimental DEX
patch and debug signature. It is a diagnostic, not a distributable release.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import zipfile
import zlib

spec = importlib.util.spec_from_file_location('repair', Path(__file__).with_name('repair-split-guid-probe.py'))
repair = importlib.util.module_from_spec(spec)
spec.loader.exec_module(repair)
OBB = 'main.1.com.megabonk.nativeport.splittest.obb'
PINS = {
    'old_apk': 'd130551c64c64a3980c86aa85260a0a41ac016de1b0b7196a35af82e21d4e6ff',
    'new_apk': '5d3db6250ae33441872df4bd242278c7ca4a6eb4e971c02cc687f5c8dc47349a',
    'source_obb': 'a304957b263d31e5e9ae03f7ef46c705807da442a9863f2bbe3984fdfc586cbb',
    'target_apk': '30f6cf7da796760875b1fec5186383425bb6a44dc68c942b8c8a85179d4e4019',
    'target_obb': 'a3bcca988a39ac3868dc39a2f6384b0e5c633ce95d68b180f73ae3bb66e43400',
}
ALLOWED = {'classes.dex', 'assets/dexopt/baseline.prof', 'lib/arm64-v8a/libgame.so',
           'lib/arm64-v8a/libswappywrapper.so'}


def patch(dex, old, new):
    previous, following = repair.dex_bounds(dex, old)
    if not previous < new < following:
        raise ValueError('target OBB hash cannot preserve DEX string order')
    data = bytearray(dex.replace(old, new))
    data[12:32] = hashlib.sha1(data[32:]).digest()
    data[8:12] = (zlib.adler32(data[12:]) & 0xffffffff).to_bytes(4, 'little')
    return bytes(data)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('old_split', type=Path); p.add_argument('new_split', type=Path)
    p.add_argument('rebuilt', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); out = a.output.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    for root in (a.old_split, a.new_split, a.rebuilt):
        root = root.resolve()
        if out.resolve().is_relative_to(root) or root.is_relative_to(out.resolve()): p.error('output must be separate from inputs')
    paths = {'old_apk': a.old_split/'OpenBonk-diagnostic.apk', 'new_apk': a.new_split/'OpenBonk-diagnostic.apk',
             'source_obb': a.old_split/OBB, 'target_apk': a.rebuilt/'OpenBonk-diagnostic.apk', 'target_obb': a.rebuilt/OBB}
    for key, path in paths.items():
        if not path.is_file() or repair.sha(path) != PINS[key]: raise ValueError('incompatible input: '+str(path))
    if repair.sha(a.new_split/OBB) != PINS['source_obb']: raise ValueError('split OBBs differ')
    unity = Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer'
    bt, java, key = unity/'SDK/build-tools/34.0.0', unity/'OpenJDK', Path.home()/'.android/debug.keystore'
    env = os.environ.copy(); env['JAVA_HOME'] = str(java); env['PATH'] = str(java/'bin') + os.pathsep + env.get('PATH', '')
    with tempfile.TemporaryDirectory(prefix='.openbonk-cleanup-', dir=out.parent) as tmp:
        tmp = Path(tmp); unsigned, aligned, signed = tmp/'unsigned.apk', tmp/'aligned.apk', tmp/'OpenBonk-diagnostic.apk'
        with zipfile.ZipFile(paths['old_apk']) as old, zipfile.ZipFile(paths['new_apk']) as new, zipfile.ZipFile(paths['target_apk']) as target, zipfile.ZipFile(paths['target_obb']) as obb:
            for z in (old, new, target, obb): repair.unique(z)
            names = {n for n in old.namelist() if not repair.jar_signature(n)}
            if names != {n for n in new.namelist() if not repair.jar_signature(n)}: raise ValueError('split APK inventories differ')
            changed = {n for n in names if old.read(n) != new.read(n)}
            if changed != ALLOWED: raise ValueError('unexpected split changes: '+str(sorted(changed)))
            if target.read('assets/unity_obb_guid') != obb.read('unity_obb_guid'): raise ValueError('target GUID mismatch')
            source_hash, target_hash = PINS['source_obb'].encode(), PINS['target_obb'].encode()
            if patch(old.read('classes.dex'), source_hash, target_hash) != target.read('classes.dex'):
                raise ValueError('target launcher is not the expected previous DEX')
            updated = patch(new.read('classes.dex'), source_hash, target_hash)
            with zipfile.ZipFile(unsigned, 'x', allowZip64=True) as dst:
                for info in target.infolist():
                    if not repair.jar_signature(info.filename):
                        dst.writestr(copy.copy(info), updated if info.filename == 'classes.dex' else target.read(info))
        def run(args): subprocess.run(['steam-run', *map(str,args)], env=env, check=True)
        run([bt/'zipalign','-p','4',unsigned,aligned])
        run([bt/'apksigner','sign','--ks',key,'--ks-key-alias','androiddebugkey','--ks-pass','pass:android','--key-pass','pass:android','--out',signed,aligned])
        run([bt/'zipalign','-c','4',signed]); run([bt/'apksigner','verify','--verbose',signed])
        with zipfile.ZipFile(signed) as result, zipfile.ZipFile(paths['target_apk']) as target:
            repair.unique(result)
            for n in target.namelist():
                if not repair.jar_signature(n) and n != 'classes.dex' and result.read(n) != target.read(n):
                    raise ValueError('unrelated APK entry changed: '+n)
            if result.read('classes.dex') != updated: raise ValueError('DEX mismatch')
        report = {'kind':'local diagnostic only, not for distribution', 'apk_sha256':repair.sha(signed),
                  'obb_sha256':PINS['target_obb'], 'replaced':['classes.dex'],
                  'limitations':['Game and third-party material remain.', 'Experimental DEX patch and debug signature.', 'No complete device validation.']}
        (tmp/'report.json').write_text(json.dumps(report, indent=2)+'\n')
        out.mkdir()
        try:
            shutil.copyfile(signed, out/signed.name); shutil.copyfile(paths['target_obb'], out/OBB); shutil.copyfile(tmp/'report.json', out/'report.json')
        except BaseException:
            shutil.rmtree(out); raise
        print(json.dumps(report, indent=2))


if __name__ == '__main__': main()
