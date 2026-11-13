#!/usr/bin/env python3
"""Extract four pinned runtime entries for local research, not distribution."""
import hashlib, os, pathlib, tempfile, zipfile

ROOT=pathlib.Path(__file__).resolve().parents[2]
source=ROOT/'build/android/transaction-runtime-test/OpenBonk-diagnostic.apk'
out=ROOT/'build/android/local-runtime-compatibility.zip'
pin='870953f5294c6b26f878e901f61d1bbf50f0c4569129039ca77c3d9b8e95b967'
entries={'assets/bin/Data/boot.config':'a763a7d051c098aae40aa4e0a4a2394db4e7ad2435b97b95ac0009cb9f993509',
         'assets/bin/Data/unity_app_guid':'8211495a2635c338ba72207f2c3fb242a0d81583f5230860d896860dffc3986b',
         'assets/dexopt/baseline.prof':'c3e2d99bd84bb7ea57ea6af30f5b40caf557ba0ca3b46d75582cdbfbd7159e15',
         'lib/arm64-v8a/libswappywrapper.so':'3ad4e9a8049fbebf26cc7e5263bb92462ffdfacd4d5262283351c6538085d68a'}
with source.open('rb') as f:
 if hashlib.file_digest(f,'sha256').hexdigest()!=pin:raise ValueError('incompatible source')
with zipfile.ZipFile(source) as z:
 if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate entries')
 data={n:z.read(n) for n in entries}
if any(hashlib.sha256(data[n]).hexdigest()!=h for n,h in entries.items()):raise ValueError('entry mismatch')
if out.exists() or out.is_symlink():raise FileExistsError(out)
with tempfile.TemporaryDirectory(prefix='.compatibility-',dir=out.parent) as tmp:
 staged=pathlib.Path(tmp)/'entries.zip'
 with zipfile.ZipFile(staged,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
  for n in sorted(data):
   info=zipfile.ZipInfo(n,(2020,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
   z.writestr(info,data[n],compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 os.link(staged,out)
with out.open('rb') as f: digest=hashlib.file_digest(f,'sha256').hexdigest()
print(out,digest,'LOCAL RESEARCH ONLY')
