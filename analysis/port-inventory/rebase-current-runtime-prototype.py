#!/usr/bin/env python3
"""Rebase a pinned local rebuilt-data diagnostic onto a newer local runtime.

This is an experimental APK/OBB pair, not a release or user-side installer.
It retains original game data, reconstructed code and third-party dependencies.
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

OLD_APK = 'b3255411ab3e3f00001daa22c3ecd48e7f1db3c31f290bdd545237c65c45bb38'
OLD_OBB = 'ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961'
NEW_APK = '4127d391f7dd76b67ccac51318ee7facbf16fd128350290048cb815bf31f6605'
NEW_OBB = 'd794c498d2631f32307c1b0c97b9af77906991f42e07fb33ec3a0baf3ca9a5f5'
REBUILT_APK = '993b08a83c93a7b73e5269b016cae6848d32dc1d459ed74343399405a9d885d5'
REBUILT_CONTENT_SHA = None  # Optional pinned payload digest, independent of APK signature.
REBUILT_OBB = 'ca916fd870e4546c70a66428591272ef155987140495c1a0dd446a332b578cc2'
OBB_NAME = 'main.1.com.megabonk.nativeport.splittest.obb'
REPLACEMENTS = {'assets/bin/Data/Managed/Metadata/global-metadata.dat', 'assets/bin/Data/boot.config',
                'assets/bin/Data/unity_app_guid', 'assets/dexopt/baseline.prof', 'assets/dexopt/baseline.profm',
                'classes.dex', 'lib/arm64-v8a/libil2cpp.so', 'lib/arm64-v8a/libmegabonk_lua.so',
                'lib/arm64-v8a/libswappywrapper.so'}
# Optional pinned local compatibility entries; still not cleared for distribution.
OVERRIDE_PATH = None
OVERRIDE_SHA = None
OVERRIDE_HASHES = {}
OBB_INVENTORIES = {}  # source OBB SHA-256 -> (inventory path, inventory SHA-256)
APK_INVENTORIES = {}  # source APK SHA-256 -> (inventory, hash, payload, hash)


def enable_local_obb_inventories():
    global OBB_INVENTORIES
    root = Path(__file__).resolve().parents[2]/'build/android/local-split-obb-inventories'
    OBB_INVENTORIES = {
        'ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961': (root/'old.json', 'd4fb4b0da8553042474bd68664d3ba08964cf9975fea295b0da0bcba23b2ccde'),
        'd794c498d2631f32307c1b0c97b9af77906991f42e07fb33ec3a0baf3ca9a5f5': (root/'current.json', '8923485df7585d4d694ebe1c3c484c84de0ab5a0d308060c5011237c168cc1a4'),
        '3a6ce0ac78310852c1a2252fe65c267a9812b9b900e94c3e9062472a4ada2d58': (root/'transaction.json', '7f05aa60769321cd9762c8dc2886ed27e8756c8c56cc61b2d32c4d1b9f95af42')}


def enable_local_apk_inventories():
    global APK_INVENTORIES
    root=Path(__file__).resolve().parents[2]/'build/android/local-split-apk-inventories'
    APK_INVENTORIES={
        'b3255411ab3e3f00001daa22c3ecd48e7f1db3c31f290bdd545237c65c45bb38':(root/'old.json','79f7712afec39536a9c5dc3436f021764a466c60339d79e7bc460dbad2e0f69b',None,None),
        '4127d391f7dd76b67ccac51318ee7facbf16fd128350290048cb815bf31f6605':(root/'current.json','26929bb2c723072fde4802c48887453513f059cb37fc1b7c0f8ba4bde431c2a8',root/'current.zip','57a48a1d587a43bd859df9e5e81e84900dc62fa05817b38696014577adaf1941'),
        'f337676127872d1bb6dcfabb2e9e465709c691087378685b8fb207b9a0e02486':(root/'transaction.json','972cbc2cfc52d619cde2ac7a3cd0e06d738dc332e8ff6bd39420984c5332b823',root/'transaction.zip','219d80a0beb10ab4883aec3eadc7ae4f40a7ca151427e2b0f9606255f1d9ee40')}


def require(path, expected):
    if not path.is_file() or repair.sha(path) != expected:
        raise ValueError('incompatible input: ' + str(path))


class EntryHashInventory:
    """Only supports equality checks and the GUID, never returns game data."""
    def __init__(self, path, inventory_sha, source_sha):
        require(path, inventory_sha)
        document = json.loads(path.read_text())
        if document.get('version') != 1 or document.get('source_obb_sha256') != source_sha:
            raise ValueError('incompatible OBB inventory')
        self.entries = document['entries']
        self.guid = document['unity_obb_guid'].encode('ascii')
        if not isinstance(self.entries, dict) or 'unity_obb_guid' not in self.entries:
            raise ValueError('invalid OBB inventory entries')
        row = self.entries['unity_obb_guid']
        if len(self.guid) != row['size'] or hashlib.sha256(self.guid).hexdigest() != row['sha256']:
            raise ValueError('invalid OBB GUID inventory')

    def __enter__(self): return self
    def __exit__(self, *args): pass
    def namelist(self): return list(self.entries)
    def testzip(self): return None  # generation read and CRC-checked every pinned entry
    def read(self, name):
        if name == 'unity_obb_guid': return self.guid
        row = self.entries[name]
        # Size and digest represent the old entry only for equality checks.
        return row['size'], row['sha256']


def open_source_obb(path, expected):
    if OBB_INVENTORIES:
        try: inventory, inventory_sha = OBB_INVENTORIES[expected]
        except KeyError: raise ValueError('missing pinned OBB inventory')
        return EntryHashInventory(inventory, inventory_sha, expected)
    return zipfile.ZipFile(path)


class ApkEntryInventory:
    """Pinned APK inventory plus only the payload entries used by the rebase."""
    def __init__(self, expected):
        try: inventory, digest, payload, payload_digest = APK_INVENTORIES[expected]
        except KeyError: raise ValueError('missing pinned APK inventory')
        require(inventory,digest)
        doc=json.loads(inventory.read_text())
        if doc.get('version')!=1 or doc.get('source_apk_sha256')!=expected:raise ValueError('incompatible APK inventory')
        self.entries=doc['entries'];self.guid=doc['unity_obb_guid'].encode('ascii');self.payload=None
        if not isinstance(self.entries,dict) or self.signature('assets/unity_obb_guid')!=(len(self.guid),hashlib.sha256(self.guid).hexdigest()):raise ValueError('invalid APK GUID inventory')
        names=doc['payload_names']
        if payload is None:
            if names:raise ValueError('unexpected APK payload')
        else:
            require(payload,payload_digest)
            self.payload=zipfile.ZipFile(payload)
            try:
                repair.unique(self.payload)
                if len(names)!=len(set(names)) or set(self.payload.namelist())!=set(names):raise ValueError('unexpected APK payload inventory')
                for name in names:
                    data=self.payload.read(name)
                    if (len(data),hashlib.sha256(data).hexdigest())!=self.signature(name):raise ValueError('APK payload differs: '+name)
            except BaseException:
                self.payload.close();raise
    def __enter__(self):return self
    def __exit__(self,*args):
        if self.payload:self.payload.close()
    def namelist(self):return list(self.entries)
    def testzip(self):return None  # pinned source and payload were read and CRC-checked
    def signature(self,name):
        row=self.entries[name];return row['size'],row['sha256']
    def read(self,name):
        if name=='assets/unity_obb_guid':return self.guid
        if self.payload is None:raise ValueError('APK entry unavailable: '+name)
        return self.payload.read(name)


def open_source_apk(path, expected):
    return ApkEntryInventory(expected) if APK_INVENTORIES else zipfile.ZipFile(path)


def entry_signature(archive,name):
    if isinstance(archive,ApkEntryInventory):return archive.signature(name)
    data=archive.read(name)
    return len(data),hashlib.sha256(data).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('old_split', type=Path); p.add_argument('new_split', type=Path)
    p.add_argument('rebuilt', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); out = a.output.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir():
        p.error('output must be a new directory')
    for source in (a.old_split, a.new_split, a.rebuilt):
        s = source.resolve()
        if out.resolve().is_relative_to(s) or s.is_relative_to(out.resolve()):
            p.error('output must be separate from inputs')
    paths = [(a.old_split/'OpenBonk-diagnostic.apk', OLD_APK), (a.old_split/OBB_NAME, OLD_OBB),
             (a.new_split/'OpenBonk-diagnostic.apk', NEW_APK), (a.new_split/OBB_NAME, NEW_OBB),
             (a.rebuilt/'OpenBonk-diagnostic.apk', REBUILT_APK), (a.rebuilt/OBB_NAME, REBUILT_OBB)]
    for index, (path, expected) in enumerate(paths):
        if (OBB_INVENTORIES and index in (1, 3)) or (APK_INVENTORIES and index in (0, 2)): continue
        if index == 4 and REBUILT_CONTENT_SHA:
            spec = importlib.util.spec_from_file_location('apk_content_digest', Path(__file__).resolve().parents[2]/'tools/apk-content-digest.py')
            content = importlib.util.module_from_spec(spec); spec.loader.exec_module(content)
            if not path.is_file() or content.digest(path) != REBUILT_CONTENT_SHA:
                raise ValueError('incompatible rebuilt APK payload: '+str(path))
        else:
            require(path, expected)
    overrides = {}
    if OVERRIDE_PATH is not None:
        require(OVERRIDE_PATH, OVERRIDE_SHA)
        with zipfile.ZipFile(OVERRIDE_PATH) as z:
            repair.unique(z)
            if set(z.namelist()) != set(OVERRIDE_HASHES) or not set(OVERRIDE_HASHES).issubset(REPLACEMENTS-{'classes.dex'}):
                raise ValueError('unexpected compatibility entry inventory')
            for name, expected in OVERRIDE_HASHES.items():
                data=z.read(name)
                if hashlib.sha256(data).hexdigest()!=expected: raise ValueError('compatibility entry differs: '+name)
                overrides[name]=data
    unity = Path(os.environ.get('OPENBONK_UNITY_EDITOR', str(Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor'))).expanduser()/'Data/PlaybackEngines/AndroidPlayer'
    tools = Path(os.environ.get('OPENBONK_ANDROID_SDK_ROOT', str(unity/'SDK'))).expanduser()/'build-tools/34.0.0'
    java = Path(os.environ.get('OPENBONK_JAVA_HOME', str(unity/'OpenJDK'))).expanduser()
    key = Path(os.environ.get('OPENBONK_DEBUG_KEYSTORE', str(Path.home()/'.android/debug.keystore'))).expanduser()
    for path in (tools/'zipalign', tools/'apksigner', java/'bin/java', key):
        if not path.is_file(): p.error('missing prerequisite: '+str(path))
    runner = os.environ.get('OPENBONK_TOOL_RUNNER', 'steam-run' if shutil.which('steam-run') else 'none')
    if runner != 'none' and not shutil.which(runner): p.error('configured runner missing: '+runner)
    prefix = [] if runner == 'none' else [runner]
    env = os.environ.copy(); env['JAVA_HOME'] = str(java); env['PATH'] = str(java/'bin')+os.pathsep+env.get('PATH','')
    if REBUILT_CONTENT_SHA:
        subprocess.run(prefix+[str(tools/'zipalign'), '-c', '-p', '4', str(paths[4][0])], env=env, check=True)
        subprocess.run(prefix+[str(tools/'apksigner'), 'verify', str(paths[4][0])], env=env, check=True)
    with tempfile.TemporaryDirectory(prefix='.openbonk-runtime-rebase-', dir=out.parent) as tmp:
        tmp = Path(tmp); obb = tmp/OBB_NAME; unsigned = tmp/'unsigned.apk'; aligned = tmp/'aligned.apk'; signed = tmp/'OpenBonk-diagnostic.apk'
        with open_source_apk(paths[0][0], OLD_APK) as old, open_source_obb(paths[1][0], OLD_OBB) as old_data, open_source_apk(paths[2][0], NEW_APK) as new, open_source_obb(paths[3][0], NEW_OBB) as new_data, zipfile.ZipFile(paths[4][0]) as base, zipfile.ZipFile(paths[5][0]) as base_data:
            for z in (old, old_data, new, new_data, base, base_data): repair.unique(z)
            if set(old_data.namelist()) != set(new_data.namelist()): raise ValueError('split OBB inventories differ')
            if any(old_data.read(n) != new_data.read(n) for n in old_data.namelist() if n != 'unity_obb_guid'):
                raise ValueError('split OBB data differs beyond GUID')
            if old.read('assets/unity_obb_guid') != old_data.read('unity_obb_guid') or new.read('assets/unity_obb_guid') != new_data.read('unity_obb_guid') or base.read('assets/unity_obb_guid') != base_data.read('unity_obb_guid'):
                raise ValueError('input GUID mismatch')
            old_names = {n for n in old.namelist() if not repair.jar_signature(n)}
            new_names = {n for n in new.namelist() if not repair.jar_signature(n)}
            if old_names != new_names: raise ValueError('split APK inventory differs')
            changed = {n for n in old_names if entry_signature(old,n) != entry_signature(new,n)}
            if changed != REPLACEMENTS | {'assets/unity_obb_guid'}: raise ValueError('unexpected split APK changes: '+str(sorted(changed)))
            for name in REPLACEMENTS - {'classes.dex'}:
                if name not in base.namelist() or entry_signature(base,name) != entry_signature(old,name): raise ValueError('rebuilt runtime differs from old split: '+name)
            dex = new.read('classes.dex'); previous, following = repair.dex_bounds(dex, NEW_OBB.encode('ascii'))
            shutil.copyfile(paths[5][0], obb)
            with zipfile.ZipFile(obb, 'a') as z: z.comment = b'00000000'
            h = hashlib.sha256()
            with obb.open('rb') as f:
                remaining = obb.stat().st_size - 8
                while remaining:
                    block = f.read(min(1048576, remaining))
                    if not block: raise ValueError('short OBB')
                    h.update(block); remaining -= len(block)
            for nonce in range(1_000_000):
                comment = f'{nonce:08x}'.encode(); candidate = h.copy(); candidate.update(comment)
                target = candidate.hexdigest().encode('ascii')
                if previous < target < following and target != NEW_OBB.encode(): break
            else: raise ValueError('cannot preserve DEX string order')
            with obb.open('r+b') as f: f.seek(-8, 2); f.write(comment)
            if repair.sha(obb).encode() != target: raise ValueError('OBB digest mismatch')
            patched = bytearray(dex.replace(NEW_OBB.encode(), target)); patched[12:32] = hashlib.sha1(patched[32:]).digest()
            patched[8:12] = (zlib.adler32(patched[12:]) & 0xffffffff).to_bytes(4, 'little')
            with zipfile.ZipFile(unsigned, 'x', allowZip64=True) as dst:
                for info in base.infolist():
                    name = info.filename
                    if repair.jar_signature(name): continue
                    data = bytes(patched) if name == 'classes.dex' else overrides[name] if name in overrides else new.read(name) if name in REPLACEMENTS else base.read(info)
                    dst.writestr(copy.copy(info), data)
        def run(args): subprocess.run(prefix + list(map(str,args)), env=env, check=True)
        run([tools/'zipalign','-p','4',unsigned,aligned])
        run([tools/'apksigner','sign','--ks',key,'--ks-key-alias','androiddebugkey','--ks-pass','pass:android','--key-pass','pass:android','--out',signed,aligned])
        run([tools/'zipalign','-c','4',signed]); run([tools/'apksigner','verify','--verbose',signed])
        with zipfile.ZipFile(signed) as az, zipfile.ZipFile(obb) as oz, zipfile.ZipFile(paths[5][0]) as old_final, open_source_apk(paths[2][0], NEW_APK) as new_runtime:
            for z in (az,oz): repair.unique(z)
            if az.read('assets/unity_obb_guid') != oz.read('unity_obb_guid') or az.read('classes.dex').count(target) != 1:
                raise ValueError('repaired pair mismatch')
            if set(oz.namelist()) != set(old_final.namelist()) or any(oz.read(n) != old_final.read(n) for n in oz.namelist()):
                raise ValueError('rebuilt OBB content changed')
            for name in REPLACEMENTS - {'classes.dex'}:
                if az.read(name) != (overrides[name] if name in overrides else new_runtime.read(name)): raise ValueError('runtime entry differs')
        report = {'kind':'local diagnostic only, not for distribution', 'apk_sha256':repair.sha(signed), 'obb_sha256':repair.sha(obb),
                  'replaced':sorted(REPLACEMENTS), 'new_source_apk_sha256':NEW_APK, 'old_rebuilt_obb_sha256':REBUILT_OBB,
                  'limitations':['Contains original game data and third-party code.', 'Experimental DEX patch and debug signature.', 'Runtime and serialization compatibility not established.']}
        (tmp/'report.json').write_text(json.dumps(report, indent=2)+'\n')
        out.mkdir()
        try:
            for f in (signed, obb, tmp/'report.json'): shutil.copyfile(f,out/f.name)
        except BaseException:
            shutil.rmtree(out); raise
        print(json.dumps(report, indent=2))


if __name__ == '__main__': main()
