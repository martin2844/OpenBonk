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
    'old_apk': '5d3db6250ae33441872df4bd242278c7ca4a6eb4e971c02cc687f5c8dc47349a',
    'new_apk': 'f337676127872d1bb6dcfabb2e9e465709c691087378685b8fb207b9a0e02486',
    'source_obb': 'a304957b263d31e5e9ae03f7ef46c705807da442a9863f2bbe3984fdfc586cbb',
    'target_apk': '7f698da0808cbc9191ad08f66580073df016329a3a55525c6d4f051fb5b2d3ab',
    'new_source_obb': '3a6ce0ac78310852c1a2252fe65c267a9812b9b900e94c3e9062472a4ada2d58',
    'target_obb': 'a3bcca988a39ac3868dc39a2f6384b0e5c633ce95d68b180f73ae3bb66e43400',
}
ALLOWED = {'assets/bin/Data/boot.config', 'assets/bin/Data/unity_app_guid', 'assets/dexopt/baseline.prof', 'assets/unity_obb_guid', 'classes.dex', 'lib/arm64-v8a/libgame.so', 'lib/arm64-v8a/libswappywrapper.so'}
EXTRA_REPLACEMENTS = set()
TARGET_APK_CONTENT = None


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
        if key == 'target_apk' and TARGET_APK_CONTENT is not None:
            spec = importlib.util.spec_from_file_location('apk_content', Path(__file__).resolve().parents[2]/'tools/apk-content-digest.py')
            digest = importlib.util.module_from_spec(spec); spec.loader.exec_module(digest)
            if not path.is_file() or digest.digest(path) != TARGET_APK_CONTENT: raise ValueError('incompatible input: '+str(path))
            continue
        if not path.is_file() or repair.sha(path) != PINS[key]: raise ValueError('incompatible input: '+str(path))
    if repair.sha(a.new_split/OBB) != PINS['new_source_obb']: raise ValueError('unexpected new OBB')
    with zipfile.ZipFile(a.old_split/OBB) as old_obb, zipfile.ZipFile(a.new_split/OBB) as new_obb:
        repair.unique(old_obb); repair.unique(new_obb)
        if set(old_obb.namelist()) != set(new_obb.namelist()): raise ValueError('split OBB inventories differ')
        if any(old_obb.read(n) != new_obb.read(n) for n in old_obb.namelist() if n != 'unity_obb_guid'): raise ValueError('split OBB content differs')
    unity = Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer'
    bt = Path(os.environ.get('OPENBONK_ANDROID_SDK_ROOT', str(unity/'SDK'))).expanduser()/'build-tools/34.0.0'
    java = Path(os.environ.get('OPENBONK_JAVA_HOME', str(unity/'OpenJDK'))).expanduser()
    key = Path(os.environ.get('OPENBONK_DEBUG_KEYSTORE', str(Path.home()/'.android/debug.keystore'))).expanduser()
    runner = os.environ.get('OPENBONK_TOOL_RUNNER', 'steam-run' if shutil.which('steam-run') else 'none')
    if runner != 'none' and not shutil.which(runner): raise ValueError('configured runner missing')
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
            for name in EXTRA_REPLACEMENTS:
                if name not in names or old.read(name) != target.read(name):
                    raise ValueError('target runtime is not the expected previous version: '+name)
            # Select an equivalent ZIP comment whose hash fits the new DEX's
            # sorted string table; no archive entry content changes.
            new_dex = new.read('classes.dex')
            previous, following = repair.dex_bounds(new_dex, PINS['new_source_obb'].encode())
            replacement_obb = tmp/OBB
            shutil.copyfile(paths['target_obb'], replacement_obb)
            with replacement_obb.open('rb') as f:
                data=f.read()
            if len(data)<8 or obb.comment != data[-8:] or len(obb.comment)!=8:
                raise ValueError('unexpected target OBB comment')
            prefix=hashlib.sha256(data[:-8])
            for nonce in range(1000000):
                comment=f'{nonce:08x}'.encode(); h=prefix.copy(); h.update(comment); new_hash=h.hexdigest().encode()
                if previous < new_hash < following: break
            else: raise ValueError('cannot preserve DEX string order')
            with replacement_obb.open('r+b') as f:
                f.seek(-8,2); f.write(comment)
            if repair.sha(replacement_obb).encode()!=new_hash: raise ValueError('OBB hash mismatch')
            updated = patch(new_dex, PINS['new_source_obb'].encode(), new_hash)
            with zipfile.ZipFile(unsigned, 'x', allowZip64=True) as dst:
                for info in target.infolist():
                    if not repair.jar_signature(info.filename):
                        data = updated if info.filename == 'classes.dex' else new.read(info.filename) if info.filename in EXTRA_REPLACEMENTS else target.read(info)
                        dst.writestr(copy.copy(info), data)
        def run(args): subprocess.run(([] if runner == 'none' else [runner]) + list(map(str,args)), env=env, check=True)
        run([bt/'zipalign','-p','4',unsigned,aligned])
        run([bt/'apksigner','sign','--ks',key,'--ks-key-alias','androiddebugkey','--ks-pass','pass:android','--key-pass','pass:android','--out',signed,aligned])
        run([bt/'zipalign','-c','4',signed]); run([bt/'apksigner','verify','--verbose',signed])
        with zipfile.ZipFile(signed) as result, zipfile.ZipFile(paths['target_apk']) as target:
            repair.unique(result)
            for n in target.namelist():
                if not repair.jar_signature(n) and n != 'classes.dex' and n not in EXTRA_REPLACEMENTS and result.read(n) != target.read(n):
                    raise ValueError('unrelated APK entry changed: '+n)
            if result.read('classes.dex') != updated: raise ValueError('DEX mismatch')
            with zipfile.ZipFile(paths['new_apk']) as new:
                for n in EXTRA_REPLACEMENTS:
                    if result.read(n) != new.read(n): raise ValueError('runtime entry mismatch: '+n)
        report = {'kind':'local diagnostic only, not for distribution', 'apk_sha256':repair.sha(signed),
                  'obb_sha256':repair.sha(replacement_obb), 'replaced':['classes.dex', *sorted(EXTRA_REPLACEMENTS)],
                  'limitations':['Game and third-party material remain.', 'Experimental DEX patch and debug signature.', 'No complete device validation.']}
        (tmp/'report.json').write_text(json.dumps(report, indent=2)+'\n')
        out.mkdir()
        try:
            shutil.copyfile(signed, out/signed.name); shutil.copyfile(replacement_obb, out/OBB); shutil.copyfile(tmp/'report.json', out/'report.json')
        except BaseException:
            shutil.rmtree(out); raise
        print(json.dumps(report, indent=2))


if __name__ == '__main__': main()
