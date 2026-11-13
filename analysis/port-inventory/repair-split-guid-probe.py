#!/usr/bin/env python3
"""Repair one local two-pass diagnostic pair; not a release or installer.

Use the second OBB and APK (matching Unity GUIDs); retarget the one DEX hash
to that OBB after choosing a ZIP comment that preserves DEX string ordering.
The original files are never changed. Output uses the local debug key.
"""
import argparse
import copy
import hashlib
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import tempfile
import zipfile
import zlib


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()


def unique(z):
    if len(z.namelist()) != len(set(z.namelist())) or z.testzip() is not None:
        raise ValueError('invalid or duplicate archive entries')


def jar_signature(name):
    upper = name.upper()
    if not upper.startswith('META-INF/'): return False
    leaf = upper[len('META-INF/'):]
    return '/' not in leaf and (leaf == 'MANIFEST.MF' or leaf.endswith(('.SF', '.RSA', '.DSA', '.EC')) or leaf.startswith('SIG-'))


def dex_bounds(dex, old):
    if not dex.startswith(b'dex\n') or len(dex) < 112:
        raise ValueError('unsupported DEX')
    if dex[12:32] != hashlib.sha1(dex[32:]).digest() or int.from_bytes(dex[8:12], 'little') != zlib.adler32(dex[12:]) & 0xffffffff:
        raise ValueError('invalid DEX checksums')
    count, offset = struct.unpack_from('<II', dex, 56)
    offsets = struct.unpack_from(f'<{count}I', dex, offset)
    def string(pos):
        while dex[pos] & 128: pos += 1
        pos += 1
        return dex[pos:dex.index(b'\0', pos)]
    found = [i for i, pos in enumerate(offsets) if string(pos) == old]
    if len(found) != 1 or not 0 < found[0] < count - 1 or dex.count(old) != 1:
        raise ValueError('expected one bounded OBB hash in DEX')
    i = found[0]
    return string(offsets[i - 1]), string(offsets[i + 1])


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('input', type=Path, help='directory with first.obb, second.obb, second.apk')
    p.add_argument('output', type=Path, help='new diagnostic output directory')
    a = p.parse_args(); out = a.output.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be a new directory')
    first, second, apk = (a.input / n for n in ('first.obb', 'second.obb', 'second.apk'))
    unity = Path.home() / 'Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer'
    build_tools = unity / 'SDK/build-tools/34.0.0'; java = unity / 'OpenJDK'; key = Path.home() / '.android/debug.keystore'
    for path in (first, second, apk, build_tools/'zipalign', build_tools/'apksigner', java/'bin/java', key):
        if not path.is_file(): p.error('missing prerequisite: ' + str(path))
    if not shutil.which('steam-run'): p.error('steam-run missing')
    old_hash = sha(first).encode('ascii')
    env = os.environ.copy(); env['JAVA_HOME'] = str(java); env['PATH'] = str(java/'bin') + os.pathsep + env.get('PATH', '')
    with tempfile.TemporaryDirectory(prefix='.openbonk-repair-', dir=out.parent) as tmp:
        tmp = Path(tmp); obb = tmp/'main.1.com.megabonk.nativeport.splittest.obb'; unsigned = tmp/'unsigned.apk'; aligned = tmp/'aligned.apk'; signed = tmp/'OpenBonk-diagnostic.apk'
        with zipfile.ZipFile(apk) as az, zipfile.ZipFile(first) as fz, zipfile.ZipFile(second) as sz:
            for z in (az, fz, sz): unique(z)
            guid = az.read('assets/unity_obb_guid')
            if not guid or guid != sz.read('unity_obb_guid') or guid == fz.read('unity_obb_guid'):
                raise ValueError('unexpected two-pass GUID relationship')
            dex = az.read('classes.dex'); previous, following = dex_bounds(dex, old_hash)
            shutil.copyfile(second, obb)
            with zipfile.ZipFile(obb, 'a') as oz: oz.comment = b'00000000'
            prefix = hashlib.sha256()
            with obb.open('rb') as source:
                remaining = obb.stat().st_size - 8
                while remaining:
                    block = source.read(min(1024 * 1024, remaining))
                    if not block: raise ValueError('short OBB')
                    prefix.update(block); remaining -= len(block)
            for nonce in range(1_000_000):
                comment = f'{nonce:08x}'.encode('ascii'); h = prefix.copy(); h.update(comment)
                new_hash = h.hexdigest().encode('ascii')
                if previous < new_hash < following and new_hash != old_hash: break
            else: raise ValueError('could not preserve DEX string ordering')
            with obb.open('r+b') as dest: dest.seek(-8, 2); dest.write(comment)
            if sha(obb).encode('ascii') != new_hash: raise ValueError('OBB digest mismatch')
            updated = bytearray(dex.replace(old_hash, new_hash))
            updated[12:32] = hashlib.sha1(updated[32:]).digest()
            updated[8:12] = (zlib.adler32(updated[12:]) & 0xffffffff).to_bytes(4, 'little')
            with zipfile.ZipFile(unsigned, 'x', allowZip64=True) as dest:
                for info in az.infolist():
                    if jar_signature(info.filename): continue
                    data = bytes(updated) if info.filename == 'classes.dex' else az.read(info)
                    dest.writestr(copy.copy(info), data)
        def run(args): subprocess.run(['steam-run', *map(str, args)], env=env, check=True)
        run([build_tools/'zipalign', '-p', '4', unsigned, aligned])
        run([build_tools/'apksigner', 'sign', '--ks', key, '--ks-key-alias', 'androiddebugkey', '--ks-pass', 'pass:android', '--key-pass', 'pass:android', '--out', signed, aligned])
        run([build_tools/'zipalign', '-c', '4', signed]); run([build_tools/'apksigner', 'verify', '--verbose', signed])
        with zipfile.ZipFile(signed) as az, zipfile.ZipFile(obb) as oz:
            unique(az); unique(oz)
            if az.read('assets/unity_obb_guid') != oz.read('unity_obb_guid') or az.read('classes.dex').count(new_hash) != 1 or old_hash in az.read('classes.dex'):
                raise ValueError('final APK/OBB relationship failed')
        report = {'kind': 'local diagnostic only, not for distribution', 'input_apk_sha256': sha(apk), 'first_obb_sha256': old_hash.decode(), 'second_obb_sha256': sha(second), 'apk_sha256': sha(signed), 'obb_sha256': sha(obb), 'unity_obb_guid': guid.decode('ascii'), 'zip_comment': comment.decode('ascii'), 'limitations': ['Contains game and third-party content.', 'Experimental DEX modification and debug signature.', 'Not yet tested on physical Android devices.']}
        (tmp/'report.json').write_text(json.dumps(report, indent=2)+'\n')
        out.mkdir()
        try:
            for f in (signed, obb, tmp/'report.json'): shutil.copyfile(f, out/f.name)
        except BaseException:
            shutil.rmtree(out)
            raise
        print(json.dumps(report, indent=2))


if __name__ == '__main__': main()
