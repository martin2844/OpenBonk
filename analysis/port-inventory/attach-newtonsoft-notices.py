#!/usr/bin/env python3
"""Attach pinned local Unity Newtonsoft notice documents to an unsigned APK.

This is a diagnostic packaging step, not a complete third-party notice set or a
finding that the build is distributable. It does not sign or install the APK.
"""
import hashlib,json,sys,zipfile
from pathlib import Path
DOCS={
 'Unity-Newtonsoft-License.md':('LICENSE.md','a5177bfecfea04d225e4a26a631db86721a03b4080350867847e71dc26b44f85'),
 'Unity-Newtonsoft-Third-Party-Notices.md':('Third Party Notices.md','f07d2ea4af53fd1c9800b345bb4fbfa3ab0181d8c87625d6c265cd65142df097'),
}
def sha(data):return hashlib.sha256(data).hexdigest()
def has_apk_signing_block(path, central_directory_offset):
 # APK Signature Scheme v2/v3 places a signing block immediately before the
 # ZIP central directory, even when there are no META-INF/JAR signatures.
 if central_directory_offset<16:return False
 with path.open('rb') as f:
  f.seek(central_directory_offset-16)
  return f.read(16)==b'APK Sig Block 42'
if len(sys.argv)!=4:raise SystemExit('usage: attach-newtonsoft-notices.py INPUT.apk PACKAGE_DIR OUTPUT.apk')
src,package,out=map(Path,sys.argv[1:])
if out.exists() or not out.parent.is_dir() or out.resolve()==src.resolve():raise ValueError('output must not exist and parent must exist')
notices={}
for dest,(name,expected) in DOCS.items():
 data=(package/name).read_bytes()
 if sha(data)!=expected:raise ValueError('missing or incompatible package notice: '+name)
 notices['assets/ThirdPartyNotices/'+dest]=data
created=False
try:
 with zipfile.ZipFile(src) as source:
  names=source.namelist()
  if len(names)!=len(set(names)) or set(names)&set(notices):raise ValueError('duplicate notice or APK member')
  if any(n.upper().startswith('META-INF/') for n in names) or has_apk_signing_block(src,source.start_dir):raise ValueError('input must be unsigned')
  assemblies=json.loads(source.read('assets/bin/Data/ScriptingAssemblies.json'))['names'];metadata=source.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
  if 'Newtonsoft.Json.dll' not in assemblies or b'Newtonsoft.Json.dll' not in metadata:raise ValueError('expected Newtonsoft assembly evidence missing')
  with zipfile.ZipFile(out,'x',allowZip64=True) as dest:
   created=True
   for info in source.infolist():dest.writestr(info,source.read(info))
   for name,data in notices.items():
    info=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
    info.compress_type=zipfile.ZIP_DEFLATED
    info.external_attr=0o100644<<16
    dest.writestr(info,data)
 with zipfile.ZipFile(src) as before,zipfile.ZipFile(out) as after:
  if after.testzip() is not None or len(after.namelist())!=len(set(after.namelist())):raise ValueError('invalid output APK')
  if set(after.namelist())!=set(before.namelist())|set(notices):raise ValueError('unexpected APK entries')
  for name in before.namelist():
   if sha(before.read(name))!=sha(after.read(name)):raise ValueError('existing entry changed: '+name)
  for name,data in notices.items():
   if after.read(name)!=data:raise ValueError('notice content changed')
 print('Added',len(notices),'pinned Unity Newtonsoft notices; all pre-existing entries unchanged; not a complete notice set')
except Exception:
 if created:out.unlink(missing_ok=True)
 raise
