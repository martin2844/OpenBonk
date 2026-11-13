#!/usr/bin/env python3
"""Apply control acknowledgement to a pinned private diagnostic, not a release."""
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
OLD_APK = '56c61efba14d4cc987a867a215cf56bbd39197829c4b50095bccdaeef290d98b'
NEW_APK = 'e9524f7015f9507ec66fa7f8614c7daa6e2252c1ff1c1468480d8242c5cb4aa2'
OLD_OBB = '00a50941a48f3263569b0f2ed4aad64d57c9187165ba545e5dd6aa944147e2cf'
NEW_OBB = 'b0588720eec5ff380b29e2a91baa63aa223a1d62eca942d122a24be72a59a0db'
DELTA_DIR = ROOT/'build/android/control-ack-runtime'
PATCHES = {
    'classes.dex': ('classes.bsdiff', 'b47f4ba098b454c3e5db8d0ec41004670021d633265c031bbfcec3d01ccdbd95', 'b1c5d532eb361be8602ece4553660d448f569731faad2e4194f0195cacd3874d', 6663568, 'fc13b5f77a6443f1a184d9c9c990fed19d99e8dcd28379df1e23b7e7fdf43f91'),
    'assets/bin/Data/Managed/Metadata/global-metadata.dat': ('metadata.bsdiff', 'ceb5b26085406ab20f9c7ab5cfa7bee69f5e9c3e4e38e17995a0ca5ab2a861b2', 'd46e3bb0263da61f5b627ae8bea3866518c7552daed1176c067ba03f2f8f8ff8', 10600764, 'eb3889da0b7bf2772f51290d14861433a9d180971f20e3e552a4fa917c904930'),
    'lib/arm64-v8a/libil2cpp.so': ('runtime.bsdiff', 'a0bdcce5938eec8cfeb1d732048ef11822b5cb4d556259725251f1d831d4a7a0', '850e9a88409feade5249b3f4a352baac8298228ee3054e3c4b33e39c4c9701dd', 65058968, 'c744f2ee1548c5d5c2d8f4f5c1e2cfe4f1da79683c9b5dd7bcfb995df6d68644'),
}


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
    if digest.digest(apk) != OLD_APK or sha(obb) != OLD_OBB:
        raise ValueError('incompatible diagnostic or launcher payload')
    for filename, expected, *_ in PATCHES.values():
        if sha(DELTA_DIR/filename) != expected: raise ValueError('incompatible launcher patch: '+filename)
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
            if z.comment != b'0000000f': raise ValueError('unexpected OBB comment')
        with new_obb.open('r+b') as f: f.seek(-8, 2); f.write(b'00000003')
        if sha(new_obb) != NEW_OBB: raise ValueError('unexpected OBB output')
        with zipfile.ZipFile(apk) as src, zipfile.ZipFile(new_obb) as oz, zipfile.ZipFile(unsigned, 'x') as dst:
            repair.unique(src)
            bsdiff = load('bsdiff_pinned', ROOT/'tools/apply-bsdiff-pinned.py')
            replacements = {}
            for name, (filename, _, source_sha, size, result_sha) in PATCHES.items():
                source = src.read(name)
                if hashlib.sha256(source).hexdigest() != source_sha: raise ValueError('incompatible source: '+name)
                replacements[name] = bsdiff.apply(source, (DELTA_DIR/filename).read_bytes(), size, result_sha)
            dex = replacements['classes.dex']
            if src.read('assets/unity_obb_guid') != oz.read('unity_obb_guid') or dex.count(NEW_OBB.encode()) != 1:
                raise ValueError('launcher and data do not match')
            for info in src.infolist():
                if not repair.jar_signature(info.filename):
                    dst.writestr(copy.copy(info), replacements[info.filename] if info.filename in replacements else src.read(info))
        run(tools/'zipalign', '-p', '4', unsigned, aligned)
        run(tools/'apksigner', 'sign', '--ks', key, '--ks-key-alias', 'androiddebugkey', '--ks-pass', 'pass:android', '--key-pass', 'pass:android', '--out', signed, aligned)
        run(tools/'zipalign', '-c', '-p', '4', signed); run(tools/'apksigner', 'verify', signed)
        if digest.digest(signed) != NEW_APK: raise ValueError('output APK differs from tested launcher')
        report = {'kind':'local diagnostic only, not for distribution', 'apk_sha256':sha(signed), 'obb_sha256':sha(new_obb), 'replaced':list(PATCHES), 'limitations':['Game and third-party material remain.', 'Experimental binary modification and debug signature.', 'No complete device validation.']}
        out.mkdir()
        try:
            shutil.copyfile(signed, out/signed.name); shutil.copyfile(new_obb, out/OBB)
            (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        except BaseException:
            shutil.rmtree(out); raise
    print('Applied tested control acknowledgement; local diagnostic only.')


if __name__ == '__main__': main()
