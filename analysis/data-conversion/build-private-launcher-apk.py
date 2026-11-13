#!/usr/bin/env python3
"""Stage a local-only launcher test APK; signing and rights review are separate."""
import argparse,hashlib,zipfile
from pathlib import Path
BASE_SHA='0bf818e3978f4363623931d5636eee73a9ac5458b6aa52e0458ebfd9495043d9'
NOTICES=Path(__file__).resolve().parents[2]/'third_party/openbonk-texture-notices'
STRIPPED_SHA={'libgame.so':'2962415ebbdb0a95b68813aa4f07dcd47b29ce1cd5514e6b8fd3712f086a6d13','libswappywrapper.so':'94364670b52ba3f518166b43fbc189dc82af7df6a6a127f19b1c33bb9eca84da','libil2cpp.so':'badd7f169b112450f0676a9e7f396feb661e44bc9f76c44694ec493ff4da4931'}
def sha(path):
 with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('base',type=Path);p.add_argument('prototype',type=Path);p.add_argument('output',type=Path);p.add_argument('--stripped-libs',type=Path,required=True);a=p.parse_args()
 if not a.base.is_file() or sha(a.base)!=BASE_SHA:raise ValueError('incompatible private base APK')
 if not a.prototype.is_file() or a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():raise ValueError('invalid prototype or output')
 stripped={}
 for name,expected_sha in STRIPPED_SHA.items():
  path=a.stripped_libs/name
  if path.is_symlink() or not path.is_file() or sha(path)!=expected_sha:raise ValueError('stripped library differs: '+name)
  stripped['lib/arm64-v8a/'+name]=path.read_bytes()
 with zipfile.ZipFile(a.base) as source,zipfile.ZipFile(a.prototype) as built:
  sn=source.namelist();bn=built.namelist()
  if len(sn)!=len(set(sn)) or len(bn)!=len(set(bn)):raise ValueError('duplicate APK entries')
  additions={n for n in bn if n.startswith('assets/openbonk-diagnostic/') or n in ('lib/arm64-v8a/libopenbonk_texture.so','lib/arm64-v8a/libopenbonk_zlib.so')}
  notice_names={'README.md','bcdec-dual-license.txt','crn_decomp.h-notice.txt','crnlib.h-notice.txt','etcpak-BSD-3-Clause.txt','etcpak-wrapper-MIT.txt','texture2ddecoder-MIT.txt','zlib-1.3.2-license.txt','Lua-5.4.9.txt'}
  expected={'assets/openbonk-diagnostic/shared0.zip','assets/openbonk-diagnostic/base.obb','assets/openbonk-diagnostic/marker','assets/openbonk-diagnostic/plans.zip','assets/openbonk-diagnostic/openbonk_mascot.png','lib/arm64-v8a/libopenbonk_texture.so','lib/arm64-v8a/libopenbonk_zlib.so'}|{'assets/openbonk-diagnostic/notices/'+n for n in notice_names}
  if additions!=expected or 'classes.dex' not in bn or 'classes.dex' not in sn:raise ValueError('invalid prototype inventory')
  if built.read('assets/openbonk-diagnostic/marker')!=b'OPENBONK_PRIVATE_DIAGNOSTIC_V1':raise ValueError('invalid prototype marker')
  for n in notice_names:
   content=built.read('assets/openbonk-diagnostic/notices/'+n)
   if n=='Lua-5.4.9.txt':
    if hashlib.sha256(content).hexdigest()!='d14fe4a91721fd1d0f6650137594ee83c692f422864a00e69bd6a9b42cdda005':raise ValueError('Lua notice differs')
   elif content!=(NOTICES/n).read_bytes():raise ValueError('texture notice differs: '+n)
  try:
   with zipfile.ZipFile(a.output,'x',allowZip64=True) as out:
    for item in source.infolist():
     if item.filename.startswith('META-INF/') or item.filename in additions:continue
     out.writestr(item,built.read('classes.dex') if item.filename=='classes.dex' else stripped[item.filename] if item.filename in stripped else source.read(item.filename))
    for name in sorted(additions):out.writestr(built.getinfo(name),built.read(name))
  except BaseException:
   a.output.unlink(missing_ok=True);raise
 print('Staged private research APK with private inputs and texture converter notices. Not distributable.')
if __name__=='__main__':main()
