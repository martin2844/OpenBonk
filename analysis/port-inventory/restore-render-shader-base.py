#!/usr/bin/env python3
"""Restore pinned locally compiled Unity render shaders to a research base."""
import argparse,copy,hashlib,importlib.util,os,shutil,subprocess,sys,tempfile,zipfile,zlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('repair',Path(__file__).with_name('repair-split-guid-probe.py'));repair=importlib.util.module_from_spec(spec);spec.loader.exec_module(repair)
NEW_APK='f09f0a911bbb0836e230d23104fd672cbd59e5cf8dab604d3e54a1b251dd2809';NEW_OBB='ca14aa5a5e05cc2fd7c75ad5202258899c903a711112fa35f9bdb1913ab32890'
OLD_APK='608c0d0b89f311ea69c265c80979bb1e5bd31f42f94cb1186e0f70dec1620e68';OLD_OBB='ca89e905047743f85e9d491b926b45d5aebc8b91173200b40726b17307cf3c76'
MANIFEST='ce07ca801c3107d55d5058d605a11c081a8e5a6ca151ff970663181d3d1dc5ad';OLD_COMMENT=b'00000010'
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('base',type=Path);p.add_argument('shader_bundles',type=Path);p.add_argument('output',type=Path);a=p.parse_args();out=a.output.absolute()
 if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
 for src in (a.base,a.shader_bundles):
  src=src.resolve()
  if out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve()):p.error('output must be separate from inputs')
 for path,expected in ((a.base/'base.apk',NEW_APK),(a.base/'base.obb',NEW_OBB),(a.base/'render-shader-manifest.json',MANIFEST)):
  if not path.is_file() or repair.sha(path)!=expected:raise ValueError('incompatible input: '+str(path))
 with tempfile.TemporaryDirectory(prefix='.compute-restore-',dir=out.parent) as tmp:
  tmp=Path(tmp);template=tmp/'template';template.mkdir();apk=tmp/'base.apk';obb=tmp/'base.obb';replacements={}
  with zipfile.ZipFile(a.base/'base.apk') as az,zipfile.ZipFile(a.base/'base.obb') as oz:
   for z in (az,oz):repair.unique(z)
   if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('GUID mismatch')
   prefix='assets/bin/Data/sharedassets1.assets.split';parts=sorted((n for n in oz.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
   if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('invalid fragments')
   (template/'sharedassets1.assets').write_bytes(b''.join(oz.read(n) for n in parts));shutil.copyfile(a.base/'render-shader-manifest.json',template/'manifest.json');restored=tmp/'restored'
   subprocess.run([sys.executable,str(ROOT/'analysis/data-conversion/shared1-render-shaders-template.py'),'restore',str(a.shader_bundles),str(template),str(restored)],check=True)
   data=(restored/'sharedassets1.assets').read_bytes();pos=0
   for name in parts:
    size=oz.getinfo(name).file_size;replacements[name]=data[pos:pos+size];pos+=size
   with zipfile.ZipFile(obb,'x',allowZip64=True) as dst:
    for info in oz.infolist():dst.writestr(copy.copy(info),replacements.get(info.filename,oz.read(info)))
    dst.comment=OLD_COMMENT
   if repair.sha(obb)!=OLD_OBB:raise ValueError('restored OBB differs')
   dex=az.read('classes.dex');previous,following=repair.dex_bounds(dex,NEW_OBB.encode())
   if not previous<OLD_OBB.encode()<following:raise ValueError('DEX string order differs')
   patched=bytearray(dex.replace(NEW_OBB.encode(),OLD_OBB.encode()));patched[12:32]=hashlib.sha1(patched[32:]).digest();patched[8:12]=(zlib.adler32(patched[12:])&0xffffffff).to_bytes(4,'little')
   with zipfile.ZipFile(apk,'x',allowZip64=True) as dst:
    for info in az.infolist():
     if not repair.jar_signature(info.filename):dst.writestr(copy.copy(info),bytes(patched) if info.filename=='classes.dex' else az.read(info))
  if repair.sha(apk)!=OLD_APK:raise ValueError('restored APK differs')
  out.mkdir(mode=0o700)
  try:
   for f in a.base.iterdir():
    if f.is_file() and f.name not in ('base.apk','base.obb','report.json'):shutil.copyfile(f,out/f.name)
   for i in (0,2,4):
    target=out/f'material-extra-{i}';target.mkdir();shutil.copyfile(a.base/f'material-extra-{i}'/'manifest.json',target/'manifest.json')
   shutil.copyfile(apk,out/'base.apk');shutil.copyfile(obb,out/'base.obb')
   (out/'report.json').write_text('{"kind":"local research only, not for distribution"}\n')
  except BaseException:shutil.rmtree(out);raise
 print('Restored Unity shader base exactly; local research only.')
if __name__=='__main__':main()
