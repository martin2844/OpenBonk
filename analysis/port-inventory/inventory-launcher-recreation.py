#!/usr/bin/env python3
"""Inventory the local launcher diagnostic, retaining only required entries.

The payload contains game and third-party code and is not distributable.
"""
import hashlib,json,os,shutil,tempfile,zipfile
from pathlib import Path
root=Path(__file__).resolve().parents[2];base=root/'build/android/launcher-recreation-split-repaired';out=root/'build/android/launcher-recreation-inventories'
apk=base/'OpenBonk-diagnostic.apk';obb=base/'main.1.com.megabonk.nativeport.splittest.obb'
apk_pin='c061669f391230774908dd008658d12ece2ab95b9c5a31c7ab21c35fc9a38714';obb_pin='a7e98aa97ac67547225d9f01853240c36f2614c16a967a0132422a8ce2323d1e'
payload_names=json.loads((root/'build/android/local-split-apk-inventories/latest-direct.json').read_text())['payload_names']
if out.exists() or out.is_symlink():raise FileExistsError(out)
def digest(p):
 with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def inventory(path,expected,guid_name):
 if digest(path)!=expected:raise ValueError('incompatible source')
 with zipfile.ZipFile(path) as z:
  names=z.namelist()
  if len(names)!=len(set(names)):raise ValueError('duplicate entry')
  entries={}
  for n in sorted(names):
   info=z.getinfo(n)
   with z.open(info) as f:h=hashlib.file_digest(f,'sha256').hexdigest()
   entries[n]={'size':info.file_size,'sha256':h}
  guid=z.read(guid_name).decode('ascii')
 if digest(path)!=expected:raise ValueError('source changed while reading')
 return entries,guid
apk_entries,apk_guid=inventory(apk,apk_pin,'assets/unity_obb_guid');obb_entries,obb_guid=inventory(obb,obb_pin,'unity_obb_guid')
if apk_guid!=obb_guid or not set(payload_names).issubset(apk_entries):raise ValueError('source pair mismatch')
with tempfile.TemporaryDirectory(prefix='.launcher-hint-',dir=out.parent) as t:
 t=Path(t);stage=t/'stage';stage.mkdir()
 adoc={'version':1,'source_apk_sha256':apk_pin,'unity_obb_guid':apk_guid,'entries':apk_entries,'payload_names':payload_names}
 odoc={'version':1,'source_obb_sha256':obb_pin,'unity_obb_guid':obb_guid,'entries':obb_entries}
 for name,doc in (('apk.json',adoc),('obb.json',odoc)):(stage/name).write_text(json.dumps(doc,sort_keys=True,separators=(',',':'))+'\n')
 with zipfile.ZipFile(apk) as src,zipfile.ZipFile(stage/'payload.zip','x',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as dst:
  for name in sorted(payload_names):
   data=src.read(name);row=apk_entries[name]
   if len(data)!=row['size'] or hashlib.sha256(data).hexdigest()!=row['sha256']:raise ValueError('payload differs')
   info=zipfile.ZipInfo(name,(2020,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
   dst.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 if digest(apk)!=apk_pin:raise ValueError('APK changed while creating payload')
 out.mkdir(mode=0o700)
 try:
  for f in stage.iterdir():os.link(f,out/f.name)
 except BaseException:shutil.rmtree(out);raise
for f in sorted(out.iterdir()):print(f.name,f.stat().st_size,digest(f))
