#!/usr/bin/env python3
"""Replace only a blank splash asset in an unsigned local diagnostic APK."""
import copy
import hashlib
import importlib.util
from pathlib import Path
import sys
import zipfile

spec=importlib.util.spec_from_file_location('repair', Path(__file__).resolve().parents[1]/'port-inventory/repair-split-guid-probe.py')
repair=importlib.util.module_from_spec(spec);spec.loader.exec_module(repair)
if len(sys.argv)!=4: raise SystemExit('usage: patch-current-splash-apk.py UNSIGNED.apk RESTORED_ASSET OUTPUT.apk')
source, asset, output = map(Path,sys.argv[1:])
if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be new')
replacement=asset.read_bytes()
if hashlib.sha256(replacement).hexdigest()!='a520ab88ffb10a0800028c84f18e8fb8b04fedcbe4c6474cd511a7dc3d929168': raise ValueError('incompatible restored asset')
created=False
try:
    with zipfile.ZipFile(source) as z:
        names=z.namelist()
        if len(names)!=len(set(names)) or any(repair.jar_signature(n) for n in names): raise ValueError('signed or duplicate input')
        with source.open('rb') as f:
            f.seek(max(0,z.start_dir-16));magic=f.read(16)
        if magic==b'APK Sig Block 42': raise ValueError('input must be unsigned')
        prefix='assets/bin/Data/globalgamemanagers.assets.split'
        parts=sorted((n for n in names if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
        if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))): raise ValueError('invalid asset fragments')
        old=b''.join(z.read(n) for n in parts)
        if hashlib.sha256(old).hexdigest()!='bcc84f47ac5c5f36932c38e26ab49dc6f744d8c58d94e11b231fb3b63be61383' or len(old)!=len(replacement):
            raise ValueError('incompatible blank asset')
        chunks={};offset=0
        for name in parts:
            size=z.getinfo(name).file_size;chunks[name]=replacement[offset:offset+size];offset+=size
        with zipfile.ZipFile(output,'x',allowZip64=True) as dest:
            created=True
            for info in z.infolist():dest.writestr(copy.copy(info),chunks.get(info.filename,z.read(info)))
            dest.comment=z.comment
    with zipfile.ZipFile(source) as a,zipfile.ZipFile(output) as b:
        if a.namelist()!=b.namelist() or b.testzip() is not None:raise ValueError('output archive invalid')
        for name in a.namelist():
            if b.read(name)!=(chunks[name] if name in chunks else a.read(name)):raise ValueError('unexpected change: '+name)
    print('Restored',len(parts),'splash asset fragments; local unsigned diagnostic only')
except BaseException:
    if created:output.unlink(missing_ok=True)
    raise
