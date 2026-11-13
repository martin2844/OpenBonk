#!/usr/bin/env python3
"""Create a local diagnostic base with the current Unity splash payload blank.

Other game and third-party data and code remain. Not a distributable package.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import zipfile

APK = 'd20c58a2548196270e57b6e0392b4a103e09014ee4f01d4f4b970bb2924026a2'
OBB = 'ca41168e07591229b061a7dfa8c46d70e56a455e4de1e57d5dd00425d09ae43d'
ORIGINAL = '349ba8a5672bf84b982f1eda17ecab568804c78d5c49e577f3330891515fb081'
BLANK = 'bcc84f47ac5c5f36932c38e26ab49dc6f744d8c58d94e11b231fb3b63be61383'
spec = importlib.util.spec_from_file_location('repair', Path(__file__).with_name('repair-split-guid-probe.py'))
repair = importlib.util.module_from_spec(spec); spec.loader.exec_module(repair)


def digest(b): return hashlib.sha256(b).hexdigest()
def sha(p):
    with p.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('base', type=Path); p.add_argument('template', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); out = a.output.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    for source in (a.base, a.template):
        source = source.resolve()
        if out.resolve().is_relative_to(source) or source.is_relative_to(out.resolve()): p.error('output must be separate from inputs')
    apk, obb = a.base/'base.apk', a.base/'base.obb'
    if sha(apk) != APK or sha(obb) != OBB: raise ValueError('incompatible base')
    manifest = json.loads((a.template/'manifest.json').read_text()); blank = (a.template/'globalgamemanagers.assets').read_bytes()
    if (manifest.get('apk_sha256') != APK or manifest.get('original_sha256') != ORIGINAL
            or manifest.get('template_sha256') != BLANK or digest(blank) != BLANK): raise ValueError('incompatible template')
    with tempfile.TemporaryDirectory(prefix='.openbonk-splash-base-', dir=out.parent) as tmp:
        tmp = Path(tmp); new_apk = tmp/'base.apk'; chunks = {}
        with zipfile.ZipFile(apk) as source:
            names = source.namelist()
            if len(names) != len(set(names)) or any(repair.jar_signature(n) for n in names): raise ValueError('base is signed or has duplicate entries')
            with apk.open('rb') as f:
                f.seek(max(0, source.start_dir - 16)); magic = f.read(16)
            if magic == b'APK Sig Block 42': raise ValueError('base has APK signing block')
            prefix='assets/bin/Data/globalgamemanagers.assets.split'
            parts=sorted((n for n in names if n.startswith(prefix)), key=lambda n:int(n[len(prefix):]))
            if not parts or [int(n[len(prefix):]) for n in parts] != list(range(len(parts))): raise ValueError('invalid split asset')
            original=b''.join(source.read(n) for n in parts)
            start, size = manifest['offset'], manifest['size']
            if (digest(original) != ORIGINAL or len(original) != len(blank) or size != 2796240 or start < 0
                    or start+size > len(original) or original[:start] != blank[:start]
                    or original[start+size:] != blank[start+size:] or any(blank[start:start+size])):
                raise ValueError('template changes more than the splash payload')
            pos=0
            for name in parts:
                size=source.getinfo(name).file_size; chunks[name]=blank[pos:pos+size]; pos+=size
            with zipfile.ZipFile(new_apk, 'x', allowZip64=True) as dst:
                for info in source.infolist(): dst.writestr(copy.copy(info), chunks.get(info.filename, source.read(info)))
                dst.comment=source.comment
        with zipfile.ZipFile(apk) as before, zipfile.ZipFile(new_apk) as after:
            if before.namelist() != after.namelist() or after.testzip() is not None: raise ValueError('invalid output archive')
            for n in before.namelist():
                if after.read(n) != (chunks[n] if n in chunks else before.read(n)): raise ValueError('unexpected APK change: '+n)
        report={'kind':'local research only, not for distribution', 'apk_sha256':sha(new_apk), 'obb_sha256':OBB,
                'splash_template_sha256':BLANK, 'selected_bytes':manifest['size'],
                'limitations':['Other game, Unity and third-party material remains.', 'Not a distributable base.']}
        out.mkdir(mode=0o700)
        try:
            for path in a.base.iterdir():
                if path.name == 'base.apk': shutil.copyfile(new_apk, out/path.name)
                elif path.is_file() and path.name != 'report.json': shutil.copyfile(path, out/path.name)
            shutil.copyfile(a.template/'manifest.json', out/'splash-manifest.json')
            (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        except BaseException:
            shutil.rmtree(out); raise
    print(json.dumps(report,indent=2))


if __name__ == '__main__': main()
