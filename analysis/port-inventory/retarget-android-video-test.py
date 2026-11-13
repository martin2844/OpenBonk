#!/usr/bin/env python3
"""Make a local APK to test one Android-reconstructed OBB; not for sharing."""
import argparse
import copy
import importlib.util
from pathlib import Path
import os
import shutil
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module

repair = load('repair', ROOT/'analysis/port-inventory/repair-split-guid-probe.py')
rebase = load('rebase', ROOT/'analysis/port-inventory/rebase-transaction-launcher-prototype.py')
content = load('content', ROOT/'tools/apk-content-digest.py')
OLD_APK = '3211adfea4ac9ad4569ed99b2a02498adfda92ab3829adba98508a9abbd97a34'
OLD_OBB = 'e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691'
NEW_OBB = 'e845f267cfa96d70299fbb3c9516c5906db36ba94f85515b4bc731929361efe3'

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('apk', type=Path); p.add_argument('obb', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); apk=a.apk.resolve(strict=True); obb=a.obb.resolve(strict=True); out=a.output.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    if content.digest(apk)!=OLD_APK or repair.sha(obb)!=NEW_OBB: raise ValueError('incompatible input')
    unity=Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer'
    bt=unity/'SDK/build-tools/34.0.0'; java=unity/'OpenJDK'; key=Path.home()/'.android/debug.keystore'
    env=os.environ.copy();env['JAVA_HOME']=str(java);env['PATH']=str(java/'bin')+os.pathsep+env.get('PATH','')
    with tempfile.TemporaryDirectory(prefix='.openbonk-video-test-',dir=out.parent) as t:
        t=Path(t); unsigned=t/'unsigned.apk';aligned=t/'aligned.apk';signed=t/'signed.apk'
        with zipfile.ZipFile(apk) as az, zipfile.ZipFile(obb) as oz, zipfile.ZipFile(unsigned,'x') as dst:
            repair.unique(az);repair.unique(oz)
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('OBB GUID differs')
            dex=rebase.patch(az.read('classes.dex'),OLD_OBB.encode(),NEW_OBB.encode())
            for info in az.infolist():
                if not repair.jar_signature(info.filename):dst.writestr(copy.copy(info),dex if info.filename=='classes.dex' else az.read(info))
        def run(*args):subprocess.run(['steam-run',*map(str,args)],env=env,check=True)
        run(bt/'zipalign','-p','4',unsigned,aligned)
        run(bt/'apksigner','sign','--ks',key,'--ks-key-alias','androiddebugkey','--ks-pass','pass:android','--key-pass','pass:android','--out',signed,aligned)
        run(bt/'apksigner','verify',signed)
        with zipfile.ZipFile(signed) as result,zipfile.ZipFile(apk) as original:
            repair.unique(result)
            for name in original.namelist():
                if not repair.jar_signature(name) and name!='classes.dex' and original.read(name)!=result.read(name):raise ValueError('unrelated APK content changed')
        os.link(signed,out)
    print('Local video-stage test APK:',out,'SHA-256',repair.sha(out))

if __name__=='__main__':main()
