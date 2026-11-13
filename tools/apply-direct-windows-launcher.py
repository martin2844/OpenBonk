#!/usr/bin/env python3
"""Apply the Windows-aware launcher to a pinned private diagnostic, not a release."""
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
OLD_APK = '557d14a4632e6c08004ebad37204b804b17cacbd855fb6c0e579385044c3fe38'
NEW_APK = '92f2c58d6d8eb82b5b32114ba84cb191ecabee03d922c321869843a2b91a251a'
OLD_OBB = 'e13453698c86d8082a7c4183a905a9f189cf9d660257b85a062f160044538378'
NEW_OBB = 'abea33cdc20042f03f434bd1eb554cd095e7a5f2e8162fa9bc8038cbdf539fb0'
DELTA_DIR = ROOT/'build/android/direct-windows-runtime'
PATCHES = {
    'classes.dex': ('classes.bsdiff', '00f6a99f2626460ac0b3634ba4ccab0b4a86999143cef6f3c1997ba8a26112df', 'd376552781d950c395e83ce35889db1ef56ce3f18b5e914066a183e6e7582ffa', 6665904, 'ebd688bb1bfd53dc847d0ca00222da73c03bf2ff2c83e4ec2fae6185dd3a6b38'),
    'assets/bin/Data/Managed/Metadata/global-metadata.dat': ('metadata.bsdiff', '10757794ce292d5def0573abb5e1440e2602b5be16b6641276e235964ef912e1', '20b3a600f8c94586f72da73b1a7e6ee335977ee567de4a76aacd7083e79d895d', 10601192, '8ece60c5c79a96361820f3a79be9cbd83f05354f38e9d8e6e6cda1022e23d8ba'),
    'lib/arm64-v8a/libil2cpp.so': ('runtime.bsdiff', 'f1afbe5e78b2b6133707d5daa108b002bc7d77a6d715de436466584be0edf7fd', 'cdcc2599c40f1145d422c88281b60e7972e8ddc47607d5bdd8c27e23014ef094', 65060176, 'd9ee86b3710971bab9ac82b4031ff00f21f562c5f6e500e447676d8866a49b1c'),
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
            if z.comment != b'0000001a': raise ValueError('unexpected OBB comment')
        with new_obb.open('r+b') as f: f.seek(-8, 2); f.write(b'00000710')
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
    print('Applied tested direct Windows ZIP launcher; local diagnostic only.')


if __name__ == '__main__': main()
