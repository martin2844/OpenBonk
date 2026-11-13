#!/usr/bin/env python3
"""Make local APK entry inventories and minimal runtime payloads for research.

The payloads include game code and third-party material and are not distributable.
"""
import hashlib, json, os, shutil, tempfile, zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BUILD=ROOT/'build/android';APK='OpenBonk-diagnostic.apk'
SOURCES={'old':('no-rewired-split-repaired-v2','b3255411ab3e3f00001daa22c3ecd48e7f1db3c31f290bdd545237c65c45bb38'),
 'current':('no-rewired-current-split-repaired','4127d391f7dd76b67ccac51318ee7facbf16fd128350290048cb815bf31f6605'),
 'transaction':('transaction-split-repaired','f337676127872d1bb6dcfabb2e9e465709c691087378685b8fb207b9a0e02486')}
COMMON={'assets/bin/Data/Managed/Metadata/global-metadata.dat','assets/bin/Data/boot.config','assets/bin/Data/unity_app_guid',
 'assets/dexopt/baseline.prof','assets/dexopt/baseline.profm','classes.dex','lib/arm64-v8a/libil2cpp.so','lib/arm64-v8a/libswappywrapper.so'}
SELECT={'old':set(),'current':COMMON|{'lib/arm64-v8a/libmegabonk_lua.so'},'transaction':COMMON|{'assets/bin/Data/RuntimeInitializeOnLoads.json'}}
out=BUILD/'local-split-apk-inventories'
if out.exists() or out.is_symlink():raise FileExistsError(out)
with tempfile.TemporaryDirectory(prefix='.apk-inventory-',dir=BUILD) as tmp:
 stage=Path(tmp)/'inventory';stage.mkdir()
 for label,(folder,pin) in SOURCES.items():
  path=BUILD/folder/APK
  with path.open('rb') as f:
   if hashlib.file_digest(f,'sha256').hexdigest()!=pin:raise ValueError('unexpected APK: '+label)
  with zipfile.ZipFile(path) as z:
   names=z.namelist()
   if len(names)!=len(set(names)) or not SELECT[label].issubset(names):raise ValueError('invalid APK inventory')
   entries={};payload={}
   for name in sorted(names):
    info=z.getinfo(name)
    with z.open(info) as f:h=hashlib.file_digest(f,'sha256').hexdigest()
    entries[name]={'size':info.file_size,'sha256':h}
    if name in SELECT[label]:payload[name]=z.read(name)
   guid=z.read('assets/unity_obb_guid').decode('ascii')
  with path.open('rb') as f:
   if hashlib.file_digest(f,'sha256').hexdigest()!=pin:raise ValueError('APK changed while reading')
  if label!='old':
   with zipfile.ZipFile(stage/(label+'.zip'),'x',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for name,data in sorted(payload.items()):
     info=zipfile.ZipInfo(name,(2020,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
     z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
  doc={'version':1,'source_apk_sha256':pin,'unity_obb_guid':guid,'entries':entries,'payload_names':sorted(payload)}
  (stage/(label+'.json')).write_text(json.dumps(doc,sort_keys=True,separators=(',',':'))+'\n')
  if label=='transaction':
   # The direct path also needs Lua, which was unchanged in the previous
   # intermediate-runtime rebase and thus absent from that payload.
   with zipfile.ZipFile(path) as z: lua=z.read('lib/arm64-v8a/libmegabonk_lua.so')
   row=entries['lib/arm64-v8a/libmegabonk_lua.so']
   if (len(lua),hashlib.sha256(lua).hexdigest())!=(row['size'],row['sha256']):raise ValueError('Lua payload changed')
   latest=dict(payload);latest['lib/arm64-v8a/libmegabonk_lua.so']=lua
   with zipfile.ZipFile(stage/'latest-direct.zip','x',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for name,data in sorted(latest.items()):
     info=zipfile.ZipInfo(name,(2020,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
     z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
   doc['payload_names']=sorted(latest)
   (stage/'latest-direct.json').write_text(json.dumps(doc,sort_keys=True,separators=(',',':'))+'\n')
 for path in sorted(stage.iterdir()):
  with path.open('rb') as f:print(path.name,path.stat().st_size,hashlib.file_digest(f,'sha256').hexdigest())
 out.mkdir(mode=0o700)
 try:
  for path in stage.iterdir():os.link(path,out/path.name)
 except BaseException:
  shutil.rmtree(out);raise
