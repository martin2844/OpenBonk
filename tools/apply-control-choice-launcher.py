#!/usr/bin/env python3
"""Apply the tested launcher to one pinned local diagnostic, not a release."""
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

ROOT = Path(__file__).resolve().parents[1]
OBB = 'main.1.com.megabonk.nativeport.splittest.obb'
OLD_APK = '727e72a793deb8e4ab2219015798d2acd7bb5ece20780ee74d0c2f263d341cdb'
NEW_APK = '56c61efba14d4cc987a867a215cf56bbd39197829c4b50095bccdaeef290d98b'
OLD_OBB = 'a7c5a320c7de060ca99908c0d69bb5a78e4fb1bd0c98a461f6c5c2c5e8bf111b'
NEW_OBB = '00a50941a48f3263569b0f2ed4aad64d57c9187165ba545e5dd6aa944147e2cf'
DEX_SHA = 'b1c5d532eb361be8602ece4553660d448f569731faad2e4194f0195cacd3874d'
DELTA = ROOT/'build/android/control-choice-runtime/launcher.bsdiff'
DELTA_SHA = '5bc8b067d68372d0b3b3b6454dceb59988859c1e30ad5f9203aa0e5b7a6c951d'


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('input', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); source = a.input.resolve(); out = a.output.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    if out.resolve().is_relative_to(source) or source.is_relative_to(out.resolve()): p.error('output must be separate')
    digest = load('content', ROOT/'tools/apk-content-digest.py')
    repair = load('repair', ROOT/'analysis/port-inventory/repair-split-guid-probe.py')
    apk, obb = source/'OpenBonk-diagnostic.apk', source/OBB
    if digest.digest(apk) != OLD_APK or sha(obb) != OLD_OBB or sha(DELTA) != DELTA_SHA:
        raise ValueError('incompatible diagnostic or launcher payload')
    unity = Path(os.environ.get('OPENBONK_UNITY_EDITOR', str(Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor'))).expanduser()/'Data/PlaybackEngines/AndroidPlayer'
    tools = Path(os.environ.get('OPENBONK_ANDROID_SDK_ROOT', str(unity/'SDK'))).expanduser()/'build-tools/34.0.0'
    java = Path(os.environ.get('OPENBONK_JAVA_HOME', str(unity/'OpenJDK'))).expanduser()
    key = Path(os.environ.get('OPENBONK_DEBUG_KEYSTORE', str(Path.home()/'.android/debug.keystore'))).expanduser()
    for path in (tools/'zipalign', tools/'apksigner', java/'bin/java', key):
        if not path.is_file(): raise ValueError('missing prerequisite: '+str(path))
    runner = os.environ.get('OPENBONK_TOOL_RUNNER', 'steam-run' if shutil.which('steam-run') else 'none')
    if runner != 'none' and not shutil.which(runner): raise ValueError('configured runner missing')
    env = os.environ.copy(); env['JAVA_HOME'] = str(java); env['PATH'] = str(java/'bin')+os.pathsep+env.get('PATH','')
    def run(*args): subprocess.run(([] if runner == 'none' else [runner])+list(map(str,args)), env=env, check=True)
    run(tools/'zipalign', '-c', '-p', '4', apk); run(tools/'apksigner', 'verify', apk)
    with tempfile.TemporaryDirectory(prefix='.openbonk-control-', dir=out.parent) as tmp:
        tmp = Path(tmp); new_obb = tmp/OBB; unsigned = tmp/'unsigned.apk'; aligned = tmp/'aligned.apk'; signed = tmp/'OpenBonk-diagnostic.apk'
        shutil.copyfile(obb, new_obb)
        with zipfile.ZipFile(new_obb) as z:
            repair.unique(z)
            if z.comment != b'0000002c': raise ValueError('unexpected OBB comment')
        with new_obb.open('r+b') as f: f.seek(-8, 2); f.write(b'0000000f')
        if sha(new_obb) != NEW_OBB: raise ValueError('unexpected OBB output')
        with zipfile.ZipFile(apk) as src, zipfile.ZipFile(new_obb) as oz, zipfile.ZipFile(unsigned, 'x') as dst:
            repair.unique(src)
            bsdiff = load('bsdiff_pinned', ROOT/'tools/apply-bsdiff-pinned.py')
            dex = bsdiff.apply(src.read('classes.dex'), DELTA.read_bytes(), 6662952, DEX_SHA)
            if src.read('assets/unity_obb_guid') != oz.read('unity_obb_guid') or dex.count(NEW_OBB.encode()) != 1:
                raise ValueError('launcher and data do not match')
            for info in src.infolist():
                if not repair.jar_signature(info.filename):
                    dst.writestr(copy.copy(info), dex if info.filename == 'classes.dex' else src.read(info))
        run(tools/'zipalign', '-p', '4', unsigned, aligned)
        run(tools/'apksigner', 'sign', '--ks', key, '--ks-key-alias', 'androiddebugkey', '--ks-pass', 'pass:android', '--key-pass', 'pass:android', '--out', signed, aligned)
        run(tools/'zipalign', '-c', '-p', '4', signed); run(tools/'apksigner', 'verify', signed)
        if digest.digest(signed) != NEW_APK: raise ValueError('output APK differs from tested launcher')
        report = {'kind':'local diagnostic only, not for distribution', 'apk_sha256':sha(signed), 'obb_sha256':sha(new_obb), 'replaced':['classes.dex'], 'limitations':['Game and third-party material remain.', 'Experimental DEX modification and debug signature.', 'No complete device validation.']}
        out.mkdir()
        try:
            shutil.copyfile(signed, out/signed.name); shutil.copyfile(new_obb, out/OBB)
            (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        except BaseException:
            shutil.rmtree(out); raise
    print('Applied tested control-choice launcher; local diagnostic only.')


if __name__ == '__main__': main()
